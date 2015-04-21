// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Globalization
{
    sealed partial class IdnMapping
    {
        // TODO: Rather than having a Unix-specific implementation, we could also consider having a purely
        // managed implementation as is available in mscorlib for pre-Windows 8.  This would require including 
        // the relevant data tables.

        // NOTE: The "implementation" here actually fails for IDN'd names, but will pass ASCII stuff along just fine
        // that's by design, since this code is hit in some common paths but the data is usually ASCII and we can do
        // the right thing there.

        private string GetAsciiCore(string unicode)
        {
            for (int i = 0; i < unicode.Length; i++)
            {
                if (unicode[i] > 0x7F)
                {
                    throw NotImplemented.ByDesign;
                }
            }

            return unicode;
        }

        private string GetUnicodeCore(string ascii)
        {
            if (ascii.Contains("xn--"))
            {
                throw NotImplemented.ByDesign;
            }

            return ascii;
        }
    }
}
