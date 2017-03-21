using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoProxyGenerator;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ProxyFactory<ITestInterface>();
            Test(factory);
        }
        private static void Test(ProxyFactory<ITestInterface> factory)
        {
            var proxyOfTestClass = factory.GenerateProxy(new TestClass());

            Console.WriteLine(proxyOfTestClass.GetCalls()); // 0
            proxyOfTestClass.Sleep1s();
            proxyOfTestClass.Sleep2s();
            Console.WriteLine(proxyOfTestClass.GetCalls()); // 2
            proxyOfTestClass.Sleep1s();
            proxyOfTestClass.Sleep2s();
            Console.WriteLine(proxyOfTestClass.GetCalls()); // Still 2
            Thread.Sleep(5000);
            proxyOfTestClass.Sleep1s();
            proxyOfTestClass.Sleep2s();
            Console.WriteLine(proxyOfTestClass.GetCalls()); // 4

        }
    }
}
