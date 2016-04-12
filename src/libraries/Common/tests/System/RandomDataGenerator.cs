// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System
{
    public class RandomDataGenerator
    {
        private Random _rand = new Random();
        private int? _seed = null;

        public int? Seed
        {
            get { return _seed; }
            set
            {
                if (!_seed.HasValue && value.HasValue)
                {
                    _seed = value;
                    _rand = new Random(value.Value);
                }
            }
        }

        // returns a byte array of random data
        public void GetBytes(int newSeed, byte[] buffer)
        {
            Seed = newSeed;
            GetBytes(buffer);
        }

        public void GetBytes(byte[] buffer)
        {
            _rand.NextBytes(buffer);
        }

        // returns a non-negative Int64 between 0 and Int64.MaxValue
        public long GetInt64(int newSeed)
        {
            Seed = newSeed;
            return GetInt64();
        }

        public long GetInt64()
        {
            byte[] buffer = new byte[8];
            GetBytes(buffer);
            long result = BitConverter.ToInt64(buffer, 0);
            return result != long.MinValue ? Math.Abs(result) : long.MaxValue;
        }

        // returns a non-negative Int32 between 0 and Int32.MaxValue
        public int GetInt32(int new_seed)
        {
            Seed = new_seed;
            return GetInt32();
        }

        public int GetInt32()
        {
            return _rand.Next();
        }

        // returns a non-negative Int16 between 0 and Int16.MaxValue
        public short GetInt16(int new_seed)
        {
            Seed = new_seed;
            return GetInt16();
        }

        public short GetInt16()
        {
            return (short)_rand.Next(1 + short.MaxValue);
        }

        // returns a non-negative Byte between 0 and Byte.MaxValue
        public byte GetByte(int new_seed)
        {
            Seed = new_seed;
            return GetByte();
        }

        public byte GetByte()
        {
            return (byte)_rand.Next(1 + byte.MaxValue);
        }

        // returns a non-negative Double between 0.0 and 1.0
        public double GetDouble(int new_seed)
        {
            Seed = new_seed;
            return GetDouble();
        }

        public double GetDouble()
        {
            return _rand.NextDouble();
        }

        // returns a non-negative Single between 0.0 and 1.0
        public float GetSingle(int newSeed)
        {
            Seed = newSeed;
            return GetSingle();
        }
        public float GetSingle()
        {
            return (float)_rand.NextDouble();
        }

        // returns a valid char that is a letter
        public char GetCharLetter(int newSeed)
        {
            Seed = newSeed;
            return GetCharLetter();
        }
        public char GetCharLetter()
        {
            return GetCharLetter(allowSurrogate: true);
        }

        // returns a valid char that is a letter
        // if allowsurrogate is true then surrogates are valid return values
        public char GetCharLetter(int newSeed, bool allowSurrogate)
        {
            Seed = newSeed;
            return GetCharLetter(allowSurrogate);
        }

        public char GetCharLetter(bool allowSurrogate)
        {
            return GetCharLetter(allowSurrogate, allowNoWeight: true);
        }

        // returns a valid char that is a letter
        // if allowsurrogate is true then surrogates are valid return values
        // if allownoweight is true, then no-weight characters are valid return values
        public char GetCharLetter(int newSeed, bool allowSurrogate, bool allowNoWeight)
        {
            Seed = newSeed;
            return GetCharLetter(allowSurrogate, allowNoWeight);
        }

        public char GetCharLetter(bool allowSurrogate, bool allowNoWeight)
        {
            short iVal;
            char c = 'a';
            int counter;
            bool loopCondition = true;

            // attempt to randomly find a letter
            counter = 100;
            do
            {
                counter--;
                iVal = GetInt16();

                if (false == allowNoWeight)
                {
                    throw new NotSupportedException("allownoweight = false is not supported in TestLibrary with FEATURE_NOPINVOKES");
                }

                c = Convert.ToChar(iVal);
                loopCondition = allowSurrogate ? (!char.IsLetter(c)) : (!char.IsLetter(c) || char.IsSurrogate(c));
            }
            while (loopCondition && 0 < counter);

            if (!char.IsLetter(c))
            {
                // we tried and failed to get a letter
                //  Grab an ASCII letter
                c = Convert.ToChar(GetInt16() % 26 + 'A');
            }

            return c;
        }

        // returns a valid char that is a number
        public char GetCharNumber(int newSeed)
        {
            Seed = newSeed;
            return GetCharNumber();
        }

        public char GetCharNumber()
        {
            return GetCharNumber(true);
        }

        // returns a valid char that is a number
        // if allownoweight is true, then no-weight characters are valid return values
        public char GetCharNumber(int newSeed, bool allowNoWeight)
        {
            Seed = newSeed;
            return GetCharNumber(allowNoWeight);
        }

        public char GetCharNumber(bool allowNoWeight)
        {
            char c = '0';
            int counter;
            short iVal;
            bool loopCondition = true;

            // attempt to randomly find a number
            counter = 100;
            do
            {
                counter--;
                iVal = GetInt16();

                if (false == allowNoWeight)
                {
                    throw new InvalidOperationException("allownoweight = false is not supported in TestLibrary with FEATURE_NOPINVOKES");
                }

                c = Convert.ToChar(iVal);
                loopCondition = !char.IsNumber(c);
            }
            while (loopCondition && 0 < counter);

            if (!char.IsNumber(c))
            {
                // we tried and failed to get a letter
                //  Grab an ASCII number
                c = Convert.ToChar(GetInt16() % 10 + '0');
            }

            return c;
        }

        // returns a valid char
        public char GetChar(int newSeed)
        {
            Seed = newSeed;
            return GetChar();
        }

        public char GetChar()
        {
            return GetChar(allowSurrogate: true);
        }

        // returns a valid char
        // if allowsurrogate is true then surrogates are valid return values
        public char GetChar(int newSeed, bool allowSurrogate)
        {
            Seed = newSeed;
            return GetChar(allowSurrogate);
        }

        public char GetChar(bool allowSurrogate)
        {
            return GetChar(allowSurrogate, allowNoWeight: true);
        }

        // returns a valid char
        // if allowsurrogate is true then surrogates are valid return values
        // if allownoweight characters then noweight characters are valid return values
        public char GetChar(int newSeed, bool allowSurrogate, bool allowNoWeight)
        {
            Seed = newSeed;
            return GetChar(allowSurrogate, allowNoWeight);
        }

        public char GetChar(bool allowSurrogate, bool allowNoWeight)
        {
            short iVal = GetInt16();

            char c = (char)(iVal);

            if (!char.IsLetter(c))
            {
                // we tried and failed to get a letter
                // Just grab an ASCII letter
                c = (char)(GetInt16() % 26 + 'A');
            }

            return c;
        }

        // returns a  string.  If "validPath" is set, only valid path characters
        //  will be included
        public string GetString(int newSeed, bool validPath, int minLength, int maxLength)
        {
            Seed = newSeed;
            return GetString(validPath, minLength, maxLength);
        }

        public string GetString(bool validPath, int minLength, int maxLength)
        {
            return GetString(validPath, true, true, minLength, maxLength);
        }

        public string GetString(int newSeed, bool validPath, bool allowNulls, int minLength, int maxLength)
        {
            Seed = newSeed;
            return GetString(validPath, allowNulls, minLength, maxLength);
        }

        public string GetString(bool validPath, bool allowNulls, int minLength, int maxLength)
        {
            return GetString(validPath, allowNulls, true, minLength, maxLength);
        }

        public string GetString(int newSeed, bool validPath, bool allowNulls, bool allowNoWeight, int minLength, int maxLength)
        {
            Seed = newSeed;
            return GetString(validPath, allowNulls, allowNoWeight, minLength, maxLength);
        }

        public string GetString(bool validPath, bool allowNulls, bool allowNoWeight, int minLength, int maxLength)
        {
            StringBuilder sVal = new StringBuilder();
            char c;
            int length;

            if (0 == minLength && 0 == maxLength) return string.Empty;
            if (minLength > maxLength) return null;

            length = minLength;

            if (minLength != maxLength)
            {
                length = (GetInt32() % (maxLength - minLength)) + minLength;
            }

            for (int i = 0; length > i; i++)
            {
                if (validPath)
                {
                    if (0 == (GetByte() % 2))
                    {
                        c = GetCharLetter(true, allowNoWeight);
                    }
                    else
                    {
                        c = GetCharNumber(allowNoWeight);
                    }
                }
                else if (!allowNulls)
                {
                    do
                    {
                        c = GetChar(true, allowNoWeight);
                    } while (c == '\u0000');
                }
                else
                {
                    c = GetChar(true, allowNoWeight);
                }

                sVal.Append(c);
            }
            string s = sVal.ToString();

            return s;
        }

        public string[] GetStrings(int newSeed, bool validPath, int minLength, int maxLength)
        {
            Seed = newSeed;
            return GetStrings(validPath, minLength, maxLength);
        }

        public string[] GetStrings(bool validPath, int minLength, int maxLength)
        {
            string validString;
            const char c_LATIN_A = '\u0100';
            const char c_LOWER_A = 'a';
            const char c_UPPER_A = 'A';
            const char c_ZERO_WEIGHT = '\uFEFF';
            const char c_DOUBLE_WIDE_A = '\uFF21';
            const string c_SURROGATE_UPPER = "\uD801\uDC00";
            const string c_SURROGATE_LOWER = "\uD801\uDC28";
            const char c_LOWER_SIGMA1 = (char)0x03C2;
            const char c_LOWER_SIGMA2 = (char)0x03C3;
            const char c_UPPER_SIGMA = (char)0x03A3;
            const char c_SPACE = ' ';

            if (2 >= minLength && 2 >= maxLength || minLength > maxLength)
                return null;

            validString = GetString(validPath, minLength - 1, maxLength - 1);

            string[] retStrings = new string[12];
            retStrings[0] = GetString(validPath, minLength, maxLength);
            retStrings[1] = validString + c_LATIN_A;
            retStrings[2] = validString + c_LOWER_A;
            retStrings[3] = validString + c_UPPER_A;
            retStrings[4] = validString + c_ZERO_WEIGHT;
            retStrings[5] = validString + c_DOUBLE_WIDE_A;
            retStrings[6] = GetString(validPath, minLength - 2, maxLength - 2) + c_SURROGATE_UPPER;
            retStrings[7] = GetString(validPath, minLength - 2, maxLength - 2) + c_SURROGATE_LOWER;
            retStrings[8] = validString + c_LOWER_SIGMA1;
            retStrings[9] = validString + c_LOWER_SIGMA2;
            retStrings[10] = validString + c_UPPER_SIGMA;
            retStrings[11] = validString + c_SPACE;
            return retStrings;
        }

        public DateTime GetDateTime(int newSeed) => new DateTime(GetInt64(newSeed) % (DateTime.MaxValue.Ticks + 1));

        public static void VerifyRandomDistribution(byte[] random)
        {
            // Better tests for randomness are available.  For now just use a simple
            // check that compares the number of 0s and 1s in the bits.
            VerifyNeutralParity(random);
        }

        private static void VerifyNeutralParity(byte[] random)
        {
            int zeroCount = 0, oneCount = 0;

            for (int i = 0; i < random.Length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (((random[i] >> j) & 1) == 1)
                    {
                        oneCount++;
                    }
                    else
                    {
                        zeroCount++;
                    }
                }
            }

            // Over the long run there should be about as many 1s as 0s.
            // This isn't a guarantee, just a statistical observation.
            // Allow a 7% tolerance band before considering it to have gotten out of hand.
            double bitDifference = Math.Abs(zeroCount - oneCount) / (double)(zeroCount + oneCount);
            const double AllowedTolerance = 0.07;
            if (bitDifference > AllowedTolerance)
            {
                throw new InvalidOperationException("Expected bitDifference < " + AllowedTolerance + ", got " + bitDifference + ".");
            }
        }
    }
}
