// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using Windows.Storage.Streams;

namespace System.IO
{
    internal static class WinRtIOHelper
    {
        internal const int DefaultIOBufferSize = 0x3000;  // = 12 KBytes = 12288 Bytes


        internal static ExceptionDispatchInfo NativeExceptionToIOExceptionInfo(Exception nativeException)
        {
            // If the interop layer gave us a specific exception type, we assume it knew what it was doing.
            // If it gave us an ExternalException or a generic Exception, we assume that it hit a
            // general/unknown case and wrap it into an IOException as this is what Stream users expect.

            // We will return a captured ExceptionDispatchInfo such that we can invoke .Throw() close to where
            // nativeException was caught - this will result in the most readable call stack.

            Debug.Assert(nativeException != null);

            if (!(nativeException.GetType().Equals(typeof(Exception)) /*|| nativeException is ExternalException */))
                return ExceptionDispatchInfo.Capture(nativeException);

            // If we do not have a meaningful message, we use a general IO error message:
            string message = nativeException.Message;
            if (string.IsNullOrWhiteSpace(message))
                message = SR.IO_General;

            return ExceptionDispatchInfo.Capture(new IOException(message, nativeException));
        }


        internal static void EnsureResultsInUserBuffer(IBuffer userBuffer, IBuffer resultBuffer)
        {
            // Results buffer may be different from user specified buffer. If so - copy data to the user.

            Debug.Assert(userBuffer != null);
            Debug.Assert(resultBuffer != null);

            if (resultBuffer.IsSameData(userBuffer))
                return;

            resultBuffer.CopyTo(userBuffer);
            userBuffer.Length = resultBuffer.Length;
        }
    }  // class WinRtIOHelper
}  // namespace

