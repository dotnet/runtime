// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

using FILE_NOTIFY_INFORMATION = Interop.Kernel32.FILE_NOTIFY_INFORMATION;

namespace System.IO
{
    public partial class FileSystemWatcher
    {
        /// <summary>Start monitoring the current directory.</summary>
        private void StartRaisingEvents()
        {
            // If we're called when "Initializing" is true, set enabled to true
            if (IsSuspended())
            {
                _enabled = true;
                return;
            }

            // If we're already running, don't do anything.
            if (!IsHandleClosed(_directoryHandle))
                return;

            // Create handle to directory being monitored
            SafeFileHandle directoryHandle = Interop.Kernel32.CreateFile(
                lpFileName: _directory,
                dwDesiredAccess: Interop.Kernel32.FileOperations.FILE_LIST_DIRECTORY,
                dwShareMode: FileShare.Read | FileShare.Delete | FileShare.Write,
                dwCreationDisposition: FileMode.Open,
                dwFlagsAndAttributes: Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS | Interop.Kernel32.FileOperations.FILE_FLAG_OVERLAPPED);

            if (directoryHandle.IsInvalid)
            {
                throw new FileNotFoundException(SR.Format(SR.FSW_IOError, _directory));
            }

            // Create the state associated with the operation of monitoring the direction
            AsyncReadState state = new(
                Interlocked.Increment(ref _currentSession), // ignore all events that were initiated before this
                this,
                directoryHandle);
            try
            {
                unsafe
                {
                    uint bufferSize = _internalBufferSize;
                    state.Buffer = NativeMemory.Alloc(bufferSize);
                    state.BufferByteLength = bufferSize;
                }

                state.ThreadPoolBinding = ThreadPoolBoundHandle.BindHandle(directoryHandle);

                // Store all state, including a preallocated overlapped, into the state object that'll be
                // passed from iteration to iteration during the lifetime of the operation.  The buffer will be pinned
                // from now until the end of the operation.
                unsafe
                {
                    state.PreAllocatedOverlapped = new PreAllocatedOverlapped((errorCode, numBytes, overlappedPointer) =>
                    {
                        AsyncReadState state = (AsyncReadState)ThreadPoolBoundHandle.GetNativeOverlappedState(overlappedPointer)!;
                        state.ThreadPoolBinding!.FreeNativeOverlapped(overlappedPointer);

                        if (state.WeakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                        {
                            watcher.ReadDirectoryChangesCallback(errorCode, numBytes, state);
                        }
                        else
                        {
                            state.Dispose();
                        }
                    }, state, null);
                }
            }
            catch
            {
                state.Dispose();
                throw;
            }

            // Start monitoring
            _directoryHandle = directoryHandle;
            _enabled = true;
            Monitor(state);
        }

        /// <summary>Stop monitoring the current directory.</summary>
        private void StopRaisingEvents()
        {
            _enabled = false;

            if (IsSuspended())
                return;

            // If we're not running, do nothing.
            if (IsHandleClosed(_directoryHandle))
                return;

            // Start ignoring all events occurring after this.
            Interlocked.Increment(ref _currentSession);

            // Close the directory handle.  This will cause the async operation to stop processing.
            // This operation doesn't need to be atomic because the API will deal with a closed
            // handle appropriately. If we get here while asynchronously waiting on a change notification,
            // closing the directory handle should cause ReadDirectoryChangesCallback be called,
            // cleaning up the operation.  Note that it's critical to also null out the handle.  If the
            // handle is currently in use in a P/Invoke, it will have its reference count temporarily
            // increased, such that the disposal operation won't take effect and close the handle
            // until that P/Invoke returns; if during that time the FSW is restarted, the IsHandleInvalid
            // check will see a valid handle, unless we also null it out.
            SafeFileHandle? handle = _directoryHandle;
            if (handle is not null)
            {
                _directoryHandle = null;
                handle.Dispose();
            }
        }

        private void FinalizeDispose()
        {
            // We must explicitly dispose the handle to ensure it gets closed before this object is finalized.
            // Otherwise, it is possible that the GC will decide to finalize the handle after this,
            // leaving a window of time where our callback could be invoked on a non-existent object.
            if (!IsHandleClosed(_directoryHandle))
                _directoryHandle.Dispose();
        }

        // Current "session" ID to ignore old events whenever we stop then restart.
        private int _currentSession;

        // Unmanaged handle to monitored directory
        private SafeFileHandle? _directoryHandle;

        /// <summary>Allocates a buffer of the requested internal buffer size.</summary>
        /// <returns>The allocated buffer.</returns>
        private unsafe byte* AllocateBuffer()
        {
            try
            {
                return (byte*)NativeMemory.Alloc(_internalBufferSize);
            }
            catch (OutOfMemoryException)
            {
                throw new OutOfMemoryException(SR.Format(SR.BufferSizeTooLarge, _internalBufferSize));
            }
        }

        private static bool IsHandleClosed([NotNullWhen(false)] SafeFileHandle? handle) =>
            handle is null || handle.IsClosed;

        /// <summary>
        /// Initiates the next asynchronous read operation if monitoring is still desired.
        /// If the directory handle has been closed due to an error or due to event monitoring
        /// being disabled, this cleans up state associated with the operation.
        /// </summary>
        private unsafe void Monitor(AsyncReadState state)
        {
            Debug.Assert(state.PreAllocatedOverlapped != null);
            Debug.Assert(state.ThreadPoolBinding is not null && state.Buffer is not null, "Members should have been set at construction");

            // This method should only ever access the directory handle via the state object passed in, and not access it
            // via _directoryHandle.  While this function is executing asynchronously, another thread could set
            // EnableRaisingEvents to false and then back to true, restarting the FSW and causing a new directory handle
            // and thread pool binding to be stored.  This function could then get into an inconsistent state by doing some
            // operations against the old handles and some against the new.

            NativeOverlapped* overlappedPointer = null;
            bool continueExecuting = false;
            int win32Error = 0;
            try
            {
                // If shutdown has been requested, exit.  The finally block will handle
                // cleaning up the entire operation, as continueExecuting will remain false.
                if (!_enabled || IsHandleClosed(state.DirectoryHandle))
                    return;

                // Get the overlapped pointer to use for this iteration.
                overlappedPointer = state.ThreadPoolBinding.AllocateNativeOverlapped(state.PreAllocatedOverlapped);
                continueExecuting = Interop.Kernel32.ReadDirectoryChangesW(
                    state.DirectoryHandle,
                    state.Buffer,
                    (uint)state.BufferByteLength,
                    _includeSubdirectories,
                    (uint)_notifyFilters,
                    null,
                    overlappedPointer,
                    null);
                if (!continueExecuting)
                {
                    win32Error = Marshal.GetLastWin32Error();
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore.  Disposing of the handle is the mechanism by which the FSW communicates
                // to the asynchronous operation to stop processing.
            }
            finally
            {
                // At this point the operation has either been initiated and we'll let the callback
                // handle things from here, or the operation has been stopped or failed, in which case
                // we need to cleanup because we're no longer executing.
                if (!continueExecuting)
                {
                    // Clean up the overlapped pointer created for this iteration
                    if (overlappedPointer != null)
                    {
                        state.ThreadPoolBinding.FreeNativeOverlapped(overlappedPointer);
                    }

                    // Check whether the directory handle is still valid _before_ we dispose of the state,
                    // which will invalidate the handle.
                    bool handleClosed = IsHandleClosed(state.DirectoryHandle);

                    // Clean up the state created for the whole operation.
                    state.Dispose();

                    // Finally, if the handle was for some reason changed or closed during this call,
                    // then don't throw an exception.  Otherwise, it's a valid error.
                    if (!handleClosed)
                    {
                        OnError(new ErrorEventArgs(new Win32Exception(win32Error)));
                    }
                }
            }
        }

        /// <summary>Callback invoked when an asynchronous read on the directory handle completes.</summary>
        private void ReadDirectoryChangesCallback(uint errorCode, uint numBytes, AsyncReadState state)
        {
            try
            {
                if (IsHandleClosed(state.DirectoryHandle))
                    return;

                if (errorCode != 0)
                {
                    // Inside a service the first completion status is false;
                    // need to monitor again.

                    const int ERROR_OPERATION_ABORTED = 995;
                    if (errorCode != ERROR_OPERATION_ABORTED)
                    {
                        EnableRaisingEvents = false;
                        OnError(new ErrorEventArgs(new Win32Exception((int)errorCode)));
                    }
                    return;
                }

                // Ignore any events that occurred before this "session",
                // so we don't get changed or error events after we
                // told FSW to stop.  Even with this check, though, there's a small
                // race condition, as immediately after we do the check, raising
                // events could be disabled.
                if (state.Session != Volatile.Read(ref _currentSession))
                    return;

                if (numBytes == 0 || numBytes > state.BufferByteLength)
                {
                    Debug.Assert(numBytes == 0, "ReadDirectoryChangesW returned more bytes than the buffer can hold!");
                    NotifyInternalBufferOverflowEvent();
                }
                else
                {
                    unsafe
                    {
                        ParseEventBufferAndNotifyForEach(new ReadOnlySpan<byte>(state.Buffer, (int)numBytes));
                    }
                }
            }
            finally
            {
                // Call Monitor again to either start the next iteration or clean up the whole operation.
                Monitor(state);
            }
        }

        private unsafe void ParseEventBufferAndNotifyForEach(ReadOnlySpan<byte> buffer)
        {
            ReadOnlySpan<char> oldName = ReadOnlySpan<char>.Empty;

            // Parse each event from the buffer and notify appropriate delegates
            while (true)
            {
                // Validate the data we received isn't truncated. This can happen if we are watching files over
                // the network with a poor connection (https://github.com/dotnet/runtime/issues/40412).
                if (sizeof(FILE_NOTIFY_INFORMATION) > (uint)buffer.Length)
                {
                    // This is defensive check. We do not expect to receive truncated data under normal circumstances.
                    Debug.Assert(false);
                    break;
                }
                ref readonly FILE_NOTIFY_INFORMATION info = ref MemoryMarshal.AsRef<FILE_NOTIFY_INFORMATION>(buffer);

                // Validate the data we received isn't truncated.
                if (info.FileNameLength > (uint)buffer.Length - sizeof(FILE_NOTIFY_INFORMATION))
                {
                    // This is defensive check. We do not expect to receive truncated data under normal circumstances.
                    Debug.Assert(false);
                    break;
                }
                ReadOnlySpan<char> fileName = MemoryMarshal.Cast<byte, char>(
                    buffer.Slice(sizeof(FILE_NOTIFY_INFORMATION), (int)info.FileNameLength));

                // A slightly convoluted piece of code follows.  Here's what's happening:
                //
                // We wish to collapse the poorly done rename notifications from the
                // ReadDirectoryChangesW API into a nice rename event. So to do that,
                // it's assumed that a FILE_ACTION_RENAMED_OLD_NAME will be followed
                // immediately by a FILE_ACTION_RENAMED_NEW_NAME in the buffer, which is
                // all that the following code is doing.
                //
                // On a FILE_ACTION_RENAMED_OLD_NAME, it asserts that no previous one existed
                // and saves its name.  If there are no more events in the buffer, it'll
                // assert and fire a RenameEventArgs with the Name field null.
                //
                // If a NEW_NAME action comes in with no previous OLD_NAME, we assert and fire
                // a rename event with the OldName field null.
                //
                // If the OLD_NAME and NEW_NAME actions are indeed there one after the other,
                // we'll fire the RenamedEventArgs normally and clear oldName.
                //
                // If the OLD_NAME is followed by another action, we assert and then fire the
                // rename event with the Name field null and then fire the next action.
                //
                // In case it's not a OLD_NAME or NEW_NAME action, we just fire the event normally.
                //
                // (Phew!)

                switch (info.Action)
                {
                    case Interop.Kernel32.FileAction.FILE_ACTION_RENAMED_OLD_NAME:
                        // Action is renamed from, save the name of the file
                        oldName = fileName;
                        break;
                    case Interop.Kernel32.FileAction.FILE_ACTION_RENAMED_NEW_NAME:
                        // oldName may be empty if we didn't receive FILE_ACTION_RENAMED_OLD_NAME first
                        NotifyRenameEventArgs(
                            WatcherChangeTypes.Renamed,
                            fileName,
                            oldName);
                        oldName = ReadOnlySpan<char>.Empty;
                        break;
                    default:
                        if (!oldName.IsEmpty)
                        {
                            // Previous FILE_ACTION_RENAMED_OLD_NAME with no new name
                            NotifyRenameEventArgs(WatcherChangeTypes.Renamed, ReadOnlySpan<char>.Empty, oldName);
                            oldName = ReadOnlySpan<char>.Empty;
                        }

                        switch (info.Action)
                        {
                            case Interop.Kernel32.FileAction.FILE_ACTION_ADDED:
                                NotifyFileSystemEventArgs(WatcherChangeTypes.Created, fileName);
                                break;
                            case Interop.Kernel32.FileAction.FILE_ACTION_REMOVED:
                                NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, fileName);
                                break;
                            case Interop.Kernel32.FileAction.FILE_ACTION_MODIFIED:
                                NotifyFileSystemEventArgs(WatcherChangeTypes.Changed, fileName);
                                break;
                            default:
                                Debug.Fail($"Unknown FileSystemEvent action type!  Value: {info.Action}");
                                break;
                        }

                        break;
                }

                if (info.NextEntryOffset == 0)
                {
                    break;
                }

                // Validate the data we received isn't truncated.
                if (info.NextEntryOffset > (uint)buffer.Length)
                {
                    // This is defensive check. We do not expect to receive truncated data under normal circumstances.
                    Debug.Assert(false);
                    break;
                }

                buffer = buffer.Slice((int)info.NextEntryOffset);
            }

            if (!oldName.IsEmpty)
            {
                // Previous FILE_ACTION_RENAMED_OLD_NAME with no new name
                NotifyRenameEventArgs(WatcherChangeTypes.Renamed, ReadOnlySpan<char>.Empty, oldName);
            }
        }

        /// <summary>
        /// State information used by the ReadDirectoryChangesW callback.  A single instance of this is used
        /// for an entire session, getting passed to each iterative call to ReadDirectoryChangesW.
        /// </summary>
        private sealed class AsyncReadState
        {
            internal AsyncReadState(int session, FileSystemWatcher parent, SafeFileHandle directoryHandle)
            {
                Session = session;
                WeakWatcher = new WeakReference<FileSystemWatcher>(parent);
                DirectoryHandle = directoryHandle;
            }

            internal int Session { get; }
            internal WeakReference<FileSystemWatcher> WeakWatcher { get; }
            internal SafeFileHandle DirectoryHandle { get; set; }

            internal unsafe void* Buffer { get; set; }
            internal uint BufferByteLength { get; set; }

            internal ThreadPoolBoundHandle? ThreadPoolBinding { get; set; }

            internal PreAllocatedOverlapped? PreAllocatedOverlapped { get; set; }

            public void Dispose()
            {
                unsafe
                {
                    NativeMemory.Free(Buffer);
                    Buffer = null;
                }

                PreAllocatedOverlapped?.Dispose();
                ThreadPoolBinding?.Dispose();
                DirectoryHandle?.Dispose();
            }
        }
    }
}
