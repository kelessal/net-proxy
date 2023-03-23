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
        private ConcurrentSet<string> _lockedFields=new ConcurrentSet<string>();
        private ConcurrentDictionary<string, dynamic> _initValues = new ConcurrentDictionary<string, dynamic>();
        private ConcurrentDictionary<string, dynamic> _newValues = new ConcurrentDictionary<string, dynamic>();

        private ConcurrentDictionary<string, object> _tags = new ConcurrentDictionary<string, object>();

        public event EventHandler<FieldChangingEventArgs> FieldChanging;
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
        void IProxyData.SetChangedField(string field)
        {
            if (this._status == ProxyDataStatus.Locked) throw new Exception("Proxy Data is Locked");
            if (this._lockedFields.Contains(field)) throw new Exception("Field is locked");
            if (this._status == ProxyDataStatus.NoTrack) return;
            if (this._status == ProxyDataStatus.New) return;
            if (this._status == ProxyDataStatus.Removed) throw new Exception("Proxy Data is in removed status");
            var prop=this.GetType().GetProperty(field);
            var newValue=prop.GetValue(this);
            this._initValues[field] = newValue;
            this._newValues[field] = newValue;
            this._status = ProxyDataStatus.Modified;
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
            if(oldValue is IDictionary oldDictionary && newValue is IDictionary newDictionary)
            {
                if (oldDictionary.Count != newDictionary.Count) return false;
                foreach(var key in oldDictionary.Keys)
                {
                    if(!newDictionary.Contains(key)) return false;
                    var oldDicValue = oldDictionary[key];
                    var newDicValue = newDictionary[key];
                    if(!this.IsEqualObject(oldDicValue,newDicValue)) return false;
                }
                return true;
            }
            else if(oldValue is IEnumerable oldList && newValue is IEnumerable newList)
            {
                var oldArray=oldList.Cast<object>().ToArray();
                var newArray=newList.Cast<object>().ToArray();
                if(oldArray.Length!= newArray.Length) return false;
                for(var i = 0; i < oldArray.Length; i++)
                {
                    var oldListItem=oldArray[i];
                    var newListItem=newArray[i];
                    if (!this.IsEqualObject(oldListItem, newListItem)) return false;
                }
                return true;
            }
            return oldValue.Equals(newValue);
        }
        protected object GetChangedNewValue(string field,object oldValue,object newValue)
        {
            if (this._lockedFields.Contains(field)) return oldValue;
            if (this._status == ProxyDataStatus.Locked) return oldValue;
            if (this._status == ProxyDataStatus.Removed) return oldValue;
            var isEqual = this.IsEqualObject(oldValue,newValue);
            if(!isEqual && !this.FieldChanging.IsNull())
            {
                var args=new FieldChangingEventArgs(field,oldValue,newValue);
                this.FieldChanging(this, args);
                newValue = args.NewValue;
            }

            switch (this._status)
            {
                case ProxyDataStatus.NoTrack: return newValue;
                case ProxyDataStatus.New: return newValue;
                case ProxyDataStatus.Modified:
                case ProxyDataStatus.UnModifed:
                    if (IsEqualObject(oldValue,newValue)) return newValue;
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
                    return newValue;
                case ProxyDataStatus.Removed: return oldValue;
                case ProxyDataStatus.Locked: return oldValue;
                default:
                    return newValue;
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

        public bool Lock(string field,bool? lockStatus = null)
        {
            if (!lockStatus.HasValue) return this._lockedFields.Contains(field);
            if (lockStatus.Value)
                this._lockedFields.Add(field);
            else
                this._lockedFields.Remove(field);
            return lockStatus.Value;
        }
    }
    public interface IProxyData
    {
        event EventHandler<FieldChangingEventArgs> FieldChanging;
        ExpandoObject GetChangedObject();
        bool StrictCompare(bool? strictCompare=default);
        ProxyDataStatus Status(ProxyDataStatus? status=default);
        bool Lock(string field, bool? lockField=default);
        IEnumerable<string> ChangedFields();
        void SetChangedField(string field);
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
