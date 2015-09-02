// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class SspiCli
    {
        internal enum KERB_LOGON_SUBMIT_TYPE : int
        {
            KerbS4ULogon = 12,
        }
    }
}
