// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.Done
{
    /// <summary>
    /// Command completion of which a DONE token reports
    /// </summary>
    public enum TDSDoneTokenCommandType : ushort
    {
        SetOn = 0xb9,
        SetOff = 0xba,
        Shutdown = 0xbf,
        Select = 0xc1,
        SelectInto = 0xc2,
        Insert = 0xc3,
        Delete = 0xc4,
        Update = 0xc5,
        TableCreate = 0xc6,
        TableDestroy = 0xc7,
        IndxCreate = 0xc8,
        IndexDestroy = 0xc9,
        Goto = 0xca,
        DatabaseCreate = 0xcb,
        DatabaseDestrot = 0xcc,
        ViewCreate = 0xcf,
        ViewDestroy = 0xd0,
        Set = 0xf9,
        Done = 0xfd,
        DoneProc = 0xfe,
        DoneInProc = 0xff
    }
}
