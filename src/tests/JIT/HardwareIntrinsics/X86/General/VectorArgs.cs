// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This test case is ported from S.N.Vector counterpart 
// https://github.com/dotnet/coreclr/blob/master/tests/src/JIT/SIMD/VectorArgs.cs

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

internal partial class IntelHardwareIntrinsicTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    internal class VectorArg128
    {
        private Vector128<float> _rgb;

        public VectorArg128(float r, float g, float b)
        {
            float[] temp = new float[4];
            temp[0] = r; temp[1] = g; temp[2] = b;
            unsafe
            {
                fixed (float* ptr = temp)
                {
                    _rgb = Sse.LoadVector128(ptr);
                }
            }
        }

        public VectorArg128(Vector128<float> _rgb)
        { this._rgb = _rgb; }

        public VectorArg128 Change(float f)
        {
            Vector128<float> t = Vector128.Create(f);
            return new VectorArg128(Sse.Add(t, _rgb));
        }

        public Vector128<float> RGB { get { return _rgb; } }
    }

    internal class VectorArg256
    {
        private Vector256<float> _rgb;

        public VectorArg256(float r, float g, float b)
        {
            float[] temp = new float[8];
            temp[0] = r; temp[1] = g; temp[2] = b;
            unsafe
            {
                fixed (float* ptr = temp)
                {
                    _rgb = Avx.LoadVector256(ptr);
                }
            }
        }

        public VectorArg256(Vector256<float> _rgb)
        { this._rgb = _rgb; }

        public VectorArg256 Change(float f)
        {
            Vector256<float> t = Vector256.Create(f);
            return new VectorArg256(Avx.Add(t, _rgb));
        }

        public Vector256<float> RGB { get { return _rgb; } }
    }

    unsafe static int Main()
    {
        int returnVal = Pass;

        if (Sse41.IsSupported)
        {
            Vector128<float> rgb = Vector128.Create(3f, 2f, 1f, 0f);
            float x = 2f;
            VectorArg128 c1 = new VectorArg128(rgb);
            VectorArg128 c2 = c1.Change(x);
            for (int i = 0; i < 4; i++)
            {
                if (((int)Sse41.Extract(c2.RGB, (byte)i)) != (3 - i) + x)
                {
                    returnVal = Fail;
                }
            }
        }

        if (Avx.IsSupported)
        {
            Vector256<float> rgb = Vector256.Create(7f, 6f, 5f, 4f, 3f, 2f, 1f, 0f);
            float x = 2f;
            VectorArg256 c1 = new VectorArg256(rgb);
            VectorArg256 c2 = c1.Change(x);
            float* buffer = stackalloc float[8];
            Avx.Store(buffer, c2.RGB);
            for (int i = 0; i < 8; i++)
            {
                if (((int)buffer[i]) != (7 - i) + x)
                {
                    returnVal = Fail;
                }
            }
        }

        return returnVal;
    }
}
