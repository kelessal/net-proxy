using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Proxy.Test
{
    public interface TestInteface:TestInterfaceBase
    {
        new string Name { get; set; }
    }
    public interface TestInterfaceBase
    {
        int Age { get; }
        string Name { get; set; }
    }
}
