using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoProxyGenerator.MethodInterceptors;
using AutoProxyGenerator.Models;

namespace AutoProxyGenerator.Services
{
    public class InterceptorManagerService : IInterceptorManager
    {
        public InterceptorManagerService()
        {
            RegisteredInterceptors = new Dictionary<string, List<IMethodInterceptor>>();
        }

        protected Dictionary<string, List<IMethodInterceptor>> RegisteredInterceptors;

        
        public virtual void RegisterMethodInterceptorChain(MethodInfo method, IEnumerable<IMethodInterceptor> interceptors)
        {
            var interceptorQueue = interceptors.ToList();
            interceptorQueue.Add(new MethodInvoker(method));

            RegisteredInterceptors.Add(method.Name + "_" + method.MetadataToken, interceptorQueue);
        }

        protected virtual Func<IMethodInterceptor> GetInterceptorChain(object[] args, object instance, string typeName, string methodName, string methodToken)
        {
            string methodIdentifier = methodName + "_" + methodToken;
            if (!RegisteredInterceptors.ContainsKey(methodIdentifier))
            {
                throw new Exception("Could not find first interceptor for " + methodIdentifier);
            }

            var interceptors = RegisteredInterceptors[methodIdentifier];

            // getNext either gets the next interceptor in the chain or null if there are none left
            // (The last interceptor will be a MethodInvoker which doesn't call getNext so this shouldn't happen.) 
            var enumerator = interceptors.GetEnumerator();
            Func<IMethodInterceptor> getNext = () =>
            {
                if (enumerator.MoveNext())
                {
                    return enumerator.Current;
                }
                return null;
            };
            return getNext;
        }

        public virtual T InvokeInterceptors<T>(object[] args, object instance, string typeName, string methodName, string methodToken)
        {
            var methodArgs = new MethodArgs(typeName, methodName, args);
            var getNext = GetInterceptorChain(args, instance, typeName, methodName, methodToken);
            var first = getNext();
            var result = first.Execute<T>(getNext, methodName, methodArgs, instance);
            return result;
        }

        public virtual void InvokeInterceptorsVoid(object[] args, object instance, string typeName, string methodName, string methodToken)
        {
            var methodArgs = new MethodArgs(typeName, methodName, args);
            var getNext = GetInterceptorChain(args, instance, typeName, methodName, methodToken);
            var first = getNext();
            first.Execute(getNext, methodName, methodArgs, instance);
        }

        public virtual bool MethodHasInterceptors(string methodName, string methodToken)
        {
            string methodIdentifier = methodName + "_" + methodToken;
            return RegisteredInterceptors.ContainsKey(methodIdentifier);
        }
    }
}
