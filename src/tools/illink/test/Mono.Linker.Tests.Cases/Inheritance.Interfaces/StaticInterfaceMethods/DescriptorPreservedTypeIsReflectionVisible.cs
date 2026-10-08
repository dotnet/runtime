// Copyright(c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
    /// <summary>
    /// Tests that a type preserved via XML descriptor is fully treated as reflection-visible:
    /// - Its static abstract interface implementations are preserved (variant casting)
    /// - Its explicit-layout fields are preserved (MarkImplicitlyUsedFields)
    /// These are all consequences of the type being obtainable via
    /// MethodBase.GetCurrentMethod().DeclaringType on a descriptor-preserved method.
    /// </summary>
    [SetupLinkerDescriptorFile("DescriptorPreservedTypeIsReflectionVisible.xml")]
    [ExpectedNoWarnings]
    public class DescriptorPreservedTypeIsReflectionVisible
    {
        [Kept]
        public static void Main()
        {
            CallMethodOnConstrainedType<DirectlyUsed>();
            UseViaReflection();
        }

        [Kept]
        interface IStaticAbstract
        {
            [Kept]
            static abstract void Method();

            [Kept]
            public static void Call<T>() where T : IStaticAbstract => T.Method();
        }

        [Kept]
        [KeptInterface(typeof(IStaticAbstract))]
        class DirectlyUsed : IStaticAbstract
        {
            [Kept]
            [KeptOverride(typeof(IStaticAbstract))]
            public static void Method() { }
        }

        // Reference type with explicit layout, preserved only via descriptor.
        // If the type is properly marked as reflection-visible, its fields should be
        // kept due to MarkImplicitlyUsedFields(explicit layout requires all fields).
        [Kept]
        [KeptInterface(typeof(IStaticAbstract))]
        [StructLayout(LayoutKind.Explicit)]
        class ExplicitLayoutPreservedViaDescriptor : IStaticAbstract
        {
            [Kept]
            [FieldOffset(0)]
            public int FieldA;

            [Kept]
            [FieldOffset(4)]
            public int FieldB;

            [Kept]
            [KeptOverride(typeof(IStaticAbstract))]
            public static void Method() { }

            [Kept]
            [ExpectedWarning("IL2026", nameof(MethodBase.GetCurrentMethod))]
            public static Type GetMyType() => MethodBase.GetCurrentMethod().DeclaringType;
        }

        [Kept]
        [ExpectedWarning("IL3050", nameof(MethodInfo.MakeGenericMethod), Tool.Analyzer | Tool.NativeAot, "")]
        static void UseViaReflection()
        {
            typeof(IStaticAbstract).GetMethod(nameof(IStaticAbstract.Call))
                .MakeGenericMethod(ExplicitLayoutPreservedViaDescriptor.GetMyType())
                .Invoke(null, null);
        }

        [Kept]
        static void CallMethodOnConstrainedType<T>() where T : IStaticAbstract
        {
            T.Method();
        }
    }
}
