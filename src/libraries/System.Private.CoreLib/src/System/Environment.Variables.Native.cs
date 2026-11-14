// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace System
{
    // The runtime keeps the process environment so that changes made by native code, such as
    // ICorProfilerInfo11::SetEnvironmentVariable or the diagnostic server, are visible here and vice versa.
    public static partial class Environment
    {
        private static unsafe string? GetEnvironmentVariableCore(string variable)
        {
            byte* value = GetEnvironmentVariableNative(variable, out Interop.BOOL needsFree);
            try
            {
                return Utf8StringMarshaller.ConvertToManaged(value);
            }
            finally
            {
                if (needsFree != Interop.BOOL.FALSE)
                {
                    NativeMemory.Free(value);
                }
            }
        }

        /// <summary>GetEnvironmentVariableCore that avoids using the ArrayPool.</summary>
        internal static string? GetEnvironmentVariableCore_NoArrayPool(string variable) =>
            // This implementation of GetEnvironmentVariableCore doesn't use the pool, so this just delegates to it.
            GetEnvironmentVariableCore(variable);

        private static void SetEnvironmentVariableCore(string variable, string? value)
        {
            switch (SetEnvironmentVariableNative(variable, value))
            {
                case 0:
                case Interop.Errors.ERROR_ENVVAR_NOT_FOUND: // Unsetting a variable that is not set is not an error.
                    return;
                case Interop.Errors.ERROR_INVALID_PARAMETER:
                    throw new ArgumentException(SR.Arg_ArgumentException, nameof(variable));
                default:
                    throw new OutOfMemoryException();
            }
        }

        public static unsafe IDictionary GetEnvironmentVariables()
        {
            var results = new Hashtable();

            byte** block = GetEnvironNative(out Interop.BOOL needsFree);
            if (block == null)
            {
                if (needsFree != Interop.BOOL.FALSE)
                {
                    throw new OutOfMemoryException();
                }

                return results;
            }

            try
            {
                for (byte** current = block; *current != null; current++)
                {
                    byte* entry = *current;

                    // Find the = separating the name and the value. Skip entries that begin with = or have no =,
                    // which can happen when the environment block is corrupted.
                    byte* separator = entry;
                    while (*separator != '=' && *separator != '\0')
                    {
                        separator++;
                    }

                    if (separator == entry || *separator == '\0')
                    {
                        continue;
                    }

                    string key = Encoding.UTF8.GetString(entry, checked((int)(separator - entry)));
                    string value = Utf8StringMarshaller.ConvertToManaged(separator + 1)!;
                    try
                    {
                        // Add may throw if the environment block was corrupted leading to duplicate entries.
                        // We allow such throws and eat them (rather than proactively checking for duplication)
                        // to provide a non-fatal notification about the corruption.
                        results.Add(key, value);
                    }
                    catch (ArgumentException) { }
                }
            }
            finally
            {
                if (needsFree != Interop.BOOL.FALSE)
                {
                    NativeMemory.Free(block);
                }
            }

            return results;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Environment_GetEnvironmentVariable", StringMarshalling = StringMarshalling.Utf8)]
        private static unsafe partial byte* GetEnvironmentVariableNative(string name, out Interop.BOOL needsFree);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Environment_SetEnvironmentVariable", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int SetEnvironmentVariableNative(string name, string? value);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Environment_GetEnviron")]
        private static unsafe partial byte** GetEnvironNative(out Interop.BOOL needsFree);
    }
}
