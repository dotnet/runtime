// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorSetTest
    {
        public static int VectorSet(float value)
        {
            int returnVal = Pass;

            Vector2 A = new Vector2(0.0f, 0.0f);

            A.X = value;
            if (!CheckValue(A.X, value)) returnVal = Fail;
            if (!CheckValue(A.Y, 0.0f)) returnVal = Fail;

            A.Y = value;
            if (!CheckValue(A.X, value)) returnVal = Fail;
            if (!CheckValue(A.Y, value)) returnVal = Fail;

            Vector3 B = new Vector3(0.0f, 0.0f, 0.0f);
            B.X = value;
            if (!CheckValue(B.X, value)) returnVal = Fail;
            if (!CheckValue(B.Y, 0.0f)) returnVal = Fail;
            if (!CheckValue(B.Z, 0.0f)) returnVal = Fail;

            B.Y = value;
            if (!CheckValue(B.X, value)) returnVal = Fail;
            if (!CheckValue(B.Y, value)) returnVal = Fail;
            if (!CheckValue(B.Z, 0.0f)) returnVal = Fail;

            B.Z = value;
            if (!CheckValue(B.X, value)) returnVal = Fail;
            if (!CheckValue(B.Y, value)) returnVal = Fail;
            if (!CheckValue(B.Z, value)) returnVal = Fail;

            Vector4 C = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            C.X = value;
            if (!CheckValue(C.X, value)) returnVal = Fail;
            if (!CheckValue(C.Y, 0.0f)) returnVal = Fail;
            if (!CheckValue(C.Z, 0.0f)) returnVal = Fail;
            if (!CheckValue(C.W, 0.0f)) returnVal = Fail;

            C.Y = value;
            if (!CheckValue(C.X, value)) returnVal = Fail;
            if (!CheckValue(C.Y, value)) returnVal = Fail;
            if (!CheckValue(C.Z, 0.0f)) returnVal = Fail;
            if (!CheckValue(C.W, 0.0f)) returnVal = Fail;

            C.Z = value;
            if (!CheckValue(C.X, value)) returnVal = Fail;
            if (!CheckValue(C.Y, value)) returnVal = Fail;
            if (!CheckValue(C.Z, value)) returnVal = Fail;
            if (!CheckValue(C.W, 0.0f)) returnVal = Fail;

            C.W = value;
            if (!CheckValue(C.X, value)) returnVal = Fail;
            if (!CheckValue(C.Y, value)) returnVal = Fail;
            if (!CheckValue(C.Z, value)) returnVal = Fail;
            if (!CheckValue(C.W, value)) returnVal = Fail;

            return returnVal;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;
        if (VectorSetTest.VectorSet(3.14f) == Fail) returnVal = Fail;
        return returnVal;
    }
}
