// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Exception class for representing Jupiter failures of some kind.
** Corresponds with E_ELEMENTNOTENABLED.
**
**
=============================================================================*/
using System;
using System.Runtime.InteropServices;

namespace Windows.UI.Xaml.Automation
{
    public class ElementNotEnabledException : Exception
    {
        public ElementNotEnabledException()
            : base(SR.ElementNotEnabled_Default)
        {
            HResult = HResults.E_ELEMENTNOTENABLED;
        }

        public ElementNotEnabledException(String message)
            : base(message)
        {
            HResult = HResults.E_ELEMENTNOTENABLED;
        }

        public ElementNotEnabledException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.E_ELEMENTNOTENABLED;
        }
    }
}
