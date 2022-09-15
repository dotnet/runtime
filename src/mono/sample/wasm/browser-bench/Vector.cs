// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                new Add(),
                new Multiply(),
                new DotInt(),
                new DotULong(),
                new DotFloat(),
                new DotDouble(),
                new SumUInt(),
                new SumDouble(),
                new MinFloat(),
                new MaxFloat(),
                new MinDouble(),
                new MaxDouble(),
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
            float result;

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
            float result;

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
                System.Console.WriteLine($"min float: {Vector128.Min(vector1, vector2)}");
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
                System.Console.WriteLine($"min double: {Vector128.Min(vector1, vector2)}");
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
    }
}
