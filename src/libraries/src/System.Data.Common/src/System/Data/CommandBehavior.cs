// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    [Flags]
    public enum CommandBehavior
    {
        Default = 0,  // with data, multiple results, may affect database
        SingleResult = 1,  // with data, force single result, may affect database
        SchemaOnly = 2,  // column info, no data, no effect on database
        KeyInfo = 4,  // column info + primary key information (if available)
        SingleRow = 8, // data, hint single row and single result, may affect database - doesn't apply to child(chapter) results
        SequentialAccess = 0x10,
        CloseConnection = 0x20,
    }
}
