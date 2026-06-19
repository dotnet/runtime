// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using static System.Security.Principal.Win32;
using CultureInfo = System.Globalization.CultureInfo;
using Luid = Interop.Advapi32.LUID;

namespace System.Security.AccessControl
{
    /// <summary>
    /// Managed wrapper for NT privileges
    /// </summary>
    internal sealed class Privilege
    {
        [ThreadStatic]
        private static TlsContents? t_tlsSlotData;
        private static readonly Dictionary<Luid, string> s_privileges = new Dictionary<Luid, string>();
        private static readonly Dictionary<string, Luid> s_luids = new Dictionary<string, Luid>();
        private static readonly ReaderWriterLockSlim s_privilegeLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private bool needToRevert;
        private bool initialState;
        private bool stateWasChanged;
        private Luid luid;
        private readonly Thread _currentThread = Thread.CurrentThread;
        private TlsContents? tlsContents;

        public const string CreateToken = "SeCreateTokenPrivilege";
        public const string AssignPrimaryToken = "SeAssignPrimaryTokenPrivilege";
        public const string LockMemory = "SeLockMemoryPrivilege";
        public const string IncreaseQuota = "SeIncreaseQuotaPrivilege";
        public const string UnsolicitedInput = "SeUnsolicitedInputPrivilege";
        public const string MachineAccount = "SeMachineAccountPrivilege";
        public const string TrustedComputingBase = "SeTcbPrivilege";
        public const string Security = "SeSecurityPrivilege";
        public const string TakeOwnership = "SeTakeOwnershipPrivilege";
        public const string LoadDriver = "SeLoadDriverPrivilege";
        public const string SystemProfile = "SeSystemProfilePrivilege";
        public const string SystemTime = "SeSystemtimePrivilege";
        public const string ProfileSingleProcess = "SeProfileSingleProcessPrivilege";
        public const string IncreaseBasePriority = "SeIncreaseBasePriorityPrivilege";
        public const string CreatePageFile = "SeCreatePagefilePrivilege";
        public const string CreatePermanent = "SeCreatePermanentPrivilege";
        public const string Backup = "SeBackupPrivilege";
        public const string Restore = "SeRestorePrivilege";
        public const string Shutdown = "SeShutdownPrivilege";
        public const string Debug = "SeDebugPrivilege";
        public const string Audit = "SeAuditPrivilege";
        public const string SystemEnvironment = "SeSystemEnvironmentPrivilege";
        public const string ChangeNotify = "SeChangeNotifyPrivilege";
        public const string RemoteShutdown = "SeRemoteShutdownPrivilege";
        public const string Undock = "SeUndockPrivilege";
        public const string SyncAgent = "SeSyncAgentPrivilege";
        public const string EnableDelegation = "SeEnableDelegationPrivilege";
        public const string ManageVolume = "SeManageVolumePrivilege";
        public const string Impersonate = "SeImpersonatePrivilege";
        public const string CreateGlobal = "SeCreateGlobalPrivilege";
        public const string TrustedCredentialManagerAccess = "SeTrustedCredManAccessPrivilege";
        public const string ReserveProcessor = "SeReserveProcessorPrivilege";

        // This routine is a wrapper around a hashtable containing mappings
        // of privilege names to LUIDs
        private static Luid LuidFromPrivilege(string privilege)
        {
            Luid luid;
            luid.LowPart = 0;
            luid.HighPart = 0;

            // Look up the privilege LUID inside the cache
            try
            {
                s_privilegeLock.EnterReadLock();

                if (s_luids.TryGetValue(privilege, out luid))
                {
                    s_privilegeLock.ExitReadLock();
                }
                else
                {
                    s_privilegeLock.ExitReadLock();

                    if (!Interop.Advapi32.LookupPrivilegeValue(null, privilege, out luid))
                    {
                        int error = Marshal.GetLastPInvokeError();

                        if (error == Interop.Errors.ERROR_NOT_ENOUGH_MEMORY)
                        {
                            throw new OutOfMemoryException();
                        }
                        else if (error == Interop.Errors.ERROR_ACCESS_DENIED)
                        {
                            throw new UnauthorizedAccessException();
                        }
                        else if (error == Interop.Errors.ERROR_NO_SUCH_PRIVILEGE)
                        {
                            throw new ArgumentException(
                                SR.Format(SR.Argument_InvalidPrivilegeName,
                                privilege));
                        }
                        else
                        {
                            System.Diagnostics.Debug.Fail($"LookupPrivilegeValue() failed with unrecognized error code {error}");
                            throw new InvalidOperationException();
                        }
                    }

                    s_privilegeLock.EnterWriteLock();
                }
            }
            finally
            {
                if (s_privilegeLock.IsReadLockHeld)
                {
                    s_privilegeLock.ExitReadLock();
                }

                if (s_privilegeLock.IsWriteLockHeld)
                {
                    if (s_luids.TryAdd(privilege, luid))
                    {
                        s_privileges[luid] = privilege;
                    }

                    s_privilegeLock.ExitWriteLock();
                }
            }

            return luid;
        }

