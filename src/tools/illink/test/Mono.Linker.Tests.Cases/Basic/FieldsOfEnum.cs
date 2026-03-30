// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    class FieldsOfEnum
    {
        [Kept]
        static void Main()
        {
            Console.WriteLine($"{MyEnum.A}");
        }
    }

    [Kept]
    [KeptMember("value__")]
    [KeptBaseType(typeof(Enum))]
    public enum MyEnum
    {
        [Kept]
        A = 0,

        [Kept]
        B = 1,

        [Kept]
        C = 2,
    };
}
