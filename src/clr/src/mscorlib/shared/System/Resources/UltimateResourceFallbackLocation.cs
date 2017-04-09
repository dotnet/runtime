// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
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

namespace System.Resources
{
    public enum UltimateResourceFallbackLocation
    {
        MainAssembly,
        Satellite
    }
}
