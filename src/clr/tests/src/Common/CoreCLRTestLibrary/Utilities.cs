// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

namespace TestLibrary
{
    public static partial class Utilities
    {

        static volatile bool verbose;
        static volatile bool verboseSet = false;
        const char HIGH_SURROGATE_START = '\ud800';
        const char HIGH_SURROGATE_END = '\udbff';
        const char LOW_SURROGATE_START = '\udc00';
        const char LOW_SURROGATE_END = '\udfff';

        static string sTestDirectory = string.Empty;

        public static string TestDirectory
        {
            get
            {
                return sTestDirectory;
            }
            set
            {
                sTestDirectory = value;
            }
        }

        public static bool Verbose
        {
            get
            {
                if (!verboseSet)
                {
                    verboseSet = true;
                    verbose = false;
                }
                return (bool)verbose;
            }
            set
            {
                verbose = value;
            }
        }

        public static bool IsBigEndian
        {
            get
            {
                return EndianessChecker.IsBigEndian();
            }
        }

        public static bool IsLittleEndian
        {
            get
            {
                return EndianessChecker.IsLittleEndian();
            }
        }

        public static bool IsWindows
        {
            [SecuritySafeCritical]
            get
            {
                return Path.DirectorySeparatorChar == '\\';
            }
        }

        public static bool IsVista
        {
            get
            {
                return false;
            }
        }

        public static bool IsVistaOrLater
        {
            get
            {
                return true;
            }
        }

        public static bool IsWin2K
        {
            get
            {
                return false;
            }
        }

        public static bool IsWin7
        {
            get
            {
                return false;
            }
        }

        public static bool IsWin7OrLater
        {
            get
            {
                return true; //Win8P is always win8+
            }
        }

        public static bool PlatformSpecificComparer(object Actual, object XPExpected, object VistaExpected, object MACPPCExpected, object MACX86Expected)
        {
            if (!IsWindows) return (Actual.Equals(IsBigEndian ? MACPPCExpected : MACX86Expected));
            if (IsVista) return (Actual.Equals(VistaExpected));
            return Actual.Equals(XPExpected);
        }

        // return whether or not the OS is a 64 bit OS
        public static bool Is64
        {
            get
            {
                return (IntPtr.Size == 8);
            }
        }

        public static string ByteArrayToString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2"));
                sb.Append(", ");
            }
            if (bytes.Length > 0)
                sb.Remove(sb.Length - 2, 2);

            return sb.ToString();
        }

        public static byte[] TrimBytes(byte[] input)
        {
            int outputLength = input.Length;
            int pos = 0;
            while (pos < input.Length && input[pos] == 0) { outputLength--; pos++; }
            int newStart = pos;
            pos = input.Length - 1;
            while (input[pos] == 0) { outputLength--; pos--; }

            byte[] output = new byte[outputLength];
            for (int i = 0; i < outputLength; i++)
            {
                output[i] = input[i + newStart];
            }

            return output;
        }

        public static bool CompareBytes(byte[] arr1, byte[] arr2)
        {
            if (arr1 == null) return (arr2 == null);
            if (arr2 == null) return false;

            if (arr1.Length != arr2.Length) return false;

            for (int i = 0; i < arr1.Length; i++) if (arr1[i] != arr2[i]) return false;

            return true;
        }

        public static bool CompareChars(char[] arr1, char[] arr2)
        {
            if (arr1 == null) return (arr2 == null);
            if (arr2 == null) return false;

            if (arr1.Length != arr2.Length) return false;

            for (int i = 0; i < arr1.Length; i++) if (arr1[i] != arr2[i]) return false;

            return true;
        }

        // Given a string, display the unicode characters in hex format, optionally displaying each 
        // characters unicode category
        public static string FormatHexStringFromUnicodeString(string string1, bool includeUnicodeCategory)
        {
            string returnString = "";
            if (null == string1)
            {
                return null;
            }

            foreach (char x in string1)
            {
                string tempString = FormatHexStringFromUnicodeChar(x, includeUnicodeCategory);
                if (!returnString.Equals("") && !includeUnicodeCategory)
                {
                    returnString = returnString + " " + tempString;
                }
                else
                {
                    returnString += tempString;
                }
            }

            return returnString;
        }

        // Given a character, display its unicode value in hex format. ProjectN doens't support 
        // unicode category as a Property on Char.
        public static string FormatHexStringFromUnicodeChar(char char1, bool includeUnicodeCategory)
        {
            if (includeUnicodeCategory)
                throw new Exception("Win8P does not support Char.UnicodeCategory");

            return ((int)char1).ToString("X4");
        }

        public static bool IsHighSurrogate(char c)
        {
            return ((c >= HIGH_SURROGATE_START) && (c <= HIGH_SURROGATE_END));
        }
        public static bool IsLowSurrogate(char c)
        {
            return ((c >= LOW_SURROGATE_START) && (c <= LOW_SURROGATE_END));
        }

        public static bool CompareCurrentCulture(String culture)
        {
            return (String.Compare(System.Globalization.CultureInfo.CurrentCulture.Name, culture, StringComparison.CurrentCultureIgnoreCase) == 0);
        }
        public static CultureInfo CurrentCulture
        {
            get { return System.Globalization.CultureInfo.CurrentCulture; }
            set
            {
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = value;
            }
        }
    }
}

public static class HelperExtensions {
   public static bool IsAssignableFrom(this Type t1, Type t2) {
      return t1.GetTypeInfo().IsAssignableFrom(t2.GetTypeInfo());
   }

   public static String ToLongDateString(this DateTime dt)
   {
       return String.Format("{0:D}", dt);
   }

   public static String ToLongTimeString(this DateTime dt)
   {
       return String.Format("{0:T}", dt);
   }


   public static String ToShortDateString(this DateTime dt)
   {
       return String.Format("{0:d}", dt);
   }

   public static String ToShortTimeString(this DateTime dt)
   {
       return String.Format("{0:t}", dt);
   }

   public static UnicodeCategory GetUnicodeCategory(this Char c)
   {
       return CharUnicodeInfo.GetUnicodeCategory(c);
   }
}
