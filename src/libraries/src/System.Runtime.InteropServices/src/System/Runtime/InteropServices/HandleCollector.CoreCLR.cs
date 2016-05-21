// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Runtime.InteropServices
{
    // CoreCLR-specific HandleCollector implementation
    public sealed partial class HandleCollector
    {
        private void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}
