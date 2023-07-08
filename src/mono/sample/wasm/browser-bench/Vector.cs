// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;

namespace Sample
{
    class VectorTask : BenchTask
    {
        public override string Name => "Vector";
        Measurement[] measurements;

        public VectorTask()
        {
            measurements = new Measurement[] {
                new Create(),
                new PackConstant(),
                new Pack(),
                new Add(),
                new Multiply(),
                new DotInt(),
                new DotULong(),
                new DotFloat(),
                new DotDouble(),
                new SumSByte(),
                new SumShort(),
                new SumUInt(),
                new SumDouble(),
                new MinFloat(),
                new MaxFloat(),
                new MinDouble(),
                new MaxDouble(),
                new Normalize(),
            };
        }

        public override Measurement[] Measurements
        {
            get
            {
                return measurements;
            }
        }

        public abstract class VectorMeasurement : BenchTask.Measurement
        {
            public override int InitialSamples => 100000;
            public override int RunLength => 500;
        }

        class Create : VectorMeasurement
        {
            Vector128<int> vector;

            public override string Name => "Create Vector128";

            public override void RunStep() => vector = Vector128.Create(0x123456);
        }

        class PackConstant : VectorMeasurement
        {
            Vector128<int> vector;

            public override string Name => "Pack Vector128 (Constant)";

            public override void RunStep() => vector = Vector128.Create(1, 2, 3, 4);
        }

        class Pack : VectorMeasurement
        {
            Vector128<int> vector;
            int a = 1, b = 2, c = 3, d = 4;

            public override string Name => "Pack Vector128";

            public override void RunStep() => vector = Vector128.Create(a, b, c, d);
        }

        class Add : VectorMeasurement
        {
            Vector128<int> vector1, vector2, vector3;

            public override string Name => "Add 2 Vector128's";

            public Add()
            {
                vector1 = Vector128.Create(0x12345678);
                vector2 = Vector128.Create(0x23456789);
            }

            public override void RunStep() => vector3 = vector1 + vector2;
        }

        class Multiply : VectorMeasurement
        {
            Vector128<int> vector1, vector2, vector3;

            public override string Name => "Multiply 2 Vector128's";

            public Multiply()
            {
                vector1 = Vector128.Create(0x12345678);
                vector2 = Vector128.Create(0x23456789);
            }

            public override void RunStep() => vector3 = vector1 * vector2;
        }

        class DotInt : VectorMeasurement
        {
            Vector128<int> vector1, vector2;
            int result;

            public override string Name => "Dot product int";

            public DotInt()
            {
                vector1 = Vector128.Create(12, 34, 56, 78);
                vector2 = Vector128.Create(23, 45, 67, 89);
            }

            public override void RunStep()
            {
                result = Vector128.Dot(vector1, vector2);
            }
        }

        class DotULong : VectorMeasurement
        {
            Vector128<ulong> vector1, vector2;
            ulong result;

            public override string Name => "Dot product ulong";

            public DotULong()
            {
                vector1 = Vector128.Create(12ul, 34);
                vector2 = Vector128.Create(23ul, 45);
            }

            public override void RunStep()
            {
                result = Vector128.Dot(vector1, vector2);
            }
        }

        class DotFloat : VectorMeasurement
        {
            Vector128<float> vector1, vector2;
            float result;

            public override string Name => "Dot product float";

            public DotFloat()
            {
                vector1 = Vector128.Create(12f, 34, 56, 78);
                vector2 = Vector128.Create(23f, 45, 67, 89);
            }

            public override void RunStep() {
                result = Vector128.Dot(vector1, vector2);
            }
        }

        class DotDouble : VectorMeasurement
        {
            Vector128<double> vector1, vector2;
            double result;

            public override string Name => "Dot product double";

            public DotDouble()
            {
                vector1 = Vector128.Create(12d, 34);
                vector2 = Vector128.Create(23d, 45);
            }

            public override void RunStep() {
                result = Vector128.Dot(vector1, vector2);
            }
        }

        class SumUInt : VectorMeasurement
        {
            Vector128<uint> vector1;
            uint result;

            public override string Name => "Sum uint";

            public SumUInt()
            {
                vector1 = Vector128.Create(12u, 34, 56, 78);
            }

            public override void RunStep() {
                result = Vector128.Sum(vector1);
            }
        }

        class SumDouble : VectorMeasurement
        {
            Vector128<double> vector1;
            double result;

            public override string Name => "Sum double";

            public SumDouble()
            {
                vector1 = Vector128.Create(12d, 34);
            }

            public override void RunStep() {
                result = Vector128.Sum(vector1);
            }
        }

        class SumShort : VectorMeasurement
        {
            Vector128<short> vector1;
            short result;

            public override string Name => "Sum short";

            public SumShort()
            {
                vector1 = Vector128.Create(12, 34, 56, 78, 23, 45, 67, 89);
            }

            public override void RunStep() {
                result = Vector128.Sum(vector1);
            }
        }

        class SumSByte : VectorMeasurement
        {
            Vector128<sbyte> vector1;
            sbyte result;

            public override string Name => "Sum sbyte";

            public SumSByte()
            {
                vector1 = Vector128.Create(1, -3, 2, -5, 4, -6, 8, -7, 10, -9, 12, -11, 14, -13, 16, -15);
            }

            public override void RunStep() {
                result = Vector128.Sum(vector1);
            }
        }

        class MinFloat : VectorMeasurement
        {
            Vector128<float> vector1;
            Vector128<float> vector2;
            Vector128<float> result;

            public override string Name => "Min float";

            public MinFloat()
            {
                vector1 = Vector128.Create(12f, 34, 56, 78);
                vector2 = Vector128.Create(13f, 32, 57, 77);
            }

            public override void RunStep() {
                result = Vector128.Min(vector1, vector2);
            }
        }

        class MaxFloat : VectorMeasurement
        {
            Vector128<float> vector1;
            Vector128<float> vector2;
            Vector128<float> result;

            public override string Name => "Max float";

            public MaxFloat()
            {
                vector1 = Vector128.Create(12f, 34, 56, 78);
                vector2 = Vector128.Create(13f, 32, 57, 77);
            }

            public override void RunStep() {
                result = Vector128.Max(vector1, vector2);
            }
        }

        class MinDouble : VectorMeasurement
        {
            Vector128<double> vector1;
            Vector128<double> vector2;
            Vector128<double> result;

            public override string Name => "Min double";

            public MinDouble()
            {
                vector1 = Vector128.Create(12d, 34);
                vector2 = Vector128.Create(13d, 32);
            }

            public override void RunStep() {
                result = Vector128.Min(vector1, vector2);
            }
        }

        class MaxDouble : VectorMeasurement
        {
            Vector128<double> vector1;
            Vector128<double> vector2;
            Vector128<double> result;

            public override string Name => "Max double";

            public MaxDouble()
            {
                vector1 = Vector128.Create(12d, 34);
                vector2 = Vector128.Create(13d, 32);
            }

            public override void RunStep() {
                result = Vector128.Max(vector1, vector2);
            }
        }

        class Normalize : VectorMeasurement
        {
            Vector128<float> result;
            float x, y, z, w;
            public override string Name => "Normalize float";

            public Normalize()
            {
                x = Random.Shared.NextSingle();
                y = Random.Shared.NextSingle();
                z = Random.Shared.NextSingle();
                w = Random.Shared.NextSingle();
            }

            public override void RunStep() {
                Vector128<float> vector = Vector128.Create(x, y, z, w);
                result = vector / (float)Math.Sqrt(Vector128.Dot(vector, vector));
            }
        }
    }
}
