using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Proxy
{
    [AttributeUsage(AttributeTargets.Property,AllowMultiple =false)]
    public class NoTrackDataAttribute:Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Property,AllowMultiple =false)]
    public class StrictCompareDataAttribute : Attribute
    {
    }
}
