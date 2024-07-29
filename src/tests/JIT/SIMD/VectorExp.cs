// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using Xunit;

public partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorExpTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorExp(Vector<T> x, T checkValue, T epsilon, T allowableError)
        {
            Vector<T> sum = Vector<T>.One;
            Vector<T> count = Vector<T>.One;
            Vector<T> term = x;
            Vector<T> epsilonVec = new Vector<T>(epsilon);

            do
            {
                if (Vector.LessThanOrEqualAll<T>(Vector.Abs(term), epsilonVec)) break;

                sum = sum + term;
                count = count + Vector<T>.One;
                term = term * (x / count);
            }
            while (true);

            if (Vector.LessThanOrEqualAll<T>((Vector.Abs(sum) - new Vector<T>(checkValue)), new Vector<T>(allowableError)))
            {
                return Pass;
            }
            else
            {
                Console.WriteLine("Failed " + typeof(T).Name);
                VectorPrint("  sum: ", sum);
                return Fail;
            }
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = Pass;

        if (VectorExpTest<float>.VectorExp(Vector<float>.One, (float)Math.Exp(1d), Single.Epsilon, 1E-06f) != Pass)
        {
            returnVal = Fail;
        }

        if (VectorExpTest<double>.VectorExp(Vector<double>.One, Math.Exp(1d), Double.Epsilon, 1E-14) != Pass)
        {
            returnVal = Fail;
        }

        if (VectorExpTest<int>.VectorExp(Vector<int>.One, (int)Math.Exp(1), 0, 0) != Pass)
        {
            returnVal = Fail;
        }

        if (VectorExpTest<long>.VectorExp(Vector<long>.One, (long)Math.Exp(1), 0, 0) != Pass)
        {
            returnVal = Fail;
        }

        return returnVal;
    }
}
