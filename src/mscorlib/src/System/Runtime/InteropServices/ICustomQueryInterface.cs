// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: This the interface that be implemented by class that want to 
**          customize the behavior of QueryInterface.
**
**
=============================================================================*/

using System;

namespace System.Runtime.InteropServices
{
    //====================================================================
    // The enum of the return value of IQuerable.GetInterface
    //====================================================================
    public enum CustomQueryInterfaceResult
    {
        Handled = 0,
        NotHandled = 1,
        Failed = 2,
    }

    //====================================================================
    // The interface for customizing IQueryInterface
    //====================================================================
    public interface ICustomQueryInterface
    {
        CustomQueryInterfaceResult GetInterface([In]ref Guid iid, out IntPtr ppv);
    }
}
