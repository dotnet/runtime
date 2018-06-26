// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public XamlParseException(string message)
            : base(message)
        {
            HResult = HResults.E_XAMLPARSEFAILED;
        }

        public XamlParseException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.E_XAMLPARSEFAILED;
        }
    }
}
