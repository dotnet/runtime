// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Diagnostics
{
    public partial class StackTrace
    {
#pragma warning disable CA1822 // Mark members as static
        private void InitializeForCurrentThread(int skipFrames, bool needFileInfo)
        {
            throw new PlatformNotSupportedException(SR.Arg_PlatformNotSupported);
        }

        internal void ToString(TraceFormat traceFormat, StringBuilder builder)
        {
            throw new PlatformNotSupportedException(SR.Arg_PlatformNotSupported);
        }
#pragma warning restore CA1822
    }
}
