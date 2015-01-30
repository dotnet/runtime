// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: This enumeration represents how a process can be interrupted with
**          a cancel request from the user, like Control-C and Control-Break.
**          We may eventually add in a small number of other cases.
**
**
=============================================================================*/

using System.Runtime.InteropServices;

namespace System {
    [Serializable]

    public enum ConsoleSpecialKey
    {
        // We realize this is incomplete, and may add values in the future.
        ControlC = 0,
        ControlBreak = 1,
    }
}
