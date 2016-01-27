// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: defining the different flavor's assembly version compatibility
**
**
===========================================================*/
namespace System.Configuration.Assemblies {
    
    using System;
     [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum AssemblyVersionCompatibility
    {
        SameMachine         = 1,
        SameProcess         = 2,
        SameDomain          = 3,
    }
}
