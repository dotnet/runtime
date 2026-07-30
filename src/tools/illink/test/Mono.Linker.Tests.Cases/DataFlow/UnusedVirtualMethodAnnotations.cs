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

        interface IUnused
        {
            [RequiresUnreferencedCode(nameof(Method))]
            void Method([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type);
        }

        class UnusedImplementation : IUnused
        {
            [ExpectedWarning("IL2046", Tool.Analyzer, "Analyzer does not track reachability")]
            [ExpectedWarning("IL2092", Tool.Analyzer, "Analyzer does not track reachability")]
            public void Method(Type type) { }
        }

        interface ITypeOnly
        {
            [RequiresUnreferencedCode(nameof(Method))]
            void Method([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type);
        }

        class TypeOnlyImplementation : ITypeOnly
        {
            [ExpectedWarning("IL2046", Tool.Analyzer, "Analyzer does not track reachability")]
            [ExpectedWarning("IL2092", Tool.Analyzer, "Analyzer does not track reachability")]
            public void Method(Type type) { }
        }

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
            [ExpectedWarning("IL2046", Tool.Analyzer, "Analyzer does not track reachability")]
            [ExpectedWarning("IL2092", Tool.Analyzer, "Analyzer does not track reachability")]
            public void Method(Type type) { }
        }
    }
}
