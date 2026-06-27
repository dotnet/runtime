// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Synchronizes access to a shared resource or region of code in a multi-threaded
**             program.
**
**
=============================================================================*/

namespace System.Threading
{
    public static partial class Monitor
    {
        #region Object->Lock mapping
        internal static Lock GetLockObject(object obj)
        {
            return ObjectHeader.GetLockObject(obj);
        }
        #endregion
    }
}
