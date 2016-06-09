// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
These HRESULTs are used for mapping managed exceptions to COM error codes
and vice versa through COM Interop.  For background on COM error codes see
http://msdn.microsoft.com/library/default.asp?url=/library/en-us/com/error_9td2.asp.

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
        internal const int Configuration = unchecked((int)0x80131902);

        // Xml
        internal const int Xml = unchecked((int)0x80131940);
        internal const int XmlSchema = unchecked((int)0x80131941);
        internal const int XmlXslt = unchecked((int)0x80131942);
        internal const int XmlXPath = unchecked((int)0x80131943);

        // DataSet
        internal const int Data = unchecked((int)0x80131920);
        internal const int DataDeletedRowInaccessible = unchecked((int)0x80131921);
        internal const int DataDuplicateName = unchecked((int)0x80131922);
        internal const int DataInRowChangingEvent = unchecked((int)0x80131923);
        internal const int DataInvalidConstraint = unchecked((int)0x80131924);
        internal const int DataMissingPrimaryKey = unchecked((int)0x80131925);
        internal const int DataNoNullAllowed = unchecked((int)0x80131926);
        internal const int DataReadOnly = unchecked((int)0x80131927);
        internal const int DataRowNotInTable = unchecked((int)0x80131928);
        internal const int DataVersionNotFound = unchecked((int)0x80131929);
        internal const int DataConstraint = unchecked((int)0x8013192A);
        internal const int StrongTyping = unchecked((int)0x8013192B);

        // Managed Providers
        internal const int SqlType = unchecked((int)0x80131930);
        internal const int SqlNullValue = unchecked((int)0x80131931);
        internal const int SqlTruncate = unchecked((int)0x80131932);
        internal const int AdapterMapping = unchecked((int)0x80131933);
        internal const int DataAdapter = unchecked((int)0x80131934);
        internal const int DBConcurrency = unchecked((int)0x80131935);
        internal const int OperationAborted = unchecked((int)0x80131936);
        internal const int InvalidUdt = unchecked((int)0x80131937);
        internal const int Metadata = unchecked((int)0x80131939);
        internal const int InvalidQuery = unchecked((int)0x8013193A);
        internal const int CommandCompilation = unchecked((int)0x8013193B);
        internal const int CommandExecution = unchecked((int)0x8013193C);


        internal const int SqlException = unchecked((int)0x80131904); // System.Data.SqlClient.SqlClientException
        internal const int OdbcException = unchecked((int)0x80131937);   // System.Data.Odbc.OdbcException
        internal const int OracleException = unchecked((int)0x80131938); // System.Data.OracleClient.OracleException
        internal const int ConnectionPlanException = unchecked((int)0x8013193d); // System.Data.SqlClient.ConnectionPlanException

        // Configuration encryption
        internal const int NteBadKeySet = unchecked((int)0x80090016);

        // Win32
        internal const int Win32AccessDenied = unchecked((int)0x80070005);
        internal const int Win32InvalidHandle = unchecked((int)0x80070006);


#if !FEATURE_PAL
        internal const int License = unchecked((int)0x80131901);
        internal const int InternalBufferOverflow = unchecked((int)0x80131905);
        internal const int ServiceControllerTimeout = unchecked((int)0x80131906);
        internal const int Install = unchecked((int)0x80131907);

        // Win32
        internal const int EFail = unchecked((int)0x80004005);
#endif
    }
}