        private sealed class TlsContents : IDisposable
        {
            private bool disposed;
            private int referenceCount = 1;
            private SafeTokenHandle? threadHandle = new SafeTokenHandle(IntPtr.Zero);
            private readonly bool isImpersonating;

            private static SafeTokenHandle processHandle = new SafeTokenHandle(IntPtr.Zero);
            private static readonly object syncRoot = new object();

            public TlsContents()
            {
                int error = 0;
                int cachingError = 0;
                bool success = true;

                if (processHandle.IsInvalid)
                {
                    lock (syncRoot)
                    {
                        if (processHandle.IsInvalid)
                        {
                            SafeTokenHandle localProcessHandle;
                            if (!Interop.Advapi32.OpenProcessToken(
                                            Interop.Kernel32.GetCurrentProcess(),
                                            TokenAccessLevels.Duplicate,
                                            out localProcessHandle))
                            {
                                cachingError = Marshal.GetLastPInvokeError();
                                success = false;
                            }
                            processHandle = localProcessHandle;
                        }
                    }
                }

                try
                {
                    // Open the thread token; if there is no thread token, get one from
                    // the process token by impersonating self.
                    SafeTokenHandle? threadHandleBefore = threadHandle;
                    error = OpenThreadToken(
                                  TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges,
                                  WinSecurityContext.Process,
                                  out threadHandle);
                    unchecked { error &= ~(int)0x80070000; }

                    if (error != 0)
                    {
                        if (success)
                        {
                            threadHandle = threadHandleBefore;

                            if (error != Interop.Errors.ERROR_NO_TOKEN)
                            {
                                success = false;
                            }

                            System.Diagnostics.Debug.Assert(!isImpersonating, "Incorrect isImpersonating state");

                            if (success)
                            {
                                error = 0;
                                if (!Interop.Advapi32.DuplicateTokenEx(
                                                processHandle,
                                                TokenAccessLevels.Impersonate | TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges,
                                                IntPtr.Zero,
                                                Interop.Advapi32.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                                                System.Security.Principal.TokenType.TokenImpersonation,
                                                ref threadHandle))
                                {
                                    error = Marshal.GetLastPInvokeError();
                                    success = false;
                                }
                            }

                            if (success)
                            {
                                error = SetThreadToken(threadHandle);
                                unchecked { error &= ~(int)0x80070000; }

                                if (error != 0)
                                {
                                    success = false;
                                }
                            }

                            if (success)
                            {
                                isImpersonating = true;
                            }
                        }
                        else
                        {
                            error = cachingError;
                        }
                    }
                    else
                    {
                        success = true;
                    }
                }
                finally
                {
                    if (!success)
                    {
                        Dispose();
                    }
                }

                if (error == Interop.Errors.ERROR_NOT_ENOUGH_MEMORY)
                {
                    throw new OutOfMemoryException();
                }
                else if (error == Interop.Errors.ERROR_ACCESS_DENIED ||
                    error == Interop.Errors.ERROR_CANT_OPEN_ANONYMOUS)
                {
                    throw new UnauthorizedAccessException();
                }
                else if (error != 0)
                {
                    System.Diagnostics.Debug.Fail($"WindowsIdentity.GetCurrentThreadToken() failed with unrecognized error code {error}");
                    throw new InvalidOperationException();
                }
            }

            ~TlsContents()
            {
                if (!disposed)
                {
                    Dispose(false);
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (disposed) return;

                if (disposing)
                {
                    if (threadHandle != null)
                    {
                        threadHandle.Dispose();
                        threadHandle = null!;
                    }
                }

                if (isImpersonating)
                {
                    Interop.Advapi32.RevertToSelf();
                }

                disposed = true;
            }

            public void IncrementReferenceCount()
            {
                referenceCount++;
            }

            public int DecrementReferenceCount()
            {
                int result = --referenceCount;

                if (result == 0)
                {
                    Dispose();
                }

                return result;
            }

            public int ReferenceCountValue
            {
                get { return referenceCount; }
            }

            public SafeTokenHandle ThreadHandle
            {
                get
                {
                    return threadHandle!;
                }
            }

            public bool IsImpersonating
            {
                get { return isImpersonating; }
            }
        }

        public Privilege(string privilegeName)
        {
            ArgumentNullException.ThrowIfNull(privilegeName);

            luid = LuidFromPrivilege(privilegeName);
        }

        // Finalizer simply ensures that the privilege was not leaked
        ~Privilege()
        {
            System.Diagnostics.Debug.Assert(!needToRevert, "Must revert privileges that you alter!");

            if (needToRevert)
            {
                Revert();
            }
        }

        public void Enable()
        {
            ToggleState(true);
        }

        public bool NeedToRevert
        {
            get { return needToRevert; }
        }

