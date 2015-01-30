// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** 
** 
**
** 
**
** Purpose: Tells the ResourceManager where to find the
**          ultimate fallback resources for your assembly.
**
**
===========================================================*/

using System;

namespace System.Resources {

[Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum UltimateResourceFallbackLocation
    {
        MainAssembly,
        Satellite
    }
}
