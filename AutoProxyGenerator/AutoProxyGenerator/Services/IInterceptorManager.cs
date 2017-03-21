using System;
using System.Collections.Generic;
using System.Reflection;

namespace AutoProxyGenerator.Services
{
    /// <summary>
    /// Controls how interceptors are run and in what order
    /// </summary>
    public interface IInterceptorManager
    {
        /// <summary>
        /// Registers interceptors against a method.
        /// </summary>
        /// <param name="method">Method to intercept</param>
        /// <param name="interceptors">Interceptors to execute. Executed in the supplied order</param>
        void RegisterMethodInterceptorChain(MethodInfo method, IEnumerable<IMethodInterceptor> interceptors);
        /// <summary>
        /// Invoke the first interceptor in a chain
        /// </summary>
        /// <typeparam name="T">Type returned by the intercepted method</typeparam>
        /// <param name="args">Array of method arguments</param>
        /// <param name="instance">Object that the method resides in</param>
        /// <param name="typeName">Full assembly qualified name of the type the method resides in</param>
        /// <param name="methodName">Name of method</param>
        /// <param name="methodToken">Metadata token that uniquely identifies the method</param>
        /// <returns>Method return value after passing through every interceptor</returns>
        T InvokeInterceptors<T>(object[] args, object instance, string typeName, string methodName, string methodToken);
        /// <summary>
        /// Invoke the first interceptor in a chain
        /// </summary>
        /// <typeparam name="T">Type returned by the intercepted method</typeparam>
        /// <param name="args">Array of method arguments</param>
        /// <param name="instance">Object that the method resides in</param>
        /// <param name="typeName">Full assembly qualified name of the type the method resides in</param>
        /// <param name="methodName">Name of method</param>
        /// <param name="methodToken">Metadata token that uniquely identifies the method</param>
        void InvokeInterceptorsVoid(object[] args, object instance, string typeName, string methodName, string methodToken);
        /// <summary>
        /// Returns true if the method has an interceptors associated with it
        /// </summary>
        /// <param name="methodName">Name of method</param>
        /// <param name="methodToken">Metadata token that uniquely identifies the method</param>
        /// <returns></returns>
        bool MethodHasInterceptors(string methodName, string methodToken);
    }
}