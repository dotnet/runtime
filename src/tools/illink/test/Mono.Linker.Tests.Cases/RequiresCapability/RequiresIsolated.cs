// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    class RequiresIsolated
    {
        public static void Main()
        {
            MembersOnClassWithRequires<int>.Test();
        }

        class MembersOnClassWithRequires<T>
        {
            public class RequiresAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] U>
            {
            }

            [RequiresDynamicCode("--ClassWithRequires--", ExcludeStatics = true)]
            public class ClassWithRequires
            {
                [UnexpectedWarning("IL2091", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/113249")]
                public static RequiresAll<T> field;
            }

            [UnexpectedWarning("IL2091", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/113249")]
            public static void Test(ClassWithRequires inst = null)
            {
                var f = ClassWithRequires.field;
            }

        }
    }
}
