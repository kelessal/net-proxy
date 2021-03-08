using Net.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

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
                    var propertyBuilder=proxyTypeBuilder.AddProperty(prop.Name, prop.PropertyType);
                    var attrData = prop.GetCustomAttributesData();
                    foreach(var data in attrData)
                    {
                        var ctorInfo = data.Constructor;
                        var ctorArgs = data.ConstructorArguments.Select(p => p.Value).ToArray();
                        var namedFields = data.NamedArguments.Where(p => p.IsField).Select(p => p.MemberInfo)
                            .Cast<FieldInfo>().ToArray();
                        var namedFieldValues = data.NamedArguments.Where(p => p.IsField).Select(p => p.TypedValue.Value)
                            .ToArray();
                        var namedProps = data.NamedArguments.Where(p => !p.IsField).Select(p => p.MemberInfo)
                          .Cast<PropertyInfo>().ToArray();
                        var namedPropertyValues = data.NamedArguments.Where(p => !p.IsField).Select(p => p.TypedValue.Value)
                            .ToArray();
                        var attrBuilder = new CustomAttributeBuilder(data.Constructor,ctorArgs, namedProps, namedPropertyValues, namedFields, namedFieldValues);
                        propertyBuilder.SetCustomAttribute(attrBuilder);
                    }
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
                var propertyNameSet = new HashSet<string>();
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
                        .Where(x => !propertyNameSet.Contains(x.Name)).ToArray();

                    propertyInfos.InsertRange(0, newPropertyInfos);
                    foreach(var item in newPropertyInfos)
                        propertyNameSet.Add(item.Name);
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
