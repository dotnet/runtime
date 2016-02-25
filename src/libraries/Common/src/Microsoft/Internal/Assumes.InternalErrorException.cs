// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Internal
{
    internal partial class Assumes
    {
        // The exception that is thrown when an internal assumption failed.
        [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
        private sealed class InternalErrorException : Exception
        {
            public InternalErrorException(string message)
                : base(string.Format(CultureInfo.CurrentCulture, CommonStrings.Diagnostic_InternalExceptionMessage, message))
            {
            }
        }
    }
}
