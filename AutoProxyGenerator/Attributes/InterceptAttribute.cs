using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoProxyGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    /// <summary>
    /// Methods marked with this attribute will be intercepted by the proxy.
    /// This should be overriden.
    /// </summary>
    public abstract class InterceptAttribute : Attribute
    {
        public abstract Type InterceptorType { get; }
        //public bool Negate { get; set; }
    }
}
