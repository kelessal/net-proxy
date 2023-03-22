using Net.Extensions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Net.Proxy
{
    public abstract class ProxyData:IProxyData
    {
        private bool _strictCompare = false;
        private ProxyDataStatus _status;
        private ConcurrentDictionary<string, dynamic> _initValues = new ConcurrentDictionary<string, dynamic>();
        private ConcurrentDictionary<string, dynamic> _newValues = new ConcurrentDictionary<string, dynamic>();

        private ConcurrentDictionary<string, object> _tags = new ConcurrentDictionary<string, object>();

        ProxyDataStatus IProxyData.Status(ProxyDataStatus? newStatus)
        {
            if (!newStatus.HasValue) return this._status;
            if (newStatus == this._status) return this._status;
            this._initValues.Clear();
            this._newValues.Clear();
            this._status = newStatus.Value;
            return this._status;
        }

        bool IProxyData.StrictCompare(bool? newValue)
        {
            if (!newValue.HasValue) return this._strictCompare;
            this._strictCompare = newValue.Value;
            return this._strictCompare;
        }

        private bool IsEqualObject(object oldValue,object newValue)
        {
            if (this._strictCompare) return oldValue == newValue;
            if (oldValue is string strOld && strOld.IsEmpty())
                oldValue = null;
            if (newValue is string strNew && strNew.IsEmpty())
                newValue = null;
            if (oldValue is IEnumerable oldEnum && oldEnum.IsEmpty())
                oldValue = default;
            if (newValue is IEnumerable newEnum && newEnum.IsEmpty())
                newValue = null;
            if (oldValue == newValue) return true;
            if (oldValue == null && newValue != null) return false;
            if (oldValue != null && newValue == null) return false;
            
         
            return oldValue.Equals(newValue);
        }
        bool IProxyData.SetChangedField(string field, dynamic oldValue, dynamic newValue)
        {
            return this.SetChange(field,oldValue, newValue);
        }
        protected bool SetChange(string field,dynamic oldValue,dynamic newValue)
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
                    {
                        this._initValues.TryRemove(field, out _);
                        this._newValues.TryRemove(field, out _);
                    }
                    else
                    {
                        this._initValues[field] = oldValue;
                        this._newValues[field] = newValue;
                    }
                    this._status =this._initValues.Count==0?
                        ProxyDataStatus.UnModifed:ProxyDataStatus.Modified;
                    return true;
                case ProxyDataStatus.Removed: return false;
                case ProxyDataStatus.Locked: return false;
                default:
                    return false;
            }
        }
        IEnumerable<string> IProxyData.ChangedFields()
        {
            return this._newValues.Keys.AsEnumerable();
        }
        dynamic IProxyData.OldValue(string field)
        {
            return this._initValues.GetSafeValue(field);
        }
        dynamic IProxyData.NewValue(string field)
        {
            return this._newValues.GetSafeValue(field);
        }
        bool IProxyData.IsChangedField(string field)
        {
            return this._newValues.ContainsKey(field);
        }

        T IProxyData.Tag<T>(string key)
        {
            key = key.IsEmpty() ? "default" : key;
            if (this._tags.TryGetValue(key, out var result))
                return (T)result;
            return default(T);
        }

        void IProxyData.Tag<T>(string key, T item)
        {
            key = key.IsEmpty() ? "default" : key;
            this._tags.AddOrUpdate(key, item, (old, value) => item);
        }

        ExpandoObject IProxyData.GetChangedObject()
        {
            if (this._newValues.IsEmpty()) return default;
            var result = new ExpandoObject() as IDictionary<string,object>;
            foreach(var kv in this._newValues)
                result[kv.Key] = kv.Value;
            return (ExpandoObject)result;
        }
    }
    public interface IProxyData
    {
        ExpandoObject GetChangedObject();
        bool StrictCompare(bool? strictCompare);
        ProxyDataStatus Status(ProxyDataStatus? status);
        IEnumerable<string> ChangedFields();
        bool SetChangedField(string field,dynamic oldValue,dynamic newValue);
        dynamic OldValue(string field);
        dynamic NewValue(string field);
        bool IsChangedField(string field);
        T Tag<T>(string key=default);
        void Tag<T>(string key, T field);

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
