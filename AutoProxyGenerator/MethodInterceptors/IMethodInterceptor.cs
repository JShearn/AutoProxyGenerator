using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoProxyGenerator.Attributes;
using AutoProxyGenerator.Models;

namespace AutoProxyGenerator.Services
{
    /// <summary>
    /// Executes the next interceptor, optionally transforming arguments and the return value.
    /// IMPORTANT: These should be pure functions. 
    /// If this is being reused for multiple methods then be very careful when writing code that manipulates instance variables
    /// They will be persisted between different methods. 
    /// </summary>
    public interface IMethodInterceptor
    {
        /// <summary>
        /// Runs custom interceptor code
        /// </summary>
        /// <typeparam name="T">Return type of proxied method</typeparam>
        /// <param name="getNext">Function that returns the next interceptor (or null if this is the last interceptor)</param>
        /// <param name="methodName">Name of method</param>
        /// <param name="args">Method arguments</param>
        /// <param name="instance">Object that the method resides in</param>
        /// <returns>Result of this interceptor, all subsequent interceptors and the proxied method</returns>
        T Execute<T>(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance);
        /// <summary>
        /// Runs custom interceptor code
        /// </summary>
        /// <param name="getNext">Function that returns the next interceptor (or null if this is the last interceptor)</param>
        /// <param name="methodName">Name of method</param>
        /// <param name="args">Method arguments</param>
        /// <param name="instance">Object that the method resides in</param>
        void Execute(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance);
    }
    
}
