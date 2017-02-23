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

using System;

namespace System.Configuration.Assemblies
{
    [Serializable]
    public enum AssemblyVersionCompatibility
    {
        SameMachine = 1,
        SameProcess = 2,
        SameDomain = 3,
    }
}
