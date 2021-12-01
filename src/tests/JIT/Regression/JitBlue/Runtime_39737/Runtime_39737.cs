// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test was deleting the hardware intrinsic leaving unconsumed GT_OBJ on top of the stack
// that was leading to an assert failure.

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System;

class Runtime_39403
{ 
    public static int Main()
    {
        if (Sse41.IsSupported)
        {
            Vector128<int> left = Vector128.Create(1);
            Vector128<int> right = Vector128.Create(2);
            ref var rightRef = ref right;
            Vector128<int> mask = Vector128.Create(3);
            Sse41.BlendVariable(left, rightRef, mask);
        }
        return 100;
    }
}

