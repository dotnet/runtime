// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private static int VectorIntEquals()
    {
        const int Pass = 100;
        const int Fail = -1;

        Vector<int> A = new Vector<int>(3);
        Vector<int> B = new Vector<int>(3);
        Vector<int> C = new Vector<int>(5);


        bool result = A.Equals(B);
        if (!result) return Fail;

        result = A.Equals(C);
        if (result) return Fail;

        return Pass;
    }

    private static int Main()
    {
        return VectorIntEquals();
    }
}
