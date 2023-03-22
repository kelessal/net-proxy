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
            proxyData.Status(ProxyDataStatus.UnModifed);
            proxyData.StrictCompare(false);
            var fields=proxyData.ChangedFields();
            proxyData.SetChangedField("Abc", "Salih", "Keleş");
            fields = proxyData.ChangedFields();
            result.SurName = "Salih";
            var hasOldValue = proxyData.IsChangedField("Abc");
            fields = proxyData.ChangedFields();
            proxyData.Status(ProxyDataStatus.UnModifed);
            fields = proxyData.ChangedFields();
            var aa=proxyData.Tag<string>();
            proxyData.Tag(default, "abc");

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
        [Fact]
        public void NoTraceTest()
        {
            var result = InterfaceType.NewProxy<TestInteface>();
            var proxy = result as IProxyData;
            proxy.Status(ProxyDataStatus.UnModifed);
            result.SurName = "Keleş";
            var found = proxy.Status(default);
            var intType=InterfaceType.GetIntefaceTypeOfProxy(result);
        }
        [Fact]
        public void ChangedObjectTest()
        {
            var result = InterfaceType.NewProxy<TestInteface>();
            var proxy = result as IProxyData;
            proxy.Status(ProxyDataStatus.UnModifed);
            result.SurName = "Keleş";
            var found = proxy.GetChangedObject();
        }
        [Fact]
        public void StringEqualTest()
        {
            var result = InterfaceType.NewProxy<TestInteface>();
            var proxy = result as IProxyData;
            proxy.Status(ProxyDataStatus.UnModifed);
            result.SecondName = "sa";
            var found = proxy.GetChangedObject();
        }
        [Fact]
        public void EnumerableEqualTest()
        {
            var result = InterfaceType.NewProxy<TestInteface>();
            var proxy = result as IProxyData;
            proxy.Status(ProxyDataStatus.UnModifed);
            result.RefList = new EntityDescriptor[] { new EntityDescriptor()};
            var found = proxy.GetChangedObject();
        }
    }
}
