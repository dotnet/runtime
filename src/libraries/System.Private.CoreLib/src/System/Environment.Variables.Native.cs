// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
        private static string? GetEnvironmentVariableCore(string variable)
        {
            Span<byte> buffer = stackalloc byte[256];
            byte[]? rented = null;
            try
            {
                while (true)
                {
                    int length = GetEnvironmentVariableNative(variable, ref MemoryMarshal.GetReference(buffer), buffer.Length);
                    if (length < 0)
                    {
                        return null;
                    }

                    if (length < buffer.Length)
                    {
                        return Encoding.UTF8.GetString(buffer.Slice(0, length));
                    }

                    // The value did not fit, and length is the required size. Retry in case the value changed.
                    if (rented != null)
                    {
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                    buffer = rented = ArrayPool<byte>.Shared.Rent(length);
                }
            }
            finally
            {
                if (rented != null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        /// <summary>GetEnvironmentVariableCore that avoids using the ArrayPool. Environment variable values must be less than 256 bytes in UTF-8 or won't be found.</summary>
        internal static string? GetEnvironmentVariableCore_NoArrayPool(string variable)
        {
            Span<byte> buffer = stackalloc byte[256];
            int length = GetEnvironmentVariableNative(variable, ref MemoryMarshal.GetReference(buffer), buffer.Length);
            return length >= 0 && length < buffer.Length ?
                Encoding.UTF8.GetString(buffer.Slice(0, length)) :
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
            // The block is a sequence of null-terminated UTF-8 "name=value" strings, followed by an empty string.
            byte* block = GetEnvironmentStringsNative();
            if (block == null)
            {
                throw new OutOfMemoryException();
            }

            try
            {
                var results = new Hashtable();

                byte* current = block;
                while (true)
                {
                    ReadOnlySpan<byte> variable = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(current);
                    if (variable.IsEmpty)
                    {
                        break;
                    }

                    // Skip entries that begin with = or have no =, which can happen when the environment block is corrupted.
                    int i = variable.IndexOf((byte)'=');
                    if (i > 0)
                    {
                        string key = Encoding.UTF8.GetString(variable.Slice(0, i));
                        string value = Encoding.UTF8.GetString(variable.Slice(i + 1));
                        try
                        {
                            // Add may throw if the environment block was corrupted leading to duplicate entries.
                            // We allow such throws and eat them (rather than proactively checking for duplication)
                            // to provide a non-fatal notification about the corruption.
                            results.Add(key, value);
                        }
                        catch (ArgumentException) { }
                    }

                    current += variable.Length + 1;
                }

                return results;
            }
            finally
            {
                FreeEnvironmentStringsNative(block);
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Environment_GetEnvironmentVariable", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int GetEnvironmentVariableNative(string name, ref byte buffer, int bufferSize);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Environment_SetEnvironmentVariable", StringMarshalling = StringMarshalling.Utf8)]
        private static partial Interop.BOOL SetEnvironmentVariableNative(string name, string? value);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Environment_GetEnvironmentStrings")]
        private static unsafe partial byte* GetEnvironmentStringsNative();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Environment_FreeEnvironmentStrings")]
        private static unsafe partial void FreeEnvironmentStringsNative(byte* strings);
    }
}
