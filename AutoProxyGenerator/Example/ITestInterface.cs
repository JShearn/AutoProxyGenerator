using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Example
{
    public interface ITestInterface
    {
        [Caching]
        [Logging]
        int Sleep1s();
        [Caching]
        [Logging]
        int Sleep2s();

        int GetCalls();
    }
}
