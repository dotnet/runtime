// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
