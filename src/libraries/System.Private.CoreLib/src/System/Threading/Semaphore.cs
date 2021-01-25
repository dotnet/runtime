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
        public Semaphore(int initialCount, int maximumCount) : this(initialCount, maximumCount, null) { }

        public Semaphore(int initialCount, int maximumCount, string? name) :
            this(initialCount, maximumCount, name, out _)
        {
        }

        public Semaphore(int initialCount, int maximumCount, string? name, out bool createdNew)
        {
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (maximumCount < 1)
                throw new ArgumentOutOfRangeException(nameof(maximumCount), SR.ArgumentOutOfRange_NeedPosNum);

            if (initialCount > maximumCount)
                throw new ArgumentException(SR.Argument_SemaphoreInitialMaximum);

            CreateSemaphoreCore(initialCount, maximumCount, name, out createdNew);
        }

        [SupportedOSPlatform("windows")]
        public static Semaphore OpenExisting(string name)
        {
            switch (OpenExistingWorker(name, out Semaphore? result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();
                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                case OpenExistingResult.PathNotFound:
                    throw new IOException(SR.Format(SR.IO_PathNotFound_Path, name));
                default:
                    Debug.Assert(result != null, "result should be non-null on success");
                    return result;
            }
        }

        [SupportedOSPlatform("windows")]
        public static bool TryOpenExisting(string name, [NotNullWhen(true)] out Semaphore? result) =>
            OpenExistingWorker(name, out result!) == OpenExistingResult.Success;

        public int Release() => ReleaseCore(1);

        // increase the count on a semaphore, returns previous count
        public int Release(int releaseCount)
        {
            if (releaseCount < 1)
                throw new ArgumentOutOfRangeException(nameof(releaseCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            return ReleaseCore(releaseCount);
        }
    }
}
