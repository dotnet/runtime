// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
