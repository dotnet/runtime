// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Globalization
{
    sealed partial class IdnMapping
    {
        // TODO: Rather than having a Unix-specific implementation, we could also consider having a purely
        // managed implementation as is available in mscorlib for pre-Windows 8.  This would require including 
        // the relevant data tables.

        private string GetAsciiCore(string unicode)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        private string GetUnicodeCore(string ascii)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }
    }
}
