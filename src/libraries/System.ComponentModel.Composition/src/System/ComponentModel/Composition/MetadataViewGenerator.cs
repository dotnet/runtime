// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Internal;

namespace System.ComponentModel.Composition
{
    // // Assume TMetadataView is
    // //interface Foo
    // //{
    // //    public typeRecord1 Record1 { get; }
    // //    public typeRecord2 Record2 { get; }
    // //    public typeRecord3 Record3 { get; }
    // //    public typeRecord4 Record4 { get; }
    // //}
    // // The class to be generated will look approximately like:
    // public class __Foo__MedataViewProxy : TMetadataView
    // {
    //     public static object Create(IDictionary<string, object> metadata)
    //     {
    //        return new __Foo__MedataViewProxy(metadata);
    //     }
    //
    //     public __Foo__MedataViewProxy (IDictionary<string, object> metadata)
    //     {
    //         if (metadata == null)
    //         {
    //             throw InvalidArgumentException("metadata");
    //         }
    //         try
    //         {
    //              Record1 = (typeRecord1)Record1;
    //              Record2 = (typeRecord1)Record2;
    //              Record3 = (typeRecord1)Record3;
    //              Record4 = (typeRecord1)Record4;
    //          }
    //          catch (InvalidCastException ice)
    //          {
    //              //Annotate exception .Data with diagnostic info
    //          }
    //          catch (NulLReferenceException ice)
    //          {
    //              //Annotate exception .Data with diagnostic info
    //          }
    //     }
    //     // Interface
    //     public typeRecord1 Record1 { get; }
    //     public typeRecord2 Record2 { get; }
    //     public typeRecord3 Record3 { get; }
    //     public typeRecord4 Record4 { get; }
    // }
    internal static class MetadataViewGenerator
    {
        public delegate object MetadataViewFactory(IDictionary<string, object?> metadata);

        public const string MetadataViewType        = "MetadataViewType";
        public const string MetadataItemKey         = "MetadataItemKey";
        public const string MetadataItemTargetType  = "MetadataItemTargetType";
        public const string MetadataItemSourceType  = "MetadataItemSourceType";
        public const string MetadataItemValue       = "MetadataItemValue";
        public const string MetadataViewFactoryName = "Create";

        private static readonly Lock _lock = new Lock();
        private static readonly Dictionary<Type, MetadataViewFactory> _metadataViewFactories = new Dictionary<Type, MetadataViewFactory>();
        private static readonly AssemblyName ProxyAssemblyName = new AssemblyName($"MetadataViewProxies_{Guid.NewGuid()}");
        private static ModuleBuilder? transparentProxyModuleBuilder;

        private static readonly Type[] CtorArgumentTypes = new Type[] { typeof(IDictionary<string, object>) };
        private static readonly MethodInfo _mdvDictionaryTryGet = CtorArgumentTypes[0].GetMethod("TryGetValue")!;
        private static readonly MethodInfo ObjectGetType = typeof(object).GetMethod("GetType", Type.EmptyTypes)!;
        private static readonly ConstructorInfo ObjectCtor = typeof(object).GetConstructor(Type.EmptyTypes)!;

        // Must be called with _lock held
        private static ModuleBuilder GetProxyModuleBuilder(bool requiresCritical)
        {
            if (transparentProxyModuleBuilder == null)
            {
                // make a new assemblybuilder and modulebuilder
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(ProxyAssemblyName, AssemblyBuilderAccess.Run);
                transparentProxyModuleBuilder = assemblyBuilder.DefineDynamicModule("MetadataViewProxiesModule");
            }

            return transparentProxyModuleBuilder;
        }

