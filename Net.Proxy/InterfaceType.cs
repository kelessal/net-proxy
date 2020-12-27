using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Net.Proxy
{
    public static class InterfaceType
    {
        static ConcurrentDictionary<Type, Type> _ConcreteTypes = new ConcurrentDictionary<Type, Type>();

        public static Type GetProxyType(this Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("It should be interface type");

            if (_ConcreteTypes.ContainsKey(interfaceType)) return _ConcreteTypes[interfaceType];
            lock (_ConcreteTypes)
            {
                if (_ConcreteTypes.ContainsKey(interfaceType)) return _ConcreteTypes[interfaceType];

                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
                {
                    _ConcreteTypes[interfaceType] = typeof(Dictionary<,>).MakeGenericType(interfaceType.GetGenericArguments());
                    return _ConcreteTypes[interfaceType];
                }

                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEqualityComparer<>))
                {
                    _ConcreteTypes[interfaceType] = typeof(EqualityComparer<>).MakeGenericType(interfaceType.GetGenericArguments());
                    return _ConcreteTypes[interfaceType];
                }

                var typeName = $"{interfaceType.Name.Substring(1)}_interface_proxy";
                var proxyTypeBuilder = RuntimeTypeBuilder.CreateTypeBuilder(typeName);
                proxyTypeBuilder.AddInterfaceImplementation(interfaceType);
                foreach (var prop in FindProperties(interfaceType))
                {
                    proxyTypeBuilder.AddProperty(prop.Name, prop.PropertyType);
                }
                var proxyType = proxyTypeBuilder.CreateTypeInfo();
                _ConcreteTypes[interfaceType] = proxyType;
                return proxyType;
            }

        }

        internal static PropertyInfo[] FindProperties(Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);

        }

        public static T NewProxy<T>(Action<T> init=default)
        {
            var proxy = typeof(T).GetProxyType();
            var instance= (T)Activator.CreateInstance(proxy);
            if (init == null) return instance;
            init(instance);
            return instance;
        }
       
    }
}
