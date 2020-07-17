// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace GitHub_16065b
{
    struct Array2D
    {
        public int Offset;
        public int LeadingDimension;

        public Array2D(int offset, int leadingDimension)
        {
            Offset = offset;
            LeadingDimension = leadingDimension;
        }

        public int GetIndex(int row, int column)
        { 
            return this.Offset + row + this.LeadingDimension * column;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public ArraySlice Diagonal(int index)
        {
            int offset = (index > 0) ? GetIndex(0, index) : GetIndex(-index, 0);
            // The problem is with this line:
            int stride = this.LeadingDimension + 1;
            return new ArraySlice(offset, stride);
        }
        public ArraySlice GetStride(int index)
        {
            int offset = (index > 0) ? GetIndex(0, index) : GetIndex(-index, 0);
            // The problem is with this line, when inlined:
            int stride = this.LeadingDimension + 1;
            return new ArraySlice(offset, stride);
        }
    }
    struct ArraySlice
    {
        public int Offset;
        public int Stride;

        public ArraySlice(int offset, int stride)
        {
            Offset = offset;
            Stride = stride;
        }
    }

    class Vector
    {
        public ArraySlice Storage;
        public Vector(ArraySlice storage)
        {
            Storage = storage;
        }
    }
    class Matrix
    {
        public Array2D Storage;

        public Matrix(Array2D storage)
        {
            Storage = storage;
        }
        public Vector GetDiagonal(int index)
        {
            ArraySlice storage = this.Storage.Diagonal(index);
            return new Vector(storage);
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            int result = 0;
            var A = new Matrix(new Array2D(0, 4));
            var d = A.GetDiagonal(0);
            Console.WriteLine("Expected: 0:5");
            Console.WriteLine("Actual: {0}:{1}", d.Storage.Offset, d.Storage.Stride);
            if ((d.Storage.Offset == 0) && (d.Storage.Stride == 5))
            {
                Console.WriteLine("PASSED");
                result = 100;
            }
            else
            {
                Console.WriteLine("FAILED");
                result = 101;
            }

            return result;
        }
    }
}
