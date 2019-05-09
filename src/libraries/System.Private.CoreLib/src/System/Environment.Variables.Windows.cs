// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class Environment
    {
        private static string? GetEnvironmentVariableCore(string variable)
        {
            Span<char> buffer = stackalloc char[128]; // a somewhat reasonable default size
            int requiredSize = Interop.Kernel32.GetEnvironmentVariable(variable, buffer);

            if (requiredSize == 0 && Marshal.GetLastWin32Error() == Interop.Errors.ERROR_ENVVAR_NOT_FOUND)
            {
                return null;
            }

            if (requiredSize <= buffer.Length)
            {
                return new string(buffer.Slice(0, requiredSize));
            }

            char[] chars = ArrayPool<char>.Shared.Rent(requiredSize);
            try
            {
                buffer = chars;
                requiredSize = Interop.Kernel32.GetEnvironmentVariable(variable, buffer);
                if ((requiredSize == 0 && Marshal.GetLastWin32Error() == Interop.Errors.ERROR_ENVVAR_NOT_FOUND) ||
                    requiredSize > buffer.Length)
                {
                    return null;
                }

                return new string(buffer.Slice(0, requiredSize));
            }
            finally
            {
                ArrayPool<char>.Shared.Return(chars);
            }
        }

        private static void SetEnvironmentVariableCore(string variable, string? value)
        {
            if (!Interop.Kernel32.SetEnvironmentVariable(variable, value))
            {
                int errorCode = Marshal.GetLastWin32Error();
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
            char* pStrings = Interop.Kernel32.GetEnvironmentStrings();
            if (pStrings == null)
            {
                throw new OutOfMemoryException();
            }

            try
            {
                // Format for GetEnvironmentStrings is:
                // [=HiddenVar=value\0]* [Variable=value\0]* \0
                // See the description of Environment Blocks in MSDN's
                // CreateProcess page (null-terminated array of null-terminated strings).

                // Search for terminating \0\0 (two unicode \0's).
                char* p = pStrings;
                while (!(*p == '\0' && *(p + 1) == '\0'))
                {
                    p++;
                }
                Span<char> block = new Span<char>(pStrings, (int)(p - pStrings + 1));

                // Format for GetEnvironmentStrings is:
                // (=HiddenVar=value\0 | Variable=value\0)* \0
                // See the description of Environment Blocks in MSDN's
                // CreateProcess page (null-terminated array of null-terminated strings).
                // Note the =HiddenVar's aren't always at the beginning.

                // Copy strings out, parsing into pairs and inserting into the table.
                // The first few environment variable entries start with an '='.
                // The current working directory of every drive (except for those drives
                // you haven't cd'ed into in your DOS window) are stored in the 
                // environment block (as =C:=pwd) and the program's exit code is 
                // as well (=ExitCode=00000000).

                var results = new Hashtable();
                for (int i = 0; i < block.Length; i++)
                {
                    int startKey = i;

                    // Skip to key. On some old OS, the environment block can be corrupted.
                    // Some will not have '=', so we need to check for '\0'. 
                    while (block[i] != '=' && block[i] != '\0')
                    {
                        i++;
                    }

                    if (block[i] == '\0')
                    {
                        continue;
                    }

                    // Skip over environment variables starting with '='
                    if (i - startKey == 0)
                    {
                        while (block[i] != 0)
                        {
                            i++;
                        }

                        continue;
                    }

                    string key = new string(block.Slice(startKey, i - startKey));
                    i++;  // skip over '='

                    int startValue = i;
                    while (block[i] != 0)
                    {
                        i++; // Read to end of this entry 
                    }

                    string value = new string(block.Slice(startValue, i - startValue)); // skip over 0 handled by for loop's i++
                    try
                    {
                        results.Add(key, value);
                    }
                    catch (ArgumentException)
                    {
                        // Throw and catch intentionally to provide non-fatal notification about corrupted environment block
                    }
                }
                return results;
            }
            finally
            {
                bool success = Interop.Kernel32.FreeEnvironmentStrings(pStrings);
                Debug.Assert(success);
            }
        }
    }
}
