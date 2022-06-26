// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace System.Transactions.Oletx
{
    [Security.SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        // Note that this PInvoke does not pass any string params but specifying a charset makes FxCop happy
        [DllImport("System.Transactions.Native.Dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int GetNotificationFactory(
            SafeHandle notificationEventHandle,
            [MarshalAs(UnmanagedType.Interface)] out IDtcProxyShimFactory ppProxyShimFactory
            );

        internal static int S_OK = 0;
        internal static int E_FAIL = -2147467259;  // 0x80004005, -2147467259
        internal static int XACT_S_READONLY = 315394;  // 0x0004D002, 315394
        internal static int XACT_S_SINGLEPHASE = 315401;  // 0x0004D009, 315401
        internal static int XACT_E_ABORTED = -2147168231;  // 0x8004D019, -2147168231
        internal static int XACT_E_NOTRANSACTION = -2147168242;  // 0x8004D00E, -2147168242
        internal static int XACT_E_CONNECTION_DOWN = -2147168228;  // 0x8004D01C, -2147168228
        internal static int XACT_E_REENLISTTIMEOUT = -2147168226;  // 0x8004D01E, -2147168226
        internal static int XACT_E_RECOVERYALREADYDONE = -2147167996;  // 0x8004D104, -2147167996
        internal static int XACT_E_TMNOTAVAILABLE = -2147168229; // 0x8004d01b, -2147168229
        internal static int XACT_E_INDOUBT = -2147168234; // 0x8004d016,
        internal static int XACT_E_ALREADYINPROGRESS = -2147168232; // x08004d018,
        internal static int XACT_E_TOOMANY_ENLISTMENTS = -2147167999; // 0x8004d101
        internal static int XACT_E_PROTOCOL = -2147167995; // 8004d105
        internal static int XACT_E_FIRST = -2147168256; // 0x8004D000
        internal static int XACT_E_LAST = -2147168215; // 0x8004D029
        internal static int XACT_E_NOTSUPPORTED = -2147168241; // 0x8004D00F
        internal static int XACT_E_NETWORK_TX_DISABLED = -2147168220; // 0x8004D024
    }

    internal enum ShimNotificationType
    {
        None = 0,
        Phase0RequestNotify = 1,
        VoteRequestNotify = 2,
        PrepareRequestNotify = 3,
        CommitRequestNotify = 4,
        AbortRequestNotify = 5,
        CommittedNotify = 6,
        AbortedNotify = 7,
        InDoubtNotify = 8,
        EnlistmentTmDownNotify = 9,
        ResourceManagerTmDownNotify = 10
    }

    internal enum OletxPrepareVoteType
    {
        ReadOnly = 0,
        SinglePhase = 1,
        Prepared = 2,
        Failed = 3,
        InDoubt = 4
    }

    internal enum OletxTransactionOutcome
    {
        NotKnownYet = 0,
        Committed = 1,
        Aborted = 2
    }

    internal enum OletxTransactionIsolationLevel
    {
        ISOLATIONLEVEL_UNSPECIFIED = -1,
        ISOLATIONLEVEL_CHAOS = 0x10,
        ISOLATIONLEVEL_READUNCOMMITTED = 0x100,
        ISOLATIONLEVEL_BROWSE = 0x100,
        ISOLATIONLEVEL_CURSORSTABILITY = 0x1000,
        ISOLATIONLEVEL_READCOMMITTED = 0x1000,
        ISOLATIONLEVEL_REPEATABLEREAD = 0x10000,
        ISOLATIONLEVEL_SERIALIZABLE = 0x100000,
        ISOLATIONLEVEL_ISOLATED = 0x100000
    }

    [Flags]
    internal enum OletxTransactionIsoFlags
    {
        ISOFLAG_NONE = 0,
        ISOFLAG_RETAIN_COMMIT_DC = 1,
        ISOFLAG_RETAIN_COMMIT = 2,
        ISOFLAG_RETAIN_COMMIT_NO = 3,
        ISOFLAG_RETAIN_ABORT_DC = 4,
        ISOFLAG_RETAIN_ABORT = 8,
        ISOFLAG_RETAIN_ABORT_NO = 12,
        ISOFLAG_RETAIN_DONTCARE = ISOFLAG_RETAIN_COMMIT_DC | ISOFLAG_RETAIN_ABORT_DC,
        ISOFLAG_RETAIN_BOTH = ISOFLAG_RETAIN_COMMIT | ISOFLAG_RETAIN_ABORT,
        ISOFLAG_RETAIN_NONE = ISOFLAG_RETAIN_COMMIT_NO | ISOFLAG_RETAIN_ABORT_NO,
        ISOFLAG_OPTIMISTIC = 16,
        ISOFLAG_READONLY = 32
    }

    [Flags]
    internal enum OletxXacttc
    {
        XACTTC_NONE = 0,
        XACTTC_SYNC_PHASEONE = 1,
        XACTTC_SYNC_PHASETWO = 2,
        XACTTC_SYNC = 2,
        XACTTC_ASYNC_PHASEONE = 4,
        XACTTC_ASYNC = 4
    }

    internal enum OletxTransactionStatus
    {
        OLETX_TRANSACTION_STATUS_NONE = 0,
        OLETX_TRANSACTION_STATUS_OPENNORMAL = 0x1,
        OLETX_TRANSACTION_STATUS_OPENREFUSED = 0x2,
        OLETX_TRANSACTION_STATUS_PREPARING = 0x4,
        OLETX_TRANSACTION_STATUS_PREPARED = 0x8,
        OLETX_TRANSACTION_STATUS_PREPARERETAINING = 0x10,
        OLETX_TRANSACTION_STATUS_PREPARERETAINED = 0x20,
        OLETX_TRANSACTION_STATUS_COMMITTING = 0x40,
        OLETX_TRANSACTION_STATUS_COMMITRETAINING = 0x80,
        OLETX_TRANSACTION_STATUS_ABORTING = 0x100,
        OLETX_TRANSACTION_STATUS_ABORTED = 0x200,
        OLETX_TRANSACTION_STATUS_COMMITTED = 0x400,
        OLETX_TRANSACTION_STATUS_HEURISTIC_ABORT = 0x800,
        OLETX_TRANSACTION_STATUS_HEURISTIC_COMMIT = 0x1000,
        OLETX_TRANSACTION_STATUS_HEURISTIC_DAMAGE = 0x2000,
        OLETX_TRANSACTION_STATUS_HEURISTIC_DANGER = 0x4000,
        OLETX_TRANSACTION_STATUS_FORCED_ABORT = 0x8000,
        OLETX_TRANSACTION_STATUS_FORCED_COMMIT = 0x10000,
        OLETX_TRANSACTION_STATUS_INDOUBT = 0x20000,
        OLETX_TRANSACTION_STATUS_CLOSED = 0x40000,
        OLETX_TRANSACTION_STATUS_OPEN = 0x3,
        OLETX_TRANSACTION_STATUS_NOTPREPARED = 0x7ffc3,
        OLETX_TRANSACTION_STATUS_ALL = 0x7ffff
    }

    [ComVisible(false)]
    internal struct OletxXactTransInfo
    {
        internal Guid uow;
        internal OletxTransactionIsolationLevel isoLevel;
        internal OletxTransactionIsoFlags isoFlags;
        internal int grfTCSupported;
        internal int grfRMSupported;
        internal int grfTCSupportedRetaining;
        internal int grfRMSupportedRetaining;

        // This structure is only ever filled in by a call to the DTC proxy.  But if we don't have this constructor,
        // the compiler complains with a warning because the fields are never initialized away from their default value.
        // So we added this constructor to get rid of the warning.  But since the structure is only ever filled in by
        // unmanaged code through a proxy call, FXCop complains that this internal method is never called.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal OletxXactTransInfo(Guid guid, OletxTransactionIsolationLevel isoLevel)
        {
            this.uow = guid;
            this.isoLevel = isoLevel;
            this.isoFlags = OletxTransactionIsoFlags.ISOFLAG_NONE;
            this.grfTCSupported = 0;
            this.grfRMSupported = 0;
            this.grfTCSupportedRetaining = 0;
            this.grfRMSupportedRetaining = 0;
        }
    }

    [Security.SuppressUnmanagedCodeSecurity,
    ComImport,
    Guid("A5FAB903-21CB-49eb-93AE-EF72CD45169E"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVoterBallotShim
    {
        void Vote([MarshalAs(UnmanagedType.Bool)] bool voteYes);
    }

    [Security.SuppressUnmanagedCodeSecurity,
    ComImport,
    Guid("55FF6514-948A-4307-A692-73B84E2AF53E"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPhase0EnlistmentShim
    {
        void Unenlist();

        void Phase0Done([MarshalAs(UnmanagedType.Bool)] bool voteYes);
    }

    [Security.SuppressUnmanagedCodeSecurity,
    ComImport,
    Guid("5EC35E09-B285-422c-83F5-1372384A42CC"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEnlistmentShim
    {
        void PrepareRequestDone(OletxPrepareVoteType voteType);

        void CommitRequestDone();

        void AbortRequestDone();
    }

    [Security.SuppressUnmanagedCodeSecurity,
    ComImport,
    Guid("279031AF-B00E-42e6-A617-79747E22DD22"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITransactionShim
    {
        void Commit();

        void Abort();

        void GetITransactionNative([MarshalAs(UnmanagedType.Interface)] out IDtcTransaction transactionNative);

        void Export(
            [MarshalAs(UnmanagedType.U4)] uint whereaboutsSize,
            [MarshalAs(UnmanagedType.LPArray)] byte[] whereabouts,
            [MarshalAs(UnmanagedType.I4)] out int cookieIndex,
            [MarshalAs(UnmanagedType.U4)] out uint cookieSize,
            out CoTaskMemHandle cookieBuffer);

        void CreateVoter(
            IntPtr managedIdentifier,
            [MarshalAs(UnmanagedType.Interface)] out IVoterBallotShim voterBallotShim);

        void GetPropagationToken(
            [MarshalAs(UnmanagedType.U4)] out uint propagationTokeSize,
            out CoTaskMemHandle propagationToken);

        void Phase0Enlist(
            IntPtr managedIdentifier,
            [MarshalAs(UnmanagedType.Interface)] out IPhase0EnlistmentShim phase0EnlistmentShim);

        void GetTransactionDoNotUse(out IntPtr transaction);
    }

    [Security.SuppressUnmanagedCodeSecurity,
    ComImport,
    Guid("27C73B91-99F5-46d5-A247-732A1A16529E"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IResourceManagerShim
    {
        void Enlist(
            [MarshalAs(UnmanagedType.Interface)] ITransactionShim transactionShim,
            IntPtr managedIdentifier,
            [MarshalAs(UnmanagedType.Interface)] out IEnlistmentShim enlistmentShim);

        void Reenlist(
            [MarshalAs(UnmanagedType.U4)] uint prepareInfoSize,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] prepareInfo,
            out OletxTransactionOutcome outcome);

        void ReenlistComplete();
    }

    [Security.SuppressUnmanagedCodeSecurity,
    ComImport,
    Guid("467C8BCB-BDDE-4885-B143-317107468275"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDtcProxyShimFactory
    {
        // See https://github.com/dotnet/runtime/issues/45633
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CoTaskMemHandle))]
        void ConnectToProxy(
            [MarshalAs(UnmanagedType.LPWStr)] string nodeName,
            Guid resourceManagerIdentifier,
            IntPtr managedIdentifier,
            [MarshalAs(UnmanagedType.Bool)] out bool nodeNameMatches,
            [MarshalAs(UnmanagedType.U4)] out uint whereaboutsSize,
            out CoTaskMemHandle whereaboutsBuffer,
            [MarshalAs(UnmanagedType.Interface)] out IResourceManagerShim resourceManagerShim);

        void GetNotification(
            out IntPtr managedIdentifier,
            [MarshalAs(UnmanagedType.I4)] out ShimNotificationType shimNotificationType,
            [MarshalAs(UnmanagedType.Bool)] out bool isSinglePhase,
            [MarshalAs(UnmanagedType.Bool)] out bool abortingHint,
            [MarshalAs(UnmanagedType.Bool)] out bool releaseRequired,
            [MarshalAs(UnmanagedType.U4)] out uint prepareInfoSize,
            out CoTaskMemHandle prepareInfo);

        void ReleaseNotificationLock();

        void BeginTransaction(
            [MarshalAs(UnmanagedType.U4)] uint timeout,
            OletxTransactionIsolationLevel isolationLevel,
            IntPtr managedIdentifier,
            out Guid transactionIdentifier,
            [MarshalAs(UnmanagedType.Interface)] out ITransactionShim transactionShim);

        void CreateResourceManager(
            Guid resourceManagerIdentifier,
            IntPtr managedIdentifier,
            [MarshalAs(UnmanagedType.Interface)] out IResourceManagerShim resourceManagerShim            );

        void Import(
            [MarshalAs(UnmanagedType.U4)] uint cookieSize,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] cookie,
            IntPtr managedIdentifier,
            out Guid transactionIdentifier,
            out OletxTransactionIsolationLevel isolationLevel,
            [MarshalAs(UnmanagedType.Interface)] out ITransactionShim transactionShim);

        void ReceiveTransaction(
            [MarshalAs(UnmanagedType.U4)] uint  propagationTokenSize,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] propgationToken,
            IntPtr managedIdentifier,
            out Guid transactionIdentifier,
            out OletxTransactionIsolationLevel isolationLevel,
            [MarshalAs(UnmanagedType.Interface)] out ITransactionShim transactionShim);

        void CreateTransactionShim(
            [MarshalAs(UnmanagedType.Interface)] IDtcTransaction transactionNative,
            IntPtr managedIdentifier,
            out Guid transactionIdentifier,
            out OletxTransactionIsolationLevel isolationLevel,
            [MarshalAs(UnmanagedType.Interface)] out ITransactionShim transactionShim);
    }

    // We need to leave this here because if we are given an ITransactionNative and need to
    // create an OletxTransaction (OletxInterop.GetTranasctionFromTransactionNative),
    // we want to be able to check to see if we already have one.
    // So we use the GetTransactionInfo method to get the GUID identifier and do various table
    // lookups.
    [Security.SuppressUnmanagedCodeSecurity,
    ComImport,
    Guid("0fb15084-af41-11ce-bd2b-204c4f4f5020"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITransactionNativeInternal
    {
        void Commit(int retaining, [MarshalAs(UnmanagedType.I4)] OletxXacttc commitType, int reserved);

        void Abort(IntPtr reason, int retaining, int async);

        void GetTransactionInfo(out OletxXactTransInfo xactInfo);
    }
}
