// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using System.Runtime.Intrinsics.Wasm;

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
            public override int InitialSamples => 10;
        }

        class ReverseByte : SpanMeasurement
        {
            public override string Name => "Reverse bytes";
            byte[] data;
            int len = 64 * 1024;

            public override Task BeforeBatch()
            {
                //Console.WriteLine($"wasm: {WasmBase.IsSupported}");

                data = new byte[len];
                Random.Shared.NextBytes(data);

                Console.Write("orig:");
                for(int i=0; i<48; i++)
                    Console.Write($" {data[i]}");
                Console.WriteLine();

                var span = new Span<byte>(data);
                span.Reverse<byte>();

                Console.Write("rev: ");
                for (int i = 0; i < 48; i++)
                    Console.Write($" {data[len-i-1]}");
                Console.WriteLine();

                span.Reverse<byte>();

                Console.Write("rev2: ");
                for (int i = 0; i < 48; i++)
                    Console.Write($" {data[i]}");
                Console.WriteLine();

                return Task.CompletedTask;
            }

            public override Task AfterBatch()
            {
                data = null;

                return Task.CompletedTask;
            }

            public override void RunStep()
            {
                var span = new Span<byte>(data);
                span.Reverse<byte>();
            }
        }

        class ReverseChar : SpanMeasurement
        {
            public override string Name => "Reverse chars";
            char[] data;
            int len = 64 * 1024;

            public override Task BeforeBatch()
            {
                //Console.WriteLine($"wasm: {WasmBase.IsSupported}");

                data = new char[len];
                for (int i = 0; i < len; i++)
                    data[i] = (char)Random.Shared.Next(0x10000);

                Console.Write("orig:");
                for (int i = 0; i < 48; i++)
                    Console.Write($" {data[i]}");
                Console.WriteLine();

                var span = new Span<char>(data);
                span.Reverse<char>();

                Console.Write("rev: ");
                for (int i = 0; i < 48; i++)
                    Console.Write($" {data[len - i - 1]}");
                Console.WriteLine();

                span.Reverse<char>();

                Console.Write("rev2: ");
                for (int i = 0; i < 48; i++)
                    Console.Write($" {data[i]}");
                Console.WriteLine();

                return Task.CompletedTask;
            }

            public override Task AfterBatch()
            {
                data = null;

                return Task.CompletedTask;
            }

            public override void RunStep()
            {
                var span = new Span<char>(data);
                span.Reverse<char>();
            }
        }
    }
}
