// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    [RequiresDynamicCode("Enabling interop between source-generated and runtime-generated COM requires dynamic code generation.")]
    internal sealed class ComImportInteropInterfaceDetailsStrategy : IIUnknownInterfaceDetailsStrategy
    {
        public static readonly IIUnknownInterfaceDetailsStrategy Instance = new ComImportInteropInterfaceDetailsStrategy();

        private readonly ConditionalWeakTable<Type, Type> _forwarderInterfaceCache = new();

        // TODO: Support exposing ComImport interfaces through StrategyBasedComWrappers?
        public IComExposedDetails? GetComExposedTypeDetails(RuntimeTypeHandle type) => DefaultIUnknownInterfaceDetailsStrategy.Instance.GetComExposedTypeDetails(type);

        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Runtime-based COM interop is not supported with trimming enabled.")]
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Runtime-based COM interop is not supported with trimming enabled.")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Runtime-based COM interop is not supported with trimming enabled.")]
        public IIUnknownDerivedDetails? GetIUnknownDerivedDetails(RuntimeTypeHandle type)
        {
            Type runtimeType = Type.GetTypeFromHandle(type)!;
            if (!runtimeType.IsImport)
            {
                return DefaultIUnknownInterfaceDetailsStrategy.Instance.GetIUnknownDerivedDetails(type);
            }

            Type implementationType = _forwarderInterfaceCache.GetValue(runtimeType, runtimeType =>
            {
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ComImportForwarder"), AssemblyBuilderAccess.RunAndCollect);
                ModuleBuilder module = assembly.DefineDynamicModule("ComImportForwarder");

                ConstructorInfo ignoresAccessChecksToAttributeConstructor = GetMagicAttributeConstructor(module);

                assembly.SetCustomAttribute(new CustomAttributeBuilder(ignoresAccessChecksToAttributeConstructor, new object[] { typeof(IComImportAdapter).Assembly.GetName().Name! }));

                TypeBuilder implementation = module.DefineType("InterfaceForwarder", TypeAttributes.Interface | TypeAttributes.Abstract, parent: null, interfaces: runtimeType.GetInterfaces());
                implementation.AddInterfaceImplementation(runtimeType);
                implementation.SetCustomAttribute(new CustomAttributeBuilder(typeof(DynamicInterfaceCastableImplementationAttribute).GetConstructor(Array.Empty<Type>())!, Array.Empty<object>()));

                foreach (Type iface in implementation.GetInterfaces())
                {
                    assembly.SetCustomAttribute(new CustomAttributeBuilder(ignoresAccessChecksToAttributeConstructor, new object[] { iface.Assembly.GetName().Name! }));
                    foreach (MethodInfo method in iface.GetMethods())
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        var parameterTypes = new Type[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            parameterTypes[i] = parameters[i].ParameterType;
                        }
                        MethodBuilder builder = implementation.DefineMethod(method.Name, MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual, method.ReturnType, parameterTypes);
                        ILGenerator il = builder.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Castclass, typeof(IComImportAdapter));
                        il.Emit(OpCodes.Callvirt, IComImportAdapter.GetRuntimeCallableWrapperMethod);
                        il.Emit(OpCodes.Castclass, iface);
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            il.Emit(OpCodes.Ldarg, i + 1);
                        }
                        il.Emit(OpCodes.Callvirt, method);
                        il.Emit(OpCodes.Ret);
                        implementation.DefineMethodOverride(builder, method);
                    }
                }

                return implementation.CreateType();
            });

            return new ComImportDetails(runtimeType.GUID, implementationType);
        }

        private static void SkipVisibilityChecksFor(AssemblyBuilder assemblyBuilder, ConstructorInfo ignoresAccessChecksToAttributeConstructor, Type type)
        {
            var attributeBuilder = new CustomAttributeBuilder(ignoresAccessChecksToAttributeConstructor, new object[] { type.GetTypeInfo().Assembly.GetName().Name! });
            assemblyBuilder.SetCustomAttribute(attributeBuilder);
        }

        private static ConstructorInfo GetMagicAttributeConstructor(ModuleBuilder moduleBuilder)
        {
            var magicAttribute = EmitMagicAttribute(moduleBuilder);
            return magicAttribute.GetConstructor(new Type[] { typeof(string) })!;
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private static Type EmitMagicAttribute(ModuleBuilder moduleBuilder)
        {
            var tb = moduleBuilder.DefineType(
                "System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
                TypeAttributes.NotPublic,
                typeof(Attribute));

            var attributeUsage = new CustomAttributeBuilder(
                AttributeUsageCtor,
                new object[] { AttributeTargets.Assembly },
                new PropertyInfo[] { AttributeUsageAllowMultipleProperty },
                new object[] { false });
            tb.SetCustomAttribute(attributeUsage);

            var cb = tb.DefineConstructor(
                MethodAttributes.Public |
                MethodAttributes.HideBySig |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                new Type[] { typeof(string) });
            cb.DefineParameter(1, ParameterAttributes.None, "assemblyName");

            var il = cb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, AttributeBaseClassCtor);
            il.Emit(OpCodes.Ret);

            return tb.CreateType()!;
        }

        /// <summary>
        /// The <see cref="Attribute.Attribute()"/> constructor.
        /// </summary>
        private static readonly ConstructorInfo AttributeBaseClassCtor = typeof(Attribute).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];

        /// <summary>
        /// The <see cref="AttributeUsageAttribute(AttributeTargets)"/> constructor.
        /// </summary>
        private static readonly ConstructorInfo AttributeUsageCtor = typeof(AttributeUsageAttribute).GetConstructor(new Type[] { typeof(AttributeTargets) })!;

        /// <summary>
        /// The <see cref="AttributeUsageAttribute.AllowMultiple"/> property.
        /// </summary>
        private static readonly PropertyInfo AttributeUsageAllowMultipleProperty = typeof(AttributeUsageAttribute).GetProperty(nameof(AttributeUsageAttribute.AllowMultiple))!;

        private sealed class ComImportDetails(Guid iid, Type implementation) : IIUnknownDerivedDetails
        {
            public Guid Iid { get; } = iid;

            public Type Implementation { get; } = implementation;

            public unsafe void** ManagedVirtualMethodTable => null;
        }

        internal interface IComImportAdapter
        {
            internal static readonly MethodInfo GetRuntimeCallableWrapperMethod = typeof(IComImportAdapter).GetMethod(nameof(GetRuntimeCallableWrapper))!;

            object GetRuntimeCallableWrapper();
        }
    }
}
