// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for representing Jupiter GUI layout problems,
** corresponding with E_LAYOUTCYCLE.
**
**
=============================================================================*/
using System;
using System.Runtime.InteropServices;

namespace Windows.UI.Xaml
{
    public class LayoutCycleException : Exception
    {
        public LayoutCycleException()
            : base(SR.LayoutCycle_Default)
        {
            HResult = HResults.E_LAYOUTCYCLE;
        }

        public LayoutCycleException(string message)
            : base(message)
        {
            HResult = HResults.E_LAYOUTCYCLE;
        }

        public LayoutCycleException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.E_LAYOUTCYCLE;
        }
    }
}
