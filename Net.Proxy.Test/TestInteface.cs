using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Net.Proxy.Test
{
    
    public interface TestInteface:TestInterfaceBase
    {
        [Obsolete]
        new string Name { get; set; }
        public new int MyDefaultAge
        {
            get { return 43; }
        }
        [NoTraceCustom]
        string SurName { get; set; }
        string SecondName { get; set; }
        EntityDescriptor[] RefList { get; set; }
        EntityDescriptor Ref { get; set; }
        public string ToString()
        {
            return this.SurName;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class NoTraceCustomAttribute : NoTraceAttribute
    {

    }
    public class EntityDescriptor
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public override bool Equals(object obj)
        {
            var ed = obj as EntityDescriptor;
            if (ed == null) return false;
            return this.Id == ed.Id;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    public interface TestInterfaceBase
    {
        int Age { get; set; }
        [Display(AutoGenerateField =true)]
        string Name { get; set; }
        int MyDefaultAge { get; set; }
        [Test(new string[] { "A", "B" })]
        int ComputedProp => this.MyDefaultAge;
       
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class TestAttribute:Attribute
    {
        public string[] Items { get; set; }
        public TestAttribute(string[] items)
        {
            this.Items = items;

        }
    }
}
