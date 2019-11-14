// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
These HRESULTs are used for mapping managed exceptions to COM error codes
and vice versa through COM Interop.  For background on COM error codes see
https://docs.microsoft.com/en-us/windows/desktop/com/com-error-codes.

FACILITY_URT is defined as 0x13 (0x8013xxxx). The facility range is reserved
for the .NET Framework SDK teams.

Within that range, the following subranges have been allocated for different
feature areas:

0x10yy for Execution Engine
0x11yy for Metadata, TypeLib Export, and CLDB
0x12yy for MetaData Validator
0x13yy for Debugger and Profiler errors
0x14yy for Security
0x15yy for BCL
0x1600 - 0x161F for Reflection
0x1620 - 0x163F for System.IO
0x1640 - 0x165F for Security
0x1660 - 0x16FF for BCL
0x17yy for shim
0x18yy for IL Verifier
0x19yy for .NET Framework
0x1Ayy for .NET Framework
0x1Byy for MetaData Validator
0x30yy for VSA errors

CLR HRESULTs are defined in corerror.h. If you make any modifications to
the range allocations described above, please make sure the corerror.h file
gets updated.
*/

namespace System
{
    using System;

    internal static class HResults
    {
        // Xml
        internal const int Xml = unchecked((int)0x80131940);
        internal const int XmlSchema = unchecked((int)0x80131941);
        internal const int XmlXslt = unchecked((int)0x80131942);
        internal const int XmlXPath = unchecked((int)0x80131943);
    }
}
