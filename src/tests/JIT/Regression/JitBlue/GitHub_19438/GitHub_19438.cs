// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

// This test executes the same computation on a wrapped Vector4 ('float4') and a
// (not wrapped) Vector4. The code should be similar.
// This was a perf regression issue, so this test is mostly useful for running
// asm diffs.

namespace GitHub_19438
{
    public class Program
    {
        struct float4
        {
            public Vector4 v;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float4(float _x, float _y, float _z, float _w)
            {
                v = new Vector4(_x, _y, _z, _w);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float4(Vector4 _v)
            {
                v = _v;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float4 operator +(float4 a, float4 b)
            {
                return new float4(a.v + b.v);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float4 operator -(float4 a, float4 b)
            {
                return new float4(a.v - b.v);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float4 operator *(float4 a, float4 b)
            {
                return new float4(a.v * b.v);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float4 operator /(float4 a, float4 b)
            {
                return new float4(a.v / b.v);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override string ToString()
            {
                return v.ToString();
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            const int iterationCount = 10;
            const int itemCount = 1000000;

            long totalTaskTime = 0;

            // First, use a wrapped Vector4.
            List<float4> items = new List<float4>(itemCount);
            for (int iteration = 0; iteration < iterationCount; ++iteration)
            {
                var taskTimer = Stopwatch.StartNew();

                for (int item = 0; item < itemCount; ++item)
                {
                    float4 v0 = new float4(1.0f, 2.0f, 3.0f, 4.0f);
                    float4 v1 = new float4(5.0f, 6.0f, 7.0f, 8.0f);
                    float4 v2 = (v0 * v1) - (v1 / v0 + v1);
                    float4 v3 = (v2 * v0) - (v2 / v0 + v1);

                    items.Add(v2 * v3);
                }

                taskTimer.Stop();
                totalTaskTime += taskTimer.ElapsedMilliseconds;

                items.Clear();
                GC.Collect();
            }
            Console.WriteLine("Wrapped Average Time: " + totalTaskTime / iterationCount + "ms");

            // Now, a plain Vector4
            totalTaskTime = 0;
            List<Vector4> items2 = new List<Vector4>(itemCount);
            for (int iteration = 0; iteration < iterationCount; ++iteration)
            {
                var taskTimer = Stopwatch.StartNew();

                for (int item = 0; item < itemCount; ++item)
                {
                    Vector4 v0 = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
                    Vector4 v1 = new Vector4(5.0f, 6.0f, 7.0f, 8.0f);
                    Vector4 v2 = (v0 * v1) - (v1 / v0 + v1);
                    Vector4 v3 = (v2 * v0) - (v2 / v0 + v1);

                    items2.Add(v2 * v3);
                }

                taskTimer.Stop();
                totalTaskTime += taskTimer.ElapsedMilliseconds;

                items2.Clear();
                GC.Collect();
            }
            Console.WriteLine("Vector4 Average Time: " + totalTaskTime / iterationCount + "ms");

            return 100;
        }
    }
}
