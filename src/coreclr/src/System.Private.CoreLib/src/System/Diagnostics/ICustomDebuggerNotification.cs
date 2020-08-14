// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
** This interface is implemented by classes that support custom debugger notifications.
**
===========================================================*/

namespace System.Diagnostics
{
    // Defines an interface indicating that a custom debugger notification is requested under specific
    // conditions. Users should implement this interface to be used as an argument to
    // System.Diagnostics.Debugger.CustomNotification.
    internal interface ICustomDebuggerNotification
    {
        // Interface does not need to be marked with the serializable attribute
    }
}
