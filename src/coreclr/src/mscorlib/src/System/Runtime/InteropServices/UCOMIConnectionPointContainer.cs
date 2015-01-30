// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: UCOMIConnectionPointContainer interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices
{
    using System;

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.IConnectionPointContainer instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Guid("B196B284-BAB4-101A-B69C-00AA00341D07")]   
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface UCOMIConnectionPointContainer
    {
        void EnumConnectionPoints(out UCOMIEnumConnectionPoints ppEnum);        
        void FindConnectionPoint(ref Guid riid, out UCOMIConnectionPoint ppCP);
    }
}
