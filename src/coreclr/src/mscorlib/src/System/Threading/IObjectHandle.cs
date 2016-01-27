// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** IObjectHandle defines the interface for unwrapping objects.
** Objects that are marshal by value object can be returned through 
** an indirection allowing the caller to control when the
** object is loaded into their domain. The caller can unwrap
** the object from the indirection through this interface.
**
** 
===========================================================*/
namespace System.Runtime.Remoting {

    using System;
    using System.Runtime.InteropServices;

    [ InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
      GuidAttribute("C460E2B4-E199-412a-8456-84DC3E4838C3") ]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IObjectHandle {
        // Unwrap the object. Implementers of this interface
        // typically have an indirect referece to another object.
        Object Unwrap();
    }
}

