// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
