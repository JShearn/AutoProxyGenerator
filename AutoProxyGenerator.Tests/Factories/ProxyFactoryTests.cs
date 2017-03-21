using AutoProxyGenerator.Attributes;
using AutoProxyGenerator.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoProxyGenerator.Models;
using Xunit;

namespace AutoProxyGenerator.Tests.Factories
{
    public class ProxyFactoryTests
    {
        private readonly Mock<IMethodInterceptor> _concatenateInterceptor;
        private readonly Mock<IMethodInterceptor> _reverseInterceptor;
        private readonly TestInterceptorManager _interceptorManager;
        private readonly Mock<ITest> _proxiedObj;

        public ProxyFactoryTests()
        {
            _proxiedObj = new Mock<ITest>();
            _proxiedObj.Setup(i => i.Direct(1)).Returns("test");
            _proxiedObj.Setup(i => i.Intercepted(1)).Returns("test");
            _proxiedObj.Setup(i => i.InterceptedAsync(1)).ReturnsAsync("test");

            _concatenateInterceptor = MockMethodInterceptor((getNext, name, args, instance) =>
            {
                var result = getNext().Execute<string>(getNext, name, args, instance);
                return result == null ? null
                    : (result + "_" + name + "_" + args.ArgumentPairs.Single().Key + ":" +
                       args.ArgumentPairs.Single().Value);
            });
            _reverseInterceptor = MockMethodInterceptor((getNext, name, args, instance) =>
            {
                var result = getNext().Execute<string>(getNext, name, args, instance);
                return result == null ? null : string.Concat(result.Reverse());
            });

            _interceptorManager = new TestInterceptorManager();
        }

        private Mock<IMethodInterceptor> MockMethodInterceptor<T>(Func<Func<IMethodInterceptor>, string, MethodArgs, object, T> callback, string expectedMethodName = null, List<KeyValuePair<string, object>> expectedArgs = null)
        {
            var argsToCompare = expectedArgs ??
                               new List<KeyValuePair<string, object>>(new[]
                               {
                                   new KeyValuePair<string, object>("foo", 1)
                               });
            var interceptor = new Mock<IMethodInterceptor>();
            interceptor.SetupAllProperties();
            interceptor.Setup(i =>
                    i.Execute<T>(It.IsAny<Func<IMethodInterceptor>>(),
                    It.Is<string>(s => s == expectedMethodName || expectedMethodName == null)
                    , It.Is<MethodArgs>(m => ArgsEqual(m.ArgumentPairs.ToList(), argsToCompare))
                        , It.IsAny<object>()))
                .Returns(callback)
                .Verifiable();
            return interceptor;
        }

        /// <summary>
        /// Little helper method to compare List<KeyValuePair<stringobject>> 
        /// since I don't want to override .Equals just to do some testing.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private bool ArgsEqual(List<KeyValuePair<string, object>> x, List<KeyValuePair<string, object>> y)
        {
            if (x.Count != y.Count) return false;
            for (int i = 0; i < x.Count; i++)
            {
                if (!x[i].Key.Equals(y[i].Key) ||
                    !x[i].Value.ToString().Equals(y[i].Value.ToString()))
                    return false;
            }
            return true;
        }

        [Fact]
        public void Should_call_inner_object_and_return_value()
        {
            var interceptorSource = new TestInterceptorSource(new List<IMethodInterceptor>());
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(_proxiedObj.Object);

            string returnedValue = proxy.Direct(1);

            Assert.Equal("test", returnedValue);
            _proxiedObj.Verify(i => i.Direct(1));
        }

        [Fact]
        public void Should_defer_execution_to_interceptor_if_interceptor_matches()
        {
            var interceptorSource = new TestInterceptorSource(new[] {_concatenateInterceptor.Object});
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(_proxiedObj.Object);
            
            string returnedValue = proxy.Intercepted(1);

            _proxiedObj.Verify(i => i.Intercepted(1));
            _concatenateInterceptor.Verify();
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);
            Assert.Equal("test_Intercepted_foo:1", returnedValue);
        }

