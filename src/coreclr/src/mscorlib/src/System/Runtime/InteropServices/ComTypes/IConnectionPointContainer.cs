// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: IConnectionPointContainer interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    [Guid("B196B284-BAB4-101A-B69C-00AA00341D07")]   
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IConnectionPointContainer
    {
        void EnumConnectionPoints(out IEnumConnectionPoints ppEnum);
        void FindConnectionPoint([In] ref Guid riid, out IConnectionPoint ppCP);
    }
}
