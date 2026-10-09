// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Runtime.CompilerServices;

public static partial class RuntimeHelpers
{
    private static partial class DelegateTypeFactory
    {
        private static readonly Dictionary<RuntimeType[], Type> s_types = new(SignatureComparer.Instance);
        private static ModuleBuilder? s_module;
        private static int s_nextTypeId;

        [RequiresDynamicCode("Creating a delegate type may require generating code at runtime.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The generated delegate only defines runtime-implemented methods and does not call MulticastDelegate constructors.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111:ReflectionToDynamicallyAccessedMembers",
            Justification = "The generated delegate does not call members of MulticastDelegate or Delegate.")]
        internal static Type GetCustomDelegateType(Type[] typeArgs)
        {
            RuntimeType[] signature = GetSignature(typeArgs, out _);
            lock (s_types)
            {
                if (s_types.TryGetValue(signature, out Type? result))
                {
                    return result;
                }

                if (s_module is null)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("RuntimeDelegates"), AssemblyBuilderAccess.Run);
                    s_module = assembly.DefineDynamicModule("RuntimeDelegates");
                }

                string name = "Delegate" + (s_nextTypeId++).ToString(CultureInfo.InvariantCulture);
                TypeBuilder builder = s_module.DefineType(name,
                    TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AutoClass,
                    typeof(MulticastDelegate));
                const MethodImplAttributes ImplementationFlags = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
                builder.DefineConstructor(
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName,
                    CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) })
                    .SetImplementationFlags(ImplementationFlags);
                builder.DefineMethod("Invoke",
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    signature[^1], signature[..^1])
                    .SetImplementationFlags(ImplementationFlags);
                result = builder.CreateTypeInfo();
                s_types.Add(signature, result);
                return result;
            }
        }
    }
}
