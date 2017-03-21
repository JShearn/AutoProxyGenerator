using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoProxyGenerator.Models;
using Xunit;

namespace AutoProxyGenerator.Tests.Collections
{
    public class MethodArgsTests
    {
        [Fact]
        public void Should_return_arguments_as_object_array_without_touching_the_method_info()
        {
            var args = new object[] {1, 2, 3};
            var methodArgs = new MethodArgs(string.Empty,string.Empty, args);

            var result = methodArgs.Arguments;

            Assert.Equal(args,result);
        }

        [Fact]
        public void Should_return_list_of_argument_names_and_values_using_reflection()
        {
            var args = new object[] { 1, null };
            var methodArgs = new MethodArgs(this.GetType().AssemblyQualifiedName,"TestMethod", args);

            var result = methodArgs.ArgumentPairs.ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(new KeyValuePair<string, object>("foo", 1), result.First());
            Assert.Equal(new KeyValuePair<string, object>("bar", null), result.Last());
        }

        public string TestMethod(int foo, IEnumerable<DateTime> bar)
        {
            return string.Empty; // THIS IS USED
        }
    }
}
