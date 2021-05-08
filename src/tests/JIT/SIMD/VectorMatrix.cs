// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private const int DefaultSeed = 20010415;
    private static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
    {
        string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
        string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
        _ => DefaultSeed
    };

    // Matrix for test purposes only - no per-dim bounds checking, etc.
    public struct Matrix<T>
        where T : struct, IComparable<T>, IEquatable<T>
    {
        // data is a flattened matrix.
        private T[] _data;
        public int xCount, yCount;
        private int _xTileCount;
        private int _yTileCount;
        private int _flattenedCount;
        private static readonly int s_tileSize = Vector<T>.Count;

        public
        Matrix(int theXCount, int theYCount)
        {
            // Round up the dimensions so that we don't have to deal with remnants.
            int vectorCount = Vector<T>.Count;
            _xTileCount = (theXCount + vectorCount - 1) / vectorCount;
            _yTileCount = (theYCount + vectorCount - 1) / vectorCount;
            xCount = _xTileCount * vectorCount;
            yCount = _yTileCount * vectorCount;
            _flattenedCount = xCount * yCount;
            _data = new T[_flattenedCount];
        }

        public T this[int indexX, int indexY]
        {
            get
            {
                return _data[(indexX * yCount) + indexY];
            }
            set
            {
                _data[(indexX * yCount) + indexY] = value;
            }
        }

        public static void Transpose(Matrix<T> m, int xStart, int yStart, Vector<T>[] result)
        {
            int Count = result.Length;
            T[][] tempResult = new T[Count][];
            if (Count != Vector<T>.Count)
            {
                throw new ArgumentException();
            }
            for (int i = 0; i < Count; i++)
            {
                tempResult[i] = new T[Count];
            }
            for (int i = 0; i < Count; i++)
            {
                for (int j = 0; j < Count; j++)
                {
                    tempResult[j][i] = m[xStart + i, yStart + j];
                }
            }
            for (int i = 0; i < Count; i++)
            {
                result[i] = new Vector<T>(tempResult[i]);
            }
        }

        public static Matrix<T> operator *(Matrix<T> left, Matrix<T> right)
        {
            int newXCount = left.xCount;
            int newYCount = right.yCount;
            int innerCount = left.yCount;
            Matrix<T> result = new Matrix<T>(newXCount, newYCount);
            Vector<T>[] temp = new Vector<T>[s_tileSize];
            T[] temp2 = new T[s_tileSize];
            T[] temp3 = new T[s_tileSize];
            if (left.yCount != right.xCount)
            {
                throw new ArgumentException();
            }
            for (int i = 0; i < result.xCount; i += s_tileSize)
            {
                for (int j = 0; j < result.yCount; j += s_tileSize)
                {
                    for (int k = 0; k < right.xCount; k += s_tileSize)
                    {
                        // Compute the result for the tile:
                        // Result[i,j] = Left[i,k] * Right[k,j]
                        // Would REALLY like to have a Transpose intrinsic
                        // that could use shuffles.
                        Transpose(right, k, j, temp);
                        Vector<T> dot = Vector<T>.Zero;
                        for (int m = 0; m < s_tileSize; m++)
                        {
                            Vector<T> leftTileRow = new Vector<T>(left._data, (i + m) * left.yCount + k);

                            for (int n = 0; n < s_tileSize; n++)
                            {
                                temp2[n] = Vector.Dot<T>(leftTileRow, temp[n]);
                            }
                            Vector<T> resultVector = new Vector<T>(result._data, (i + m) * result.yCount + j);

                            resultVector += new Vector<T>(temp2);
                            // Store the resultTile
                            resultVector.CopyTo(result._data, ((i + m) * result.yCount + j));
                        }
                    }
                }
            }
            return result;
        }
        public void Print()
        {
            Console.WriteLine("[");
            for (int i = 0; i < xCount; i++)
            {
                Console.Write("  [");
                for (int j = 0; j < yCount; j++)
                {
                    Console.Write(this[i, j]);
                    if (j < (yCount - 1)) Console.Write(",");
                }
                Console.WriteLine("]");
            }
            Console.WriteLine("]");
        }
    }

    public static Matrix<T> GetRandomMatrix<T>(int xDim, int yDim, Random random)
        where T : struct, IComparable<T>, IEquatable<T>
    {
        Matrix<T> result = new Matrix<T>(xDim, yDim);
        for (int i = 0; i < xDim; i++)
        {
            for (int j = 0; j < yDim; j++)
            {
                int data = random.Next(100);
                result[i, j] = GetValueFromInt<T>(data);
            }
        }
        return result;
    }

    private static T compareMMPoint<T>(Matrix<T> left, Matrix<T> right, int i, int j)
        where T : struct, IComparable<T>, IEquatable<T>
    {
        T sum = Vector<T>.Zero[0];
        for (int k = 0; k < right.xCount; k++)
        {
            T l = left[i, k];
            T r = right[k, j];
            sum = Add<T>(sum, Multiply<T>(l, r));
        }
        return sum;
    }

    public static int VectorMatrix<T>(Matrix<T> left, Matrix<T> right)
        where T : struct, IComparable<T>, IEquatable<T>
    {
        int returnVal = Pass;
        Matrix<T> Result = left * right;
        for (int i = 0; i < left.xCount; i++)
        {
            for (int j = 0; j < right.yCount; j++)
            {
                T compareResult = compareMMPoint<T>(left, right, i, j);
                T testResult = Result[i, j];
                if (!(CheckValue<T>(testResult, compareResult)))
                {
                    Console.WriteLine("  Mismatch at [" + i + "," + j + "]: expected " + compareResult + " got " + testResult);
                    returnVal = Fail;
                }
            }
        }
        if (returnVal == Fail)
        {
            Console.WriteLine("FAILED COMPARE");
            Console.WriteLine("Left:");
            left.Print();
            Console.WriteLine("Right:");
            right.Print();
            Console.WriteLine("Result:");
            Result.Print();
        }
        return returnVal;
    }

    public static int Main()
    {
        int returnVal = Pass;

        Random random = new Random(Seed);

        // Float
        Matrix<float> AFloat = GetRandomMatrix<float>(3, 4, random);
        Matrix<float> BFloat = GetRandomMatrix<float>(4, 2, random);
        if (VectorMatrix<float>(AFloat, BFloat) != Pass) returnVal = Fail;

        AFloat = GetRandomMatrix<float>(33, 20, random);
        BFloat = GetRandomMatrix<float>(20, 17, random);
        if (VectorMatrix<float>(AFloat, BFloat) != Pass) returnVal = Fail;

        // Double
        Matrix<double> ADouble = GetRandomMatrix<double>(3, 4, random);
        Matrix<double> BDouble = GetRandomMatrix<double>(4, 2, random);
        if (VectorMatrix<double>(ADouble, BDouble) != Pass) returnVal = Fail;

        ADouble = GetRandomMatrix<double>(33, 20, random);
        BDouble = GetRandomMatrix<double>(20, 17, random);
        if (VectorMatrix<double>(ADouble, BDouble) != Pass) returnVal = Fail;

        return returnVal;
    }
}

