// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace System.ComponentModel
{
    public partial class Win32Exception : Exception
    {
        private static string GetErrorMessage(int error)
        {
            return Interop.Sys.StrError(error);
        }
    }
}
