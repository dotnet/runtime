// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Span
{
    class Sink
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Sink NewSink() { return new Sink(); }

        public byte b;
        public int i;
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    class BenchmarkAttribute : Attribute
    {
        public BenchmarkAttribute()
        {
        }
        private long _innerIterationsCount = 1;
        public long InnerIterationCount
        {
            get { return _innerIterationsCount; }
            set { _innerIterationsCount = value; }
        }
    }

    // A simplified xunit InlineData attribute.
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    class InlineDataAttribute : Attribute
    {
        public InlineDataAttribute(int data)
        {
            _data = data;
        }
        int _data;
        public int Data
        {
            get { return _data; }
            set { _data = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    class CategoryAttribute : Attribute
    {
        public CategoryAttribute(string name)
        {
            _name = name;
        }
        string _name;
        public string Name => _name;
    }

    public class IndexerBench
    {
        const int Iterations = 1000000;
        const int DefaultLength = 1024;
        const byte Expected = 70;
        static bool HasFailure = false;

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination")]
        public static void Ref(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestRef(s);
                }
                return result;
            },
            "Ref({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestRef(Span<byte> data)
        {
            ref byte p = ref MemoryMarshal.GetReference(data);
            int length = data.Length;
            byte x = 0;

            for (var idx = 0; idx < length; idx++)
            {
                x ^= Unsafe.Add(ref p, idx);
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination")]
        public static void Fixed1(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestFixed1(s);
                }
                return result;
            },
            "Fixed1({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe byte TestFixed1(Span<byte> data)
        {
            fixed (byte* pData = &MemoryMarshal.GetReference(data))
            {
                int length = data.Length;
                byte x = 0;
                byte* p = pData;

                for (var idx = 0; idx < length; idx++)
                {
                    x ^= *(p + idx);
                }

                return x;
            }
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination")]
        public static void Fixed2(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestFixed2(s);
                }
                return result;
            },
            "Fixed2({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe byte TestFixed2(Span<byte> data)
        {
            fixed (byte* pData = &MemoryMarshal.GetReference(data))
            {
                int length = data.Length;
                byte x = 0;

                for (var idx = 0; idx < length; idx++)
                {
                    x ^= pData[idx];
                }

                return x;
            }
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination")]
        public static void Indexer1(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestIndexer1(s);
                }
                return result;
            },
            "Indexer1({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer1(Span<byte> data)
        {
            int length = data.Length;
            byte x = 0;

            for (var idx = 0; idx < length; idx++)
            {
                x ^= data[idx];
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination")]
        public static void Indexer2(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestIndexer2(s);
                }
                return result;
            },
            "Indexer2({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer2(Span<byte> data)
        {
            byte x = 0;

            for (var idx = 0; idx < data.Length; idx++)
            {
                x ^= data[idx];
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination")]
        public static void Indexer3(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestIndexer3(s);
                }
                return result;
            },
            "Indexer3({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer3(Span<byte> data)
        {
            Span<byte> data2 = data;

            byte x = 0;

            for (var idx = 0; idx < data2.Length; idx++)
            {
                x ^= data2[idx];
            }

            return x;
        }

        [Benchmark(InnerIterationCount=Iterations / 10)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination")]
        public static void Indexer4(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                int inner = Math.Max(1, innerIterationCount);
                for (int i = 0; i < inner ; ++i)
                {
                    result = TestIndexer4(s, 10);
                }
                return result;
            },
            "Indexer4({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer4(Span<byte> data, int iterations)
        {
            byte x = 0;
            int length = data.Length;

            // This does more or less the same work as TestIndexer1
            // but is expressed as a loop nest.
            for (int i = 0; i < iterations; i++)
            {
                x = 0;

                for (var idx = 0; idx < length; idx++)
                {
                    x ^= data[idx];
                }
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination")]
        public static void Indexer5(int length)
        {
            byte[] a = GetData(length);
            int z = 0;

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestIndexer5(s, out z);
                }
                return result;
            },
            "Indexer5({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer5(Span<byte> data, out int z)
        {
            byte x = 0;
            z = -1;

            // Write to z here should not be able to modify
            // the span.
            for (var idx = 0; idx < data.Length; idx++)
            {
                x ^= data[idx];
                z = idx;
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination")]
        public static void Indexer6(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestIndexer6(s);
                }
                return result;
            },
            "Indexer6({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer6(Span<byte> data)
        {
            byte x = 0;
            Sink s = Sink.NewSink();

            // Write to s.i here should not be able to modify
            // the span.
            for (var idx = 0; idx < data.Length; idx++)
            {
                x ^= data[idx];
                s.i = 0;
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination")]
        public static void ReadOnlyIndexer1(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestReadOnlyIndexer1(s);
                }
                return result;
            },
            "ReadOnlyIndexer1({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestReadOnlyIndexer1(ReadOnlySpan<byte> data)
        {
            int length = data.Length;
            byte x = 0;

            for (var idx = 0; idx < length; idx++)
            {
                x ^= data[idx];
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination")]
        public static void ReadOnlyIndexer2(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestReadOnlyIndexer2(s);
                }
                return result;
            },
            "ReadOnlyIndexer2({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestReadOnlyIndexer2(ReadOnlySpan<byte> data)
        {
            byte x = 0;

            for (var idx = 0; idx < data.Length; idx++)
            {
                x ^= data[idx];
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination w/ writes")]
        public static void WriteViaIndexer1(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestWriteViaIndexer1(s);
                }
                return result;
            },
            "WriteViaIndexer1({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestWriteViaIndexer1(Span<byte> data)
        {
            byte q = data[0];

            for (var idx = 1; idx < data.Length; idx++)
            {
                data[0] ^= data[idx];
            }

            byte x = data[0];
            data[0] = q;

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Indexer in-loop bounds check elimination w/ writes")]
        public static void WriteViaIndexer2(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestWriteViaIndexer2(s, 0, length);
                }
                return result;
            },
            "WriteViaIndexer2({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestWriteViaIndexer2(Span<byte> data, int start, int end)
        {
            byte x = 0;

            for (var idx = start; idx < end; idx++)
            {
                // Bounds checks are redundant
                byte b = data[idx];
                x ^= b;
                data[idx] = b;
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Span known size bounds check elimination")]
        public static void KnownSizeArray(int length)
        {
            if (length != 1024)
            {
                throw new Exception("test requires 1024 byte length");
            }

            Invoke((int innerIterationCount) =>
            {
                byte result = TestKnownSizeArray(innerIterationCount);
                return result;
            },
            "KnownSizeArray({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestKnownSizeArray(int innerIterationCount)
        {
            byte[] a = new byte[1024];
            SetData(a);
            Span<byte> data = new Span<byte>(a);
            byte x = 0;

            for (int i = 0; i < innerIterationCount; i++)
            {
                x = 0;
                for (var idx = 0; idx < data.Length; idx++)
                {
                    x ^= data[idx];
                }
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Span known size bounds check elimination")]
        public static void KnownSizeCtor(int length)
        {
            if (length < 1024)
            {
                throw new Exception("test requires at least 1024 byte length");
            }

            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                byte result = TestKnownSizeCtor(a, innerIterationCount);
                return result;
            },
            "KnownSizeCtor({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestKnownSizeCtor(byte[] a, int innerIterationCount)
        {
            Span<byte> data = new Span<byte>(a, 0, 1024);
            byte x = 0;

            for (int i = 0; i < innerIterationCount; i++)
            {
                x = 0;
                for (var idx = 0; idx < data.Length; idx++)
                {
                    x ^= data[idx];
                }
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Span known size bounds check elimination")]
        public static void KnownSizeCtor2(int length)
        {
            if (length < 1024)
            {
                throw new Exception("test requires at least 1024 byte length");
            }

            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                byte result = TestKnownSizeCtor2(a, innerIterationCount);
                return result;
            },
            "KnownSizeCtor2({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestKnownSizeCtor2(byte[] a, int innerIterationCount)
        {
            Span<byte> data1 = new Span<byte>(a, 0, 512);
            Span<byte> data2 = new Span<byte>(a, 512, 512);
            byte x = 0;

            for (int i = 0; i < innerIterationCount; i++)
            {
                x = 0;
                for (var idx = 0; idx < data1.Length; idx++)
                {
                    x ^= data1[idx];
                }
                for (var idx = 0; idx < data2.Length; idx++)
                {
                    x ^= data2[idx];
                }
            }

            return x;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Same index in-loop redundant bounds check elimination")]
        public static void SameIndex1(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestSameIndex1(s, 0, length);
                }
                return result;
            },
            "SameIndex1({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestSameIndex1(Span<byte> data, int start, int end)
        {
            byte x = 0;
            byte y = 0;

            for (var idx = start; idx < end; idx++)
            {
                x ^= data[idx];
                y ^= data[idx];
            }

            byte t = (byte)(y ^ x ^ y);

            return t;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Same index in-loop redundant bounds check elimination")]
        public static void SameIndex2(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestSameIndex2(s, ref s[0], 0, length);
                }
                return result;
            },
            "SameIndex2({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestSameIndex2(Span<byte> data, ref byte b, int start, int end)
        {
            byte x = 0;
            byte y = 0;
            byte ye = 121;
            byte q = data[0];

            for (var idx = start; idx < end; idx++)
            {
                // Bounds check is redundant, but values are not CSEs.
                x ^= data[idx];
                b = 1;
                y ^= data[idx];
            }

            byte t = (byte)(y ^ x ^ ye);
            data[0] = q;

            return t;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Covered index in-loop redundant bounds check elimination")]
        public static void CoveredIndex1(int length)
        {
            if (length < 100)
            {
                throw new Exception("test requires at least 100 byte length");
            }

            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestCoveredIndex1(s, 0, length);
                }
                return result;
            },
            "CoveredIndex1({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestCoveredIndex1(Span<byte> data, int start, int end)
        {
            byte x = 0;
            byte y = 0;

            for (var idx = start; idx < end - 100; idx++)
            {
                x ^= data[idx + 100];
                y ^= data[idx];
            }

            for (var idx = end - 100; idx < end; idx++)
            {
                y ^= data[idx];
                x ^= data[idx - 100];
            }

            byte r = (byte)(x ^ y ^ x);

            return r;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Covered index in-loop redundant bounds check elimination")]
        public static void CoveredIndex2(int length)
        {
            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestCoveredIndex2(s, 0, length);
                }
                return result;
            },
            "CoveredIndex2({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestCoveredIndex2(Span<byte> data, int start, int end)
        {
            byte x = 0;
            byte y = 0;

            for (var idx = start; idx < end; idx++)
            {
                x ^= data[idx];

                if (idx != 100)
                {
                    // Should be able to eliminate this bounds check
                    y ^= data[0];
                }
            }

            byte r = (byte)(y ^ x ^ y);

            return r;
        }

        [Benchmark(InnerIterationCount = Iterations)]
        [InlineData(DefaultLength)]
        [Category("Covered index in-loop redundant bounds check elimination")]
        public static void CoveredIndex3(int length)
        {
            if (length < 50)
            {
                throw new Exception("test requires at least 100 byte length");
            }

            byte[] a = GetData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                byte result = 0;
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    result = TestCoveredIndex3(s, 0, length);
                }
                return result;
            },
            "CoveredIndex3({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestCoveredIndex3(Span<byte> data, int start, int end)
        {
            byte x = 0;
            byte y = 0;
            byte z = 0;

            for (var idx = start; idx < end; idx++)
            {
                x ^= data[idx];

                if (idx != 100)
                {
                    y ^= data[50];
                    // Should be able to eliminate this bounds check
                    z ^= data[25];
                }
            }

            byte r = (byte)(z ^ y ^ x ^ y ^ z);

            return r;
        }

        // Inner loop to be measured is taken as an Func<int, byte>, and invoked passing the number
        // of iterations that the inner loop should execute.
        static void Invoke(Func<int, byte> innerLoop, string nameFormat, params object[] nameArgs)
        {
            if (DoWarmUp)
            {
                // Run some warm-up iterations before measuring
                innerLoop(CommandLineInnerIterationCount);
                // Clear the flag since we're now warmed up (caller will
                // reset it before calling new code)
                DoWarmUp = false;
            }

            // Now do the timed run of the inner loop.
            Stopwatch sw = Stopwatch.StartNew();
            byte check = innerLoop(CommandLineInnerIterationCount);
            sw.Stop();

            // Print result.
            string name = String.Format(nameFormat, nameArgs);
            double timeInMs = sw.Elapsed.TotalMilliseconds;
            Console.Write("{0,25}: {1,7:F2}ms", name, timeInMs);

            bool failed = (check != Expected);
            if (failed)
            {
                Console.Write(" -- failed to validate, got {0} expected {1}", check, Expected);
                HasFailure = true;
            }
            Console.WriteLine();
        }

        static byte[] GetData(int size)
        {
            byte[] data = new byte[size];
            SetData(data);
            return data;
        }

        static void SetData(byte[] data)
        {
            Random Rnd = new Random(42);
            Rnd.NextBytes(data);
        }

        static int CommandLineInnerIterationCount = 1;
        static bool DoWarmUp;

        public static void Usage()
        {
            Console.WriteLine("   pass -bench for benchmark mode w/default iterations");
            Console.WriteLine("   pass [#iterations] for benchmark mode w/iterations");
            Console.WriteLine();
        }

        [Fact]
        public static int TestEntryPoint()
        {
            return Test(Array.Empty<string>());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Test(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].Equals("-bench"))
                {
                    CommandLineInnerIterationCount = Iterations;
                }
                else
                {
                    bool parsed = Int32.TryParse(args[0], out CommandLineInnerIterationCount);
                    if (!parsed)
                    {
                        Usage();
                        return -1;
                    }
                }

                Console.WriteLine("Running as command line perf test: {0} iterations",
                    CommandLineInnerIterationCount);
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Running as correctness test: {0} iterations",
                    CommandLineInnerIterationCount);
                Usage();
            }

            // Discover what tests to run via reflection
            TypeInfo t = typeof(IndexerBench).GetTypeInfo();

            var testsByCategory = new Dictionary<string, List<MethodInfo>>();

            // Do a first pass to find out what categories of benchmarks we have.
            foreach(MethodInfo m in t.DeclaredMethods)
            {
                BenchmarkAttribute benchAttr = m.GetCustomAttribute<BenchmarkAttribute>();
                if (benchAttr != null)
                {
                    string category = "none";
                    CategoryAttribute categoryAttr = m.GetCustomAttribute<CategoryAttribute>();
                    if (categoryAttr != null)
                    {
                        category = categoryAttr.Name;
                    }

                    List<MethodInfo> tests = null;

                    if (!testsByCategory.ContainsKey(category))
                    {
                        tests = new List<MethodInfo>();
                        testsByCategory.Add(category, tests);
                    }
                    else
                    {
                        tests = testsByCategory[category];
                    }

                    tests.Add(m);
                }
            }

            foreach(string categoryName in testsByCategory.Keys)
            {
                Console.WriteLine("**** {0} ****", categoryName);

                foreach(MethodInfo m in testsByCategory[categoryName])
                {
                    // Request a warm-up iteration before measuring this benchmark method.
                    DoWarmUp = true;

                    // Get the benchmark to measure as a delegate taking the number of inner-loop iterations to run
                    var invokeMethod = m.CreateDelegate(typeof(Action<int>)) as Action<int>;

                    // All the benchmarks methods in this test use [InlineData] to specify how many times and with
                    // what arguments they should be run.
                    foreach (InlineDataAttribute dataAttr in m.GetCustomAttributes<InlineDataAttribute>())
                    {
                        int data = dataAttr.Data;
                        invokeMethod(data);
                    }
                }

                Console.WriteLine();
            }

            if (HasFailure)
            {
                Console.WriteLine("Some tests failed validation");
                return -1;
            }

            return 100;
        }
    }
}
