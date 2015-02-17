// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.ComponentModel
{
    public partial class Win32Exception : Exception
    {
        private static string GetErrorMessage(int error)
        {
            return Interop.mincore.GetMessage(error);
        }
    }
}
