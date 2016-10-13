// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    internal static class HResults
    {
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

        internal const int SqlType = unchecked((int)0x80131930);
        internal const int SqlNullValue = unchecked((int)0x80131931);
        internal const int SqlTruncate = unchecked((int)0x80131932);
        internal const int DBConcurrency = unchecked((int)0x80131935);
        internal const int OperationAborted = unchecked((int)0x80131936);
    }
}
