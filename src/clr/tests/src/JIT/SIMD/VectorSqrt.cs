// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorSqrtTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorSqrt(T square, T root, T allowableError)
        {
            Vector<T> A = new Vector<T>(square);
            Vector<T> B = Vector.SquareRoot(A);

            if (Vector.LessThanOrEqualAll<T>((Vector.Abs(B) - new Vector<T>(root)), new Vector<T>(allowableError)))
            {
                return Pass;
            }
            else
            {
                Console.WriteLine("Failed " + typeof(T).Name);
                VectorPrint("  input:  ", A);
                VectorPrint("  result: ", B);
                return Fail;
            }
        }
    }


    private static int Main()
    {
        int returnVal = Pass;
        if (VectorSqrtTest<float>.VectorSqrt(25f, 5f, 1E-06f) != Pass)
        {
            returnVal = Fail;
        }
        if (VectorSqrtTest<double>.VectorSqrt(25f, 5f, 1E-14) != Pass)
        {
            returnVal = Fail;
        }
        if (VectorSqrtTest<float>.VectorSqrt(25f, 5f, 0) != Pass)
        {
            returnVal = Fail;
        }
        return returnVal;
    }
}
