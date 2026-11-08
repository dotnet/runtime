// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    // The runtime keeps the process environment so that changes made by native code, such as
    // ICorProfilerInfo11::SetEnvironmentVariable or the diagnostic server, are visible here and vice versa.
    public static partial class Environment
    {
        private static unsafe string? GetEnvironmentVariableCore(string variable)
        {
            var builder = new ValueStringBuilder(stackalloc char[128]);

            int length;
            while ((length = GetEnvironmentVariableNative(variable, ref builder.GetPinnableReference(), builder.Capacity)) > builder.Capacity)
            {
                builder.EnsureCapacity(length);
            }

            if (length < 0)
            {
                builder.Dispose();
                return null;
            }

            builder.Length = length;
            return builder.ToString();
        }

        /// <summary>GetEnvironmentVariableCore that avoids using the ArrayPool. Environment variables must be less than 128 characters in length or won't be found.</summary>
        internal static unsafe string? GetEnvironmentVariableCore_NoArrayPool(string variable)
        {
            Span<char> span = stackalloc char[128];
            int length = GetEnvironmentVariableNative(variable, ref MemoryMarshal.GetReference(span), span.Length);
            return length >= 0 && length <= span.Length ?
                span.Slice(0, length).ToString() :
                null;
        }

        private static void SetEnvironmentVariableCore(string variable, string? value)
        {
            // Unsetting a variable that is not set is not an error. Otherwise, the variable name
            // has been validated, so the only possible failure is running out of memory.
            if (SetEnvironmentVariableNative(variable, value) == Interop.BOOL.FALSE && value != null)
            {
                throw new OutOfMemoryException();
            }
        }

        public static unsafe IDictionary GetEnvironmentVariables()
        {
            // The block has the same format as GetEnvironmentStrings on Windows:
            //     [Variable=value\0]* \0
            char* stringPtr = GetEnvironmentStringsNative();
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

                    // Skip entries that begin with = or have no =, which can happen when the environment block is corrupted.
                    int i = variable.IndexOf('=');
                    if (i > 0)
                    {
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

                    currentPtr += variable.Length + 1;
                }

                return results;
            }
            finally
            {
                FreeEnvironmentStringsNative(stringPtr);
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Environment_GetEnvironmentVariable", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int GetEnvironmentVariableNative(string name, ref char buffer, int bufferSize);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Environment_SetEnvironmentVariable", StringMarshalling = StringMarshalling.Utf16)]
        private static partial Interop.BOOL SetEnvironmentVariableNative(string name, string? value);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Environment_GetEnvironmentStrings")]
        private static unsafe partial char* GetEnvironmentStringsNative();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Environment_FreeEnvironmentStrings")]
        private static unsafe partial void FreeEnvironmentStringsNative(char* strings);
    }
}
