using Net.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Net.Proxy
{
    public static class RuntimeTypeBuilder
    {
        static AssemblyBuilder assemblyBuilder;
        static ModuleBuilder moduleBuilder;
        public static Assembly Assembly => assemblyBuilder;

        static RuntimeTypeBuilder()
        {
            var assemblyName = new AssemblyName(Guid.NewGuid().ToString());
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        }
        public static TypeBuilder CreateTypeBuilder(string typeName, Type baseType = null)
        {

            TypeBuilder tb = moduleBuilder.DefineType($"Net.Proxy.{typeName}",
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    baseType
                    );
            return tb;

        }
        public static void SetClassAttribute<T>(this TypeBuilder tb, Type[] ctorParamTypes, object[] ctorParamObjects)
            where T : Attribute
        {
            ConstructorInfo classCtorInfo = typeof(T).GetConstructor(ctorParamTypes);
            CustomAttributeBuilder myCABuilder2 = new CustomAttributeBuilder(classCtorInfo, ctorParamObjects);
            tb.SetCustomAttribute(myCABuilder2);
        }
        public static PropertyBuilder AddProperty(this TypeBuilder tb, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig |
                  MethodAttributes.Virtual,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);

            return propertyBuilder;

        }
        public static PropertyBuilder AddInterfaceDefaultProperty(this TypeBuilder tb, PropertyInfo propInfo)
        {
            var propertyName = propInfo.Name ;
            var propertyType = propInfo.PropertyType;
            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.EmitCall(OpCodes.Call, propInfo.GetMethod,Type.EmptyTypes);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
               tb.DefineMethod("set_" + propertyName,
                 MethodAttributes.Public |
                 MethodAttributes.SpecialName |
                 MethodAttributes.HideBySig |
                 MethodAttributes.Virtual,
                 null, new[] { propertyType });
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);


            propertyBuilder.SetGetMethod(getPropMthdBldr);

            return propertyBuilder;

        }
        public static PropertyBuilder SetPropertyAttribute<TAttribute>(this PropertyBuilder pb, Type[] ctorParamTypes, object[] ctorParamObjects, PropertyInfo[] propertyInfos = null, object[] propertyValues = null)
            where TAttribute : Attribute
        {
            ConstructorInfo classCtorInfo = typeof(TAttribute).GetConstructor(ctorParamTypes);
            CustomAttributeBuilder myCABuilder2 = propertyInfos == null ?
                new CustomAttributeBuilder(classCtorInfo, ctorParamObjects) :
                new CustomAttributeBuilder(classCtorInfo, ctorParamObjects, propertyInfos, propertyValues);

            pb.SetCustomAttribute(myCABuilder2);
            return pb;

        }
        public static PropertyBuilder SetPropertyAttribute(this PropertyBuilder pb, IList<CustomAttributeData> attributes)
        {
            if (attributes == null || attributes.Count == 0) return pb;
            foreach (var attr in attributes)
            {

                var ctorInfo = attr.Constructor;
                var ctorArgs = attr.ConstructorArguments.Select(p => p.Value).ToArray();
                var fieldInfos = attr.NamedArguments.Where(p => p.IsField).Select(p => p.MemberInfo as FieldInfo).ToArray();
                var fieldValues = attr.NamedArguments.Where(p => p.IsField).Select(p => p.TypedValue.Value).ToArray();
                var propInfos = attr.NamedArguments.Where(p => !p.IsField).Select(p => p.MemberInfo as PropertyInfo).ToArray();
                var propValues = attr.NamedArguments.Where(p => !p.IsField).Select(p => p.TypedValue.Value).ToArray();
                var builder = new CustomAttributeBuilder(ctorInfo, ctorArgs, propInfos, propValues, fieldInfos, fieldValues);
                pb.SetCustomAttribute(builder);
            }
            return pb;

        }
        public static PropertyBuilder CreateSubPropertyReferencePropertyBuilder(this TypeBuilder tb, string propertyName, Type propertyType, MethodInfo refPropertyGetter, string refPropertySubName)
        {
            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig;

            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, methodAttributes, propertyType, Type.EmptyTypes);
            // Preparing Reflection instances
            MethodInfo method2 = refPropertyGetter.ReturnType.GetProperty(refPropertySubName).GetMethod;
            // Setting return type
            getPropMthdBldr.SetReturnType(propertyType);
            // Adding parameters
            ILGenerator gen = getPropMthdBldr.GetILGenerator();
            // Preparing locals
            LocalBuilder str = gen.DeclareLocal(propertyType);
            // Preparing labels
            Label label22 = gen.DefineLabel();
            Label label23 = gen.DefineLabel();
            Label label26 = gen.DefineLabel();
            // Writing body
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, refPropertyGetter);
            gen.Emit(OpCodes.Brfalse_S, label22);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, refPropertyGetter);
            gen.Emit(OpCodes.Callvirt, method2);
            gen.Emit(OpCodes.Br_S, label23);
            gen.MarkLabel(label22);
            gen.Emit(OpCodes.Ldnull);
            gen.MarkLabel(label23);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Br_S, label26);
            gen.MarkLabel(label26);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ret);
            // finished
            propertyBuilder.SetGetMethod(getPropMthdBldr);
            return propertyBuilder;

        }
        public static TypeBuilder CloneType<T>(string name)
            where T : class, new()
        {
            var typeBuilder = CreateTypeBuilder(name);
            foreach(var prop in InterfaceType.FindProperties(typeBuilder))
            {
                var probBuilder=AddProperty(typeBuilder, prop.Name, prop.PropertyType);
                
            }
            return typeBuilder;
        }
    }
}
