// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace System.IO
{
    public partial class FileSystemWatcher
    {
        private void StartRaisingEvents()
        {
            // If we're attached, don't do anything.
            if (!IsHandleInvalid)
            {
                return;
            }

            // Create handle to directory being monitored
            _directoryHandle = NativeMethods.CreateFile(_directory,            // Directory name
                                UnsafeNativeMethods.FILE_LIST_DIRECTORY,           // access (read-write) mode
                                UnsafeNativeMethods.FILE_SHARE_READ |
                                    UnsafeNativeMethods.FILE_SHARE_DELETE |
                                    UnsafeNativeMethods.FILE_SHARE_WRITE,          // share mode
                                null,                                              // security descriptor
                                UnsafeNativeMethods.OPEN_EXISTING,                 // how to create
                                UnsafeNativeMethods.FILE_FLAG_BACKUP_SEMANTICS |
                                    UnsafeNativeMethods.FILE_FLAG_OVERLAPPED,      // file attributes
                                new SafeFileHandle(IntPtr.Zero, false)             // file with attributes to copy
                            );

            if (IsHandleInvalid)
            {
                throw new FileNotFoundException(SR.Format(SR.FSW_IOError, _directory));
            }

            _stopListening = false;
            // Start ignoring all events that were initiated before this.
            Interlocked.Increment(ref _currentSession);

            // Attach handle to thread pool

            _threadPoolBinding = ThreadPoolBoundHandle.BindHandle(_directoryHandle);
            _enabled = true;

            // Setup IO completion port
            Monitor(null);
        }

        /// <devdoc>
        ///    Stops monitoring the specified directory.
        /// </devdoc>
        private void StopRaisingEvents()
        {
            // If we're not attached, do nothing.
            if (IsHandleInvalid)
            {
                return;
            }

            // Close directory handle
            // This operation doesn't need to be atomic because the API will deal with a closed
            // handle appropriately.
            // Ensure that the directoryHandle is set to INVALID_HANDLE before closing it, so that
            // the Monitor() can shutdown appropriately.
            // If we get here while asynchronously waiting on a change notification, closing the
            // directory handle should cause CompletionStatusChanged be be called
            // thus freeing the pinned buffer.
            _stopListening = true;
            _directoryHandle.Dispose();
            _threadPoolBinding.Dispose();

            // Start ignoring all events occurring after this.
            Interlocked.Increment(ref _currentSession);

            // Set enabled to false
            _enabled = false;
        }

        private void FinalizeDispose()
        {
            // We must explicitly dispose the handle to insure it gets closed before this object is finalized.
            // Otherwise it is possible that the GC will decide to finalize the handle after this
            // leaving a window of time where our callback could be invoked on a non-existent object.
            _stopListening = true;
            if (!IsHandleInvalid)
            {
                _directoryHandle.Dispose();
                _threadPoolBinding.Dispose();
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        // Unmanaged handle to monitored directory
        private SafeFileHandle _directoryHandle;

        private ThreadPoolBoundHandle _threadPoolBinding;

        // Current "session" ID to ignore old events whenever we stop then restart.
        private int _currentSession;

        // Thread gate holder and constats
        private bool _stopListening = false;

        private bool IsHandleInvalid
        {
            get
            {
                return (_directoryHandle == null || _directoryHandle.IsInvalid || _directoryHandle.IsClosed);
            }
        }

        /// <devdoc>
        ///     Calls native API and sets up handle with the directory change API.
        /// </devdoc>
        /// <internalonly/>
        private unsafe void Monitor(byte[] buffer)
        {
            if (!_enabled || IsHandleInvalid)
            {
                return;
            }

            if (buffer == null)
            {
                buffer = AllocateBuffer();
            }

            // Pass "session" counter to callback:
            FSWAsyncResult asyncResult = new FSWAsyncResult();
            asyncResult.session = _currentSession;
            asyncResult.buffer = buffer;

            NativeOverlapped* overlappedPointer = _threadPoolBinding.AllocateNativeOverlapped(new IOCompletionCallback(this.CompletionStatusChanged), asyncResult, buffer);

            // Can now call OS:
            int size;
            bool ok = false;

            try
            {
                // There could be a race in user code between calling StopRaisingEvents (where we close the handle)
                // and when we get here from CompletionStatusChanged.
                // We might need to take a lock to prevent race absolutely, instead just catch
                // ObjectDisposedException from SafeHandle in case it is disposed
                if (!IsHandleInvalid)
                {
                    // An interrupt is possible here
                    fixed (byte* buffPtr = buffer)
                    {
                        ok = UnsafeNativeMethods.ReadDirectoryChangesW(_directoryHandle,
                                                           buffPtr,
                                                           _internalBufferSize,
                                                           _includeSubdirectories ? 1 : 0,
                                                           (int)_notifyFilters,
                                                           out size,
                                                           overlappedPointer,
                                                           IntPtr.Zero);
                    }
                }
            }
            catch (ObjectDisposedException)
            { //Ignore
            }
            catch (ArgumentNullException)
            { //Ignore
                Debug.Assert(IsHandleInvalid, "ArgumentNullException from something other than SafeHandle?");
            }
            finally
            {
                if (!ok)
                {
                    _threadPoolBinding.FreeNativeOverlapped(overlappedPointer);

                    // If the handle was for some reason changed or closed during this call, then don't throw an
                    // exception.  Else, it's a valid error.
                    if (!IsHandleInvalid)
                    {
                        OnError(new ErrorEventArgs(new Win32Exception()));
                    }
                }
            }
        }

        /// <devdoc>
        ///     Callback from thread pool.
        /// </devdoc>
        /// <internalonly/>
        private unsafe void CompletionStatusChanged(uint errorCode, uint numBytes, NativeOverlapped* overlappedPointer)
        {
            FSWAsyncResult asyncResult = (FSWAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(overlappedPointer);

            try
            {
                if (_stopListening)
                {
                    return;
                }

                lock (this)
                {
                    if (errorCode != 0)
                    {
                        if (errorCode == 995 /* ERROR_OPERATION_ABORTED */)
                        {
                            //Inside a service the first completion status is false
                            //cannot return without monitoring again.
                            //Because this return statement is inside a try/finally block,
                            //the finally block will execute. It does restart the monitoring.
                            return;
                        }
                        else
                        {
                            OnError(new ErrorEventArgs(new Win32Exception((int)errorCode)));
                            EnableRaisingEvents = false;
                            return;
                        }
                    }

                    // Ignore any events that occurred before this "session",
                    // so we don't get changed or error events after we
                    // told FSW to stop.
                    if (asyncResult.session != _currentSession)
                        return;


                    if (numBytes == 0)
                    {
                        NotifyInternalBufferOverflowEvent();
                    }
                    else
                    {  // Else, parse each of them and notify appropriate delegates
                        /******
                            Format for the buffer is the following C struct:

                            typedef struct _FILE_NOTIFY_INFORMATION {
                               DWORD NextEntryOffset;
                               DWORD Action;
                               DWORD FileNameLength;
                               WCHAR FileName[1];
                            } FILE_NOTIFY_INFORMATION;

                            NOTE1: FileNameLength is length in bytes.
                            NOTE2: The Filename is a Unicode string that's NOT NULL terminated.
                            NOTE3: A NextEntryOffset of zero means that it's the last entry
                        *******/

                        // Parse the file notify buffer:
                        int offset = 0;
                        int nextOffset, action, nameLength;
                        string oldName = null;
                        string name = null;

                        do
                        {
                            fixed (byte* buffPtr = asyncResult.buffer)
                            {
                                // Get next offset:
                                nextOffset = *((int*)(buffPtr + offset));

                                // Get change flag:
                                action = *((int*)(buffPtr + offset + 4));

                                // Get filename length (in bytes):
                                nameLength = *((int*)(buffPtr + offset + 8));
                                name = new string((char*)(buffPtr + offset + 12), 0, nameLength / 2);
                            }


                            /* A slightly convoluted piece of code follows.  Here's what's happening:

                               We wish to collapse the poorly done rename notifications from the
                               ReadDirectoryChangesW API into a nice rename event. So to do that,
                               it's assumed that a FILE_ACTION_RENAMED_OLD_NAME will be followed
                               immediately by a FILE_ACTION_RENAMED_NEW_NAME in the buffer, which is
                               all that the following code is doing.

                               On a FILE_ACTION_RENAMED_OLD_NAME, it asserts that no previous one existed
                               and saves its name.  If there are no more events in the buffer, it'll
                               assert and fire a RenameEventArgs with the Name field null.

                               If a NEW_NAME action comes in with no previous OLD_NAME, we assert and fire
                               a rename event with the OldName field null.

                               If the OLD_NAME and NEW_NAME actions are indeed there one after the other,
                               we'll fire the RenamedEventArgs normally and clear oldName.

                               If the OLD_NAME is followed by another action, we assert and then fire the
                               rename event with the Name field null and then fire the next action.

                               In case it's not a OLD_NAME or NEW_NAME action, we just fire the event normally.

                               (Phew!)
                             */

                            // If the action is RENAMED_FROM, save the name of the file
                            if (action == Direct.FILE_ACTION_RENAMED_OLD_NAME)
                            {
                                Debug.Assert(oldName == null, "FileSystemWatcher: Two FILE_ACTION_RENAMED_OLD_NAME " +
                                                              "in a row!  [" + oldName + "], [ " + name + "]");

                                oldName = name;
                            }
                            else if (action == Direct.FILE_ACTION_RENAMED_NEW_NAME)
                            {
                                if (oldName != null)
                                {
                                    NotifyRenameEventArgs(WatcherChangeTypes.Renamed, name, oldName);
                                    oldName = null;
                                }
                                else
                                {
                                    Debug.Fail("FileSystemWatcher: FILE_ACTION_RENAMED_NEW_NAME with no" +
                                                                  "old name! [ " + name + "]");

                                    NotifyRenameEventArgs(WatcherChangeTypes.Renamed, name, oldName);
                                    oldName = null;
                                }
                            }
                            else
                            {
                                if (oldName != null)
                                {
                                    Debug.Fail("FileSystemWatcher: FILE_ACTION_RENAMED_OLD_NAME with no" +
                                                                  "new name!  [" + oldName + "]");

                                    NotifyRenameEventArgs(WatcherChangeTypes.Renamed, null, oldName);
                                    oldName = null;
                                }

                                // Notify each file of change
                                NotifyFileSystemEventArgs(action, name);
                            }

                            offset += nextOffset;
                        } while (nextOffset != 0);

                        if (oldName != null)
                        {
                            Debug.Fail("FileSystemWatcher: FILE_ACTION_RENAMED_OLD_NAME with no" +
                                                          "new name!  [" + oldName + "]");

                            NotifyRenameEventArgs(WatcherChangeTypes.Renamed, null, oldName);
                            oldName = null;
                        }
                    }
                }
            }
            finally
            {
                _threadPoolBinding.FreeNativeOverlapped(overlappedPointer);
                if (!_stopListening)
                {
                    Monitor(asyncResult.buffer);
                }
            }
        }

        // Additional state information to pass to callback.  Note that we
        // never return this object to users, but we do pass state in it.
        private sealed class FSWAsyncResult
        {
            internal int session;
            internal byte[] buffer;
        }

        /// <devdoc>
        ///     Raises the event to each handler in the list.
        /// </devdoc>
        /// <internalonly/>
        private void NotifyFileSystemEventArgs(int action, string name)
        {
            switch (action)
            {
                case Direct.FILE_ACTION_ADDED:
                    NotifyFileSystemEventArgs(WatcherChangeTypes.Created, name);
                    break;
                case Direct.FILE_ACTION_REMOVED:
                    NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, name);
                    break;
                case Direct.FILE_ACTION_MODIFIED:
                    NotifyFileSystemEventArgs(WatcherChangeTypes.Changed, name);
                    break;
                default:
                    Debug.Fail("Unknown FileSystemEvent action type!  Value: " + action);
                    break;
            }
        }

        /// <devdoc>
        ///    Helper class to hold to N/Direct call declaration and flags.
        /// </devdoc>
        private static class Direct
        {
            // All possible action flags
            public const int FILE_ACTION_ADDED = 1;
            public const int FILE_ACTION_REMOVED = 2;
            public const int FILE_ACTION_MODIFIED = 3;
            public const int FILE_ACTION_RENAMED_OLD_NAME = 4;
            public const int FILE_ACTION_RENAMED_NEW_NAME = 5;

            // All possible notifications flags
            public const int FILE_NOTIFY_CHANGE_FILE_NAME = 0x00000001;
            public const int FILE_NOTIFY_CHANGE_DIR_NAME = 0x00000002;
            public const int FILE_NOTIFY_CHANGE_NAME = 0x00000003;
            public const int FILE_NOTIFY_CHANGE_ATTRIBUTES = 0x00000004;
            public const int FILE_NOTIFY_CHANGE_SIZE = 0x00000008;
            public const int FILE_NOTIFY_CHANGE_LAST_WRITE = 0x00000010;
            public const int FILE_NOTIFY_CHANGE_LAST_ACCESS = 0x00000020;
            public const int FILE_NOTIFY_CHANGE_CREATION = 0x00000040;
            public const int FILE_NOTIFY_CHANGE_SECURITY = 0x00000100;
        }
    }
}
