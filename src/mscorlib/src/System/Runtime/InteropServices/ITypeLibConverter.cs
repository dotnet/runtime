// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Methods used to convert a TypeLib to metadata and vice versa.
**
**
=============================================================================*/

// ***************************************************************************
// *** Note: The following definitions must remain synchronized with the IDL
// ***       in src/inc/TlbImpExp.idl.
// ***************************************************************************

namespace System.Runtime.InteropServices {
    
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum ImporterEventKind
    {
        NOTIF_TYPECONVERTED = 0,
        NOTIF_CONVERTWARNING = 1,
        ERROR_REFTOINVALIDTYPELIB = 2,
    }

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum ExporterEventKind
    {
        NOTIF_TYPECONVERTED = 0,
        NOTIF_CONVERTWARNING = 1,
        ERROR_REFTOINVALIDASSEMBLY = 2
    }
}
