// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;

namespace System.IO
{
    /// <summary>
    /// These extensions are responsible for translating exceptions we get from WinRT to those
    /// that match what folks expect from our IO APIs.
    /// </summary>
    internal static class WinRTIOExtensions
    {
        public static ConfiguredTaskAwaitable<TResult> TranslateWinRTTask<TResult>(this IAsyncOperation<TResult> operation, string filePath, bool isDirectory = false)
        {
            return TranslateWinRTTaskCore(operation, filePath, isDirectory).ConfigureAwait(false);
        }

        public static ConfiguredTaskAwaitable TranslateWinRTTask(this IAsyncAction operation, string filePath, bool isDirectory = false)
        {
            return TranslateWinRTTaskCore(operation, filePath, isDirectory).ConfigureAwait(false);
        }

        private static async Task<TResult> TranslateWinRTTaskCore<TResult>(IAsyncOperation<TResult> operation, string filePath, bool isDirectory)
        {
            try
            {
                return await operation.AsTask().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw TranslateWinRTException(exception, filePath, isDirectory);
            }
        }

        private static async Task TranslateWinRTTaskCore(IAsyncAction operation, string filePath, bool isDirectory)
        {
            try
            {
                await operation.AsTask().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw TranslateWinRTException(exception, filePath, isDirectory);
            }
        }

        public static Exception TranslateWinRTException(this Exception exception, string filePath, bool isDirectory = false)
        {
            int errorCode = Win32Marshal.TryMakeWin32ErrorCodeFromHR(exception.HResult);

            if (isDirectory)
            {
                // WinRT remaps all ERROR_PATH_NOT_FOUND to ERROR_FILE_NOT_FOUND
                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND)
                    errorCode = Interop.Errors.ERROR_PATH_NOT_FOUND;
            }
            else
            {
                // Existing comment from FileStream:
                //   NT5 oddity - when trying to open "C:\" as a FileStream,
                //   we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
                //   probably be consistent w/ every other directory.
                // This remaps the error for non-existent drives which is incorrect
                // but we need to preserve it for compatibility
                if (errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND && filePath.Equals(Directory.InternalGetDirectoryRoot(filePath)))
                    errorCode = Interop.Errors.ERROR_ACCESS_DENIED;

                // Known issue: WinRT pre-check's the find data of a fullPath before trying to create it, if the type doesn't match
                // (IE: open file on a directory) it will return E_INVALIDARG instead of ERROR_ACCESS_DENIED

                // CreateFile returns ERROR_PATH_NOT_FOUND when given a fullPath that ends in a backslash.
                // WinRT remaps all ERROR_PATH_NOT_FOUND to ERROR_FILE_NOT_FOUND
                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND && filePath.Length > 0 && filePath[filePath.Length - 1] == Path.DirectorySeparatorChar)
                    errorCode = Interop.Errors.ERROR_PATH_NOT_FOUND;
            }

            // Known issue: We still can't handle ERROR_SHARING_VIOLATION because WinRT APIs are mapping this to ERROR_ACCESS_DENIED

            // Maps all unknown exceptions to IOException to be consistent with Win32 behavior
            return Win32Marshal.GetExceptionForWin32Error(errorCode, filePath);
        }
    }
}
