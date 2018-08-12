// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    /// <summary>
    /// Generates random values for SQL Server testing, such as DateTime and Money. Instances of this class are not thread safe!
    /// </summary>
    public sealed class SqlRandomizer : Randomizer
    {
        /// <summary>
        /// default limit for allocation size
        /// </summary>
        public const int DefaultMaxDataSize = 0x10000; // 1Mb

        /// <summary>
        /// must be set exactly once during construction only since it is part of random state
        /// </summary>
        private int _maxDataSize;

        /// <summary>
        /// when creating random buffers, this value is used to decide how much of random buffer 
        /// will be true random data. The true random data is filled in the beginning and rest of the
        /// buffer repeats the same information
        /// </summary>
        private const int RepeatThreshold = 32;

        public SqlRandomizer()
            : this(CreateSeed(), DefaultMaxDataSize)
        { } // do not add code here

        public SqlRandomizer(int seed)
            : this(seed, DefaultMaxDataSize)
        { } // do not add code here

        public SqlRandomizer(int seed, int maxDataSize)
            : base(seed)
        {
            _maxDataSize = maxDataSize;
        }

        public SqlRandomizer(State state)
            : base(state)
        {
            // Deserialize will read _maxDataSize too
        }

        protected override int BinaryStateSize
        {
            get
            {
                return base.BinaryStateSize + 4; // 4 bytes for _maxDataSize
            }
        }

        protected override void Serialize(byte[] binState, out int nextOffset)
        {
            base.Serialize(binState, out nextOffset);

            SerializeInt(_maxDataSize, binState, ref nextOffset);
        }

        protected internal override void Deserialize(byte[] binState, out int nextOffset)
        {
            base.Deserialize(binState, out nextOffset);

            _maxDataSize = DeserializeInt(binState, ref nextOffset);
        }

        /// <summary>
        /// generates random bitmap array (optimized)
        /// </summary>
        public BitArray NextBitmap(int bitCount)
        {
            if (bitCount <= 0)
                throw new ArgumentOutOfRangeException("bitCount");

            // optimize for any number of bits
            byte[] randValues = new byte[(bitCount + 7) / 8];
            base.NextBytes(randValues);
            BitArray bitMap = new BitArray(randValues);
            // the bitmap was created with length rounded up to 8, truncate to ensure correct size
            bitMap.Length = bitCount;
            return bitMap;
        }

        /// <summary>
        /// generates random bitmap array, using probability for null (nullOdds is from 0 to 100)
        /// </summary>
        public BitArray NextBitmap(int bitCount, int nullOdds)
        {
            if (bitCount <= 0)
                throw new ArgumentOutOfRangeException("bitCount");

            if (nullOdds < 0 || nullOdds > 100)
                throw new ArgumentOutOfRangeException("nullOdds");

            // optimize for any number of bits
            BitArray bitMap = new BitArray(bitCount, false);

            for (int i = 0; i < bitCount; i++)
            {
                bitMap[i] = Next(100) < nullOdds;
            }

            return bitMap;
        }

        /// <summary>
        /// generates random list of columns in random order, no repeated columns
        /// </summary>
        public int[] NextIndices(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException("count");

            int[] indices = new int[count];
            for (int c = 0; c < count; c++)
            {
                indices[c] = c;
            }

            // shuffle
            Shuffle(indices);

            return indices;
        }

        /// <summary>
        /// Shuffles the values in given array, numbers are taken from the whole array, even if valuesToSet is provided.
        /// </summary>
        /// <param name="valuesToSet">if provided, only the beginning of the array up to this index will be shuffled</param>
        public void Shuffle<T>(T[] values, int? valuesToSet = null)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            int count = values.Length;
            int selectValues = count;
            if (valuesToSet.HasValue)
            {
                selectValues = valuesToSet.Value;
                if (selectValues < 0 || selectValues > count)
                    throw new ArgumentOutOfRangeException("valuesToShuffle");
            }

            for (int i = 0; i < selectValues; i++)
            {
                int nextIndex = NextIntInclusive(i, maxValueInclusive: count - 1);
                // swap
                T temp = values[i]; values[i] = values[nextIndex]; values[nextIndex] = temp;
            }
        }

        private enum LowValueEnforcementLevel
        {
            Uniform = 32,
            Weak = 16,
            Medium = 10,
            Strong = 4,
            VeryStrong = 2
        }

        /// <summary>
        /// generates size value with low probability of large size values within the given range
        /// </summary>
        /// <param name="lowValuesEnforcementLevel">
        /// lowValuesEnforcementLevel is value between 0 and 31; 
        /// 0 means uniform distribution in the min/max range;
        /// 31 means very low chances for high values
        /// </param>
        private int NextAllocationUnit(int minSize, int maxSize, LowValueEnforcementLevel lowValuesLevel)
        {
            if (minSize < 0 || maxSize < 0 || minSize > maxSize)
                throw new ArgumentOutOfRangeException("minSize or maxSize are out of range");

            if (lowValuesLevel < LowValueEnforcementLevel.VeryStrong || lowValuesLevel > LowValueEnforcementLevel.Uniform)
                throw new ArgumentOutOfRangeException("lowValuesLevel");

            if (minSize == maxSize)
                return minSize; // shortcut for fixed size

            long longRange = (long)maxSize - (long)minSize + 1;

            // create a sample in range [0, 1) (it is never 1)
            double sample = base.NextDouble();

            // decrease chances of large size values based on the how many bits digits are set in the maxValue
            int bitsPerLevel = (int)lowValuesLevel;
            long maxBitsLeft = longRange >> bitsPerLevel;
            while (maxBitsLeft > 0)
            {
                sample *= base.NextDouble();
                maxBitsLeft >>= bitsPerLevel;
            }

            int res = minSize + (int)(sample * longRange);
            Debug.Assert(res >= minSize && res <= maxSize);
            return res;
        }

        /// <summary>
        /// Generates a random number to be used as a size for memory allocations.
        /// This method will return with low numbers most of the time, but it has very low probability to generate large ones.
        /// The limit is currently set to MaxData (even if maxSize is larger)
        /// </summary>
        public int NextAllocationSizeBytes(int minSize = 0, int? maxSize = null)
        {
            if (minSize > _maxDataSize)
                throw new ArgumentOutOfRangeException("minSize cannot be greater than a maximum defined data size");
            if (!maxSize.HasValue || maxSize.Value > _maxDataSize)
                maxSize = _maxDataSize;
            return NextAllocationUnit(minSize, maxSize.Value, LowValueEnforcementLevel.Strong);
        }

        /// <summary>
        /// used by random table generators to select random number of columns and rows. This method will return very low numbers with high probability, 
        /// </summary>
        public void NextTableDimentions(int maxRows, int maxColumns, int maxTotalSize, out int randRows, out int randColumns)
        {
            // prefer really low values to ensure table size will not go up way too much too frequently
            const LowValueEnforcementLevel level = LowValueEnforcementLevel.Medium;
            if (NextBit())
            {
                // select rows first, then columns
                randColumns = NextAllocationUnit(1, maxColumns, level);
                randRows = NextAllocationUnit(1, Math.Min(maxRows, maxTotalSize / randColumns), level);
            }
            else
            {
                randRows = NextAllocationUnit(1, maxRows, level);
                randColumns = NextAllocationUnit(1, Math.Min(maxColumns, maxTotalSize / randRows), level);
            }
        }

        #region byte and char array generators

        /// <summary>
        /// used internally to repeat randomly generated portions of the array
        /// </summary>
        private void Repeat<T>(T[] result, int trueRandomCount)
        {
            // repeat the first chunk into rest of the array
            int remainder = result.Length - trueRandomCount;
            int offset = trueRandomCount;

            // repeat whole chunks
            while (remainder >= trueRandomCount)
            {
                Array.Copy(result, 0, result, offset, trueRandomCount);
                remainder -= trueRandomCount;
                offset += trueRandomCount;
            }

            // complete the last (partial) chunk in the end, if any
            if (remainder > 0)
                Array.Copy(result, 0, result, offset, remainder);
        }

        /// <summary>
        /// fill byte array with pseudo random data. Only the beginning of the array is filled with true random numbers, rest of it
        /// is repeated data.
        /// </summary>
        public void FillByteArray(byte[] result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (result.Length == 0)
                return;

            // generate the first chunk of the array with true random values
            int trueRandomCount = base.Next(1, Math.Min(result.Length, RepeatThreshold));
            for (int i = 0; i < trueRandomCount; i++)
                result[i] = unchecked((byte)base.Next());

            Repeat(result, trueRandomCount);
        }

        /// <summary>
        /// Fill the  array with pseudo random ANSI characters (ascii code less than 128). This method can be used to generate
        /// char, varchar and text values.
        /// </summary>
        public void FillAnsiCharArray(char[] result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (result.Length == 0)
                return;

            // generate the first chunk of the array with true random values
            int trueRandomCount = base.Next(1, Math.Min(result.Length, RepeatThreshold));
            for (int i = 0; i < trueRandomCount; i++)
                result[i] = (char)NextIntInclusive(0, maxValueInclusive: 127);

            Repeat(result, trueRandomCount);
        }

        /// <summary>
        /// Fill the array with pseudo random unicode characters, not including the surrogate ranges.  This method can be used to generate
        /// nchar, nvarchar and ntext values.
        /// </summary>
        public void FillUcs2CharArray(char[] result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (result.Length == 0)
                return;

            // generate the first chunk of the array with true random values
            int trueRandomCount = base.Next(1, Math.Min(result.Length, RepeatThreshold));
            for (int i = 0; i < trueRandomCount; i++)
                result[i] = (char)NextIntInclusive(0, maxValueInclusive: 0xD800 - 1); // do not include surrogates

            Repeat(result, trueRandomCount);
        }

        /// <summary>
        /// generates random byte array with high probability of small-size arrays
        /// </summary>
        public byte[] NextByteArray(int minSize = 0, int? maxSize = null)
        {
            int size = NextAllocationSizeBytes(minSize, maxSize);
            byte[] resArray = new byte[size];
            FillByteArray(resArray);
            return resArray;
        }

        /// <summary>
        /// generates random Ucs2 array with high probability of small-size arrays. The result does not include surrogate pairs or characters.
        /// </summary>
        public char[] NextUcs2Array(int minSize = 0, int? maxByteSize = null)
        {
            // enforce max data in characters
            if (!maxByteSize.HasValue || maxByteSize.Value > _maxDataSize)
                maxByteSize = _maxDataSize;

            int charSize = NextAllocationSizeBytes(minSize, maxByteSize) / 2;
            char[] resArray = new char[charSize];
            FillUcs2CharArray(resArray);
            return resArray;
        }

        /// <summary>
        /// generates random array with high probability of small-size arrays. The result includes only characters with code less than 128.
        /// </summary>
        public char[] NextAnsiArray(int minSize = 0, int? maxSize = null)
        {
            // enforce max allocation size for char array
            if (!maxSize.HasValue || maxSize.Value > _maxDataSize / 2)
                maxSize = _maxDataSize / 2;

            int size = NextAllocationSizeBytes(minSize, maxSize);
            char[] resArray = new char[size];
            FillAnsiCharArray(resArray);
            return resArray;
        }

        /// <summary>
        /// generates random binary value for SQL Server, with high probability of small-size arrays.
        /// </summary>
        public byte[] NextBinary(int minSize = 0, int maxSize = 8000)
        {
            return NextByteArray(minSize, maxSize);
        }

        /// <summary>
        /// returns a random 8-byte array as a timestamp (rowversion) value
        /// </summary>
        public byte[] NextRowVersion()
        {
            return NextByteArray(8, 8);
        }

        /// <summary>
        /// returns a random GUID to be used as unique identifier in SQL. Note that this method is deterministic for fixed seed random instances
        /// (it does NOT use Guid.NewGuid).
        /// </summary>
        public Guid NextUniqueIdentifier()
        {
            return new Guid(NextByteArray(16, 16));
        }

        #endregion

        #region Date and Time values

        /// <summary>
        /// generates random, but valid datetime-type value, for SQL Server, with 3msec resolution (0, 3, 7msec)
        /// </summary>
        public DateTime NextDateTime()
        {
            DateTime dt = NextDateTime(SqlDateTime.MinValue.Value, SqlDateTime.MaxValue.Value);
            // round to datetime type resolution (increments of .000, .003, or .007 seconds)
            long totalMilliseconds = dt.Ticks / TimeSpan.TicksPerMillisecond;
            int lastDigit = (int)(totalMilliseconds % 10);
            if (lastDigit < 3)
                lastDigit = 0;
            else if (lastDigit < 7)
                lastDigit = 3;
            else
                lastDigit = 7;

            totalMilliseconds = (totalMilliseconds / 10) * 10 + lastDigit;
            return new DateTime(totalMilliseconds * TimeSpan.TicksPerMillisecond);
        }

        /// <summary>
        /// generates random, but valid datetime2 value for SQL Server
        /// </summary>
        public DateTime NextDateTime2()
        {
            return NextDateTime(DateTime.MinValue, DateTime.MaxValue);
        }

        /// <summary>
        /// generates random, but valid datetimeoffset value for SQL Server
        /// </summary>
        public DateTimeOffset NextDateTimeOffset()
        {
            return new DateTimeOffset(NextDateTime2());
        }

        /// <summary>
        /// generates random, but valid date value for SQL Server
        /// </summary>
        public DateTime NextDate()
        {
            return NextDateTime2().Date;
        }

        /// <summary>
        /// generates random DateTime value in the given range.
        /// </summary>
        public DateTime NextDateTime(DateTime minValue, DateTime maxValueInclusive)
        {
            double ticksRange = unchecked((double)maxValueInclusive.Ticks - minValue.Ticks + 1);
            long ticks = minValue.Ticks + (long)(ticksRange * base.NextDouble());
            return new DateTime(ticks);
        }

        /// <summary>
        /// generates random smalldatetime value for SQL server, in the range of January 1, 1900 to June 6, 2079, to an accuracy of one minute
        /// </summary>
        public DateTime NextSmallDateTime()
        {
            DateTime dt = NextDateTime(
                        minValue: new DateTime(1900, 1, 1, 0, 0, 0),
                        maxValueInclusive: new DateTime(2079, 6, 6));
            // truncate minutes
            dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
            return dt;
        }

        /// <summary>
        /// generates random TIME value for SQL Server (one day clock, 100 nano second precision)
        /// </summary>
        public TimeSpan NextTime()
        {
            return TimeSpan.FromTicks(Math.Abs(NextBigInt()) % TimeSpan.TicksPerDay);
        }

        #endregion

        #region Double values

        /// <summary>
        /// generates random Double value in the given range
        /// </summary>
        public double NextDouble(double minValue, double maxValueExclusive)
        {
            double res;
            if (minValue >= maxValueExclusive)
                throw new ArgumentException("minValue >= maxValueExclusive");

            double rand01 = base.NextDouble();

            if (((minValue >= 0) && (maxValueExclusive >= 0)) ||
                ((minValue <= 0) && (maxValueExclusive <= 0)))
            {
                // safe to diff
                double diff = maxValueExclusive - minValue;
                res = minValue + diff * rand01;
            }
            else
            {
                // not safe to diff, (max-min) may cause overflow
                res = minValue - minValue * rand01 + maxValueExclusive * rand01;
            }

            Debug.Assert(res >= minValue && res < maxValueExclusive);
            return res;
        }

        /// <summary>
        /// generates random Double value in the given range and up to precision specified
        /// </summary>
        public double NextDouble(double minValue, double maxValueExclusive, int precision)
        {
            // ensure input values are rounded
            if (maxValueExclusive != Math.Round(maxValueExclusive, precision))
            {
                throw new ArgumentException("maxValueExclusive must be rounded to the given precision");
            }
            if (minValue != Math.Round(minValue, precision))
            {
                throw new ArgumentException("minValue must be rounded to the given precision");
            }

            // this call will also ensure that minValue < maxValueExclusive
            double res = NextDouble(minValue, maxValueExclusive);
            res = Math.Round(res, precision);

            if (res >= maxValueExclusive)
            {
                // this can happen after rounding up value which was too close to the max edge
                // just use minValue instead
                res = minValue;
            }

            Debug.Assert(res >= minValue && res < maxValueExclusive);
            return res;
        }

        /// <summary>
        /// generates random real value for SQL server. Note that real is a single precision floating number, mapped to 'float' in .Net.
        /// </summary>
        public float NextReal()
        {
            return (float)NextDouble(float.MinValue, float.MaxValue);
        }

        #endregion

        #region Integral values

        /// <summary>
        /// generates random number in the given range, both min and max values can be in the range of returned values.
        /// </summary>
        public int NextIntInclusive(int minValue = int.MinValue, int maxValueInclusive = int.MaxValue)
        {
            if (minValue == maxValueInclusive)
                return minValue;

            int res;
            if (maxValueInclusive == int.MaxValue)
            {
                if (minValue == int.MinValue)
                {
                    byte[] temp = new byte[4];
                    base.NextBytes(temp);
                    res = BitConverter.ToInt32(temp, 0);
                }
                else
                {
                    res = base.Next(minValue - 1, maxValueInclusive) + 1;
                }
            }
            else // maxValue < int.MaxValue
            {
                res = base.Next(minValue, maxValueInclusive + 1);
            }

            Debug.Assert(res >= minValue && res <= maxValueInclusive);
            return res;
        }

        /// <summary>
        /// random bigint 64-bit value
        /// </summary>
        public long NextBigInt()
        {
            byte[] temp = new byte[8];
            base.NextBytes(temp);
            return BitConverter.ToInt64(temp, 0);
        }

        /// <summary>
        /// random smallint (16-bit) value
        /// </summary>
        public short NextSmallInt()
        {
            return (short)NextIntInclusive(short.MinValue, maxValueInclusive: short.MaxValue);
        }

        /// <summary>
        /// generates a tinyint value (8 bit, unsigned)
        /// </summary>
        public byte NextTinyInt()
        {
            return (byte)NextIntInclusive(0, maxValueInclusive: byte.MaxValue);
        }

        /// <summary>
        /// random bit
        /// </summary>
        public bool NextBit()
        {
            return base.Next() % 2 == 0;
        }

        #endregion

        #region Monetary types

        /// <summary>
        /// generates random SMALLMONEY value
        /// </summary>
        public decimal NextSmallMoney()
        {
            return (decimal)NextDouble(
                minValue: -214748.3648,
                maxValueExclusive: 214748.3647,
                precision: 4);
        }

        /// <summary>
        /// generates random MONEY value
        /// </summary>
        /// <returns></returns>
        public decimal NextMoney()
        {
            return (decimal)NextDouble((double)SqlMoney.MinValue.Value, (double)SqlMoney.MaxValue.Value);
        }

        #endregion

        #region helper methods to create random SQL object names

        /// <summary>
        /// Generates a random name to be used for database object. The length will be no more then (16 + prefix.Length + escapeLeft.Length + escapeRight.Length)
        /// Note this method is not deterministic, it uses Guid.NewGuild to generate unique name to avoid name conflicts between test runs.
        /// </summary>
        private static string GenerateUniqueObjectName(string prefix, string escapeLeft, string escapeRight)
        {
            string uniqueName = string.Format("{0}{1}_{2}_{3}{4}",
                escapeLeft,
                prefix,
                DateTime.Now.Ticks.ToString("X", CultureInfo.InvariantCulture), // up to 8 characters
                Guid.NewGuid().ToString().Substring(0, 6), // take the first 6 characters only
                escapeRight);
            return uniqueName;
        }

        /// <summary>
        /// Generates a random name to be used for SQL Server database object. SQL Server supports long names (up to 128 characters), add extra info for troubleshooting.
        /// Note this method is not deterministic, it uses Guid.NewGuild to generate unique name to avoid name conflicts between test runs.
        /// </summary>
        public static string GenerateUniqueObjectNameForSqlServer(string prefix)
        {
            Process currentProcess = Process.GetCurrentProcess();
            string extendedPrefix = string.Format(
                "{0}_{1}@{2}",
                prefix,
                currentProcess.ProcessName,
                currentProcess.MachineName,
                DateTime.Now.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture));
            string name = GenerateUniqueObjectName(extendedPrefix, "[", "]");
            if (name.Length > 128)
            {
                throw new ArgumentOutOfRangeException("the name is too long - SQL Server names are limited to 128");
            }
            return name;
        }

        /// <summary>
        /// Generates a random temp table name for SQL Server.
        /// Note this method is not deterministic, it uses Guid.NewGuild to generate unique name to avoid name conflicts between test runs.
        /// </summary>
        public static string GenerateUniqueTempTableNameForSqlServer()
        {
            return GenerateUniqueObjectNameForSqlServer("#T");
        }

        #endregion
    }
}
