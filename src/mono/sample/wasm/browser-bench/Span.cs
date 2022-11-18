// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sample
{
    class SpanTask : BenchTask
    {
        public override string Name => "Span";
        Measurement[] measurements;

        public SpanTask()
        {
            measurements = new Measurement[] {
                new ReverseByte(),
                new ReverseChar(),
                new IndexOfByte(),
                new IndexOfChar(),
            };
        }

        public override Measurement[] Measurements
        {
            get
            {
                return measurements;
            }
        }

        public abstract class SpanMeasurement : BenchTask.Measurement
        {
            public override int InitialSamples => 30;
            protected Random random;
        }

        abstract class SpanByteMeasurement : SpanMeasurement
        {
            protected byte[] data;
            int len = 64 * 1024;

            public override Task BeforeBatch()
            {
                data = new byte[len];
                random = new(123456);
                random.NextBytes(data);

                return Task.CompletedTask;
            }

            public override Task AfterBatch()
            {
                data = null;

                return Task.CompletedTask;
            }
        }

        class ReverseByte : SpanByteMeasurement
        {
            public override string Name => "Reverse bytes";

            public override void RunStep()
            {
                var span = new Span<byte>(data);
                span.Reverse<byte>();
            }
        }

        class IndexOfByte : SpanByteMeasurement
        {
            public override string Name => "IndexOf bytes";
            public override int InitialSamples => 1000;

            public override void RunStep()
            {
                var span = new Span<byte>(data);
                span.IndexOf<byte> ((byte)random.Next(256));
            }
        }

        abstract class SpanCharMeasurement : SpanMeasurement
        {
            protected char[] data;
            int len = 64 * 1024;

            public override Task BeforeBatch()
            {
                data = new char[len];
                random = new(123456);
                for (int i = 0; i < len; i++)
                    data[i] = (char)random.Next(0x10000);

                return Task.CompletedTask;
            }

            public override Task AfterBatch()
            {
                data = null;

                return Task.CompletedTask;
            }
        }

        class ReverseChar : SpanCharMeasurement
        {
            public override string Name => "Reverse chars";
            public override void RunStep()
            {
                var span = new Span<char>(data);
                span.Reverse<char>();
            }
        }

        class IndexOfChar : SpanCharMeasurement
        {
            public override string Name => "IndexOf chars";
            public override void RunStep()
            {
                var span = new Span<char>(data);
                span.IndexOf<char>((char)random.Next(0x10000));
            }
        }
    }
}
