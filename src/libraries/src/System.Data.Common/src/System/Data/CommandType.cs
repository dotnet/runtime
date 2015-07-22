// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

namespace System.Data
{
    public enum CommandType
    {
        Text = 0x1,
        //Table           = 0x2,
        StoredProcedure = 0x4,
        //File            = 0x100,
        TableDirect = 0x200,
    }
}
