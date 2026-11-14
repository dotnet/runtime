// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public static partial class Environment
    {
        private static unsafe string? GetEnvironmentVariableCore(string variable)
        {
            var builder = new ValueStringBuilder(stackalloc char[128]);

            uint length;
            while ((length = Interop.Kernel32.GetEnvironmentVariable(variable, ref builder.GetPinnableReference(), (uint)builder.Capacity)) > builder.Capacity)
            {
                builder.EnsureCapacity((int)length);
            }

            if (length == 0 && Marshal.GetLastPInvokeError() == Interop.Errors.ERROR_ENVVAR_NOT_FOUND)
            {
                builder.Dispose();
                return null;
            }

            builder.Length = (int)length;
            return builder.ToString();
        }

        /// <summary>GetEnvironmentVariableCore that avoids using the ArrayPool. Environment variables must be less than 128 characters in length or won't be found.</summary>
        internal static unsafe string? GetEnvironmentVariableCore_NoArrayPool(string variable)
        {
            Span<char> span = stackalloc char[128];
            uint length = Interop.Kernel32.GetEnvironmentVariable(variable, ref MemoryMarshal.GetReference(span), (uint)span.Length);
            return length > 0 && length <= span.Length ?
                span.Slice(0, (int)length).ToString() :
                null;
        }

        private static void SetEnvironmentVariableCore(string variable, string? value)
        {
            if (!Interop.Kernel32.SetEnvironmentVariable(variable, value))
            {
                int errorCode = Marshal.GetLastPInvokeError();
                switch (errorCode)
                {
                    case Interop.Errors.ERROR_ENVVAR_NOT_FOUND:
                        // Allow user to try to clear a environment variable
                        return;

                    case Interop.Errors.ERROR_FILENAME_EXCED_RANGE:
                        // The error message from Win32 is "The filename or extension is too long",
                        // which is not accurate.
                        throw new ArgumentException(SR.Argument_LongEnvVarValue);

                    case Interop.Errors.ERROR_NOT_ENOUGH_MEMORY:
                    case Interop.Errors.ERROR_NO_SYSTEM_RESOURCES:
                        throw new OutOfMemoryException(Marshal.GetPInvokeErrorMessage(errorCode));

                    default:
                        throw new ArgumentException(Marshal.GetPInvokeErrorMessage(errorCode));
                }
            }
        }

        public static unsafe IDictionary GetEnvironmentVariables()
        {
            char* stringPtr = Interop.Kernel32.GetEnvironmentStringsW();
            if (stringPtr == null)
            {
                throw new OutOfMemoryException();
            }

            try
            {
                return EnvironmentBlockParser.Parse(stringPtr);
            }
            finally
            {
                Interop.BOOL success = Interop.Kernel32.FreeEnvironmentStringsW(stringPtr);
                Debug.Assert(success != Interop.BOOL.FALSE);
            }
        }
    }
}
