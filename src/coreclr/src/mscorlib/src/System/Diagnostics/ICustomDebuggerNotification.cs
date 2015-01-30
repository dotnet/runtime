// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
** This interface is implemented by classes that support custom debugger notifications.
**
===========================================================*/
namespace System.Diagnostics {
    
    using System;
    // Defines an interface indicating that a custom debugger notification is requested under specific 
    // conditions. Users should implement this interface to be used as an argument to 
    // System.Diagnostics.Debugger.CustomNotification.  
    internal interface ICustomDebuggerNotification
    {
        // Interface does not need to be marked with the serializable attribute
    }
}
