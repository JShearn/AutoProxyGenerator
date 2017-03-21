using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoProxyGenerator.Attributes;

namespace Example
{
    class CachingAttribute : InterceptAttribute
    {
        public override Type InterceptorType => typeof(CachingInterceptor);


    }

    class LoggingAttribute : InterceptAttribute
    {
        public override Type InterceptorType => typeof(LoggingInterceptor);
    }
}
