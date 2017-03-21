using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoProxyGenerator.Attributes;

namespace AutoProxyGenerator.Services
{
    public class TypeParsingService : IInterceptorSource
    {
        private Type _typeToParse;
        public TypeParsingService(Type typeToParse)
        {
            if (!typeToParse.IsInterface)
            {
                throw new TypeLoadException("Expected an interface but got " + typeToParse.Name);
            }
            _typeToParse = typeToParse;
        }

        public virtual IEnumerable<IMethodInterceptor> GetInterceptors()
        {
            var attrs = _typeToParse.GetMethods()
                .SelectMany(m => m.GetCustomAttributes())
                .Where(a => a is InterceptAttribute)
                .Cast<InterceptAttribute>();
            return attrs
                .Select(a => a.InterceptorType).Distinct()
                .Select(t => (IMethodInterceptor)Activator.CreateInstance(t));
        }

        public virtual IEnumerable<IMethodInterceptor> FindMatchingInterceptors(TypeInfo type, MethodInfo method)
        {
            //var typeAttrs = type.GetCustomAttributes(true).Where(a => a is InterceptAttribute).Cast<InterceptAttribute>();
            var methodAttrs = method.GetCustomAttributes(true).Where(a => a is InterceptAttribute).Cast<InterceptAttribute>();

            if (/*typeAttrs.Any() ||*/ methodAttrs.Any())
            {
                return methodAttrs.Select(a => a.InterceptorType).Distinct().Select(t => (IMethodInterceptor)Activator.CreateInstance(t));
            }
            return new List<IMethodInterceptor>();
        }
    }
}
