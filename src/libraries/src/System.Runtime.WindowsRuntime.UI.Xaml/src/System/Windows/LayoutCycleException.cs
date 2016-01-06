// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public LayoutCycleException(String message)
            : base(message)
        {
            HResult = HResults.E_LAYOUTCYCLE;
        }

        public LayoutCycleException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.E_LAYOUTCYCLE;
        }
    }
}
