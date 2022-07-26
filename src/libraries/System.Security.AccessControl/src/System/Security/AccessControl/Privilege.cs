// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading;
using CultureInfo = System.Globalization.CultureInfo;
using Luid = Interop.Advapi32.LUID;
using static System.Security.Principal.Win32;

namespace System.Security.AccessControl
{
    /// <summary>
    /// Managed wrapper for NT privileges
    /// </summary>
    internal sealed class Privilege
    {
        [ThreadStatic]
        private static TlsContents? t_tlsSlotData;
        private static readonly Dictionary<Luid, string> privileges = new Dictionary<Luid, string>();
        private static readonly Dictionary<string, Luid> luids = new Dictionary<string, Luid>();
        private static readonly ReaderWriterLockSlim privilegeLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private bool needToRevert;
        private bool initialState;
        private bool stateWasChanged;
        private Luid luid;
        private readonly Thread currentThread = Thread.CurrentThread;
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

        //
        // This routine is a wrapper around a hashtable containing mappings
        // of privilege names to LUIDs
        //

        private static Luid LuidFromPrivilege(string privilege)
        {
            Luid luid;
            luid.LowPart = 0;
            luid.HighPart = 0;

            //
            // Look up the privilege LUID inside the cache
            //

            try
            {
                privilegeLock.EnterReadLock();

                if (luids.ContainsKey(privilege))
                {
                    luid = luids[privilege];

                    privilegeLock.ExitReadLock();
                }
                else
                {
                    privilegeLock.ExitReadLock();

                    if (false == Interop.Advapi32.LookupPrivilegeValue(null, privilege, out luid))
                    {
                        int error = Marshal.GetLastWin32Error();

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

                    privilegeLock.EnterWriteLock();
                }
            }
            finally
            {
                if (privilegeLock.IsReadLockHeld)
                {
                    privilegeLock.ExitReadLock();
                }

                if (privilegeLock.IsWriteLockHeld)
                {
                    if (!luids.ContainsKey(privilege))
                    {
                        luids[privilege] = luid;
                        privileges[luid] = privilege;
                    }

                    privilegeLock.ExitWriteLock();
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

            private static volatile SafeTokenHandle processHandle = new SafeTokenHandle(IntPtr.Zero);
            private static readonly object syncRoot = new object();

            #region Constructor and Finalizer

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
                            if (false == Interop.Advapi32.OpenProcessToken(
                                            Interop.Kernel32.GetCurrentProcess(),
                                            TokenAccessLevels.Duplicate,
                                            out localProcessHandle))
                            {
                                cachingError = Marshal.GetLastWin32Error();
                                success = false;
                            }
                            processHandle = localProcessHandle;
                        }
                    }
                }

                try
                {
                    //
                    // Open the thread token; if there is no thread token, get one from
                    // the process token by impersonating self.
                    //

                    SafeTokenHandle? threadHandleBefore = this.threadHandle;
                    error = OpenThreadToken(
                                  TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges,
                                  WinSecurityContext.Process,
                                  out this.threadHandle);
                    unchecked { error &= ~(int)0x80070000; }

                    if (error != 0)
                    {
                        if (success)
                        {
                            this.threadHandle = threadHandleBefore;

                            if (error != Interop.Errors.ERROR_NO_TOKEN)
                            {
                                success = false;
                            }

                            System.Diagnostics.Debug.Assert(this.isImpersonating == false, "Incorrect isImpersonating state");

                            if (success)
                            {
                                error = 0;
                                if (false == Interop.Advapi32.DuplicateTokenEx(
                                                processHandle,
                                                TokenAccessLevels.Impersonate | TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges,
                                                IntPtr.Zero,
                                                Interop.Advapi32.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                                                System.Security.Principal.TokenType.TokenImpersonation,
                                                ref this.threadHandle))
                                {
                                    error = Marshal.GetLastWin32Error();
                                    success = false;
                                }
                            }

                            if (success)
                            {
                                error = SetThreadToken(this.threadHandle);
                                unchecked { error &= ~(int)0x80070000; }

                                if (error != 0)
                                {
                                    success = false;
                                }
                            }

                            if (success)
                            {
                                this.isImpersonating = true;
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
                if (!this.disposed)
                {
                    Dispose(false);
                }
            }
            #endregion

            #region IDisposable implementation

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (this.disposed) return;

                if (disposing)
                {
                    if (this.threadHandle != null)
                    {
                        this.threadHandle.Dispose();
                        this.threadHandle = null!;
                    }
                }

                if (this.isImpersonating)
                {
                    Interop.Advapi32.RevertToSelf();
                }

                this.disposed = true;
            }
            #endregion

            #region Reference Counting

            public void IncrementReferenceCount()
            {
                this.referenceCount++;
            }

            public int DecrementReferenceCount()
            {
                int result = --this.referenceCount;

                if (result == 0)
                {
                    Dispose();
                }

                return result;
            }

            public int ReferenceCountValue
            {
                get { return this.referenceCount; }
            }
            #endregion

            #region Properties

            public SafeTokenHandle ThreadHandle
            {
                get
                {
                    return this.threadHandle!;
                }
            }

            public bool IsImpersonating
            {
                get { return this.isImpersonating; }
            }
            #endregion
        }

        #region Constructors

        public Privilege(string privilegeName)
        {
            ArgumentNullException.ThrowIfNull(privilegeName);

            this.luid = LuidFromPrivilege(privilegeName);
        }
        #endregion

        //
        // Finalizer simply ensures that the privilege was not leaked
        //

        ~Privilege()
        {
            System.Diagnostics.Debug.Assert(!this.needToRevert, "Must revert privileges that you alter!");

            if (this.needToRevert)
            {
                Revert();
            }
        }

        #region Public interface
        public void Enable()
        {
            this.ToggleState(true);
        }

        public bool NeedToRevert
        {
            get { return this.needToRevert; }
        }

        #endregion

        private unsafe void ToggleState(bool enable)
        {
            int error = 0;

            //
            // All privilege operations must take place on the same thread
            //

            if (!this.currentThread.Equals(Thread.CurrentThread))
            {
                throw new InvalidOperationException(SR.InvalidOperation_MustBeSameThread);
            }

            //
            // This privilege was already altered and needs to be reverted before it can be altered again
            //

            if (this.needToRevert)
            {
                throw new InvalidOperationException(SR.InvalidOperation_MustRevertPrivilege);
            }

            try
            {
                //
                // Retrieve TLS state
                //

                this.tlsContents = t_tlsSlotData;

                if (this.tlsContents == null)
                {
                    this.tlsContents = new TlsContents();
                    t_tlsSlotData = this.tlsContents;
                }
                else
                {
                    this.tlsContents.IncrementReferenceCount();
                }

                Interop.Advapi32.TOKEN_PRIVILEGE newState;
                newState.PrivilegeCount = 1;
                newState.Privileges.Luid = this.luid;
                newState.Privileges.Attributes = enable ? Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_ENABLED : Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_DISABLED;

                Interop.Advapi32.TOKEN_PRIVILEGE previousState = default;
                uint previousSize = 0;

                //
                // Place the new privilege on the thread token and remember the previous state.
                //

                if (!Interop.Advapi32.AdjustTokenPrivileges(
                                  this.tlsContents.ThreadHandle,
                                  false,
                                  &newState,
                                  (uint)sizeof(Interop.Advapi32.TOKEN_PRIVILEGE),
                                  &previousState,
                                  &previousSize))
                {
                    error = Marshal.GetLastWin32Error();
                }
                else if (Interop.Errors.ERROR_NOT_ALL_ASSIGNED == Marshal.GetLastWin32Error())
                {
                    error = Interop.Errors.ERROR_NOT_ALL_ASSIGNED;
                }
                else
                {
                    //
                    // This is the initial state that revert will have to go back to
                    //

                    this.initialState = ((previousState.Privileges.Attributes & Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_ENABLED) != 0);

                    //
                    // Remember whether state has changed at all
                    //

                    this.stateWasChanged = (this.initialState != enable);

                    //
                    // If we had to impersonate, or if the privilege state changed we'll need to revert
                    //

                    this.needToRevert = this.tlsContents.IsImpersonating || this.stateWasChanged;
                }
            }
            finally
            {
                if (!this.needToRevert)
                {
                    this.Reset();
                }
            }

            if (error == Interop.Errors.ERROR_NOT_ALL_ASSIGNED)
            {
                throw new PrivilegeNotHeldException(privileges[this.luid]);
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

            if (!this.currentThread.Equals(Thread.CurrentThread))
            {
                throw new InvalidOperationException(SR.InvalidOperation_MustBeSameThread);
            }

            if (!this.NeedToRevert)
            {
                return;
            }

            bool success = true;

            try
            {
                //
                // Only call AdjustTokenPrivileges if we're not going to be reverting to self,
                // on this Revert, since doing the latter obliterates the thread token anyway
                //

                if (this.stateWasChanged &&
                    (this.tlsContents!.ReferenceCountValue > 1 ||
                      !this.tlsContents.IsImpersonating))
                {
                    Interop.Advapi32.TOKEN_PRIVILEGE newState;
                    newState.PrivilegeCount = 1;
                    newState.Privileges.Luid = this.luid;
                    newState.Privileges.Attributes = (this.initialState ? Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_ENABLED : Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_DISABLED);

                    if (!Interop.Advapi32.AdjustTokenPrivileges(
                                      this.tlsContents.ThreadHandle,
                                      false,
                                      &newState,
                                      0,
                                      null,
                                      null))
                    {
                        error = Marshal.GetLastWin32Error();
                        success = false;
                    }
                }
            }
            finally
            {
                if (success)
                {
                    this.Reset();
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
            this.stateWasChanged = false;
            this.initialState = false;
            this.needToRevert = false;

            if (this.tlsContents != null)
            {
                if (0 == this.tlsContents.DecrementReferenceCount())
                {
                    this.tlsContents = null;
                    t_tlsSlotData = null;
                }
            }
        }
    }
}
