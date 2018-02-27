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

        public static bool IsWindows
        {
            [SecuritySafeCritical]
            get
            {
                return Path.DirectorySeparatorChar == '\\';
            }
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

        public static bool CompareBytes(byte[] arr1, byte[] arr2)
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
