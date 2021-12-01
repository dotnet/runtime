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
        private static string? GetEnvironmentVariableCore(string variable)
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
                        throw new OutOfMemoryException(Interop.Kernel32.GetMessage(errorCode));

                    default:
                        throw new ArgumentException(Interop.Kernel32.GetMessage(errorCode));
                }
            }
        }

        public static unsafe IDictionary GetEnvironmentVariables()
        {
            // Format for GetEnvironmentStrings is:
            //     [=HiddenVar=value\0]* [Variable=value\0]* \0
            // See the description of Environment Blocks in MSDN's CreateProcess
            // page (null-terminated array of null-terminated strings). Note
            // the =HiddenVar's aren't always at the beginning.

            // Copy strings out, parsing into pairs and inserting into the table.
            // The first few environment variable entries start with an '='.
            // The current working directory of every drive (except for those drives
            // you haven't cd'ed into in your DOS window) are stored in the
            // environment block (as =C:=pwd) and the program's exit code is
            // as well (=ExitCode=00000000).

            char* stringPtr = Interop.Kernel32.GetEnvironmentStringsW();
            if (stringPtr == null)
            {
                throw new OutOfMemoryException();
            }

            try
            {
                var results = new Hashtable();

                char* currentPtr = stringPtr;
                while (true)
                {
                    ReadOnlySpan<char> variable = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(currentPtr);
                    if (variable.IsEmpty)
                    {
                        break;
                    }

                    // Find the = separating the key and value. We skip entries that begin with =.  We also skip entries that don't
                    // have =, which can happen on some older OSes when the environment block gets corrupted.
                    int i = variable.IndexOf('=');
                    if (i > 0)
                    {
                        // Add the key and value.
                        string key = new string(variable.Slice(0, i));
                        string value = new string(variable.Slice(i + 1));
                        try
                        {
                            // Add may throw if the environment block was corrupted leading to duplicate entries.
                            // We allow such throws and eat them (rather than proactively checking for duplication)
                            // to provide a non-fatal notification about the corruption.
                            results.Add(key, value);
                        }
                        catch (ArgumentException) { }
                    }

                    // Move to the end of this variable, after its terminator.
                    currentPtr += variable.Length + 1;
                }

                return results;
            }
            finally
            {
                Interop.BOOL success = Interop.Kernel32.FreeEnvironmentStringsW(stringPtr);
                Debug.Assert(success != Interop.BOOL.FALSE);
            }
        }
    }
}
