// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Xml
{
    internal abstract class BinHexEncoder
    {
        private const string s_hexDigits = "0123456789ABCDEF";
        private const int CharsChunkSize = 128;

        internal static string Encode(byte[] inArray, int offsetIn, int count)
        {
            if (null == inArray)
            {
                throw new ArgumentNullException("inArray");
            }
            if (0 > offsetIn)
            {
                throw new ArgumentOutOfRangeException("offsetIn");
            }
            if (0 > count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (count > inArray.Length - offsetIn)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            char[] outArray = new char[2 * count];
            int lenOut = Encode(inArray, offsetIn, count, outArray);
            return new String(outArray, 0, lenOut);
        }

        private static int Encode(byte[] inArray, int offsetIn, int count, char[] outArray)
        {
            int curOffsetOut = 0, offsetOut = 0;
            byte b;
            int lengthOut = outArray.Length;

            for (int j = 0; j < count; j++)
            {
                b = inArray[offsetIn++];
                outArray[curOffsetOut++] = s_hexDigits[b >> 4];
                if (curOffsetOut == lengthOut)
                {
                    break;
                }
                outArray[curOffsetOut++] = s_hexDigits[b & 0xF];
                if (curOffsetOut == lengthOut)
                {
                    break;
                }
            }
            return curOffsetOut - offsetOut;
        } // function
    } // class
} // namespace
