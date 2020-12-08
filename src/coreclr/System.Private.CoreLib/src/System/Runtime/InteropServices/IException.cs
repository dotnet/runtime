// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Interface: _Exception
**
**
** Purpose: COM backwards compatibility with v1 Exception
**        object layout.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;

    [Guid("b36b5c63-42ef-38bc-a07e-0b34c98f164a")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    [CLSCompliant(false)]
    internal interface _Exception
    {
        //
        // This method is intentionally included in CoreCLR to make Exception.get_InnerException "newslot virtual final".
        // Some phone apps include MEF from desktop Silverlight. MEF's ComposablePartException depends on implicit interface
        // implementations of get_InnerException to be provided by the base class. It works only if Exception.get_InnerException
        // is virtual.
        //
        Exception? InnerException {
            get;
        }
   }
}
