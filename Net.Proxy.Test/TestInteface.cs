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
    }
    public interface TestInterfaceBase
    {
        int Age { get; }
        [Display(AutoGenerateField =true)]
        string Name { get; set; }
        int MyDefaultAge { get; set; }

       
    }
}
