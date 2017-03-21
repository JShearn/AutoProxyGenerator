using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using AutoProxyGenerator.Models;
using AutoProxyGenerator.Services;

namespace Example
{
    public class CachingInterceptor : IMethodInterceptor
    {
        // THIS IS JUST AN EXAMPLE - YOU SHOULD USE MORE COMPLETE CODE AND A BETTER DATA STORE IN PRODUCTION (Redis)
        private static MemoryCache _cache = new MemoryCache("Test");

        public T Execute<T>(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
        {
            string key = methodName + "_" + string.Join(",", args.Arguments.Select(a => a.ToString()));
            if (_cache[key] == null)
            {
                var result = getNext().Execute<T>(getNext, methodName, args, instance);
                _cache.Add(key,result,DateTimeOffset.Now.AddSeconds(3));
            }
            return (T)_cache[key];
        }

        public void Execute(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
        {
            throw new NotImplementedException();
        }
    }

    public class LoggingInterceptor : IMethodInterceptor
    {
        public T Execute<T>(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
        {
            Console.WriteLine($"{methodName} was not found in the cache. Calling directly.");
            return getNext().Execute<T>(getNext, methodName, args, instance);
        }

        public void Execute(Func<IMethodInterceptor> getNext, string methodName, MethodArgs args, object instance)
        {
            throw new NotImplementedException();
        }
    }
}
