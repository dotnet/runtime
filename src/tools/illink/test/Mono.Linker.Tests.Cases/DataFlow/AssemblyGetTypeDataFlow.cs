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

        [ExpectedWarning("IL2057")]
        static void TestUnknownType()
        {
            string typeName = GetUnknownString();
            typeof(AssemblyGetTypeDataFlow).Assembly.GetType(typeName);
        }

        static void TestNullTypeName()
        {
            typeof(AssemblyGetTypeDataFlow).Assembly.GetType(null).RequiresAll();
        }

        [ExpectedWarning("IL2096")]
        static void TestCaseInsensitive()
        {
            typeof(AssemblyGetTypeDataFlow).Assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType", false, true);
        }

        [ExpectedWarning("IL2128")]
        static void TestUnknownAssembly()
        {
            Assembly assembly = GetUnknownAssembly();
            assembly.GetType("Mono.Linker.Tests.Cases.DataFlow.AssemblyGetTypeDataFlow+InnerType");
        }

        static string GetUnknownString() => "unknown";

        static Assembly GetUnknownAssembly() => typeof(AssemblyGetTypeDataFlow).Assembly;
    }
}
