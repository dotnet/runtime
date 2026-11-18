// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
    [SetupCompileBefore("library.dll", new[] { "Dependencies/DynamicDependencyWithNestedTypeReferenceLibrary.cs" })]
    public class DynamicDependencyWithNestedReturnTypeReference
    {
        public static void Main()
        {
            Dependency();
        }

        [Kept]
        [DynamicDependency("Method()~Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.Outer.Nested", typeof(Target))]
        [DynamicDependency("GenericMethod()~Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.GenericOuter{System.Int32}.GenericMiddle{System.String}.GenericNested{System.Boolean}", typeof(Target))]
        [DynamicDependency("NonGenericMethod()~Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.GenericOuter{System.Int32}.NonGenericNested", typeof(Target))]
        private static void Dependency()
        {
        }

        private static class Target
        {
            [Kept]
            private static Outer.Nested Method()
            {
                return null;
            }

            [Kept]
            private static GenericOuter<int>.GenericMiddle<string>.GenericNested<bool> GenericMethod()
            {
                return null;
            }

            [Kept]
            private static GenericOuter<int>.NonGenericNested NonGenericMethod()
            {
                return null;
            }
        }
    }
}
