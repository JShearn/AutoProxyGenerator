using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoProxyGenerator.Services;

namespace AutoProxyGenerator.Tests
{
    public class TestInterceptorSource : IInterceptorSource
    {
        private IEnumerable<IMethodInterceptor> _interceptors;
        private bool _shouldMatchAll;

        public TestInterceptorSource(IEnumerable<IMethodInterceptor> interceptors, bool shouldMatchAll = true)
        {
            _interceptors = interceptors;
            _shouldMatchAll = shouldMatchAll;
        }

        public bool CalledFindMatchingInterceptors { get; set; }

        public IEnumerable<IMethodInterceptor> GetInterceptors()
        {
            return _interceptors;
        }

        public IEnumerable<IMethodInterceptor> FindMatchingInterceptors(TypeInfo type, MethodInfo method)
        {
            CalledFindMatchingInterceptors = true;
            return _shouldMatchAll ? _interceptors : new List<IMethodInterceptor>();
        }
    }
}
