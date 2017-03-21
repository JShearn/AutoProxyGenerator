using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoProxyGenerator.Attributes;
using AutoProxyGenerator.Models;
using AutoProxyGenerator.Services;

namespace AutoProxyGenerator.MethodInterceptors
{
    /// <summary>
    /// Runs the supplied method with the supplied args
    /// This is always the last interceptor in a chain
    /// </summary>
    public class MethodInvoker : IMethodInterceptor
    {
        private readonly MethodInfo _method;

        public MethodInvoker(MethodInfo method)
        {
            _method = method;
        }

        public T Execute<T>(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
        {
            try
            {
                return (T) _method.Invoke(instance, args.Arguments.ToArray());
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        public void Execute(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
        {
            try
            {
                _method.Invoke(instance, args.Arguments.ToArray());
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        public bool ShouldProcess(IEnumerable<InterceptAttribute> typeAttrs, IEnumerable<InterceptAttribute> methodAttrs)
        {
            //TODO: Segregate ShouldProcess in to a different interface as it isn't used here, violates SOLID
            return true;
        }
    }
}
