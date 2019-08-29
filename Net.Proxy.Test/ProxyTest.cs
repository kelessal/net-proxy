using System;
using Xunit;

namespace Net.Proxy.Test
{

    public class ProxyTest
    {
        [Fact]
        public void CreateTestType()
        {
            var result=InterfaceType.NewProxy<TestInteface>();
            Assert.Equal("Net.Proxy", result.GetType().Namespace);
        }
    }
}
