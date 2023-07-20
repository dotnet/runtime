// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// An interface details strategy that enables discovering both interfaces defined with source-generated COM (i.e. <see cref="GeneratedComInterfaceAttribute"/> and <see cref="IUnknownDerivedAttribute{T, TImpl}"/>) and built-in COM (i.e. <see cref="ComImportAttribute"/>).
    /// </summary>
    /// <remarks>
    /// This strategy is meant for intermediary adoption scenarios and is not compatible with trimming or NativeAOT by design. Since built-in COM is not trim friendly or AOT-compatible, these restrictions are okay.
    /// This strategy only supports "COM Object Wrapper" scenarios, so casting a COM object wrapper to a <see cref="ComImportAttribute"/>-attributed type. It does not support exposing a <see cref="ComImportAttribute"/>-attributed type as an additional interface on a managed object wrapper.
    /// The strategy provides <see cref="DynamicInterfaceCastableImplementationAttribute"/>-based implementations of <see cref="ComImportAttribute"/>-attributed interfaces by dynamically generating an interface using <see cref="System.Reflection.Emit"/> that has the following shape:
    /// <code>
    /// [assembly:IgnoresAccessChecksTo("AssemblyContainingIComInterface")]
    /// [assembly:IgnoresAccessChecksTo("AssemblyContainingRetType")]
    /// [assembly:IgnoresAccessChecksTo("AssemblyContainingArgType1")]
    /// [assembly:IgnoresAccessChecksTo("AssemblyContainingArgType2")]
    /// // One attribute per containing assembly of each type used in each method signature of the interface.
    ///
    /// namespace System.Runtime.CompilerServices
    /// {
    ///     [AssemblyUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    ///     internal class IgnoresAccessChecksToAttribute : Attribute
    ///     {
    ///         public IgnoresAccessChecksToAttribute(string assemblyName) { }
    ///     }
    /// }
    ///
    /// [DynamicInterfaceCastableImplementation]
    /// interface InterfaceForwarder : IComInterface
    /// {
    ///     RetType IComInterface.Method1(ArgType1 arg1, ArgType2 arg2, ...)
    ///     {
    ///         return ((IComInterface)((IComImportAdapter)this).GetRuntimeCallableWrapper())(arg1, arg2, ...);
    ///     }
    /// }
    /// </code>
    ///
    /// This mechanism allows source-generated COM interop to allow using built-in COM interfaces with runtime-defined marshalling behavior with minimal work on the source-generated COM interop side.
    /// Additionally, by scoping the majority of the logic to this class, we make this logic more easily trimmable.
    ///
    /// We emit the <c>IgnoresAccessChecksToAttribute</c> to enable casting to internal <see cref="ComImportAttribute"/> types, which is a very common scenario (most <see cref="ComImportAttribute"/> types are internal).
    /// </remarks>
    [RequiresDynamicCode("Enabling interop between source-generated and built-in COM is not supported when trimming is enabled.")]
    [RequiresUnreferencedCode("Enabling interop between source-generated and built-in COM requires dynamic code generation.")]
    internal sealed class ComImportInteropInterfaceDetailsStrategy : IIUnknownInterfaceDetailsStrategy
    {
        public static readonly IIUnknownInterfaceDetailsStrategy Instance = new ComImportInteropInterfaceDetailsStrategy();

        private readonly ConditionalWeakTable<Type, Type> _forwarderInterfaceCache = new();

        // TODO: Support exposing ComImport interfaces through StrategyBasedComWrappers?
        public IComExposedDetails? GetComExposedTypeDetails(RuntimeTypeHandle type) => DefaultIUnknownInterfaceDetailsStrategy.Instance.GetComExposedTypeDetails(type);

        public IIUnknownDerivedDetails? GetIUnknownDerivedDetails(RuntimeTypeHandle type)
        {
            Type runtimeType = Type.GetTypeFromHandle(type)!;
            if (!runtimeType.IsImport)
            {
                return DefaultIUnknownInterfaceDetailsStrategy.Instance.GetIUnknownDerivedDetails(type);
            }

            Type implementationType = _forwarderInterfaceCache.GetValue(runtimeType, runtimeType =>
            {
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ComImportForwarder"), runtimeType.IsCollectible ? AssemblyBuilderAccess.RunAndCollect : AssemblyBuilderAccess.Run);
                ModuleBuilder module = assembly.DefineDynamicModule("ComImportForwarder");

                ConstructorInfo ignoresAccessChecksToAttributeConstructor = GetIgnoresAccessChecksToAttributeConstructor(module);

                assembly.SetCustomAttribute(new CustomAttributeBuilder(ignoresAccessChecksToAttributeConstructor, new object[] { typeof(IComImportAdapter).Assembly.GetName().Name! }));

                TypeBuilder implementation = module.DefineType("InterfaceForwarder", TypeAttributes.Interface | TypeAttributes.Abstract, parent: null, interfaces: runtimeType.GetInterfaces());
                implementation.AddInterfaceImplementation(runtimeType);
                implementation.SetCustomAttribute(new CustomAttributeBuilder(typeof(DynamicInterfaceCastableImplementationAttribute).GetConstructor(Array.Empty<Type>())!, Array.Empty<object>()));

                foreach (Type iface in implementation.GetInterfaces())
                {
                    assembly.SetCustomAttribute(new CustomAttributeBuilder(ignoresAccessChecksToAttributeConstructor, new object[] { iface.Assembly.GetName().Name! }));
                    foreach (MethodInfo method in iface.GetMethods())
                    {
                        Type[] returnTypeOptionalModifiers = method.ReturnParameter.GetOptionalCustomModifiers();
                        Type[] returnTypeRequiredModifiers = method.ReturnParameter.GetRequiredCustomModifiers();
                        ParameterInfo[] parameters = method.GetParameters();
                        var parameterTypes = new Type[parameters.Length];
                        var parameterOptionalModifiers = new Type[parameters.Length][];
                        var parameterRequiredModifiers = new Type[parameters.Length][];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            parameterTypes[i] = parameters[i].ParameterType;
                            parameterOptionalModifiers[i] = parameters[i].GetOptionalCustomModifiers();
                            parameterRequiredModifiers[i] = parameters[i].GetRequiredCustomModifiers();
                        }
                        MethodBuilder builder = implementation.DefineMethod(method.Name, MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual, CallingConventions.HasThis, method.ReturnType, returnTypeRequiredModifiers, returnTypeOptionalModifiers, parameterTypes, parameterRequiredModifiers, parameterOptionalModifiers);
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

        private static ConstructorInfo GetIgnoresAccessChecksToAttributeConstructor(ModuleBuilder moduleBuilder)
        {
            Type attributeType = EmitIgnoresAccessChecksToAttribute(moduleBuilder);
            return attributeType.GetConstructor(new Type[] { typeof(string) })!;
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private static Type EmitIgnoresAccessChecksToAttribute(ModuleBuilder moduleBuilder)
        {
            var tb = moduleBuilder.DefineType(
                "System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
                TypeAttributes.NotPublic,
                typeof(Attribute));

            var attributeUsage = new CustomAttributeBuilder(
                s_attributeUsageCtor,
                new object[] { AttributeTargets.Assembly },
                new PropertyInfo[] { s_attributeUsageAllowMultipleProperty },
                new object[] { true });
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
            il.Emit(OpCodes.Call, s_attributeBaseClassCtor);
            il.Emit(OpCodes.Ret);

            return tb.CreateType()!;
        }

        /// <summary>
        /// The <see cref="Attribute()"/> constructor.
        /// </summary>
        private static readonly ConstructorInfo s_attributeBaseClassCtor = typeof(Attribute).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];

        /// <summary>
        /// The <see cref="AttributeUsageAttribute(AttributeTargets)"/> constructor.
        /// </summary>
        private static readonly ConstructorInfo s_attributeUsageCtor = typeof(AttributeUsageAttribute).GetConstructor(new Type[] { typeof(AttributeTargets) })!;

        /// <summary>
        /// The <see cref="AttributeUsageAttribute.AllowMultiple"/> property.
        /// </summary>
        private static readonly PropertyInfo s_attributeUsageAllowMultipleProperty = typeof(AttributeUsageAttribute).GetProperty(nameof(AttributeUsageAttribute.AllowMultiple))!;

        private sealed class ComImportDetails(Guid iid, Type implementation) : IIUnknownDerivedDetails
        {
            public Guid Iid { get; } = iid;

            public Type Implementation { get; } = implementation;

            public unsafe void** ManagedVirtualMethodTable => null;
        }

        /// <summary>
        /// This interface enables a COM Object Wrapper (such as <see cref="ComObject"/>) to provide a built-in COM object to enable integration between built-in COM objects and
        /// other COM interop systems like source-generated COM.
        /// </summary>
        internal interface IComImportAdapter
        {
            internal static readonly MethodInfo GetRuntimeCallableWrapperMethod = typeof(IComImportAdapter).GetMethod(nameof(GetRuntimeCallableWrapper))!;

            /// <summary>
            /// Gets the built-in COM object that corresponds to the same underlying COM object as this wrapper.
            /// </summary>
            /// <returns>The built-in RCW</returns>
            /// <remarks>The returned object must be an object such that a call to <see cref="Marshal.IsComObject(object)"/> would return true.</remarks>
            object GetRuntimeCallableWrapper();
        }
    }
}
