// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;

namespace System.Threading
{
    public sealed partial class Semaphore : WaitHandle
    {
        // creates a nameless semaphore object
        // Win32 only takes maximum count of int.MaxValue
        public Semaphore(int initialCount, int maximumCount)
        {
            CreateSemaphoreCore(initialCount, maximumCount);
        }

        /// <summary>
        /// Creates a named or unnamed semaphore, or opens a named semaphore if a semaphore with the name already exists.
        /// </summary>
        /// <param name="initialCount">
        /// The initial number of requests for the semaphore that can be satisfied concurrently.
        /// </param>
        /// <param name="maximumCount">
        /// The maximum number of requests for the semaphore that can be satisfied concurrently.
        /// </param>
        /// <param name="name">
        /// The name, if the semaphore is to be shared with other processes; otherwise, null or an empty string.
        /// </param>
        /// <param name="options">
        /// Options for the named semaphore. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying semaphore object.
        /// </param>
        public Semaphore(int initialCount, int maximumCount, string? name, NamedWaitHandleOptions options)
        {
            CreateSemaphoreCore(initialCount, maximumCount, name, new(options), out _);
        }

        public Semaphore(int initialCount, int maximumCount, string? name)
        {
            CreateSemaphoreCore(initialCount, maximumCount, name, options: default, out _);
        }

        /// <summary>
        /// Creates a named or unnamed semaphore, or opens a named semaphore if a semaphore with the name already exists.
        /// </summary>
        /// <param name="initialCount">
        /// The initial number of requests for the semaphore that can be satisfied concurrently.
        /// </param>
        /// <param name="maximumCount">
        /// The maximum number of requests for the semaphore that can be satisfied concurrently.
        /// </param>
        /// <param name="name">
        /// The name, if the semaphore is to be shared with other processes; otherwise, null or an empty string.
        /// </param>
        /// <param name="options">
        /// Options for the named semaphore. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying semaphore object.
        /// </param>
        /// <param name="createdNew">
        /// True if the semaphore was created; false if an existing named semaphore was opened.
        /// </param>
        public Semaphore(int initialCount, int maximumCount, string? name, NamedWaitHandleOptions options, out bool createdNew)
        {
            CreateSemaphoreCore(initialCount, maximumCount, name, new(options), out createdNew);
        }

        public Semaphore(int initialCount, int maximumCount, string? name, out bool createdNew)
        {
            CreateSemaphoreCore(initialCount, maximumCount, name, options: default, out createdNew);
        }

        private static void ValidateArguments(int initialCount, int maximumCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(initialCount);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCount);

            if (initialCount > maximumCount)
            {
                throw new ArgumentException(SR.Argument_SemaphoreInitialMaximum);
            }
        }

        /// <summary>
        /// Opens an existing named semaphore.
        /// </summary>
        /// <param name="name">The name of the semaphore to be shared with other processes.</param>
        /// <param name="options">
        /// Options for the named semaphore. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying semaphore object.
        /// </param>
        /// <returns>An object that represents the named semaphore.</returns>
        [SupportedOSPlatform("windows")]
        public static Semaphore OpenExisting(string name, NamedWaitHandleOptions options)
        {
            OpenExistingResult openExistingResult = OpenExistingWorker(name, new(options), out Semaphore? result);
            if (openExistingResult != OpenExistingResult.Success)
            {
                ThrowForOpenExistingFailure(openExistingResult, name);
            }

            Debug.Assert(result != null, "result should be non-null on success");
            return result;
        }

        [SupportedOSPlatform("windows")]
        public static Semaphore OpenExisting(string name)
        {
            OpenExistingResult openExistingResult = OpenExistingWorker(name, options: default, out Semaphore? result);
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
                    throw new IOException(SR.Format(SR.IO_PathNotFound_Path, name));
                default:
                    Debug.Assert(openExistingResult == OpenExistingResult.ObjectIncompatibleWithCurrentUserOnly);
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.NamedWaitHandles_ExistingObjectIncompatibleWithCurrentUserOnly, name));
            }
        }

        /// <summary>
        /// Tries to open an existing named semaphore and returns a value indicating whether it was successful.
        /// </summary>
        /// <param name="name">The name of the semaphore to be shared with other processes.</param>
        /// <param name="options">
        /// Options for the named semaphore. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying semaphore object.
        /// </param>
        /// <param name="result">
        /// An object that represents the named semaphore if the method returns true; otherwise, null.
        /// </param>
        /// <returns>True if the named semaphore was opened successfully; otherwise, false.</returns>
        [SupportedOSPlatform("windows")]
        public static bool TryOpenExisting(string name, NamedWaitHandleOptions options, [NotNullWhen(true)] out Semaphore? result) =>
            OpenExistingWorker(name, new(options), out result!) == OpenExistingResult.Success;

        [SupportedOSPlatform("windows")]
        public static bool TryOpenExisting(string name, [NotNullWhen(true)] out Semaphore? result) =>
            OpenExistingWorker(name, options: default, out result!) == OpenExistingResult.Success;

        public int Release() => ReleaseCore(1);

        // increase the count on a semaphore, returns previous count
        public int Release(int releaseCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(releaseCount);

            return ReleaseCore(releaseCount);
        }
    }
}
