// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// <summary>
    /// Synchronization primitive that can also be used for interprocess synchronization
    /// </summary>
    public sealed partial class Mutex : WaitHandle
    {
        /// <summary>
        /// Creates a named or unnamed mutex, or opens a named mutex if a mutex with the name already exists.
        /// </summary>
        /// <param name="initiallyOwned">
        /// True to acquire the mutex on the calling thread if it's created; otherwise, false.
        /// </param>
        /// <param name="name">
        /// The name, if the mutex is to be shared with other processes; otherwise, null or an empty string.
        /// </param>
        /// <param name="options">
        /// Options for the named mutex. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying mutex object.
        /// </param>
        /// <param name="createdNew">
        /// True if the mutex was created; false if an existing named mutex was opened.
        /// </param>
        public Mutex(bool initiallyOwned, string? name, NamedWaitHandleOptions options, out bool createdNew)
        {
            CreateMutexCore(initiallyOwned, name, new(options), out createdNew);
        }

        public Mutex(bool initiallyOwned, string? name, out bool createdNew)
        {
            CreateMutexCore(initiallyOwned, name, options: default, out createdNew);
        }

        /// <summary>
        /// Creates a named or unnamed mutex, or opens a named mutex if a mutex with the name already exists.
        /// </summary>
        /// <param name="initiallyOwned">
        /// True to acquire the mutex on the calling thread if it's created; otherwise, false.
        /// </param>
        /// <param name="name">
        /// The name, if the mutex is to be shared with other processes; otherwise, null or an empty string.
        /// </param>
        /// <param name="options">
        /// Options for the named mutex. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying mutex object.
        /// </param>
        public Mutex(bool initiallyOwned, string? name, NamedWaitHandleOptions options)
        {
            CreateMutexCore(initiallyOwned, name, new(options), createdNew: out _);
        }

        public Mutex(bool initiallyOwned, string? name)
        {
            CreateMutexCore(initiallyOwned, name, options: default, createdNew: out _);
        }

        /// <summary>
        /// Creates a named or unnamed mutex, or opens a named mutex if a mutex with the name already exists.
        /// </summary>
        /// <param name="name">
        /// The name, if the mutex is to be shared with other processes; otherwise, null or an empty string.
        /// </param>
        /// <param name="options">
        /// Options for the named mutex. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying mutex object.
        /// </param>
        public Mutex(string? name, NamedWaitHandleOptions options)
        {
            CreateMutexCore(initiallyOwned: false, name, new(options), createdNew: out _);
        }

        public Mutex(bool initiallyOwned)
        {
            CreateMutexCore(initiallyOwned);
        }

        public Mutex()
        {
            CreateMutexCore(initiallyOwned: false);
        }

        private Mutex(SafeWaitHandle handle)
        {
            SafeWaitHandle = handle;
        }

        /// <summary>
        /// Opens an existing named mutex.
        /// </summary>
        /// <param name="name">The name of the mutex to be shared with other processes.</param>
        /// <param name="options">
        /// Options for the named mutex. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying mutex object.
        /// </param>
        /// <returns>An object that represents the named mutex.</returns>
        public static Mutex OpenExisting(string name, NamedWaitHandleOptions options)
        {
            OpenExistingResult openExistingResult = OpenExistingWorker(name, new(options), out Mutex? result);
            if (openExistingResult != OpenExistingResult.Success)
            {
                ThrowForOpenExistingFailure(openExistingResult, name);
            }

            Debug.Assert(result != null, "result should be non-null on success");
            return result;
        }

        public static Mutex OpenExisting(string name)
        {
            OpenExistingResult openExistingResult = OpenExistingWorker(name, options: default, out Mutex? result);
            if (openExistingResult != OpenExistingResult.Success)
            {
                ThrowForOpenExistingFailure(openExistingResult, name);
            }

            Debug.Assert(result != null, "result should be non-null on success");
            return result;
        }

        [DoesNotReturn]
        private static void ThrowForOpenExistingFailure(OpenExistingResult openExistingResult, string name)
        {
            Debug.Assert(openExistingResult != OpenExistingResult.Success);

            switch (openExistingResult)
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();
                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                case OpenExistingResult.PathNotFound:
                    throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, name));
                default:
                    Debug.Assert(openExistingResult == OpenExistingResult.ObjectIncompatibleWithCurrentUserOnly);
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.NamedWaitHandles_ExistingObjectIncompatibleWithCurrentUserOnly, name));
            }
        }

        /// <summary>
        /// Tries to open an existing named mutex and returns a value indicating whether it was successful.
        /// </summary>
        /// <param name="name">The name of the mutex to be shared with other processes.</param>
        /// <param name="options">
        /// Options for the named mutex. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying mutex object.
        /// </param>
        /// <param name="result">
        /// An object that represents the named mutex if the method returns true; otherwise, null.
        /// </param>
        /// <returns>True if the named mutex was opened successfully; otherwise, false.</returns>
        public static bool TryOpenExisting(string name, NamedWaitHandleOptions options, [NotNullWhen(true)] out Mutex? result) =>
            OpenExistingWorker(name, new(options), out result!) == OpenExistingResult.Success;

        public static bool TryOpenExisting(string name, [NotNullWhen(true)] out Mutex? result) =>
            OpenExistingWorker(name, options: default, out result!) == OpenExistingResult.Success;
    }
}
