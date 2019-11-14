// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.OleDb
{
    internal enum OleDbPropertyStatus
    {
        Ok = 0,
        NotSupported = 1,
        BadValue = 2,
        BadOption = 3,
        BadColumn = 4,
        NotAllSettable = 5,
        NotSettable = 6,
        NotSet = 7,
        Conflicting = 8,
        NotAvailable = 9,
    }
}
