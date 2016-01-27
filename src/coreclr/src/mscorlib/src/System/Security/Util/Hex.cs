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

        internal static string EncodeHexStringFromInt(byte[] sArray) {
            String result = null;
            if(sArray != null) {
                char[] hexOrder = new char[sArray.Length * 2];
            
                int i = sArray.Length;
                int digit, j=0;
                while (i-- > 0) {
                    digit = (sArray[i] & 0xf0) >> 4;
                    hexOrder[j++] = HexDigit(digit);
                    digit = sArray[i] & 0x0f;
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

        
        public static byte[] DecodeHexString(String hexString)
        {
            if (hexString == null)
                throw new ArgumentNullException( "hexString" );
            Contract.EndContractBlock();
                    
            bool spaceSkippingMode = false;    
                    
            int i = 0;
            int length = hexString.Length;
            
            if ((length >= 2) &&
                (hexString[0] == '0') &&
                ( (hexString[1] == 'x') ||  (hexString[1] == 'X') ))
            {
                length = hexString.Length - 2;
                i = 2;
            }
            
            // Hex strings must always have 2N or (3N - 1) entries.
            
            if (length % 2 != 0 && length % 3 != 2)
            {
                throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidHexFormat" ) );
            }                
                
            byte[] sArray;
                
            if (length >=3 && hexString[i + 2] == ' ')
            {
                spaceSkippingMode = true;
                    
                // Each hex digit will take three spaces, except the first (hence the plus 1).
                sArray = new byte[length / 3 + 1];
            }
            else
            {
                // Each hex digit will take two spaces
                sArray = new byte[length / 2];
            }
                
            int digit;
            int rawdigit;
            for (int j = 0; i < hexString.Length; i += 2, j++) {
                rawdigit = ConvertHexDigit(hexString[i]);
                digit = ConvertHexDigit(hexString[i+1]);
                sArray[j] = (byte) (digit | (rawdigit << 4));
                if (spaceSkippingMode)
                    i++;
            }
            return(sArray);    
        }
    }
}
