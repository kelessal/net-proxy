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
            result.Ref = new EntityDescriptor() { Id = "1" };
            var proxyData = result as IProxyData;
            proxyData.StrictCompare = false;
            result.SurName = "Salih";
            proxyData.Status = ProxyDataStatus.UnModifed;
            result.Ref = new EntityDescriptor() { Id = "1" ,Name="Salih"};
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
