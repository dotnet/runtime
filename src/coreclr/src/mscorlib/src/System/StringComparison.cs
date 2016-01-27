// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Enum:  StringComparison
**
**
** Purpose: A mechanism to expose a simplified infrastructure for 
**          Comparing strings. This enum lets you choose of the custom 
**          implementations provided by the runtime for the user.
**
** 
===========================================================*/
namespace System{
    
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum StringComparison {
        CurrentCulture = 0,
        CurrentCultureIgnoreCase = 1,
        InvariantCulture = 2,
        InvariantCultureIgnoreCase = 3,
        Ordinal = 4,
        OrdinalIgnoreCase = 5,
    }
}
