// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Generics
{
    [ExpectedNoWarnings]
    class InstantiatedGenericEquality
    {
        public static void Main()
        {
            GenericReturnType.Test();
        }

        class GenericReturnType
        {
            [KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute), By = Tool.Trimmer /* Type is needed due to IL metadata only */)]
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            class ReturnType<T>
            {
                [Kept(By = Tool.Trimmer /* https://github.com/dotnet/runtime/issues/110563 */)]
                public void Method() { }
            }

            [Kept]
            static ReturnType<T> GetGenericReturnType<T>() => default;

            // Regression test for an issue where ILLink's representation of a generic instantiated type
            // was using reference equality. The test uses a lambda to ensure that it goes through the
            // interprocedural analysis code path that merges patterns and relies on a correct implementation
            // of equality.
            [Kept]
            public static void Test()
            {
                var instance = GetGenericReturnType<int>();

                var lambda =
                () =>
                {
                    var type = instance.GetType();
                    type.GetMethod("Method");
                };

                lambda();
            }
        }
    }
}
