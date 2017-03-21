using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoProxyGenerator.MethodInterceptors;
using AutoProxyGenerator.Models;
using AutoProxyGenerator.Services;
using AutoProxyGenerator.Tests.Factories;
using Moq;
using Xunit;

namespace AutoProxyGenerator.Tests.Services
{
    public class InterceptorMatcherServiceTests
    {
        public InterceptorMatcherServiceTests()
        {
            
        }
        
        [Fact]
        public void Should_register_interceptors_then_add_MethodInvoker_then_Execute_each()
        {
            var interceptorMatcher = new InterceptorManagerService();
            var interceptor = new Mock<IMethodInterceptor>();
            interceptor.Setup(
                    i =>
                        i.Execute<bool>(It.Is<Func<IMethodInterceptor>>(next => next() is MethodInvoker), "Equals",
                            It.Is<MethodArgs>(args => args.Arguments.First() == (object)"bar"),
                            It.Is<object>(o => o == (object)"foo")))
                .Returns(() => true).Verifiable();

            string foo = "foo";
            var fooEquals = foo.GetType().GetMethod("Equals", new[] { typeof(string) });

            interceptorMatcher.RegisterMethodInterceptorChain(fooEquals, new[] { interceptor.Object });
            bool result = interceptorMatcher.InvokeInterceptors<bool>(new object[] { "bar" }, foo, foo.GetType().AssemblyQualifiedName,
                fooEquals.Name, fooEquals.MetadataToken.ToString());

            interceptor.Verify();
            Assert.True(result);
        }

        [Fact]
        public void Should_check_method_has_interceptor()
        {
            var interceptorMatcher = new InterceptorManagerService();
            string foo = "foo";
            var fooEquals = foo.GetType().GetMethod("Equals", new[] { typeof(string) });

            interceptorMatcher.RegisterMethodInterceptorChain(fooEquals, new[] { new MethodInvoker(fooEquals) });
            bool result = interceptorMatcher.MethodHasInterceptors(fooEquals.Name, fooEquals.MetadataToken.ToString());

            Assert.True(result);
        }

        [Fact]
        public void Should_check_method_doesnt_have_interceptor()
        {
            var interceptorMatcher = new InterceptorManagerService();
            string foo = "foo";
            var fooEquals = foo.GetType().GetMethod("Equals", new[] { typeof(string) });

            bool result = interceptorMatcher.MethodHasInterceptors(fooEquals.Name, fooEquals.MetadataToken.ToString());

            Assert.False(result);
        }

    }
}
