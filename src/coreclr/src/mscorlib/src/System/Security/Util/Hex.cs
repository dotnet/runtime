// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 *
 * Operations to convert to and from Hex
 *
 */

namespace System.Security.Util
{
    using System;
    using System.Security;
    using System.Diagnostics.Contracts;
    internal static class Hex
    {
        // converts number to hex digit. Does not do any range checks.
        static char HexDigit(int num) {
            return (char)((num < 10) ? (num + '0') : (num + ('A' - 10)));
        }
        
        public  static String EncodeHexString(byte[] sArray) 
        {
            String result = null;
    
            if(sArray != null) {
                char[] hexOrder = new char[sArray.Length * 2];
            
                int digit;
                for(int i = 0, j = 0; i < sArray.Length; i++) {
                    digit = (int)((sArray[i] & 0xf0) >> 4);
                    hexOrder[j++] = HexDigit(digit);
                    digit = (int)(sArray[i] & 0x0f);
                    hexOrder[j++] = HexDigit(digit);
                }
                result = new String(hexOrder);
            }
            return result;
        }
        
        public static int ConvertHexDigit(Char val)
        {
            if (val <= '9' && val >= '0')
                return (val - '0');
            else if (val >= 'a' && val <= 'f')
                return ((val - 'a') + 10);
            else if (val >= 'A' && val <= 'F')
                return ((val - 'A') + 10);
            else
                throw new ArgumentException( Environment.GetResourceString( "ArgumentOutOfRange_Index" ) );  
        }
    }
}
