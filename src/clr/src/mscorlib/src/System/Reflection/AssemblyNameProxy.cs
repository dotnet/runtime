// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** 
** 
** 
**
**
** Purpose: Remotable version the AssemblyName
**
**
===========================================================*/
namespace System.Reflection {
    using System;
    using System.Runtime.Versioning;

    [System.Runtime.InteropServices.ComVisible(true)]
    public class AssemblyNameProxy : MarshalByRefObject 
    {
        public AssemblyName GetAssemblyName(String assemblyFile)
        {
            return AssemblyName.GetAssemblyName(assemblyFile);
        }
    }
}
