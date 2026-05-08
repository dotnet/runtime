// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
    [SetupRootEntireAssembly("test")]
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    public class RequiresInRootAllAssembly
    {
        public static void Main()
        {
        }

        [RequiresDynamicCode("--MethodWhichRequires--")]
        public static void MethodWhichRequires() { }

        [RequiresDynamicCode("--InstanceMethodWhichRequires--")]
        public void InstanceMethodWhichRequires() { }

        public sealed class ClassWithDAMAnnotatedMembers
        {
            public static void Method([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type) { }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public static Type Field;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        public sealed class ClassWithDAMAnnotation
        {
            public void Method() { }
        }

        [RequiresUnreferencedCode("--ClassWithRequires--")]
        public sealed class ClassWithRequires
        {
            public static int Field;

            internal static int InternalField;

            private static int PrivateField;

            public static void Method() { }

            public void InstanceMethod() { }

            public static int Property { get; set; }

            public static event EventHandler PropertyChanged;
        }

        [AttributeWithRequires]
        [ExpectedWarning("IL2026")]
        public sealed class ClassWithAttributeWithRequires
        {
        }

        [RequiresUnreferencedCode("--AttributeWithRequiresAttribute--")]
        public sealed class AttributeWithRequiresAttribute : Attribute
        {
        }
    }
}
