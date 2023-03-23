using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Net.Proxy.Test
{

    public class ProxyTest
    {
        [Fact]
        public void CreateTestType()
        {
            IEnumerable<EntityDescriptor> items = default;
            var result=InterfaceType.NewProxy<TestInteface>();
            var proxy=result as IProxyData;
            proxy.Lock("RealAge", true);
            proxy.Lock("RealAge", false);
            result.RealAge = 3.0;
            proxy.Status(ProxyDataStatus.UnModifed);
            result.SecondName = "Salih";
            proxy.SetChangedField("RealAge");
            //proxy.FieldChanging += Proxy_FieldChanging;
            //result.Ref = new EntityDescriptor() { Id = "1" };
            var list1 = new[] { new EntityDescriptor { Id = "2",Name="Salih" } };
            var list2 = new[] { new EntityDescriptor { Id = "2" , Name = "Acaba" } };
            items = list1;
            result.RefList = list1;
            result.RefList = list2;
            Assert.Equal(result.RefList,list2);
        }

        private void Proxy_FieldChanging(object sender, FieldChangingEventArgs e)
        {
            e.NewValue = new[] { new EntityDescriptor() { Id = "2" } };
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
