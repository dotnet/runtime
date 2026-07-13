// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    class UnusedVirtualMethodAnnotations
    {
        [UnconditionalSuppressMessage("Test", "IL2026")]
        public static void Main()
        {
            _ = typeof(TypeOnlyImplementation);

            IUsed used = new UsedImplementation();
            used.Method(typeof(object));

            IPartiallyUsed partiallyUsed = new PartiallyUsedImplementation();
            partiallyUsed.Method(typeof(object));
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        interface IUnused
        {
            [RequiresUnreferencedCode(nameof(Method))]
            void Method([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type);
        }

        class UnusedImplementation : IUnused
        {
            public void Method(Type type) { }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        interface ITypeOnly
        {
            [RequiresUnreferencedCode(nameof(Method))]
            void Method([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type);
        }

        class TypeOnlyImplementation : ITypeOnly
        {
            public void Method(Type type) { }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        interface IUsed
        {
            [RequiresUnreferencedCode(nameof(Method))]
            void Method([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type);
        }

        class UsedImplementation : IUsed
        {
            [ExpectedWarning("IL2046")]
            [ExpectedWarning("IL2092")]
            public void Method(Type type) { }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        interface IPartiallyUsed
        {
            [RequiresUnreferencedCode(nameof(Method))]
            void Method([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type);
        }

        class PartiallyUsedImplementation : IPartiallyUsed
        {
            [ExpectedWarning("IL2046")]
            [ExpectedWarning("IL2092")]
            public void Method(Type type) { }
        }

        class UnusedImplementationOfPartiallyUsedInterface : IPartiallyUsed
        {
            public void Method(Type type) { }
        }
    }
}