        public static MetadataViewFactory GetMetadataViewFactory(Type viewType)
        {
            ArgumentNullException.ThrowIfNull(viewType);

            if (!viewType.IsInterface)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            MetadataViewFactory? metadataViewFactory;
            bool foundMetadataViewFactory;

            using (new ReadLock(_lock))
            {
                foundMetadataViewFactory = _metadataViewFactories.TryGetValue(viewType, out metadataViewFactory);
            }

            // No factory exists
            if (!foundMetadataViewFactory)
            {
                // Try again under a write lock if still none generate the proxy
                Type? generatedProxyType = GenerateInterfaceViewProxyType(viewType);
                if (generatedProxyType == null)
                {
                    throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                }

                MetadataViewFactory generatedMetadataViewFactory = (MetadataViewFactory)Delegate.CreateDelegate(
                    typeof(MetadataViewFactory), generatedProxyType.GetMethod(MetadataViewGenerator.MetadataViewFactoryName, BindingFlags.Public | BindingFlags.Static)!);
                if (generatedMetadataViewFactory == null)
                {
                    throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                }

                using (new WriteLock(_lock))
                {
                    if (!_metadataViewFactories.TryGetValue(viewType, out metadataViewFactory))
                    {
                        metadataViewFactory = generatedMetadataViewFactory;
                        _metadataViewFactories.Add(viewType, metadataViewFactory);
                    }
                }
            }
            return metadataViewFactory!;
        }

        public static TMetadataView CreateMetadataView<TMetadataView>(MetadataViewFactory metadataViewFactory, IDictionary<string, object?> metadata)
        {
            ArgumentNullException.ThrowIfNull(metadataViewFactory);

            // we are simulating the Activator.CreateInstance behavior by wrapping everything in a TargetInvocationException
            try
            {
                return (TMetadataView)metadataViewFactory.Invoke(metadata);
            }
            catch (Exception e)
            {
                throw new TargetInvocationException(e);
            }
        }

        private static void GenerateLocalAssignmentFromDefaultAttribute(this ILGenerator IL, DefaultValueAttribute[] attrs, LocalBuilder local)
        {
            if (attrs.Length > 0)
            {
                DefaultValueAttribute defaultAttribute = attrs[0];
                IL.LoadValue(defaultAttribute.Value);
                if ((defaultAttribute.Value != null) && (defaultAttribute.Value.GetType().IsValueType))
                {
                    IL.Emit(OpCodes.Box, defaultAttribute.Value.GetType());
                }
                IL.Emit(OpCodes.Stloc, local);
            }
        }

        private static void GenerateFieldAssignmentFromLocalValue(this ILGenerator IL, LocalBuilder local, FieldBuilder field)
        {
            IL.Emit(OpCodes.Ldarg_0);
            IL.Emit(OpCodes.Ldloc, local);
            IL.Emit(field.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, field.FieldType);
            IL.Emit(OpCodes.Stfld, field);
        }

