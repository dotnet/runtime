// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.DataFlow.Dependencies
{
    public interface IUnusedInPreservedAssembly
    {
        [RequiresUnreferencedCode(nameof(Method))]
        void Method([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type);
    }

    public interface ITypeOnlyInPreservedAssembly
    {
        [RequiresUnreferencedCode(nameof(Method))]
        void Method([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type);
    }

    public interface IPartiallyUsedInPreservedAssembly
    {
        [RequiresUnreferencedCode(nameof(Method))]
        void Method([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type);
    }

    public abstract class BaseInPreservedAssembly
    {
        [RequiresUnreferencedCode(nameof(Method))]
        public abstract void Method([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type);
    }

    // Annotations on methods of a System.Type-derived type apply to the implicit 'this' parameter,
    // so a mismatch on an override is reported as IL2094 rather than IL2092. The issue used
    // System.Reflection.IReflect, which the trimmer treats the same way.
    public abstract class TypeWithAnnotatedThisInPreservedAssembly : Type
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        public virtual void Method() { }
    }
}
