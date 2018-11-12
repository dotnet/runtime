// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public static partial class Utf8ParserTests
    {
        private const int InnerCount = 100_000;

        private static readonly string[] s_UInt32TextArray = new string[10]
        {
            "42",
            "429496",
            "429496729",
            "42949",
            "4",
            "42949672",
            "4294",
            "429",
            "4294967295",
            "4294967"
        };

        private static readonly string[] s_UInt32TextArrayHex = new string[8]
        {
            "A2",
            "A29496",
            "A2949",
            "A",
            "A2949672",
            "A294",
            "A29",
            "A294967"
        };

        private static readonly string[] s_Int16TextArray = new string[13]
        {
            "21474",
            "2",
            "-21474",
            "31484",
            "-21",
            "-2",
            "214",
            "2147",
            "-2147",
            "-9345",
            "9345",
            "1000",
            "-214"
        };

        private static readonly string[] s_Int32TextArray = new string[20]
        {
            "214748364",
            "2",
            "21474836",
            "-21474",
            "21474",
            "-21",
            "-2",
            "214",
            "-21474836",
            "-214748364",
            "2147",
            "-2147",
            "-214748",
            "-2147483",
            "214748",
            "-2147483648",
            "2147483647",
            "21",
            "2147483",
            "-214"
        };

        private static readonly string[] s_SByteTextArray = new string[17]
        {
            "95",
            "2",
            "112",
            "-112",
            "-21",
            "-2",
            "114",
            "-114",
            "-124",
            "117",
            "-117",
            "-14",
            "14",
            "74",
            "21",
            "83",
            "-127"
        };

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("2134567890")] // standard parse
        [InlineData("18446744073709551615")] // max value
        [InlineData("0")] // min value
        [InlineData("000000000000000000001235abcdfg")]
        [InlineData("21474836abcdefghijklmnop")]
        private static void StringToUInt64_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ulong.TryParse(text, out ulong value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("abcdef")] // standard parse
        [InlineData("ffffffffffffffff")] // max value
        [InlineData("0")] // min value
        private static void StringToUInt64Hex_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("2134567890")] // standard parse
        [InlineData("18446744073709551615")] // max value
        [InlineData("0")] // min value
        [InlineData("000000000000000000001235abcdfg")]
        [InlineData("21474836abcdefghijklmnop")]
        private static void ByteSpanToUInt64(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = new ReadOnlySpan<byte>(utf8ByteArray);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out ulong value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("abcdef")] // standard parse
        [InlineData("ffffffffffffffff")] // max value
        [InlineData("0")] // min value
        private static void ByteSpanToUInt64Hex(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = new ReadOnlySpan<byte>(utf8ByteArray);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out ulong value, out int bytesConsumed, 'X');
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("2134567890")] // standard parse
        [InlineData("4294967295")] // max value
        [InlineData("0")] // min value
        [InlineData("000000000000000000001235abcdfg")]
        [InlineData("21474836abcdefghijklmnop")]
        private static void StringToUInt32_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        uint.TryParse(text, out uint value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void StringToUInt32_VariableLength_Baseline()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        uint.TryParse(s_UInt32TextArray[i % 10], out uint value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("abcdef")] // standard parse
        [InlineData("ffffffff")] // max value
        [InlineData("0")] // min value
        private static void StringToUInt32Hex_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void StringToUInt32Hex_VariableLength()
        {
            int textLength = s_UInt32TextArrayHex.Length;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        string text = s_UInt32TextArrayHex[i % textLength];
                        uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("2134567890")] // standard parse
        [InlineData("4294967295")] // max value
        [InlineData("0")] // min value
        [InlineData("000000000000000000001235abcdfg")]
        [InlineData("21474836abcdefghijklmnop")]
        private static void ByteSpanToUInt32(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            var utf8ByteSpan = new ReadOnlySpan<byte>(utf8ByteArray);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out uint value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ByteSpanToUInt32_VariableLength()
        {
            int textLength = s_UInt32TextArray.Length;
            byte[][] utf8ByteArray = (byte[][])Array.CreateInstance(typeof(byte[]), textLength);
            for (var i = 0; i < textLength; i++)
            {
                utf8ByteArray[i] = Encoding.UTF8.GetBytes(s_UInt32TextArray[i]);
            }
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray[i % textLength];
                        Utf8Parser.TryParse(utf8ByteSpan, out uint value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("abcdef")] // standard parse
        [InlineData("ffffffff")] // max value
        [InlineData("0")] // min value
        private static void ByteSpanToUInt32Hex(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            var utf8ByteSpan = new ReadOnlySpan<byte>(utf8ByteArray);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out uint value, out int bytesConsumed, 'X');
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ByteSpanToUInt32Hex_VariableLength()
        {
            int textLength = s_UInt32TextArrayHex.Length;
            byte[][] utf8ByteArray = (byte[][])Array.CreateInstance(typeof(byte[]), textLength);
            for (var i = 0; i < textLength; i++)
            {
                utf8ByteArray[i] = Encoding.UTF8.GetBytes(s_UInt32TextArrayHex[i]);
            }
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray[i % textLength];
                        Utf8Parser.TryParse(utf8ByteSpan, out uint value, out int bytesConsumed, 'X');
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ByteSpanToSByte_VariableLength()
        {
            int textLength = s_SByteTextArray.Length;
            byte[][] utf8ByteArray = (byte[][])Array.CreateInstance(typeof(byte[]), textLength);
            for (var i = 0; i < textLength; i++)
            {
                utf8ByteArray[i] = Encoding.UTF8.GetBytes(s_SByteTextArray[i]);
            }

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray[i % textLength];
                        Utf8Parser.TryParse(utf8ByteSpan, out sbyte value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("0")]
        [InlineData("107")] // standard parse
        [InlineData("127")] // max value
        [InlineData("-128")] // min value
        [InlineData("-21abcdefghijklmnop")]
        [InlineData("21abcdefghijklmnop")]
        [InlineData("00000000000000000000123")]
        private static void StringToSByte_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        sbyte.TryParse(text, out sbyte value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void StringToSByte_VariableLength_Baseline()
        {
            int textLength = s_SByteTextArray.Length;
            byte[][] utf8ByteArray = (byte[][])Array.CreateInstance(typeof(byte[]), textLength);
            for (var i = 0; i < textLength; i++)
            {
                utf8ByteArray[i] = Encoding.UTF8.GetBytes(s_SByteTextArray[i]);
            }
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        sbyte.TryParse(s_SByteTextArray[i % textLength], out sbyte value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("0")]
        [InlineData("107374182")] // standard parse
        [InlineData("2147483647")] // max value
        [InlineData("-2147483648")] // min value
        [InlineData("000000000000000000001235abcdfg")]
        [InlineData("21474836abcdefghijklmnop")]
        [InlineData("-21474abcdefghijklmnop")]
        private static void ByteSpanToInt32(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            var utf8ByteSpan = new ReadOnlySpan<byte>(utf8ByteArray);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out int value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ByteSpanToInt32_VariableLength()
        {
            int textLength = s_Int32TextArray.Length;
            byte[][] utf8ByteArray = (byte[][])Array.CreateInstance(typeof(byte[]), textLength);
            for (var i = 0; i < textLength; i++)
            {
                utf8ByteArray[i] = Encoding.UTF8.GetBytes(s_Int32TextArray[i]);
            }

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray[i % textLength];
                        Utf8Parser.TryParse(utf8ByteSpan, out int value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("0")]
        [InlineData("107374182")] // standard parse
        [InlineData("2147483647")] // max value
        [InlineData("-2147483648")] // min value
        [InlineData("000000000000000000001235abcdfg")]
        [InlineData("21474836abcdefghijklmnop")]
        [InlineData("-21474abcdefghijklmnop")]
        private static void StringToInt32_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        int.TryParse(text, out int value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void StringToInt32_VariableLength_Baseline()
        {
            int textLength = s_Int32TextArray.Length;
            byte[][] utf8ByteArray = (byte[][])Array.CreateInstance(typeof(byte[]), textLength);
            for (var i = 0; i < textLength; i++)
            {
                utf8ByteArray[i] = Encoding.UTF8.GetBytes(s_Int32TextArray[i]);
            }

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        int.TryParse(s_Int32TextArray[i % textLength], out int value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ByteSpanToInt16_VariableLength()
        {
            int textLength = s_Int16TextArray.Length;
            byte[][] utf8ByteArray = (byte[][])Array.CreateInstance(typeof(byte[]), textLength);
            for (var i = 0; i < textLength; i++)
            {
                utf8ByteArray[i] = Encoding.UTF8.GetBytes(s_Int16TextArray[i]);
            }

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray[i % textLength];
                        Utf8Parser.TryParse(utf8ByteSpan, out short value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("0")]
        [InlineData("10737")] // standard parse
        [InlineData("32767")] // max value
        [InlineData("-32768")] // min value
        [InlineData("000000000000000000001235abcdfg")]
        private static void StringToInt16_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out short value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void StringToInt16_VariableLength_Baseline()
        {
            int textLength = s_Int16TextArray.Length;
            byte[][] utf8ByteArray = (byte[][])Array.CreateInstance(typeof(byte[]), textLength);
            for (var i = 0; i < textLength; i++)
            {
                utf8ByteArray[i] = Encoding.UTF8.GetBytes(s_Int16TextArray[i]);
            }

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        short.TryParse(s_Int16TextArray[i % textLength], out short value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("True")]
        [InlineData("False")]
        private static void StringToBool_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        bool.TryParse(text, out bool value);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("True")]
        [InlineData("False")]
        private static void BytesSpanToBool(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out bool value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("0")]
        [InlineData("107")] // standard parse
        [InlineData("127")] // max value
        [InlineData("-128")] // min value
        [InlineData("-21abcdefghijklmnop")]
        [InlineData("21abcdefghijklmnop")]
        [InlineData("00000000000000000000123")]
        private static void ByteSpanToSByte(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out sbyte value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("42")] // standard parse
        [InlineData("0")] // min value
        [InlineData("255")] // max value
        private static void ByteSpanToByte(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out byte value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }


        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("42")] // standard parse
        [InlineData("0")] // min value
        [InlineData("255")] // max value
        private static void StringToByte_Baseline(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        byte.TryParse(text, out byte value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("0")]
        [InlineData("4212")] // standard parse
        [InlineData("-32768")] // min value
        [InlineData("32767")] // max value
        [InlineData("000000000000000000001235abcdfg")]
        private static void ByteSpanToInt16(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out short value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("4212")] // standard parse
        [InlineData("0")] // min value
        [InlineData("65535")] // max value
        private static void ByteSpanToUInt16(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out ushort value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("12837467")] // standard parse
        [InlineData("-9223372036854775808")] // min value
        [InlineData("9223372036854775807")] // max value
        [InlineData("000000000000000000001235abcdfg")]
        [InlineData("21474836abcdefghijklmnop")]
        private static void ByteSpanToInt64(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out long value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }


        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("12837467")] // standard parse
        [InlineData("-9223372036854775808")] // min value
        [InlineData("9223372036854775807")] // max value
        [InlineData("000000000000000000001235abcdfg")]
        [InlineData("21474836abcdefghijklmnop")]
        private static void StringToInt64_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        long.TryParse(text, out long value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("Fri, 30 Jun 2000 03:15:45 GMT")] // standard parse
        private static void ByteSpanToTimeOffsetR(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out DateTimeOffset value, out int bytesConsumed, 'R');
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("Fri, 30 Jun 2000 03:15:45 GMT")] // standard parse
        private static void StringToTimeOffsetR_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        DateTimeOffset.TryParseExact(text, "r", null, DateTimeStyles.None, out DateTimeOffset value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ByteSpanToDecimal()
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes("1.23456789E+5");
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out decimal value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        // Reenable commented out test cases when https://github.com/xunit/xunit/issues/1822 is fixed.
        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("-Infinity")]                   // Negative Infinity
        [InlineData("-1.7976931348623157E+308")]    // Min Negative Normal
        [InlineData("-3.1415926535897931")]         // Negative pi
        [InlineData("-2.7182818284590451")]         // Negative e
        [InlineData("-1")]                          // Negative One
        // [InlineData("-2.2250738585072014E-308")]    // Max Negative Normal
        [InlineData("-2.2250738585072009E-308")]   // Min Negative Subnormal
        [InlineData("-4.94065645841247E-324")]     // Max Negative Subnormal (Negative Epsilon)
        [InlineData("-0.0")]                       // Negative Zero
        [InlineData("NaN")]                        // NaN
        [InlineData("0")]                          // Positive Zero
        [InlineData("4.94065645841247E-324")]      // Min Positive Subnormal (Positive Epsilon)
        [InlineData("2.2250738585072009E-308")]    // Max Positive Subnormal
        // [InlineData("2.2250738585072014E-308")]     // Min Positive Normal
        [InlineData("1")]                          // Positive One
        [InlineData("2.7182818284590451")]         // Positive e
        [InlineData("3.1415926535897931")]         // Positive pi
        [InlineData("1.7976931348623157E+308")]    // Max Positive Normal
        [InlineData("Infinity")]                   // Positive Infinity
        private static void ByteSpanToDouble(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out double value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        // Reenable commented out test cases when https://github.com/xunit/xunit/issues/1822 is fixed.
        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("-Infinity")]           // Negative Infinity
        [InlineData("-3.40282347E+38")]     // Min Negative Normal
        [InlineData("-3.14159274")]         // Negative pi
        [InlineData("-2.71828175")]         // Negative e
        [InlineData("-1")]                  // Negative One
        // [InlineData("-1.17549435E-38")]     // Max Negative Normal
        [InlineData("-1.17549421E-38")]     // Min Negative Subnormal
        [InlineData("-1.401298E-45")]       // Max Negative Subnormal (Negative Epsilon)
        [InlineData("-0.0")]                // Negative Zero
        [InlineData("NaN")]                 // NaN
        [InlineData("0")]                   // Positive Zero
        [InlineData("1.401298E-45")]        // Min Positive Subnormal (Positive Epsilon)
        [InlineData("1.17549421E-38")]      // Max Positive Subnormal
        // [InlineData("1.17549435E-38")]      // Min Positive Normal
        [InlineData("1")]                   // Positive One
        [InlineData("2.71828175")]          // Positive e
        [InlineData("3.14159274")]          // Positive pi
        [InlineData("3.40282347E+38")]      // Max Positive Normal
        [InlineData("Infinity")]            // Positive Infinity
        private static void ByteSpanToSingle(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out float value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }
    }
}