        [Fact]
        public void Should_be_able_to_invoke_a_method_multiple_times()
        {
            var interceptorSource = new TestInterceptorSource(new[] {_concatenateInterceptor.Object});
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(_proxiedObj.Object);

            proxy.Intercepted(1);
            proxy.Intercepted(1);
            string returnedValue = proxy.Intercepted(1);

            _proxiedObj.Verify(i => i.Intercepted(1), Times.Exactly(3));
            _concatenateInterceptor.Verify();
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);
            Assert.Equal("test_Intercepted_foo:1", returnedValue);
        }

        [Fact]
        public void Should_be_able_to_create_multiple_factories_of_the_same_type()
        {
            var interceptorSource = new TestInterceptorSource(new[] {_concatenateInterceptor.Object});
            var interceptorLocator1 = new TestInterceptorManager();
            var interceptorLocator2 = new TestInterceptorManager();
            var proxyFactory1 = new ProxyFactory<ITest>(interceptorSource, interceptorLocator1);
            var proxyFactory2 = new ProxyFactory<ITest>(interceptorSource, interceptorLocator2);
            var proxy1 = proxyFactory1.GenerateProxy(_proxiedObj.Object);
            var proxy2 = proxyFactory2.GenerateProxy(_proxiedObj.Object);

            string returnedValue1 = proxy1.Intercepted(1);
            string returnedValue2 = proxy2.Intercepted(1);

            _proxiedObj.Verify(i => i.Intercepted(1), Times.Exactly(2));
            _concatenateInterceptor.Verify(
                h =>
                    h.Execute<string>(It.IsAny<Func<IMethodInterceptor>>(), "Intercepted", It.IsAny<MethodArgs>(),
                        It.IsAny<object>()), Times.Exactly(2));
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);
            Assert.Equal("test_Intercepted_foo:1", returnedValue1);
            Assert.Equal("test_Intercepted_foo:1", returnedValue2);
        }

        [Fact]
        public void Should_support_multiple_interceptors()
        {
            var interceptorSource =new TestInterceptorSource(new[] {_reverseInterceptor.Object, _concatenateInterceptor.Object});
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(_proxiedObj.Object);

            string returnedValue = proxy.Intercepted(1);

            _proxiedObj.Verify(i => i.Intercepted(1));
            _concatenateInterceptor.Verify();
            _reverseInterceptor.Verify();
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);

            Assert.Equal("tset_Intercepted_foo:1", returnedValue);
        }

        [Fact]
        public void Should_pass_method_name_to_interceptor()
        {
            var interceptor = MockMethodInterceptor(
                (getNext, name, args, instance) =>
                        getNext().Execute<string>(getNext, name, args, instance) + "_" + name
                , "Intercepted");
            var interceptorSource = new TestInterceptorSource(new[] {interceptor.Object});
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(_proxiedObj.Object);

            string returnedValue = proxy.Intercepted(1);

            _proxiedObj.Verify(i => i.Intercepted(1));
            interceptor.Verify();
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);
            Assert.Equal("test_Intercepted", returnedValue);
        }

        [Fact]
        public void Should_support_multiple_interceptors_executed_in_order()
        {
            var interceptorSource = new TestInterceptorSource(new[] {_concatenateInterceptor.Object, _reverseInterceptor.Object});
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(_proxiedObj.Object);

            string returnedValue = proxy.Intercepted(1);

            _proxiedObj.Verify(i => i.Intercepted(1));
            _concatenateInterceptor.Verify();
            _reverseInterceptor.Verify();
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);

            Assert.Equal("1:oof_detpecretnI_tset", returnedValue);
        }

        [Fact]
        public void Should_not_defer_execution_if_interceptor_does_not_match()
        {
            var interceptorSource = new TestInterceptorSource(new[] {_concatenateInterceptor.Object}, false);
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(_proxiedObj.Object);

            string returnedValue = proxy.Intercepted(1);

            _proxiedObj.Verify(i => i.Intercepted(1));
            Assert.Equal("test", returnedValue);
            AssertNotRan();
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);
        }

        [Fact]
        public void Should_not_swallow_exceptions()
        {
            var interceptorSource = new TestInterceptorSource(new[] {_concatenateInterceptor.Object});
            var proxiedObj = new Mock<ITest>();
            proxiedObj.Setup(i => i.Intercepted(1)).Throws(new ArithmeticException("BOOM"));
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(proxiedObj.Object);

            Exception ex = Assert.Throws<ArithmeticException>(() => proxy.Intercepted(1));

            Assert.Equal("BOOM", ex.Message);
        }

        [Fact]
        public void Should_support_methods_that_return_null()
        {
            var interceptorSource = new TestInterceptorSource(new[] {_concatenateInterceptor.Object});
            var proxiedObj = new Mock<ITest>();
            proxiedObj.Setup(i => i.Intercepted(1)).Returns(() => null);
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(proxiedObj.Object);

            string returnedValue = proxy.Intercepted(1);

            proxiedObj.Verify(i => i.Intercepted(1));
            _concatenateInterceptor.Verify();
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);
            Assert.Null(returnedValue);
        }

        [Fact]
        public async Task Should_support_async_methods()
        {
            var proxiedObj = new Mock<ITest>();
            proxiedObj.Setup(i => i.InterceptedAsync(1)).ReturnsAsync("test").Verifiable();
            var taskInterceptor =
                MockMethodInterceptor<Task<string>>((func, s, arg3, arg4) => func().Execute<Task<string>>(func, s, arg3, arg4));
            var interceptorSource = new TestInterceptorSource(new[] { taskInterceptor.Object });
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(proxiedObj.Object);

            var returnedValue = await proxy.InterceptedAsync(1);

            proxiedObj.Verify();
            taskInterceptor.Verify();
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);
            Assert.Equal("test", returnedValue);
        }

        [Fact]
        public void Should_support_value_return_types()
        {
            var proxiedObj = new Mock<ITest>();
            
            proxiedObj.Setup(i => i.ReturnInt(It.IsAny<int>(), It.IsAny<int>())).Returns((int x, int y) => x + y).Verifiable();
            var x2interceptor = MockMethodInterceptor<int>((func, s, arg3, arg4) =>
                {
                    return func().Execute<int>(func, s, arg3, arg4)*2;
                }, "ReturnInt",
                new List<KeyValuePair<string, object>>(new[]
                    {new KeyValuePair<string, object>("x", 1), new KeyValuePair<string, object>("y", 2)}));
            var interceptorSource = new TestInterceptorSource(new[] {x2interceptor.Object});
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(proxiedObj.Object);

            int returnedValue = proxy.ReturnInt(1,2);
            
            proxiedObj.Verify();
            x2interceptor.Verify();
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);
            Assert.Equal(6, returnedValue);
        }

        [Fact]
        public void Should_support_void_return_types()
        {
            var proxiedObj = new Mock<ITest>();
            proxiedObj.Setup(i => i.ReturnVoid()).Verifiable();
            var nopInterceptor = new Mock<IMethodInterceptor>();
            nopInterceptor.Setup(i =>
                    i.Execute(It.IsAny<Func<IMethodInterceptor>>(), "ReturnVoid", It.IsAny<MethodArgs>(),
                        It.IsAny<object>()))
                        .Callback((Func<IMethodInterceptor> func, string s, MethodArgs arg3, object arg4) => func().Execute(func,s,arg3,arg4))
                        .Verifiable();
            var interceptorSource = new TestInterceptorSource(new[] {nopInterceptor.Object});
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(proxiedObj.Object);

            proxy.ReturnVoid();

            proxiedObj.Verify();
            nopInterceptor.Verify();
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);
        }

        [Fact]
        public void Should_support_complex_objects_as_arguments_and_return_types()
        {
            var proxiedObj = new Mock<ITest>();
            proxiedObj.Setup(p => p.InterceptedNonPrimitive(
                It.IsAny<Tuple<string, int>>(), It.IsAny<List<DateTime>>()))
                .Returns(new Dictionary<string, int>() { { "a", 1 } }).Verifiable();

            var expectedArgs = new List<KeyValuePair<string, object>>(new[]
            {
                new KeyValuePair<string, object>("foo", new Tuple<string, int>("a", 1)),
                new KeyValuePair<string, object>("bar", new List<DateTime>(new[] {DateTime.MinValue}))
            });

            var interceptor = MockMethodInterceptor((getNext, name, args, instance) =>
                        getNext().Execute<Dictionary<string, int>>(getNext, name, args, instance),
                null, expectedArgs);

            var interceptorSource = new TestInterceptorSource(new[] {interceptor.Object});
            var proxyFactory = new ProxyFactory<ITest>(interceptorSource, _interceptorManager);
            var proxy = proxyFactory.GenerateProxy(proxiedObj.Object);

            var returnedValue = proxy.InterceptedNonPrimitive(new Tuple<string, int>("a", 1), new List<DateTime>(new[] {DateTime.MinValue}));

            proxiedObj.Verify();
            interceptor.Verify();
            Assert.True(interceptorSource.CalledFindMatchingInterceptors);
            Assert.Contains("a", returnedValue.Keys);
        }

        private void AssertNotRan()
        {
            foreach (var interceptor in new[] { _concatenateInterceptor, _reverseInterceptor })
            {
                interceptor.Verify(h => h.Execute<string>(It.IsAny<Func<IMethodInterceptor>>(), It.IsAny<string>(),
                It.IsAny<MethodArgs>(), It.IsAny<object>()), Times.Never);
            }
        }

        public interface ITest
        {
            string Direct(int foo);
            
            string Intercepted(int foo);
            
            Dictionary<string, int> InterceptedNonPrimitive(Tuple<string, int> foo, List<DateTime> bar);
            
            Task<string> InterceptedAsync(int foo);

            int ReturnInt(int x, int y);
            void ReturnVoid();
        }
        
    }
}

//TODO: Add tests to chack instance state is preserved
//TODO: Add attribute parsing service