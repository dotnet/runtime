// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sample
{
    class StringTask : BenchTask
    {
        public override string Name => "String";
        Measurement[] measurements;

        public StringTask()
        {
            measurements = new Measurement[] {
                new NormalizeMeasurement(),
                new NormalizeMeasurementASCII(),
            };
        }

        public override Measurement[] Measurements
        {
            get
            {
                return measurements;
            }
        }

        public abstract class StringMeasurement : BenchTask.Measurement
        {
            public override int InitialSamples => 30;
            protected Random random;
        }

        public class NormalizeMeasurement : StringMeasurement
        {
            protected char[] data;
            protected int len = 64 * 1024;
            protected string str;

            public override Task BeforeBatch()
            {
                data = new char[len];
                random = new(123456);
                for (int i = 0; i < len; i++)
                {
                    data[i] = (char)random.Next(0xd800);
                }

                str = new string(data);

                return Task.CompletedTask;
            }

            public override Task AfterBatch()
            {
                data = null;

                return Task.CompletedTask;
            }

            public override string Name => "Normalize";

            public override void RunStep()
            {
                str.Normalize();
            }
        }

        public class NormalizeMeasurementASCII : NormalizeMeasurement
        {
            public override Task BeforeBatch()
            {
                data = new char[len];
                random = new(123456);
                for (int i = 0; i < len; i++)
                    data[i] = (char)random.Next(0x80);

                str = new string(data);

                return Task.CompletedTask;
            }

            public override string Name => "Normalize ASCII";
        }
    }
}
