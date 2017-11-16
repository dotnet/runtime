// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.ServiceModel
{
    /// <summary>
    /// This is the Management utility class.
    /// Adding Xml
    /// </summary>
    internal static class DiagnosticUtility
    {
        public static bool ShouldTraceInformation { get; internal set; }

        internal static class ExceptionUtility
        {
            internal static ArgumentException ThrowHelperArgument(string message)
            {
                return (ArgumentException)ThrowHelperError(new ArgumentException(message));
            }

            internal static ArgumentException ThrowHelperArgument(string paramName, string message)
            {
                return (ArgumentException)ThrowHelperError(new ArgumentException(message, paramName));
            }

            internal static ArgumentNullException ThrowHelperArgumentNull(string paramName)
            {
                return (ArgumentNullException)ThrowHelperError(new ArgumentNullException(paramName));
            }

            internal static Exception ThrowHelperError(Exception exception)
            {
                return exception;
            }

            internal static Exception ThrowHelperWarning(Exception exception)
            {
                return exception;
            }
        }
    }
}
