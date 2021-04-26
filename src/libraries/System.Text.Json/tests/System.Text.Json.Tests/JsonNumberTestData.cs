// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace System.Text.Json.Tests
{
    internal class JsonNumberTestData
    {
        public static List<byte> Bytes { get; set; }
        public static List<sbyte> SBytes { get; set; }
        public static List<short> Shorts { get; set; }
        public static List<int> Ints { get; set; }
        public static List<long> Longs { get; set; }
        public static List<ushort> UShorts { get; set; }
        public static List<uint> UInts { get; set; }
        public static List<ulong> ULongs { get; set; }
        public static List<float> Floats { get; set; }
        public static List<double> Doubles { get; set; }
        public static List<decimal> Decimals { get; set; }

        public static List<byte?> NullableBytes { get; set; }
        public static List<sbyte?> NullableSBytes { get; set; }
        public static List<short?> NullableShorts { get; set; }
        public static List<int?> NullableInts { get; set; }
        public static List<long?> NullableLongs { get; set; }
        public static List<ushort?> NullableUShorts { get; set; }
        public static List<uint?> NullableUInts { get; set; }
        public static List<ulong?> NullableULongs { get; set; }
        public static List<float?> NullableFloats { get; set; }
        public static List<double?> NullableDoubles { get; set; }
        public static List<decimal?> NullableDecimals { get; set; }

        public static byte[] JsonData { get; set; }

        static JsonNumberTestData()
        {
            var random = new Random(42);

            const int numberOfItems = 1_000;

            // Make sure we have 1_005 values in each numeric list.

            #region generate bytes and sbytes
            Bytes = new List<byte>
            {
                byte.MinValue,
                byte.MaxValue,
                64,
                128,
                144
            };

            SBytes = new List<sbyte>
            {
                0,
                64,
                -64,
                sbyte.MinValue,
                sbyte.MaxValue
            };

            byte[] byteArr = new byte[numberOfItems];
            random.NextBytes(byteArr);

            Bytes.AddRange(byteArr);

            foreach (byte item in byteArr)
            {
                SBytes.Add((sbyte)item);
            }
            #endregion

            #region generate shorts
            Shorts = new List<short>
            {
                0,
                20123,
                -20123,
                short.MaxValue,
                short.MinValue
            };
            byte[] b16 = new byte[2 * numberOfItems];
            random.NextBytes(b16);
            for (int i = 0; i < numberOfItems; i++)
            {
                Shorts.Add(BitConverter.ToInt16(b16, i * 2));
            }
            #endregion

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

            #region generate ushorts
            UShorts = new List<ushort>
            {
                ushort.MaxValue,
                ushort.MinValue,
                12345,
                34567,
                64321
            };
            byte[] ub16 = new byte[2 * numberOfItems];
            random.NextBytes(ub16);
            for (int i = 0; i < numberOfItems; i++)
            {
                UShorts.Add(BitConverter.ToUInt16(ub16, i * 2));
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
                double.MaxValue,
                double.MinValue
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

            for (int i = 0; i < Bytes.Count; i++)
            {
                builder.Append("\"byte").Append(i).Append("\": ");
                builder.Append(Bytes[i]).Append(", ");
            }
            for (int i = 0; i < SBytes.Count; i++)
            {
                builder.Append("\"sbyte").Append(i).Append("\": ");
                builder.Append(SBytes[i]).Append(", ");
            }
            for (int i = 0; i < Shorts.Count; i++)
            {
                builder.Append("\"short").Append(i).Append("\": ");
                builder.Append(Shorts[i]).Append(", ");
            }
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
            for (int i = 0; i < UShorts.Count; i++)
            {
                builder.Append("\"ushort").Append(i).Append("\": ");
                builder.Append(UShorts[i]).Append(", ");
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
                const string Format = "{0:" + JsonTestHelper.DoubleFormatString + "}, ";
                string str = string.Format(CultureInfo.InvariantCulture, Format, Doubles[i]);
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", str);
            }
            for (int i = 0; i < Floats.Count; i++)
            {
                // Use InvariantCulture to format the numbers to make sure they retain the decimal point '.'
                builder.Append("\"float").Append(i).Append("\": ");
                const string Format = "{0:" + JsonTestHelper.SingleFormatString + "}, ";
                string str = string.Format(CultureInfo.InvariantCulture, Format, Floats[i]);
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", str);
            }
            for (int i = 0; i < Decimals.Count; i++)
            {
                builder.Append("\"decimal").Append(i).Append("\": ");
                string str = string.Format(CultureInfo.InvariantCulture, "{0}, ", Decimals[i]);
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", str);
            }

            builder.Append("\"intEnd\": 0}");
            #endregion

            // Make collections of nullable numbers.
            NullableBytes = new List<byte?>(Bytes.Select(num => (byte?)num));
            NullableSBytes = new List<sbyte?>(SBytes.Select(num => (sbyte?)num));
            NullableShorts = new List<short?>(Shorts.Select(num => (short?)num));
            NullableInts = new List<int?>(Ints.Select(num => (int?)num));
            NullableLongs = new List<long?>(Longs.Select(num => (long?)num));
            NullableUShorts = new List<ushort?>(UShorts.Select(num => (ushort?)num));
            NullableUInts = new List<uint?>(UInts.Select(num => (uint?)num));
            NullableULongs = new List<ulong?>(ULongs.Select(num => (ulong?)num));
            NullableFloats = new List<float?>(Floats.Select(num => (float?)num));
            NullableDoubles = new List<double?>(Doubles.Select(num => (double?)num));
            NullableDecimals = new List<decimal?>(Decimals.Select(num => (decimal?)num));

            string jsonString = builder.ToString();
            JsonData = Encoding.UTF8.GetBytes(jsonString);
        }
    }
}
