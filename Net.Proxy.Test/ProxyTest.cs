using System;
using System.Reflection;
using Xunit;

namespace Net.Proxy.Test
{

    public class ProxyTest
    {
        [Fact]
        public void CreateTestType()
        {
            var result=InterfaceType.NewProxy<TestInteface>();
            result.MyDefaultAge = 12;
            var x = result.MyDefaultAge;
            Assert.Equal("Net.Proxy", result.GetType().Namespace);
        }
        [Fact]
        public void FindTypeProperties()
        {
            var result = InterfaceType.NewProxy<TestInteface>();
            var nameProperty=result.GetType().GetProperty("Name");
            var attrs=nameProperty.GetCustomAttributes();
        }
    }
}
