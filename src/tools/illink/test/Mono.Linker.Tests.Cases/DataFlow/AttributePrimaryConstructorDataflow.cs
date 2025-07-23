// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.DataFlow;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [Kept]
    [ExpectedNoWarnings]
    class AttributePrimaryConstructorDataflow
    {
        public static void Main()
        {
            new PrimaryConstructor(typeof(int)).UseField();
        }

        [Kept]
        [KeptMember(".ctor(System.Type)")]
        class PrimaryConstructor(
            [KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t
        )
        {
            [Kept]
            public void UseField() => t.RequiresPublicMethods();
        }
    }
}
