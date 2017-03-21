using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Example
{
    public class TestClass :ITestInterface
    {
        private int _calls = 0;

        public int Sleep1s()
        {
            Thread.Sleep(1000);
            return ++_calls;

        }

        public int Sleep2s()
        {
            Thread.Sleep(2000);
            return ++_calls;
        }

        public int GetCalls()
        {
            return _calls;
        }
    }
}
