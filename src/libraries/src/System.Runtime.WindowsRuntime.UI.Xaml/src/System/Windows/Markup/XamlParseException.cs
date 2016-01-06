// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Exception class for representing Jupiter XAML parsing failures,
** corresponding with E_XAMLPARSEFAILED.
**
**
=============================================================================*/

using System;
using System.Runtime.InteropServices;

namespace Windows.UI.Xaml.Markup
{
    public class XamlParseException : Exception
    {
        public XamlParseException()
            : base(SR.XamlParse_Default)
        {
            HResult = HResults.E_XAMLPARSEFAILED;
        }

        public XamlParseException(String message)
            : base(message)
        {
            HResult = HResults.E_XAMLPARSEFAILED;
        }

        public XamlParseException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.E_XAMLPARSEFAILED;
        }
    }
}
