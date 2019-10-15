// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Data.SqlClient;
using System.Data.SqlClient.SNI;
using System.Runtime.InteropServices;

namespace System.Data
{
    internal static partial class LocalDBAPI
    {
        private static IntPtr LoadProcAddress() =>
            throw new PlatformNotSupportedException(SR.LocalDBNotSupported); // No Registry support on UAP
    }
}