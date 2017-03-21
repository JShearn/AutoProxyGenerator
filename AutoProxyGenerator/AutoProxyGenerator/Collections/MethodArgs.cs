using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoProxyGenerator.Models
{
    /// <summary>
    /// This is a collection of a method's arguments.
    /// This should be treated as an imutable object because ArgumentPairs will only be evaluated once (lazily)
    /// If you need to modify the arguments then create a new instance of MethodArgs
    /// </summary>
    public class MethodArgs
    {
        private readonly string _typeName, _methodName;
        private readonly Lazy<List<KeyValuePair<string, object>>> _argumentPairs;

        public MethodArgs(string typeName, string methodName, object[] args)
        {
            _typeName = typeName;
            _methodName = methodName;
            Arguments = args;
            _argumentPairs = new Lazy<List<KeyValuePair<string, object>>>(ValueFactory);
        }
        
        private List<KeyValuePair<string, object>> ValueFactory()
        {
            // This is expensive so only call it once
            var type = Type.GetType(_typeName);
            var method = type?.GetMethod(_methodName);
            if (method == null)
            {
                throw new MissingMethodException($"Could not find {_methodName} in {_typeName}");
            }

            var paramInfos = method.GetParameters();
            List< KeyValuePair < string, object>> args = new List<KeyValuePair<string, object>>();
            for (int i = 0; i < paramInfos.Length; i++)
            {
                args.Add(new KeyValuePair<string, object>(paramInfos[i].Name, Arguments[i]));
            }
            return args;
        }

        /// <summary>
        /// Argument values passed to the method
        /// </summary>
        public IReadOnlyList<object> Arguments { get; }
        
        /// <summary>
        /// Argument names and values passed to the method.
        /// This is expensive to call the first time as it uses reflection.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, object>> ArgumentPairs => _argumentPairs.Value;
    }
}
