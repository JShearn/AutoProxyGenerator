using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoProxyGenerator.Attributes;
using AutoProxyGenerator.MethodInterceptors;
using AutoProxyGenerator.Models;
using AutoProxyGenerator.Services;
using Moq;
using Xunit;

namespace AutoProxyGenerator.Tests.Services
{
    public class TypeParsingServiceTests
    {
        [Fact]
        public void Should_call_ShouldProcess_on_each_interceptor_and_return_true_ones()
        {
            var interceptorSource = new TypeParsingService(typeof(ITestInterface1));

            var found = interceptorSource.FindMatchingInterceptors(typeof(ITestInterface1).GetTypeInfo(),
                typeof(ITestInterface1).GetTypeInfo().GetMethod("Method2"));

            Assert.Equal(2, found.Count());
        }

        [Fact]
        public void Should_throw_if_constructed_with_non_interface()
        {
            Assert.Throws<TypeLoadException>(() => new TypeParsingService(typeof(Object)));
        }

        [Fact]
        public void Should_get_all_interceptors()
        {
            var interceptorSource = new TypeParsingService(typeof(ITestInterface1));

            var interceptors = interceptorSource.GetInterceptors().ToList();

            Assert.Equal(3,interceptors.Count);
            Assert.IsType<Interceptor1>(interceptors[0]);
            Assert.IsType<Interceptor3>(interceptors[1]);
            Assert.IsType<Interceptor2>(interceptors[2]);
        }

        public interface ITestInterface1
        {
            void Method1();
            [Test1]
            [Test3]
            void Method2();
            [Test1]
            [Test2]
            void Method3();
        }
        
        public class Test1Attribute : InterceptAttribute
        {
            public override Type InterceptorType => typeof(Interceptor1);
        }

        public class Test2Attribute : InterceptAttribute
        {
            public override Type InterceptorType => typeof(Interceptor2);
        }

        public class Test3Attribute : InterceptAttribute
        {
            public override Type InterceptorType => typeof(Interceptor3);
        }

        public class Interceptor1 : IMethodInterceptor
        {
            public  T Execute<T>(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
            {
                throw new NotImplementedException();
            }

            public  void Execute(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
            {
                throw new NotImplementedException();
            }
        }

        public class Interceptor2 : IMethodInterceptor
        {
            public  T Execute<T>(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
            {
                throw new NotImplementedException();
            }

            public  void Execute(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
            {
                throw new NotImplementedException();
            }
        }
        public class Interceptor3 : IMethodInterceptor
        {
            public  T Execute<T>(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
            {
                throw new NotImplementedException();
            }

            public  void Execute(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
            {
                throw new NotImplementedException();
            }
        }
    }
}
