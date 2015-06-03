// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace TestLibrary
{
    public static class Generator
    {
        internal static Random s_rand = new Random(-55);

        internal static int Next()
        {
            lock (s_rand) return s_rand.Next();
        }

        internal static double NextDouble()
        {
            lock (s_rand) return s_rand.NextDouble();
        }

        public static int GetInt32()
        {
            int i = Next();
            return i;
        }

        public static short GetInt16()
        {
            short i = Convert.ToInt16(Next() % (1 + short.MaxValue));
            return i;
        }

        public static byte GetByte()
        {
            byte i = Convert.ToByte(Next() % (1 + byte.MaxValue));
            return i;
        }

        // returns a non-negative double between 0.0 and 1.0
        public static double GetDouble()
        {
            double i = NextDouble();
            return i;
        }

        // returns a non-negative float between 0.0 and 1.0
        public static float GetSingle()
        {
            float i = Convert.ToSingle(NextDouble());
            return i;
        }

        // returns a valid char that is a letter
        public static char GetCharLetter()
        {
            return GetCharLetter(true);
        }
        
        public static char GetCharLetter(bool allowsurrogate)
        {
            return GetCharLetter(allowsurrogate, true);
        }

        public static char GetCharLetter(bool allowsurrogate, bool allownoweight)
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

                if (false == allownoweight)
                {
                    throw new NotSupportedException("allownoweight = false is not supported in TestLibrary with FEATURE_NOPINVOKES");
                }

                c = Convert.ToChar(iVal);
                loopCondition = allowsurrogate ? (!char.IsLetter(c)) : (!char.IsLetter(c) || char.IsSurrogate(c));
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

        public static char GetCharNumber()
        {
            return GetCharNumber(true);
        }

        public static char GetCharNumber(bool allownoweight)
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

                if (false == allownoweight)
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

        public static char GetChar()
        {
            return GetChar(true);
        }

        public static char GetChar(bool allowsurrogate)
        {
            return GetChar(allowsurrogate, true);
        }

        public static char GetChar(bool allowsurrogate, bool allownoweight)
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

        public static string GetString(bool validPath, int minLength, int maxLength)
        {
            return GetString(validPath, true, true, minLength, maxLength);
        }

        public static string GetString(bool validPath, bool allowNulls, bool allowNoWeight, int minLength, int maxLength)
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
    }
}
