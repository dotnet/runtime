// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Diagnostics
{
#pragma warning disable CA1822 // Mark members as static
    public partial class StackTrace
    {
        private void InitializeForCurrentThread(int skipFrames, bool needFileInfo)
        {
            throw new PlatformNotSupportedException();
        }

        internal void ToString(TraceFormat traceFormat, StringBuilder builder)
        {
            throw new PlatformNotSupportedException();
        }
    }
#pragma warning restore CA1822
}
