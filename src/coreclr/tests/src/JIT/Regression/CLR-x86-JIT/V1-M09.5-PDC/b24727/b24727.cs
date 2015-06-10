// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Struct_013.sc
// <StdHeader>
// Verify struct can implement multiple interfaces that contain methods with identical signatures.
// </StdHeader>

//<Expects Status=success> </Expects>

using System;

interface Inter1
{
    int Return42();
}

interface Inter2
{
    int Return42();
}

interface Inter3
{
    int Return0();
}

struct Struct1 : Inter1, Inter2, Inter3
{
    int Inter1.Return42() { return (42); }
    int Inter2.Return42() { return (42); }
    int Inter3.Return0() { return (0); }
}

public class Test
{
    public static int Main(string[] args)
    {
        Inter1 i1 = new Struct1();

        return (i1.Return42() - ((Inter2)i1).Return42() - ((Inter3)i1).Return0()) + 100;
    }

}

