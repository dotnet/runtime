// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: This enumeration represents the keys Alt, Shift, and Control 
**          which modify the meaning of another key when pressed.
**
**
=============================================================================*/

namespace System {
[Serializable]
[Flags]
    public enum ConsoleModifiers
    {
        Alt = 1,
        Shift = 2,
        Control = 4
    }
}
