// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace System.Configuration
{
    internal static class WindowsApplication
    {
        private const int APPMODEL_ERROR_NO_PACKAGE = 15700;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private const int ERROR_SUCCESS = 0;

        internal static unsafe string GetCurrentPackageFamilyName()
        {
            uint length = 0;
            int error = Interop.Kernel32.GetCurrentPackageFamilyName(&length, null);
            if (error == APPMODEL_ERROR_NO_PACKAGE)
            {
                return null;
            }

            if (error != ERROR_INSUFFICIENT_BUFFER)
            {
                throw new Win32Exception(error);
            }

            char[] buffer = new char[length];
            fixed (char* bufferPtr = buffer)
            {
                error = Interop.Kernel32.GetCurrentPackageFamilyName(&length, bufferPtr);
            }

            if (error != ERROR_SUCCESS)
            {
                throw new Win32Exception(error);
            }

            return new string(buffer, 0, (int)length - 1);
        }
    }
}
