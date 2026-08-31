// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace System.Diagnostics.CodeAnalysis
{
    using System.Runtime.CompilerServices;

    [SkipKeptItemsValidation]
    [CompilerLoweringPreserve]
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
        AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method |
        AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
        Inherited = false)]
    public sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
        {
            MemberTypes = memberTypes;
        }

        public DynamicallyAccessedMemberTypes MemberTypes { get; }
    }
}

namespace System.Runtime.CompilerServices
{
    [SkipKeptItemsValidation]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CompilerLoweringPreserveAttribute : Attribute

    {
        public CompilerLoweringPreserveAttribute() { }
    }
}
