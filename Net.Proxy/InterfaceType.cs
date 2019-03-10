using Net.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
                var info = interfaceType.GetInfo();
                foreach (var prop in info.GetAllProperties())
                {
                    proxyTypeBuilder.AddProperty(prop.Name, prop.Type);
                }
                var proxyType = proxyTypeBuilder.CreateTypeInfo();
                _ConcreteTypes[interfaceType] = proxyType;
                return proxyType;
            }

        }
        public static T NewProxy<T>()
        {
            var proxy = typeof(T).GetProxyType();
            return (T)Activator.CreateInstance(proxy);
        }
        
    }
}
