// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;

namespace System.Security.AccessControl
{
    public sealed partial class RegistrySecurity : NativeObjectSecurity
    {
        private static Exception _HandleErrorCodeCore(int errorCode, string name, SafeHandle handle, object context)
        {
            Exception exception = null;

            switch (errorCode)
            {
                case Interop.Errors.ERROR_FILE_NOT_FOUND:
                    exception = new IOException(SR.Format(SR.Arg_RegKeyNotFound, errorCode));
                    break;

                case Interop.Errors.ERROR_INVALID_NAME:
                    exception = new ArgumentException(SR.Format(SR.Arg_RegInvalidKeyName, nameof(name)));
                    break;

                case Interop.Errors.ERROR_INVALID_HANDLE:
                    exception = new ArgumentException(SR.AccessControl_InvalidHandle);
                    break;

                default:
                    break;
            }

            return exception;
        }
    }
}
