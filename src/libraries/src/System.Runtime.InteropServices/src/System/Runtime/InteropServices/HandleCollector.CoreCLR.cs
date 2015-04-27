// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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