// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel
{
    public partial class Win32Exception
    {
        private static string GetErrorMessage(int error) => Interop.Sys.StrError(error);
    }
}
