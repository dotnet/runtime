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
                new IndexOfString(),
                new SequenceEqualByte(),
                new SequenceEqualChar(),
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
            protected int len = 64 * 1024;

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
                span.IndexOf<byte>((byte)random.Next(256));
            }
        }

        class IndexOfString : SpanMeasurement
        {
            public override string Name => "IndexOf strings";

            string input = "string1";
            string value = "string2";

            public override void RunStep()
            {
                ReadOnlySpan<char> inputSpan = input.AsSpan();
                ReadOnlySpan<char> valueSpan = value.AsSpan();

                inputSpan.IndexOf(valueSpan, StringComparison.InvariantCulture);
            }
        }

        abstract class SpanCharMeasurement : SpanMeasurement
        {
            protected char[] data;
            protected int len = 64 * 1024;

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

        class SequenceEqualByte : SpanByteMeasurement
        {
            public override string Name => "SequenceEqual bytes";

            protected byte[] data2;

            public override Task BeforeBatch()
            {
                base.BeforeBatch();

                data2 = new byte[len];
                random = new(234567);
                for (int i = 0; i < len; i++)
                    data2[i] = (i < 3 * len / 4) ? data[i] : (byte)random.Next(0x100);

                return Task.CompletedTask;
            }

            public override Task AfterBatch()
            {
                base.AfterBatch();

                data2 = null;

                return Task.CompletedTask;
            }

            public override void RunStep()
            {
                var span = new Span<byte>(data);
                var span2 = new ReadOnlySpan<byte>(data2);
                span.SequenceEqual(span2);
            }
        }

        class SequenceEqualChar : SpanCharMeasurement
        {
            public override string Name => "SequenceEqual chars";

            protected char[] data2;

            public override Task BeforeBatch()
            {
                base.BeforeBatch();

                data2 = new char[len];
                random = new(234567);
                for (int i = 0; i < len; i++)
                    data2[i] = (i < 3 * len / 4) ? data[i] : (char)random.Next(0x10000);

                return Task.CompletedTask;
            }

            public override Task AfterBatch()
            {
                base.AfterBatch();

                data2 = null;

                return Task.CompletedTask;
            }

            public override void RunStep()
            {
                var span = new Span<char>(data);
                var span2 = new ReadOnlySpan<char>(data2);
                span.SequenceEqual(span2);
            }
        }
    }
}
