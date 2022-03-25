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
    }
}
