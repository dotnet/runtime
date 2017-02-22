// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
/*=============================================================================
**
**
**
** Purpose: This class is a Delegate which defines the start method
**  for starting a thread.  That method must match this delegate.
**
**
=============================================================================*/


using System.Threading;
using System.Runtime.InteropServices;

namespace System.Threading
{
    [ComVisibleAttribute(false)]
    public delegate void ParameterizedThreadStart(object obj);
}
