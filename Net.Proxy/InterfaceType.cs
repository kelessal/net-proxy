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
        static ConcurrentDictionary<Type, Type> _InterfaceTypes = new ConcurrentDictionary<Type, Type>();

        static MethodInfo ProxyDataGetChangeNewValueMethodInfo = typeof(ProxyData).GetMethod("GetChangedNewValue", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        static MethodInfo ProxyDataToStringMethodInfo = typeof(ProxyData).GetMethod("ToString", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        static PropertyBuilder AddProxyDataProperty(TypeBuilder tb, string propertyName, Type propertyType,bool strictCompare)
        {
           
            FieldBuilder privateField = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, privateField);
            getIl.Emit(OpCodes.Ret);
            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig |
                  MethodAttributes.Virtual,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label exitSet = setIl.DefineLabel();
            //Label mainLabel = setIl.DefineLabel();
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldstr, propertyName);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldfld,privateField);
            setIl.Emit(OpCodes.Box, propertyType);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Box, propertyType);
            if (strictCompare)
                setIl.Emit(OpCodes.Ldc_I4_1);
            else
                setIl.Emit(OpCodes.Ldc_I4_0);
            setIl.Emit(OpCodes.Call, ProxyDataGetChangeNewValueMethodInfo);
            setIl.Emit(OpCodes.Unbox_Any, propertyType);
            //setIl.Emit(OpCodes.Castclass, propertyType);
            //setIl.Emit(OpCodes.Brfalse,exitSet);
            //setIl.MarkLabel(mainLabel);
            //setIl.Emit(OpCodes.Ldarg_0);
            //setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, privateField);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);


            //setIl.Emit(OpCodes.Ldarg_1);
            //setIl.Emit(OpCodes.Ldfld, fieldBuilder);
            //setIl.EmitCall(OpCodes.Call, ProxyDataSetChangeMethodInfo,new Type[] {typeof(string),typeof(object),typeof(object)});
            //setIl.Emit(OpCodes.Brfalse,exitSet);
            //setIl.Emit(OpCodes.Nop);
            //setIl.Emit(OpCodes.Ldarg_0);
            //setIl.Emit(OpCodes.Ldarg_1);
            //setIl.Emit(OpCodes.Stfld, fieldBuilder);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);

            return propertyBuilder;

        }
        static Type BindTypes(Type interfaceType,Type concreteType)
        {
            _ConcreteTypes[interfaceType] = concreteType;
            _InterfaceTypes[concreteType] = interfaceType;
            return concreteType;
        }
        public static Type GetIntefaceTypeOfProxy(object obj)
        {
            if (obj == null) return null;
            var type = obj.GetType();
            return _InterfaceTypes.GetSafeValue(type);
        }
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
                    var concreteType = typeof(Dictionary<,>).MakeGenericType(interfaceType.GetGenericArguments());
                   return BindTypes(interfaceType,concreteType);
                }

                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEqualityComparer<>))
                {
                    var concreteType = typeof(EqualityComparer<>).MakeGenericType(interfaceType.GetGenericArguments());
                    return BindTypes(interfaceType, concreteType);
                }

                var typeName = $"{interfaceType.Name.Substring(1)}_interface_proxy";
                var proxyTypeBuilder = RuntimeTypeBuilder.CreateTypeBuilder(typeName,typeof(ProxyData));
                proxyTypeBuilder.AddInterfaceImplementation(interfaceType);
                foreach (var prop in FindProperties(interfaceType))
                {
                    if(!prop.GetMethod.IsAbstract)
                    {
                        proxyTypeBuilder.AddInterfaceDefaultProperty(prop);
                        continue;
                    }
                    var noTrace = prop.GetCustomAttributes().OfType<NoTrackDataAttribute>().Any();
                    var strictCompare = prop.GetCustomAttributes().OfType<StrictCompareDataAttribute>().Any();
                    var propertyBuilder =noTrace?proxyTypeBuilder.AddProperty(prop.Name,prop.PropertyType): AddProxyDataProperty(proxyTypeBuilder,prop.Name, prop.PropertyType,strictCompare);
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
                var toStringMethod = interfaceType.GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance);
                if (toStringMethod != null && toStringMethod.ReturnType == typeof(string))
                {
                    var toStringBuilder = proxyTypeBuilder.DefineMethod("ToString", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual);
                    toStringBuilder.SetReturnType(typeof(string));
                    var il=toStringBuilder.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, toStringMethod);
                    il.Emit(OpCodes.Ret);
                    proxyTypeBuilder.DefineMethodOverride(toStringBuilder, ProxyDataToStringMethodInfo);
                }

                var proxyType = proxyTypeBuilder.CreateTypeInfo();
                return BindTypes(interfaceType,proxyType);
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
                        //.Where(p=>p.GetMethod.IsAbstract)
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
