// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace GitHub_16065a
{
    struct Array2D
    {
        public int Offset { get; }
        public int LeadingDimension { get; }

        public Array2D(int offset, int leadingDimension)
        {
            this.Offset = offset;
            this.LeadingDimension = leadingDimension;
        }

        public int GetIndex(int row, int column) 
            => this.Offset + row + this.LeadingDimension * column;

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
        public int Offset { get; }
        public int Stride { get; }

        public ArraySlice(int offset, int stride)
        {
            this.Offset = offset;
            this.Stride = stride;
        }
    }

    class Vector
    {
        public ArraySlice Storage { get; }
        public Vector(ArraySlice storage)
        {
            this.Storage = storage;
        }
    }
    class Matrix
    {
        public Array2D Storage { get; }

        public Matrix(Array2D storage)
        {
            this.Storage = storage;
        }
        public Vector GetDiagonal(int index)
        {
            ArraySlice storage = this.Storage.Diagonal(index);
            return new Vector(storage);
        }
    }

    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
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

