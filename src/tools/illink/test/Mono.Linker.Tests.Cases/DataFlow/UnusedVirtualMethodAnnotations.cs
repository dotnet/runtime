// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.DataFlow.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    [SetupCompileBefore("base.dll", new[] { "Dependencies/UnusedVirtualMethodAnnotationsBase.cs" })]
    [SetupLinkerAction("copy", "base")]
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

            _ = typeof(TypeOnlyImplementationOfPreservedInterface);

            IPartiallyUsedInPreservedAssembly partiallyUsedFromPreservedAssembly = new UsedImplementationOfPreservedInterface();
            partiallyUsedFromPreservedAssembly.Method(typeof(object));
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

        // The trimmer warns about this unused implementation because the interface method was
        // marked (by PartiallyUsedImplementation) and validation walks every override of a marked
        // virtual method, whether or not the override was kept.
        class UnusedImplementationOfPartiallyUsedInterface : IPartiallyUsed
        {
            [ExpectedWarning("IL2046", Tool.Analyzer, "Analyzer does not track reachability")]
            [UnexpectedWarning("IL2046", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/98870")]
            [ExpectedWarning("IL2092", Tool.Analyzer, "Analyzer does not track reachability")]
            [UnexpectedWarning("IL2092", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/98870")]
            public void Method(Type type) { }
        }

        // The cases below declare the base member in an assembly the trimmer doesn't link.
        // Every virtual method of such an assembly is queued for annotation validation, and each
        // of its overrides is then validated, so an implementation warns even when nothing in the
        // app can reach it. This is the shape reported in the issue, where the base member is
        // System.Reflection.IReflect in the (unlinked) framework.

        class UnusedImplementationOfPreservedInterface : IUnusedInPreservedAssembly
        {
            [ExpectedWarning("IL2046", Tool.Analyzer, "Analyzer does not track reachability")]
            [UnexpectedWarning("IL2046", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/98870")]
            [ExpectedWarning("IL2092", Tool.Analyzer, "Analyzer does not track reachability")]
            [UnexpectedWarning("IL2092", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/98870")]
            public void Method(Type type) { }
        }

        class TypeOnlyImplementationOfPreservedInterface : ITypeOnlyInPreservedAssembly
        {
            [ExpectedWarning("IL2046", Tool.Analyzer, "Analyzer does not track reachability")]
            [UnexpectedWarning("IL2046", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/98870")]
            [ExpectedWarning("IL2092", Tool.Analyzer, "Analyzer does not track reachability")]
            [UnexpectedWarning("IL2092", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/98870")]
            public void Method(Type type) { }
        }

        class UsedImplementationOfPreservedInterface : IPartiallyUsedInPreservedAssembly
        {
            [ExpectedWarning("IL2046")]
            [ExpectedWarning("IL2092")]
            public void Method(Type type) { }
        }

        class UnusedImplementationOfUsedPreservedInterface : IPartiallyUsedInPreservedAssembly
        {
            [ExpectedWarning("IL2046", Tool.Analyzer, "Analyzer does not track reachability")]
            [UnexpectedWarning("IL2046", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/98870")]
            [ExpectedWarning("IL2092", Tool.Analyzer, "Analyzer does not track reachability")]
            [UnexpectedWarning("IL2092", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/98870")]
            public void Method(Type type) { }
        }

        class UnusedOverrideOfPreservedBaseClass : BaseInPreservedAssembly
        {
            [ExpectedWarning("IL2046", Tool.Analyzer, "Analyzer does not track reachability")]
            [UnexpectedWarning("IL2046", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/98870")]
            [ExpectedWarning("IL2092", Tool.Analyzer, "Analyzer does not track reachability")]
            [UnexpectedWarning("IL2092", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/98870")]
            public override void Method(Type type) { }
        }

        // The shape from the issue, where the base member's annotation applies to the implicit
        // 'this' parameter so the mismatch is reported as IL2094 rather than IL2092. Only
        // System.Type and System.Reflection.IReflect hierarchies annotate 'this'; the issue used
        // IReflect, and deriving from System.Type covers the same validation path.
        abstract class UnusedOverrideOfPreservedTypeWithAnnotatedThis : TypeWithAnnotatedThisInPreservedAssembly
        {
            [ExpectedWarning("IL2094", Tool.Analyzer, "Analyzer does not track reachability")]
            [UnexpectedWarning("IL2094", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/98870")]
            public override void Method() { }
        }
    }
}
