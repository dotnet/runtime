// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace System.Text.Json.Tests
{
    internal class JsonNumberTestData
    {
        public static List<int> Ints { get; set; }
        public static List<long> Longs { get; set; }
        public static List<uint> UInts { get; set; }
        public static List<ulong> ULongs { get; set; }
        public static List<float> Floats { get; set; }
        public static List<double> Doubles { get; set; }
        public static List<decimal> Decimals { get; set; }
        public static byte[] JsonData { get; set; }

        static JsonNumberTestData()
        {
            var random = new Random(42);

            const int numberOfItems = 1_000;

            // Make sure we have 1_005 values in each numeric list.

            #region generate ints
            Ints = new List<int>
            {
                0,
                12345,
                -12345,
                int.MaxValue,
                int.MinValue
            };
            for (int i = 0; i < numberOfItems; i++)
            {
                int value = random.Next(int.MinValue, int.MaxValue);
                Ints.Add(value);
            }
            #endregion

            #region generate longs
            Longs = new List<long>
            {
                0,
                12345678901,
                -12345678901,
                long.MaxValue,
                long.MinValue
            };
            for (int i = 0; i < numberOfItems; i++)
            {
                long value = random.Next(int.MinValue, int.MaxValue);
                if (value < 0)
                    value += int.MinValue;
                else
                    value += int.MaxValue;
                Longs.Add(value);
            }
            #endregion

            #region generate uints
            UInts = new List<uint>
            {
                uint.MinValue,
                uint.MaxValue,
                12345,
                // next two values are just to satisfy requirement of having 5 values in the list
                67890,
                98989
            };
            byte[] b32 = new byte[4];
            for (int i = 0; i < numberOfItems; i++)
            {
                random.NextBytes(b32);
                UInts.Add(BitConverter.ToUInt32(b32, 0));
            }
            #endregion

            #region generate ulongs
            ULongs = new List<ulong>
            {
                ulong.MinValue,
                ulong.MaxValue,
                12345,
                // next two values are just to satisfy requirement of having 5 values in the list
                67890,
                98989
            };
            byte[] b64 = new byte[8];
            for (int i = 0; i < numberOfItems; i++)
            {
                random.NextBytes(b64);
                ULongs.Add(BitConverter.ToUInt64(b64, 0));
            }
            #endregion

            #region generate doubles
            Doubles = new List<double>
            {
                0.000,
                1.1234e1,
                -1.1234e1,
                1.79769313486231E+308,  // double.MaxValue doesn't round trip
                -1.79769313486231E+308  // double.MinValue doesn't round trip
            };
            for (int i = 0; i < numberOfItems / 2; i++)
            {
                double value = JsonTestHelper.NextDouble(random, double.MinValue / 10, double.MaxValue / 10);
                Doubles.Add(value);
            }
            for (int i = 0; i < numberOfItems / 2; i++)
            {
                double value = JsonTestHelper.NextDouble(random, 1_000_000, -1_000_000);
                Doubles.Add(value);
            }
            #endregion

            #region generate floats
            Floats = new List<float>
            {
                0.000f,
                1.1234e1f,
                -1.1234e1f,
                float.MaxValue,
                float.MinValue
            };
            for (int i = 0; i < numberOfItems; i++)
            {
                float value = JsonTestHelper.NextFloat(random);
                Floats.Add(value);
            }
            #endregion

            #region generate decimals
            Decimals = new List<decimal>
            {
                (decimal)0.000,
                (decimal)1.1234e1,
                (decimal)-1.1234e1,
                decimal.MaxValue,
                decimal.MinValue
            };
            for (int i = 0; i < numberOfItems / 2; i++)
            {
                decimal value = JsonTestHelper.NextDecimal(random, 78E14, -78E14);
                Decimals.Add(value);
            }
            for (int i = 0; i < numberOfItems / 2; i++)
            {
                decimal value = JsonTestHelper.NextDecimal(random, 1_000_000, -1_000_000);
                Decimals.Add(value);
            }
            #endregion

            #region generate the json
            var builder = new StringBuilder();
            builder.Append("{");

            for (int i = 0; i < Ints.Count; i++)
            {
                builder.Append("\"int").Append(i).Append("\": ");
                builder.Append(Ints[i]).Append(", ");
            }
            for (int i = 0; i < Longs.Count; i++)
            {
                builder.Append("\"long").Append(i).Append("\": ");
                builder.Append(Longs[i]).Append(", ");
            }
            for (int i = 0; i < UInts.Count; i++)
            {
                builder.Append("\"uint").Append(i).Append("\": ");
                builder.Append(UInts[i]).Append(", ");
            }
            for (int i = 0; i < ULongs.Count; i++)
            {
                builder.Append("\"ulong").Append(i).Append("\": ");
                builder.Append(ULongs[i]).Append(", ");
            }
            for (int i = 0; i < Doubles.Count; i++)
            {
                // Use InvariantCulture to format the numbers to make sure they retain the decimal point '.'
                builder.Append("\"double").Append(i).Append("\": ");
                var str = string.Format(CultureInfo.InvariantCulture, "{0}, ", Doubles[i]);
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", str);
            }
            for (int i = 0; i < Floats.Count; i++)
            {
                builder.Append("\"float").Append(i).Append("\": ");
                var str = string.Format(CultureInfo.InvariantCulture, "{0}, ", Floats[i]);
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", str);
            }
            for (int i = 0; i < Decimals.Count; i++)
            {
                builder.Append("\"decimal").Append(i).Append("\": ");
                var str = string.Format(CultureInfo.InvariantCulture, "{0}, ", Decimals[i]);
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", str);
            }

            builder.Append("\"intEnd\": 0}");
            #endregion

            string jsonString = builder.ToString();
            JsonData = Encoding.UTF8.GetBytes(jsonString);
        }

    }
}
