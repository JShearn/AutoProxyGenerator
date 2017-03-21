using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoProxyGenerator.Services
{
    public interface IInterceptorSource
    {
        /// <summary>
        /// Get all supported interceptors
        /// </summary>
        /// <returns></returns>
        IEnumerable<IMethodInterceptor> GetInterceptors();
        /// <summary>
        /// Find interceptors that are associated with a method
        /// </summary>
        /// <param name="type">Type that the method resides in</param>
        /// <param name="method">Method to check</param>
        /// <returns></returns>
        IEnumerable<IMethodInterceptor> FindMatchingInterceptors(TypeInfo type, MethodInfo method);
    }
}
