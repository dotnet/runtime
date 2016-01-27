// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

using System;
using System.Globalization;
using System.Runtime.InteropServices;

#if !FEATURE_SLJ_PROJECTION_COMPAT
#pragma warning disable 436   // Redefining types from Windows.Foundation
#endif // !FEATURE_SLJ_PROJECTION_COMPAT

#if FEATURE_SLJ_PROJECTION_COMPAT
namespace System.Windows
#else // !FEATURE_SLJ_PROJECTION_COMPAT


namespace Windows.Foundation
#endif // FEATURE_SLJ_PROJECTION_COMPAT
{
    //
    // Note that this type is owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    internal static class TokenizerHelper
    {
        internal static char GetNumericListSeparator(IFormatProvider provider)
        {
            char numericSeparator = ',';

            // Get the NumberFormatInfo out of the provider, if possible
            // If the IFormatProvider doesn't not contain a NumberFormatInfo, then
            // this method returns the current culture's NumberFormatInfo.
            NumberFormatInfo numberFormat = NumberFormatInfo.GetInstance(provider);

            // Is the decimal separator is the same as the list separator?
            // If so, we use the ";".
            if ((numberFormat.NumberDecimalSeparator.Length > 0) && (numericSeparator == numberFormat.NumberDecimalSeparator[0]))
            {
                numericSeparator = ';';
            }

            return numericSeparator;
        }
    }
}

#if !FEATURE_SLJ_PROJECTION_COMPAT
#pragma warning restore 436
#endif // !FEATURE_SLJ_PROJECTION_COMPAT