        private static void GenerateLocalAssignmentFromFlag(this ILGenerator IL, LocalBuilder local, bool flag)
        {
            IL.Emit(flag ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            IL.Emit(OpCodes.Stloc, local);
        }

        // This must be called with _readerWriterLock held for Write
        private static Type? GenerateInterfaceViewProxyType(Type viewType)
        {
            // View type is an interface let's cook an implementation
            Type? proxyType;
            TypeBuilder proxyTypeBuilder;
            Type[] interfaces = { viewType };
            bool requiresCritical = false;

            var proxyModuleBuilder = GetProxyModuleBuilder(requiresCritical);
            proxyTypeBuilder = proxyModuleBuilder.DefineType(
                $"_proxy_{viewType.FullName}_{Guid.NewGuid()}",
                TypeAttributes.Public,
                typeof(object),
                interfaces);
            // Implement Constructor
            ConstructorBuilder proxyCtor = proxyTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, CtorArgumentTypes);
            ILGenerator proxyCtorIL = proxyCtor.GetILGenerator();
            proxyCtorIL.Emit(OpCodes.Ldarg_0);
            proxyCtorIL.Emit(OpCodes.Call, ObjectCtor);

            LocalBuilder exception = proxyCtorIL.DeclareLocal(typeof(Exception));
            LocalBuilder exceptionData = proxyCtorIL.DeclareLocal(typeof(IDictionary));
            LocalBuilder sourceType = proxyCtorIL.DeclareLocal(typeof(Type));
            LocalBuilder value = proxyCtorIL.DeclareLocal(typeof(object));
            LocalBuilder usesExportedMD = proxyCtorIL.DeclareLocal(typeof(bool));

            Label tryConstructView = proxyCtorIL.BeginExceptionBlock();

            // Implement interface properties
            foreach (PropertyInfo propertyInfo in viewType.GetAllProperties())
            {
                string fieldName = $"_{propertyInfo.Name}_{Guid.NewGuid()}";

                // Cache names and type for exception
                string propertyName = propertyInfo.Name;

                Type[] propertyTypeArguments = new Type[] { propertyInfo.PropertyType };
                Type[]? optionalModifiers = null;
                Type[]? requiredModifiers = null;

                // PropertyInfo does not support GetOptionalCustomModifiers and GetRequiredCustomModifiers on Silverlight
                optionalModifiers = propertyInfo.GetOptionalCustomModifiers();
                requiredModifiers = propertyInfo.GetRequiredCustomModifiers();
                Array.Reverse(optionalModifiers);
                Array.Reverse(requiredModifiers);

                // Generate field
                FieldBuilder proxyFieldBuilder = proxyTypeBuilder.DefineField(
                    fieldName,
                    propertyInfo.PropertyType,
                    FieldAttributes.Private);

                // Generate property
                PropertyBuilder proxyPropertyBuilder = proxyTypeBuilder.DefineProperty(
                    propertyName,
                    PropertyAttributes.None,
                    propertyInfo.PropertyType,
                    propertyTypeArguments);

                // Generate constructor code for retrieving the metadata value and setting the field
                Label tryCastValue = proxyCtorIL.BeginExceptionBlock();
                Label innerTryCastValue;

                DefaultValueAttribute[] attrs = propertyInfo.GetAttributes<DefaultValueAttribute>(false);
                if (attrs.Length > 0)
                {
                    innerTryCastValue = proxyCtorIL.BeginExceptionBlock();
                }

                // In constructor set the backing field with the value from the dictionary
                Label doneGettingDefaultValue = proxyCtorIL.DefineLabel();
                GenerateLocalAssignmentFromFlag(proxyCtorIL, usesExportedMD, true);

                proxyCtorIL.Emit(OpCodes.Ldarg_1);
                proxyCtorIL.Emit(OpCodes.Ldstr, propertyInfo.Name);
                proxyCtorIL.Emit(OpCodes.Ldloca, value);
                proxyCtorIL.Emit(OpCodes.Callvirt, _mdvDictionaryTryGet);
                proxyCtorIL.Emit(OpCodes.Brtrue, doneGettingDefaultValue);

                proxyCtorIL.GenerateLocalAssignmentFromFlag(usesExportedMD, false);
                proxyCtorIL.GenerateLocalAssignmentFromDefaultAttribute(attrs, value);

                proxyCtorIL.MarkLabel(doneGettingDefaultValue);
                proxyCtorIL.GenerateFieldAssignmentFromLocalValue(value, proxyFieldBuilder);
                proxyCtorIL.Emit(OpCodes.Leave, tryCastValue);

                // catch blocks for innerTryCastValue start here
                if (attrs.Length > 0)
                {
                    proxyCtorIL.BeginCatchBlock(typeof(InvalidCastException));
                    {
                        Label notUsesExportedMd = proxyCtorIL.DefineLabel();
                        proxyCtorIL.Emit(OpCodes.Ldloc, usesExportedMD);
                        proxyCtorIL.Emit(OpCodes.Brtrue, notUsesExportedMd);
                        proxyCtorIL.Emit(OpCodes.Rethrow);
                        proxyCtorIL.MarkLabel(notUsesExportedMd);
                        proxyCtorIL.GenerateLocalAssignmentFromDefaultAttribute(attrs, value);
                        proxyCtorIL.GenerateFieldAssignmentFromLocalValue(value, proxyFieldBuilder);
                    }
                    proxyCtorIL.EndExceptionBlock();
                }

                // catch blocks for tryCast start here
                proxyCtorIL.BeginCatchBlock(typeof(NullReferenceException));
                {
                    proxyCtorIL.Emit(OpCodes.Stloc, exception);

                    proxyCtorIL.GetExceptionDataAndStoreInLocal(exception, exceptionData);
                    proxyCtorIL.AddItemToLocalDictionary(exceptionData, MetadataItemKey, propertyName);
                    proxyCtorIL.AddItemToLocalDictionary(exceptionData, MetadataItemTargetType, propertyInfo.PropertyType);
                    proxyCtorIL.Emit(OpCodes.Rethrow);
                }

                proxyCtorIL.BeginCatchBlock(typeof(InvalidCastException));
                {
                    proxyCtorIL.Emit(OpCodes.Stloc, exception);

                    proxyCtorIL.GetExceptionDataAndStoreInLocal(exception, exceptionData);
                    proxyCtorIL.AddItemToLocalDictionary(exceptionData, MetadataItemKey, propertyName);
                    proxyCtorIL.AddItemToLocalDictionary(exceptionData, MetadataItemTargetType, propertyInfo.PropertyType);
                    proxyCtorIL.Emit(OpCodes.Rethrow);
                }

                proxyCtorIL.EndExceptionBlock();

                if (propertyInfo.CanWrite)
                {
                    // The MetadataView '{0}' is invalid because property '{1}' has a property set method.
                    throw new NotSupportedException(SR.Format(
                        SR.InvalidSetterOnMetadataField,
                        viewType,
                        propertyName));
                }
                if (propertyInfo.CanRead)
                {
                    // Generate "get" method implementation.
                    MethodBuilder getMethodBuilder = proxyTypeBuilder.DefineMethod(
                        "get_" + propertyName,
                        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                        CallingConventions.HasThis,
                        propertyInfo.PropertyType,
                        requiredModifiers,
                        optionalModifiers,
                        Type.EmptyTypes, null, null);

                    proxyTypeBuilder.DefineMethodOverride(getMethodBuilder, propertyInfo.GetGetMethod()!);
                    ILGenerator getMethodIL = getMethodBuilder.GetILGenerator();
                    getMethodIL.Emit(OpCodes.Ldarg_0);
                    getMethodIL.Emit(OpCodes.Ldfld, proxyFieldBuilder);
                    getMethodIL.Emit(OpCodes.Ret);

                    proxyPropertyBuilder.SetGetMethod(getMethodBuilder);
                }
            }

            proxyCtorIL.Emit(OpCodes.Leave, tryConstructView);

            // catch blocks for constructView start here
            proxyCtorIL.BeginCatchBlock(typeof(NullReferenceException));
            {
                proxyCtorIL.Emit(OpCodes.Stloc, exception);

                proxyCtorIL.GetExceptionDataAndStoreInLocal(exception, exceptionData);
                proxyCtorIL.AddItemToLocalDictionary(exceptionData, MetadataViewType, viewType);
                proxyCtorIL.Emit(OpCodes.Rethrow);
            }
            proxyCtorIL.BeginCatchBlock(typeof(InvalidCastException));
            {
                proxyCtorIL.Emit(OpCodes.Stloc, exception);

                proxyCtorIL.GetExceptionDataAndStoreInLocal(exception, exceptionData);
                proxyCtorIL.Emit(OpCodes.Ldloc, value);
                proxyCtorIL.Emit(OpCodes.Call, ObjectGetType);
                proxyCtorIL.Emit(OpCodes.Stloc, sourceType);
                proxyCtorIL.AddItemToLocalDictionary(exceptionData, MetadataViewType, viewType);
                proxyCtorIL.AddLocalToLocalDictionary(exceptionData, MetadataItemSourceType, sourceType);
                proxyCtorIL.AddLocalToLocalDictionary(exceptionData, MetadataItemValue, value);
                proxyCtorIL.Emit(OpCodes.Rethrow);
            }
            proxyCtorIL.EndExceptionBlock();

            // Finished implementing the constructor
            proxyCtorIL.Emit(OpCodes.Ret);

            // Implemet the static factory
            // public object Create(IDictionary<string, object>)
            // {
            //    return new <ProxyClass>(dictionary);
            // }
            MethodBuilder factoryMethodBuilder = proxyTypeBuilder.DefineMethod(MetadataViewGenerator.MetadataViewFactoryName, MethodAttributes.Public | MethodAttributes.Static, typeof(object), CtorArgumentTypes);
            ILGenerator factoryIL = factoryMethodBuilder.GetILGenerator();
            factoryIL.Emit(OpCodes.Ldarg_0);
            factoryIL.Emit(OpCodes.Newobj, proxyCtor);
            factoryIL.Emit(OpCodes.Ret);

            // Finished implementing the type
            proxyType = proxyTypeBuilder.CreateTypeInfo();

            return proxyType;
        }

    }
}
