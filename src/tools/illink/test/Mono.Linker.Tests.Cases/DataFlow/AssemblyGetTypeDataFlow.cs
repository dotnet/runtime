// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
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

        static string GetUnknownString() => "unknown";

        static Assembly GetUnknownAssembly() => typeof(AssemblyGetTypeDataFlow).Assembly;
    }
}
