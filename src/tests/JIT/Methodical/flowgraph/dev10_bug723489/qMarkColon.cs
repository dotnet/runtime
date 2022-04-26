// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*
 * Basically when we have two Qmark-Colon trees used as the register arguments to a call we don’t take into account that the first one to be evaluated should add a register interference with ECX/EDX so that the next tree will not try to use that register when deciding what register it can use for enregistration of locals.
 * An OKMask Assert was being hit in this case.
 */

using System;
using Xunit;
namespace Test_qMarkColon_cs
{
public class Repro
{
    public static bool MyEquals(object obj1, object obj2)
    {
        return ((obj1 as Version) == (obj2 as Version));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Version ver0 = null;
        Version ver1 = null;

        bool result = MyEquals(ver0, ver1);
        if (result)
            return 100;
        else
            return 101;
    }
}
}