        private unsafe void ToggleState(bool enable)
        {
            int error = 0;

            // All privilege operations must take place on the same thread
            if (!_currentThread.Equals(Thread.CurrentThread))
            {
                throw new InvalidOperationException(SR.InvalidOperation_MustBeSameThread);
            }

            // This privilege was already altered and needs to be reverted before it can be altered again
            if (needToRevert)
            {
                throw new InvalidOperationException(SR.InvalidOperation_MustRevertPrivilege);
            }

            try
            {
                // Retrieve TLS state
                tlsContents = t_tlsSlotData;

                if (tlsContents == null)
                {
                    tlsContents = new TlsContents();
                    t_tlsSlotData = tlsContents;
                }
                else
                {
                    tlsContents.IncrementReferenceCount();
                }

                Interop.Advapi32.TOKEN_PRIVILEGE newState;
                newState.PrivilegeCount = 1;
                newState.Privileges.Luid = luid;
                newState.Privileges.Attributes = enable ? Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_ENABLED : Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_DISABLED;

                Interop.Advapi32.TOKEN_PRIVILEGE previousState = default;
                uint previousSize = 0;

                // Place the new privilege on the thread token and remember the previous state.
                if (!Interop.Advapi32.AdjustTokenPrivileges(
                                  tlsContents.ThreadHandle,
                                  false,
                                  &newState,
                                  (uint)sizeof(Interop.Advapi32.TOKEN_PRIVILEGE),
                                  &previousState,
                                  &previousSize))
                {
                    error = Marshal.GetLastPInvokeError();
                }
                else if (Interop.Errors.ERROR_NOT_ALL_ASSIGNED == Marshal.GetLastPInvokeError())
                {
                    error = Interop.Errors.ERROR_NOT_ALL_ASSIGNED;
                }
                else
                {
                    // This is the initial state that revert will have to go back to
                    initialState = ((previousState.Privileges.Attributes & Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_ENABLED) != 0);

                    // Remember whether state has changed at all
                    stateWasChanged = (initialState != enable);

                    // If we had to impersonate, or if the privilege state changed we'll need to revert
                    needToRevert = tlsContents.IsImpersonating || stateWasChanged;
                }
            }
            finally
            {
                if (!needToRevert)
                {
                    Reset();
                }
            }

            if (error == Interop.Errors.ERROR_NOT_ALL_ASSIGNED)
            {
                throw new PrivilegeNotHeldException(s_privileges[luid]);
            }
            if (error == Interop.Errors.ERROR_NOT_ENOUGH_MEMORY)
            {
                throw new OutOfMemoryException();
            }
            else if (error == Interop.Errors.ERROR_ACCESS_DENIED ||
                error == Interop.Errors.ERROR_CANT_OPEN_ANONYMOUS)
            {
                throw new UnauthorizedAccessException();
            }
            else if (error != 0)
            {
                System.Diagnostics.Debug.Fail($"AdjustTokenPrivileges() failed with unrecognized error code {error}");
                throw new InvalidOperationException();
            }
        }

        public unsafe void Revert()
        {
            int error = 0;

            if (!_currentThread.Equals(Thread.CurrentThread))
            {
                throw new InvalidOperationException(SR.InvalidOperation_MustBeSameThread);
            }

            if (!NeedToRevert)
            {
                return;
            }

            bool success = true;

            try
            {
                // Only call AdjustTokenPrivileges if we're not going to be reverting to self,
                // on this Revert, since doing the latter obliterates the thread token anyway
                if (stateWasChanged &&
                    (tlsContents!.ReferenceCountValue > 1 ||
                      !tlsContents.IsImpersonating))
                {
                    Interop.Advapi32.TOKEN_PRIVILEGE newState;
                    newState.PrivilegeCount = 1;
                    newState.Privileges.Luid = luid;
                    newState.Privileges.Attributes = (initialState ? Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_ENABLED : Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_DISABLED);

                    if (!Interop.Advapi32.AdjustTokenPrivileges(
                                      tlsContents.ThreadHandle,
                                      false,
                                      &newState,
                                      0,
                                      null,
                                      null))
                    {
                        error = Marshal.GetLastPInvokeError();
                        success = false;
                    }
                }
            }
            finally
            {
                if (success)
                {
                    Reset();
                }
            }

            if (error == Interop.Errors.ERROR_NOT_ENOUGH_MEMORY)
            {
                throw new OutOfMemoryException();
            }
            else if (error == Interop.Errors.ERROR_ACCESS_DENIED)
            {
                throw new UnauthorizedAccessException();
            }
            else if (error != 0)
            {
                System.Diagnostics.Debug.Fail($"AdjustTokenPrivileges() failed with unrecognized error code {error}");
                throw new InvalidOperationException();
            }
        }

        private void Reset()
        {
            stateWasChanged = false;
            initialState = false;
            needToRevert = false;

            if (tlsContents != null)
            {
                if (0 == tlsContents.DecrementReferenceCount())
                {
                    tlsContents = null;
                    t_tlsSlotData = null;
                }
            }
        }
    }
}
