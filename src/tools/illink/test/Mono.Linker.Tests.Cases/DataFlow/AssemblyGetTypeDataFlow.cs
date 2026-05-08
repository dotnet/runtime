// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    [SetupCompileArgument("/unsafe")]
    public class AssemblyGetTypeDataFlow
    {
        public static void Main()
        {
            TestKnownType();
            TestKnownTypeWithThrowOnError();
            TestUnknownType();
            TestNullTypeName();
            TestCaseInsensitive();
            TestUnknownAssembly();
            TestUnknownTypeAssemblyGetType(null);
            TestArrayType();
            TestPointerType();
            TestGenericType();
            TestTypeOnlyInCoreLib();
            TestArrayReceiver();
            TestFunctionPointerReceiver();
            TestGenericParameterReceiver<InnerType>();
            TestGenericMethodArrayReceiver<InnerType>();
            TestAssemblyQualifiedTypeName();
        }

        class InnerType
        {
        }

        static void TestKnownType()
        {
            Type type = typeof(AssemblyGetTypeDataFlow).Assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType");
            type.RequiresNone();
        }

        static void TestKnownTypeWithThrowOnError()
        {
            Type type = typeof(AssemblyGetTypeDataFlow).Assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType", false);
            type.RequiresNone();
        }

        [ExpectedWarning("IL2026")]
        static void TestUnknownType()
        {
            string typeName = GetUnknownString();
            typeof(AssemblyGetTypeDataFlow).Assembly.GetType(typeName);
        }

        static void TestNullTypeName()
        {
            typeof(AssemblyGetTypeDataFlow).Assembly.GetType(null).RequiresAll();
        }

        [ExpectedWarning("IL2026")]
        static void TestCaseInsensitive()
        {
            typeof(AssemblyGetTypeDataFlow).Assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType", false, true);
        }

        [ExpectedWarning("IL2026")]
        static void TestUnknownAssembly()
        {
            Assembly assembly = GetUnknownAssembly();
            assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType");
        }

        [ExpectedWarning("IL2026")]
        static void TestUnknownTypeAssemblyGetType(Type unknownType)
        {
            unknownType.Assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType");
        }

        // Verifies the resolver supports array type names. Without array support the analyzer
        // would treat the result as unknown (Top) instead of a known SystemTypeValue, and
        // GetProperty("Length") below would emit IL2075 instead of resolving Array.Length.
        static void TestArrayType()
        {
            Type type = typeof(AssemblyGetTypeDataFlow).Assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType[]");
            type.GetProperty("Length");
        }

        static void TestPointerType()
        {
            Type type = typeof(AssemblyGetTypeDataFlow).Assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType*");
            type.RequiresAll();
        }

        class GenericType<T>
        {
        }

        // Generic type arguments are unqualified; Assembly.GetType resolves them in the
        // receiver assembly (no corelib fallback).
        static void TestGenericType()
        {
            Type type = typeof(AssemblyGetTypeDataFlow).Assembly.GetType(
                "Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+GenericType`1[[Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType]]");
            type.RequiresAll();
        }

        // Verifies Assembly.GetType does not over-resolve to corelib. "System.Reflection.Assembly"
        // is not defined in this assembly, so Assembly.GetType must return null at runtime; the
        // analyzer must not statically resolve it to corelib's System.Reflection.Assembly. If it
        // did, RequiresAll() would mark RequiresUnreferencedCode-annotated members like LoadFrom
        // and emit IL2026.
        static void TestTypeOnlyInCoreLib()
        {
            typeof(AssemblyGetTypeDataFlow).Assembly.GetType("System.Reflection.Assembly").RequiresAll();
        }

        // The dataflow only models Type.Assembly for named types. Array, pointer, byref, function
        // pointer, and open generic parameter receivers are intentionally rejected and fall back
        // to an IL2026 warning at the Assembly.GetType call site.

        [ExpectedWarning("IL2026")]
        static void TestArrayReceiver()
        {
            typeof(InnerType[]).Assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType");
        }

        // The Roslyn analyzer models typeof(delegate*<...>) as Top (no SystemTypeValue), so
        // the Type.Assembly intrinsic short-circuits and no warning is emitted.
        [ExpectedWarning("IL2026", Tool.Trimmer | Tool.NativeAot, "Roslyn analyzer doesn't model typeof for function pointers")]
        static unsafe void TestFunctionPointerReceiver()
        {
            typeof(delegate*<InnerType, void>).Assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType");
        }

        [ExpectedWarning("IL2026")]
        static void TestGenericParameterReceiver<T>()
        {
            typeof(T).Assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType");
        }

        [ExpectedWarning("IL2026")]
        static void TestGenericMethodArrayReceiver<T>()
        {
            typeof(T[]).Assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType");
        }

        // Assembly.GetType rejects top-level assembly-qualified names at runtime (returns null).
        // The analyzer must not honor the qualifier and resolve in the named assembly. If it did,
        // RequiresAll() would mark RUC members on System.Reflection.Assembly and emit IL2026.
        static void TestAssemblyQualifiedTypeName()
        {
            typeof(AssemblyGetTypeDataFlow).Assembly.GetType(
                "System.Reflection.Assembly, System.Runtime").RequiresAll();
        }

        static string GetUnknownString() => "unknown";

        static Assembly GetUnknownAssembly() => typeof(AssemblyGetTypeDataFlow).Assembly;
    }
}
