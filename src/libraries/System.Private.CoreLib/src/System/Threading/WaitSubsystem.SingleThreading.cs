// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading;

internal static partial class WaitSubsystem
{
    private static SafeWaitHandle NewHandle(WaitableObject waitableObject)
    {
        IntPtr handle = IntPtr.Zero;
        try
        {
            SafeWaitHandle safeWaitHandle = new();
            handle = GCHandle.ToIntPtr(GCHandle.Alloc(waitableObject));
            Marshal.InitHandle(safeWaitHandle, handle);
            return safeWaitHandle;
        }
        finally
        {
            if (handle == IntPtr.Zero)
            {
                ReleaseNamedMutexReference(waitableObject);
            }
        }
    }

    private static WaitableObject FromHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero || handle == new IntPtr(-1))
        {
            WaitHandle.ThrowInvalidHandleException();
        }

        return (WaitableObject)GCHandle.FromIntPtr(handle).Target!;
    }

    public static SafeWaitHandle NewEvent(bool initiallySignaled, EventResetMode resetMode) =>
        NewHandle(new WaitableObject(
            resetMode == EventResetMode.ManualReset ? WaitableObjectType.ManualResetEvent : WaitableObjectType.AutoResetEvent,
            initiallySignaled ? 1 : 0));

    public static SafeWaitHandle NewSemaphore(int initialSignalCount, int maximumSignalCount) =>
        NewHandle(new WaitableObject(WaitableObjectType.Semaphore, initialSignalCount, maximumSignalCount));

    public static SafeWaitHandle NewMutex(bool initiallyOwned) =>
        NewHandle(new WaitableObject(WaitableObjectType.Mutex, initiallyOwned ? 0 : 1));

    public static SafeWaitHandle? CreateNamedMutex(bool initiallyOwned, string name, bool isUserScope, out bool createdNew)
    {
        if (isUserScope)
        {
            name = $"User\\{name}";
        }

        s_namedMutexes ??= new();
        if (s_namedMutexes.TryGetValue(name, out NamedMutex? mutex))
        {
            createdNew = false;
            checked { mutex.ReferenceCount++; }
        }
        else
        {
            createdNew = true;
            mutex = new NamedMutex(name);
            s_namedMutexes.Add(name, mutex);
        }

        SafeWaitHandle handle = NewHandle(mutex);
        if (initiallyOwned && createdNew)
        {
            mutex.AcceptSignal();
        }

        return handle;
    }

    public static OpenExistingResult OpenNamedMutex(string name, bool isUserScope, out SafeWaitHandle? result)
    {
        if (isUserScope)
        {
            name = $"User\\{name}";
        }

        if (s_namedMutexes is null || !s_namedMutexes.TryGetValue(name, out NamedMutex? mutex))
        {
            result = null;
            return OpenExistingResult.NameNotFound;
        }

        checked { mutex.ReferenceCount++; }
        result = NewHandle(mutex);
        return OpenExistingResult.Success;
    }

    private static void ReleaseNamedMutexReference(WaitableObject waitableObject)
    {
        if (waitableObject is NamedMutex mutex && --mutex.ReferenceCount == 0)
        {
            Debug.Assert(s_namedMutexes is not null);
            s_namedMutexes.Remove(mutex.Name);
        }
    }

    public static void DeleteHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero || handle == new IntPtr(-1))
        {
            return;
        }

        GCHandle gcHandle = GCHandle.FromIntPtr(handle);
        ReleaseNamedMutexReference((WaitableObject)gcHandle.Target!);
        gcHandle.Free();
    }

    public static void SetEvent(IntPtr handle) => FromHandle(handle).SetEvent(signaled: true);

    public static void ResetEvent(IntPtr handle) => FromHandle(handle).SetEvent(signaled: false);

    public static int ReleaseSemaphore(IntPtr handle, int count) => FromHandle(handle).ReleaseSemaphore(count);

    public static void ReleaseMutex(IntPtr handle) => FromHandle(handle).Signal();

    public static int Wait(IntPtr handle, int timeoutMilliseconds, bool interruptible)
    {
        WaitableObject waitableObject = FromHandle(handle);
        if (interruptible)
        {
            Thread.CurrentThread.ThrowIfPendingInterrupt();
        }

        return TryWait(waitableObject, timeoutMilliseconds, interruptible);
    }

    private static int TryWait(WaitableObject waitableObject, int timeoutMilliseconds, bool interruptible)
    {
        if (!waitableObject.CanAcquire)
        {
            return UnsatisfiedWait(timeoutMilliseconds, interruptible);
        }

        waitableObject.AcceptSignal();
        return WaitHandle.WaitSuccess;
    }

    public static int Wait(ReadOnlySpan<IntPtr> waitHandles, bool waitForAll, int timeoutMilliseconds)
    {
        Debug.Assert(waitHandles.Length > 0 && waitHandles.Length <= WaitHandle.MaxWaitHandles);

        // Validate the whole span before consuming an interrupt or any signal.
        for (int i = 0; i < waitHandles.Length; i++)
        {
            WaitableObject waitableObject = FromHandle(waitHandles[i]);
            if (waitForAll)
            {
                for (int j = 0; j < i; j++)
                {
                    if (waitableObject == FromHandle(waitHandles[j]))
                    {
                        throw new DuplicateWaitObjectException("waitHandles[" + i + ']');
                    }
                }
            }
        }

        Thread.CurrentThread.ThrowIfPendingInterrupt();

        if (!waitForAll)
        {
            for (int i = 0; i < waitHandles.Length; i++)
            {
                WaitableObject waitableObject = FromHandle(waitHandles[i]);
                if (waitableObject.CanAcquire)
                {
                    waitableObject.AcceptSignal();
                    return WaitHandle.WaitSuccess + i;
                }
            }
        }
        else
        {
            // With no other executing thread, availability cannot change between these passes.
            foreach (IntPtr handle in waitHandles)
            {
                if (!FromHandle(handle).CanAcquire)
                {
                    return UnsatisfiedWait(timeoutMilliseconds, interruptible: true);
                }
            }
            foreach (IntPtr handle in waitHandles)
            {
                FromHandle(handle).AcceptSignal();
            }

            return WaitHandle.WaitSuccess;
        }

        return UnsatisfiedWait(timeoutMilliseconds, interruptible: true);
    }

    private static int UnsatisfiedWait(int timeoutMilliseconds, bool interruptible)
    {
        Debug.Assert(timeoutMilliseconds >= Timeout.Infinite);
        if (timeoutMilliseconds == Timeout.Infinite)
        {
            throw new PlatformNotSupportedException();
        }

        if (timeoutMilliseconds != 0)
        {
            if (interruptible)
            {
                Thread.Sleep(timeoutMilliseconds);
            }
            else
            {
                Sleep(timeoutMilliseconds, interruptible: false);
            }
        }

        return WaitHandle.WaitTimeout;
    }

    public static int SignalAndWait(IntPtr handleToSignal, IntPtr handleToWaitOn, int timeoutMilliseconds)
    {
        WaitableObject signal = FromHandle(handleToSignal);
        WaitableObject target = FromHandle(handleToWaitOn);
        Thread.CurrentThread.ThrowIfPendingInterrupt();

        try
        {
            signal.Signal();
        }
        catch (SemaphoreFullException ex)
        {
            throw new InvalidOperationException(SR.Threading_WaitHandleTooManyPosts, ex);
        }

        return TryWait(target, timeoutMilliseconds, interruptible: true);
    }

    public static void Sleep(int timeoutMilliseconds, bool interruptible = true)
    {
        Debug.Assert(timeoutMilliseconds >= Timeout.Infinite);
        Thread thread = Thread.CurrentThread;
        if (interruptible)
        {
            thread.ThrowIfPendingInterrupt();
        }

        if (timeoutMilliseconds == 0)
        {
            return;
        }
        if (timeoutMilliseconds == Timeout.Infinite)
        {
            throw new PlatformNotSupportedException();
        }

        thread.SetWaitSleepJoinState();
        try
        {
            Interop.Error error = Interop.Sys.ThreadSleep(timeoutMilliseconds);
            if (error != Interop.Error.SUCCESS)
            {
                throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(error));
            }
        }
        finally
        {
            thread.ClearWaitSleepJoinState();
        }
    }
}
