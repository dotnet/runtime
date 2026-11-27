// Copyright(c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
    /// <summary>
    /// When a type is preserved via an XML descriptor, its static abstract interface
    /// implementations should be kept if the static abstract methods are used through
    /// constrained calls elsewhere in the program.
    /// </summary>
    [SetupLinkerDescriptorFile("StaticAbstractMethodsPreservedViaDescriptor.xml")]
    [ExpectedNoWarnings]
    public class StaticAbstractMethodsPreservedViaDescriptor
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

        // This type is NOT referenced via typeof() or instantiated in code.
        // It is only preserved via the XML descriptor file.
        // The bug: ILLink strips its static abstract implementation even though
        // the type is reflection-visible through the descriptor.
        [Kept]
        [KeptInterface(typeof(IStaticAbstract))]
        class PreservedViaDescriptorOnly : IStaticAbstract
        {
            [Kept]
            [KeptOverride(typeof(IStaticAbstract))]
            public static void Method() { }

            // This method is preserved by the XML descriptor, making it reflection-visible.
            // Its DeclaringType (PreservedViaDescriptorOnly) is therefore also reflection-visible
            // and could be passed to MakeGenericMethod for a method constrained on IStaticAbstract.
            [Kept]
            [ExpectedWarning("IL2026", nameof(MethodBase.GetCurrentMethod))]
            public static Type GetMyType() => MethodBase.GetCurrentMethod().DeclaringType;
        }

        // Mirrors the pattern where a descriptor-preserved method's DeclaringType is used
        // as a type argument to MakeGenericMethod on a method constrained by IStaticAbstract.
        [Kept]
        [ExpectedWarning("IL3050", nameof(MethodInfo.MakeGenericMethod), Tool.Analyzer | Tool.NativeAot, "")]
        static void UseViaReflection()
        {
            typeof(IStaticAbstract).GetMethod(nameof(IStaticAbstract.Call))
                .MakeGenericMethod(PreservedViaDescriptorOnly.GetMyType())
                .Invoke(null, null);
        }

        [Kept]
        static void CallMethodOnConstrainedType<T>() where T : IStaticAbstract
        {
            T.Method();
        }
    }
}
