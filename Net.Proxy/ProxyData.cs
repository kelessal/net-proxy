using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Net.Proxy
{
    public abstract class ProxyData:IProxyData
    {
        private bool _StrictCompare = false;
        private ProxyDataStatus _status;
        private ConcurrentDictionary<string, dynamic> _initValues = new ConcurrentDictionary<string, dynamic>();
        ProxyDataStatus IProxyData.Status
        {
            get { return _status; }
            set 
            {
                if (value == this._status) return;
                this._initValues.Clear();
                this._status = value;
            }
        }

        bool IProxyData.StrictCompare
        {
            get { return this._StrictCompare; }
            set { this._StrictCompare = value; }
        }

        private bool IsEqualObject(dynamic oldValue,dynamic newValue)
        {
            if (this._StrictCompare) return oldValue == newValue;
            if (oldValue == newValue) return true;
            if (oldValue == null && newValue != null) return false;
            if (oldValue != null && newValue == null) return false;
            return oldValue.Equals(newValue);
        }
        protected bool SetChanged(string field,dynamic oldValue,dynamic newValue)
        {
            switch (this._status)
            {
                case ProxyDataStatus.NoTrack: return true;
                case ProxyDataStatus.New: return true;
                case ProxyDataStatus.Modified:
                case ProxyDataStatus.UnModifed:
                    if (this.IsEqualObject(oldValue, newValue)) return true;
                    var isSameWithInitialValue=this._initValues.ContainsKey(field) 
                        && this.IsEqualObject(this._initValues[field], newValue);
                    if (isSameWithInitialValue)
                        this._initValues.Remove(field,out _);
                    else
                        this._initValues[field] = oldValue;
                    this._status =this._initValues.Count==0?
                        ProxyDataStatus.UnModifed:ProxyDataStatus.Modified;
                    return true;
                case ProxyDataStatus.Removed: return false;
                case ProxyDataStatus.Locked: return false;
                default:
                    return false;
            }
        }

        IEnumerable<string> IProxyData.GetChangedFields()
        {
            return this._initValues.Keys.AsEnumerable();
        }
    }
    public interface IProxyData
    {
        bool StrictCompare { get; set; }
        ProxyDataStatus Status { get; set; }
        IEnumerable<string> GetChangedFields();
    }
    public enum ProxyDataStatus
    {
        NoTrack=0,
        New=1,
        Modified=2,
        Removed=3,
        UnModifed=4,
        Locked=5
    }
    
}
