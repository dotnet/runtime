// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
    [IgnoreTestCase("https://github.com/dotnet/runtime/issues/131892", IgnoredBy = Tool.Trimmer)]
    [SetupCompileBefore("library.dll", new[] { "Dependencies/DynamicDependencyWithNestedTypeReferenceLibrary.cs" })]
    public class DynamicDependencyWithNestedTypeReference
    {
        public static void Main()
        {
            Dependency();
        }

        [Kept]
        [DynamicDependency("MethodWithParameter(Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.Outer.Nested)", typeof(Target))]
        private static void Dependency()
        {
        }

        private static class Target
        {
            [Kept]
            private static void MethodWithParameter(Outer.Nested value)
            {
            }
        }
    }
}
