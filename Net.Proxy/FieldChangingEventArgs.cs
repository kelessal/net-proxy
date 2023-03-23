using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Proxy
{
    public class FieldChangingEventArgs :EventArgs
    {
        public string Field { get; private set; }
        public object OldValue { get; private set; }
        public object NewValue { get; set; }
        public FieldChangingEventArgs(string field,dynamic oldValue, dynamic newValue)
        {
            this.Field = field;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

    }

}
