// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Security.Authentication;
using System.Runtime.CompilerServices;

namespace System.DirectoryServices.Protocols
{
    public delegate LdapConnection QueryForConnectionCallback(LdapConnection primaryConnection, LdapConnection referralFromConnection, string newDistinguishedName, LdapDirectoryIdentifier identifier, NetworkCredential credential, long currentUserToken);
    public delegate bool NotifyOfNewConnectionCallback(LdapConnection primaryConnection, LdapConnection referralFromConnection, string newDistinguishedName, LdapDirectoryIdentifier identifier, LdapConnection newConnection, NetworkCredential credential, long currentUserToken, int errorCodeFromBind);
    public delegate void DereferenceConnectionCallback(LdapConnection primaryConnection, LdapConnection connectionToDereference);
    public delegate X509Certificate QueryClientCertificateCallback(LdapConnection connection, byte[][] trustedCAs);
    public delegate bool VerifyServerCertificateCallback(LdapConnection connection, X509Certificate certificate);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int QUERYFORCONNECTIONInternal(IntPtr Connection, IntPtr ReferralFromConnection, IntPtr NewDNPtr, IntPtr HostName, int PortNumber, SEC_WINNT_AUTH_IDENTITY_EX.Native* SecAuthIdentity, Luid* CurrentUserToken, IntPtr* ConnectionToUse);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate Interop.BOOL NOTIFYOFNEWCONNECTIONInternal(IntPtr Connection, IntPtr ReferralFromConnection, IntPtr NewDNPtr, IntPtr HostName, IntPtr NewConnection, int PortNumber, SEC_WINNT_AUTH_IDENTITY_EX.Native* SecAuthIdentity, Luid* CurrentUser, int ErrorCodeFromBind);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int DEREFERENCECONNECTIONInternal(IntPtr Connection, IntPtr ConnectionToDereference);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate Interop.BOOL VERIFYSERVERCERT(IntPtr Connection, IntPtr pServerCert);

    [Flags]
    public enum LocatorFlags : long
    {
        None = 0,
        ForceRediscovery = 0x00000001,
        DirectoryServicesRequired = 0x00000010,
        DirectoryServicesPreferred = 0x00000020,
        GCRequired = 0x00000040,
        PdcRequired = 0x00000080,
        IPRequired = 0x00000200,
        KdcRequired = 0x00000400,
        TimeServerRequired = 0x00000800,
        WriteableRequired = 0x00001000,
        GoodTimeServerPreferred = 0x00002000,
        AvoidSelf = 0x00004000,
        OnlyLdapNeeded = 0x00008000,
        IsFlatName = 0x00010000,
        IsDnsName = 0x00020000,
        ReturnDnsName = 0x40000000,
        ReturnFlatName = 0x80000000,
    }

    public enum SecurityProtocol
    {
        Pct1Server = 0x1,
        Pct1Client = 0x2,
        Ssl2Server = 0x4,
        Ssl2Client = 0x8,
        Ssl3Server = 0x10,
        Ssl3Client = 0x20,
        Tls1Server = 0x40,
        Tls1Client = 0x80
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class SecurityPackageContextConnectionInformation
    {
        // Not marked as readonly to enable passing to Unsafe.As in GetPinnableReference.
        private SecurityProtocol _securityProtocol;
        private readonly CipherAlgorithmType _identifier;
        private readonly int _strength;
        private readonly HashAlgorithmType _hashAlgorithm;
        private readonly int _hashStrength;
        private readonly int _keyExchangeAlgorithm;
        private readonly int _exchangeStrength;

        internal SecurityPackageContextConnectionInformation()
        {
        }

        public SecurityProtocol Protocol => _securityProtocol;

        public CipherAlgorithmType AlgorithmIdentifier => _identifier;

        public int CipherStrength => _strength;

        public HashAlgorithmType Hash => _hashAlgorithm;

        public int HashStrength => _hashStrength;

        public int KeyExchangeAlgorithm => _keyExchangeAlgorithm;

        public int ExchangeStrength => _exchangeStrength;

        internal ref readonly byte GetPinnableReference() => ref Unsafe.As<SecurityProtocol, byte>(ref _securityProtocol);
    }

    public sealed class ReferralCallback
    {
        public ReferralCallback()
        {
        }

        public QueryForConnectionCallback QueryForConnection { get; set; }

        public NotifyOfNewConnectionCallback NotifyNewConnection { get; set; }

        public DereferenceConnectionCallback DereferenceConnection { get; set; }
    }

    internal struct SecurityHandle
    {
        public IntPtr Lower;
        public IntPtr Upper;
    }

    public partial class LdapSessionOptions
    {
        private readonly LdapConnection _connection;
        private ReferralCallback _callbackRoutine = new ReferralCallback();
        internal QueryClientCertificateCallback _clientCertificateDelegate;
        private VerifyServerCertificateCallback _serverCertificateDelegate;

        private readonly QUERYFORCONNECTIONInternal _queryDelegate;
        private readonly NOTIFYOFNEWCONNECTIONInternal _notifiyDelegate;
        private readonly DEREFERENCECONNECTIONInternal _dereferenceDelegate;
        private readonly VERIFYSERVERCERT _serverCertificateRoutine;

        internal unsafe LdapSessionOptions(LdapConnection connection)
        {
            _connection = connection;
            _queryDelegate = new QUERYFORCONNECTIONInternal(ProcessQueryConnection);
            _notifiyDelegate = new NOTIFYOFNEWCONNECTIONInternal(ProcessNotifyConnection);
            _dereferenceDelegate = new DEREFERENCECONNECTIONInternal(ProcessDereferenceConnection);
            _serverCertificateRoutine = new VERIFYSERVERCERT(ProcessServerCertificate);
        }

        public int ReferralHopLimit
        {
            get => GetIntValueHelper(LdapOption.LDAP_OPT_REFERRAL_HOP_LIMIT);
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.ValidValue, nameof(value));
                }

                SetIntValueHelper(LdapOption.LDAP_OPT_REFERRAL_HOP_LIMIT, value);
            }
        }

        public string HostName
        {
            get => GetStringValueHelper(LdapOption.LDAP_OPT_HOST_NAME, false);
            set => SetStringValueHelper(LdapOption.LDAP_OPT_HOST_NAME, value);
        }

        public string DomainName
        {
            get => GetStringValueHelper(LdapOption.LDAP_OPT_DNSDOMAIN_NAME, true);
            set => SetStringValueHelper(LdapOption.LDAP_OPT_DNSDOMAIN_NAME, value);
        }

        public LocatorFlags LocatorFlag
        {
            get
            {
                int result = GetIntValueHelper(LdapOption.LDAP_OPT_GETDSNAME_FLAGS);
                return (LocatorFlags)result;
            }
            set
            {
                // We don't do validation to the dirsync flag here as underneath API does not check for it and we don't want to put
                // unnecessary limitation on it.
                SetIntValueHelper(LdapOption.LDAP_OPT_GETDSNAME_FLAGS, (int)value);
            }
        }

        public bool HostReachable
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_HOST_REACHABLE);
                return outValue == 1;
            }
        }

        public TimeSpan PingKeepAliveTimeout
        {
            get
            {
                int result = GetIntValueHelper(LdapOption.LDAP_OPT_PING_KEEP_ALIVE);
                return new TimeSpan(result * TimeSpan.TicksPerSecond);
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException(SR.NoNegativeTimeLimit, nameof(value));
                }

                // Prevent integer overflow.
                if (value.TotalSeconds > int.MaxValue)
                {
                    throw new ArgumentException(SR.TimespanExceedMax, nameof(value));
                }

                int seconds = (int)(value.Ticks / TimeSpan.TicksPerSecond);
                SetIntValueHelper(LdapOption.LDAP_OPT_PING_KEEP_ALIVE, seconds);
            }
        }

        public int PingLimit
        {
            get => GetIntValueHelper(LdapOption.LDAP_OPT_PING_LIMIT);
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.ValidValue, nameof(value));
                }

                SetIntValueHelper(LdapOption.LDAP_OPT_PING_LIMIT, value);
            }
        }

        public TimeSpan PingWaitTimeout
        {
            get
            {
                int result = GetIntValueHelper(LdapOption.LDAP_OPT_PING_WAIT_TIME);
                return new TimeSpan(result * TimeSpan.TicksPerMillisecond);
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException(SR.NoNegativeTimeLimit, nameof(value));
                }

                // Prevent integer overflow.
                if (value.TotalMilliseconds > int.MaxValue)
                {
                    throw new ArgumentException(SR.TimespanExceedMax, nameof(value));
                }

                int milliseconds = (int)(value.Ticks / TimeSpan.TicksPerMillisecond);
                SetIntValueHelper(LdapOption.LDAP_OPT_PING_WAIT_TIME, milliseconds);
            }
        }

        public bool AutoReconnect
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_AUTO_RECONNECT);
                return outValue == 1;
            }
            set
            {
                int temp = value ? 1 : 0;
                SetIntValueHelper(LdapOption.LDAP_OPT_AUTO_RECONNECT, temp);
            }
        }

        public int SspiFlag
        {
            get => GetIntValueHelper(LdapOption.LDAP_OPT_SSPI_FLAGS);
            set => SetIntValueHelper(LdapOption.LDAP_OPT_SSPI_FLAGS, value);
        }

        public SecurityPackageContextConnectionInformation SslInformation
        {
            get
            {
                if (_connection._disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                var secInfo = new SecurityPackageContextConnectionInformation();
                int error = LdapPal.GetSecInfoOption(_connection._ldapHandle, LdapOption.LDAP_OPT_SSL_INFO, secInfo);
                ErrorChecking.CheckAndSetLdapError(error);

                return secInfo;
            }
        }

        public object SecurityContext
        {
            get
            {
                if (_connection._disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                SecurityHandle tempHandle = default;

                int error = LdapPal.GetSecurityHandleOption(_connection._ldapHandle, LdapOption.LDAP_OPT_SECURITY_CONTEXT, ref tempHandle);
                ErrorChecking.CheckAndSetLdapError(error);

                return tempHandle;
            }
        }

        public bool Signing
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_SIGN);
                return outValue == 1;
            }
            set
            {
                int temp = value ? 1 : 0;
                SetIntValueHelper(LdapOption.LDAP_OPT_SIGN, temp);
            }
        }

        public bool Sealing
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_ENCRYPT);
                return outValue == 1;
            }
            set
            {
                int temp = value ? 1 : 0;
                SetIntValueHelper(LdapOption.LDAP_OPT_ENCRYPT, temp);
            }
        }

        public string SaslMethod
        {
            get => GetStringValueHelper(LdapOption.LDAP_OPT_SASL_METHOD, true);
            set => SetStringValueHelper(LdapOption.LDAP_OPT_SASL_METHOD, value);
        }

        public bool RootDseCache
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_ROOTDSE_CACHE);
                return outValue == 1;
            }
            set
            {
                int temp = value ? 1 : 0;
                SetIntValueHelper(LdapOption.LDAP_OPT_ROOTDSE_CACHE, temp);
            }
        }

        public bool TcpKeepAlive
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_TCP_KEEPALIVE);
                return outValue == 1;
            }
            set
            {
                int temp = value ? 1 : 0;
                SetIntValueHelper(LdapOption.LDAP_OPT_TCP_KEEPALIVE, temp);
            }
        }

        public TimeSpan SendTimeout
        {
            get
            {
                int result = GetIntValueHelper(LdapOption.LDAP_OPT_SEND_TIMEOUT);
                return new TimeSpan(result * TimeSpan.TicksPerSecond);
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException(SR.NoNegativeTimeLimit, nameof(value));
                }

                // Prevent integer overflow.
                if (value.TotalSeconds > int.MaxValue)
                {
                    throw new ArgumentException(SR.TimespanExceedMax, nameof(value));
                }

                int seconds = (int)(value.Ticks / TimeSpan.TicksPerSecond);
                SetIntValueHelper(LdapOption.LDAP_OPT_SEND_TIMEOUT, seconds);
            }
        }

        public ReferralCallback ReferralCallback
        {
            get
            {
                if (_connection._disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                return _callbackRoutine;
            }
            set
            {
                if (_connection._disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                var tempCallback = new ReferralCallback();

                if (value != null)
                {
                    tempCallback.QueryForConnection = value.QueryForConnection;
                    tempCallback.NotifyNewConnection = value.NotifyNewConnection;
                    tempCallback.DereferenceConnection = value.DereferenceConnection;
                }
                else
                {
                    tempCallback.QueryForConnection = null;
                    tempCallback.NotifyNewConnection = null;
                    tempCallback.DereferenceConnection = null;
                }

                ProcessCallBackRoutine(tempCallback);
                _callbackRoutine = value;
            }
        }

        public QueryClientCertificateCallback QueryClientCertificate
        {
            get
            {
                if (_connection._disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                return _clientCertificateDelegate;
            }
            set
            {
                if (_connection._disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                if (value != null)
                {
                    int certError = LdapPal.SetClientCertOption(_connection._ldapHandle, LdapOption.LDAP_OPT_CLIENT_CERTIFICATE, _connection._clientCertificateRoutine);
                    if (certError != (int)ResultCode.Success)
                    {
                        if (LdapErrorMappings.IsLdapError(certError))
                        {
                            string certerrorMessage = LdapErrorMappings.MapResultCode(certError);
                            throw new LdapException(certError, certerrorMessage);
                        }
                        else
                        {
                            throw new LdapException(certError);
                        }
                    }

                    // A certificate callback is specified so automatic bind is disabled.
                    _connection.AutoBind = false;
                }

                _clientCertificateDelegate = value;
            }
        }

        public VerifyServerCertificateCallback VerifyServerCertificate
        {
            get
            {
                if (_connection._disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                return _serverCertificateDelegate;
            }
            set
            {
                if (_connection._disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                if (value != null)
                {
                    int error = LdapPal.SetServerCertOption(_connection._ldapHandle, LdapOption.LDAP_OPT_SERVER_CERTIFICATE, _serverCertificateRoutine);
                    ErrorChecking.CheckAndSetLdapError(error);
                }

                _serverCertificateDelegate = value;
            }
        }

        // In practice, this apparently rarely if ever contains useful text
        internal string ServerErrorMessage => GetStringValueHelper(LdapOption.LDAP_OPT_SERVER_ERROR, true);

        internal DereferenceAlias DerefAlias
        {
            get => (DereferenceAlias)GetIntValueHelper(LdapOption.LDAP_OPT_DEREF);
            set => SetIntValueHelper(LdapOption.LDAP_OPT_DEREF, (int)value);
        }

        internal void SetFqdnRequired()
        {
            SetIntValueHelper(LdapOption.LDAP_OPT_AREC_EXCLUSIVE, 1);
        }

        public void FastConcurrentBind()
        {
            if (_connection._disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }


            // Bump up the protocol version
            ProtocolVersion = 3;

            // Do the fast concurrent bind.
            int inValue = 1;
            int error = LdapPal.SetIntOption(_connection._ldapHandle, LdapOption.LDAP_OPT_FAST_CONCURRENT_BIND, ref inValue);
            ErrorChecking.CheckAndSetLdapError(error);
        }

        public unsafe void StartTransportLayerSecurity(DirectoryControlCollection controls)
        {
            IntPtr serverControlArray = IntPtr.Zero;
            LdapControl[] managedServerControls = null;
            IntPtr clientControlArray = IntPtr.Zero;
            LdapControl[] managedClientControls = null;
            IntPtr ldapResult = IntPtr.Zero;
            IntPtr referral = IntPtr.Zero;

            int serverError = 0;
            Uri[] responseReferral = null;

            if (_connection._disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            try
            {
                IntPtr tempPtr = IntPtr.Zero;

                // build server control
                managedServerControls = LdapConnection.BuildControlArray(controls, true);
                int structSize = Marshal.SizeOf(typeof(LdapControl));
                if (managedServerControls != null)
                {
                    serverControlArray = Utility.AllocHGlobalIntPtrArray(managedServerControls.Length + 1);
                    for (int i = 0; i < managedServerControls.Length; i++)
                    {
                        IntPtr controlPtr = Marshal.AllocHGlobal(structSize);
                        Marshal.StructureToPtr(managedServerControls[i], controlPtr, false);
                        tempPtr = (IntPtr)((long)serverControlArray + IntPtr.Size * i);
                        Marshal.WriteIntPtr(tempPtr, controlPtr);
                    }

                    tempPtr = (IntPtr)((long)serverControlArray + IntPtr.Size * managedServerControls.Length);
                    Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);
                }

                // Build client control.
                managedClientControls = LdapConnection.BuildControlArray(controls, false);
                if (managedClientControls != null)
                {
                    clientControlArray = Utility.AllocHGlobalIntPtrArray(managedClientControls.Length + 1);
                    for (int i = 0; i < managedClientControls.Length; i++)
                    {
                        IntPtr controlPtr = Marshal.AllocHGlobal(structSize);
                        Marshal.StructureToPtr(managedClientControls[i], controlPtr, false);
                        tempPtr = (IntPtr)((long)clientControlArray + IntPtr.Size * i);
                        Marshal.WriteIntPtr(tempPtr, controlPtr);
                    }

                    tempPtr = (IntPtr)((long)clientControlArray + IntPtr.Size * managedClientControls.Length);
                    Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);
                }

                int error = LdapPal.StartTls(_connection._ldapHandle, ref serverError, ref ldapResult, serverControlArray, clientControlArray);
                if (ldapResult != IntPtr.Zero)
                {
                    // Parse the referral.
                    int resultError = LdapPal.ParseResultReferral(_connection._ldapHandle, ldapResult, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref referral, IntPtr.Zero, 0 /* not free it */);
                    if (resultError == 0 && referral != IntPtr.Zero)
                    {
                        char** referralPtr = (char**)referral;
                        char* singleReferral = referralPtr[0];
                        int i = 0;
                        ArrayList referralList = new ArrayList();
                        while (singleReferral != null)
                        {
                            string s = LdapPal.PtrToString((IntPtr)singleReferral);
                            referralList.Add(s);

                            i++;
                            singleReferral = referralPtr[i];
                        }

                        // Free heap memory.
                        if (referral != IntPtr.Zero)
                        {
                            LdapPal.FreeValue(referral);
                            referral = IntPtr.Zero;
                        }

                        if (referralList.Count > 0)
                        {
                            responseReferral = new Uri[referralList.Count];
                            for (int j = 0; j < referralList.Count; j++)
                            {
                                responseReferral[j] = new Uri((string)referralList[j]);
                            }
                        }
                    }
                }

                if (error != (int)ResultCode.Success)
                {
                    if (Utility.IsResultCode((ResultCode)error))
                    {
                        // If the server failed request for whatever reason, the ldap_start_tls returns LDAP_OTHER
                        // and the ServerReturnValue will contain the error code from the server.
                        if (error == (int)ResultCode.Other)
                        {
                            error = serverError;
                        }

                        string errorMessage = OperationErrorMappings.MapResultCode(error);
                        ExtendedResponse response = new ExtendedResponse(null, null, (ResultCode)error, errorMessage, responseReferral);
                        response.ResponseName = "1.3.6.1.4.1.1466.20037";
                        throw new TlsOperationException(response);
                    }

                    if (LdapErrorMappings.IsLdapError(error))
                    {
                        string errorMessage = LdapErrorMappings.MapResultCode(error);
                        throw new LdapException(error, errorMessage);
                    }

                    throw new LdapException(error);
                }
            }
            finally
            {
                if (serverControlArray != IntPtr.Zero)
                {
                    // Release the memory from the heap.
                    for (int i = 0; i < managedServerControls.Length; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(serverControlArray, IntPtr.Size * i);
                        if (tempPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(tempPtr);
                        }
                    }
                    Marshal.FreeHGlobal(serverControlArray);
                }

                if (managedServerControls != null)
                {
                    for (int i = 0; i < managedServerControls.Length; i++)
                    {
                        if (managedServerControls[i].ldctl_oid != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(managedServerControls[i].ldctl_oid);
                        }

                        if (managedServerControls[i].ldctl_value != null)
                        {
                            if (managedServerControls[i].ldctl_value.bv_val != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(managedServerControls[i].ldctl_value.bv_val);
                            }
                        }
                    }
                }

                if (clientControlArray != IntPtr.Zero)
                {
                    // Release the memor from the heap.
                    for (int i = 0; i < managedClientControls.Length; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(clientControlArray, IntPtr.Size * i);
                        if (tempPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(tempPtr);
                        }
                    }

                    Marshal.FreeHGlobal(clientControlArray);
                }

                if (managedClientControls != null)
                {
                    for (int i = 0; i < managedClientControls.Length; i++)
                    {
                        if (managedClientControls[i].ldctl_oid != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(managedClientControls[i].ldctl_oid);
                        }

                        if (managedClientControls[i].ldctl_value != null)
                        {
                            if (managedClientControls[i].ldctl_value.bv_val != IntPtr.Zero)
                                Marshal.FreeHGlobal(managedClientControls[i].ldctl_value.bv_val);
                        }
                    }
                }

                if (referral != IntPtr.Zero)
                {
                    LdapPal.FreeValue(referral);
                }
            }
        }

        public void StopTransportLayerSecurity()
        {
            if (_connection._disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            byte result = LdapPal.StopTls(_connection._ldapHandle);
            if (result == 0)
            {
                throw new TlsOperationException(null, SR.TLSStopFailure);
            }
        }

        private int GetIntValueHelper(LdapOption option)
        {
            if (_connection._disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            int outValue = 0;
            int error = LdapPal.GetIntOption(_connection._ldapHandle, option, ref outValue);
            ErrorChecking.CheckAndSetLdapError(error);

            return outValue;
        }

        private void SetIntValueHelper(LdapOption option, int value)
        {
            if (_connection._disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            int temp = value;
            int error = LdapPal.SetIntOption(_connection._ldapHandle, option, ref temp);

            ErrorChecking.CheckAndSetLdapError(error);
        }

        private IntPtr GetPtrValueHelper(LdapOption option)
        {
            if (_connection._disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            IntPtr outValue = new IntPtr(0);
            int error = LdapPal.GetPtrOption(_connection._ldapHandle, option, ref outValue);
            ErrorChecking.CheckAndSetLdapError(error);

            return outValue;
        }

        private void SetPtrValueHelper(LdapOption option, IntPtr value)
        {
            if (_connection._disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            IntPtr temp = value;
            int error = LdapPal.SetPtrOption(_connection._ldapHandle, option, ref temp);

            ErrorChecking.CheckAndSetLdapError(error);
        }

        private string GetStringValueHelper(LdapOption option, bool releasePtr)
        {
            if (_connection._disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            IntPtr outValue = new IntPtr(0);
            int error = LdapPal.GetPtrOption(_connection._ldapHandle, option, ref outValue);
            ErrorChecking.CheckAndSetLdapError(error);

            string stringValue = null;
            if (outValue != IntPtr.Zero)
            {
                stringValue = LdapPal.PtrToString(outValue);
            }

            if (releasePtr)
            {
                LdapPal.FreeMemory(outValue);
            }

            return stringValue;
        }

        private void SetStringValueHelper(LdapOption option, string value)
        {
            if (_connection._disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            IntPtr inValue = IntPtr.Zero;
            if (value != null)
            {
                inValue = LdapPal.StringToPtr(value);
            }

            try
            {
                int error = LdapPal.SetPtrOption(_connection._ldapHandle, option, ref inValue);
                ErrorChecking.CheckAndSetLdapError(error);
            }
            finally
            {
                if (inValue != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(inValue);
                }
            }
        }

        private void ProcessCallBackRoutine(ReferralCallback tempCallback)
        {
            LdapReferralCallback value = new LdapReferralCallback()
            {
                sizeofcallback = Marshal.SizeOf(typeof(LdapReferralCallback)),
                query = tempCallback.QueryForConnection == null ? null : _queryDelegate,
                notify = tempCallback.NotifyNewConnection == null ? null : _notifiyDelegate,
                dereference = tempCallback.DereferenceConnection == null ? null : _dereferenceDelegate
            };
            int error = LdapPal.SetReferralOption(_connection._ldapHandle, LdapOption.LDAP_OPT_REFERRAL_CALLBACK, ref value);
            ErrorChecking.CheckAndSetLdapError(error);
        }

        private unsafe int ProcessQueryConnection(IntPtr PrimaryConnection, IntPtr ReferralFromConnection, IntPtr NewDNPtr, IntPtr HostNamePtr, int PortNumber, SEC_WINNT_AUTH_IDENTITY_EX.Native* SecAuthIdentity, Luid* CurrentUserToken, IntPtr* ConnectionToUse)
        {
            *ConnectionToUse = IntPtr.Zero;
            string NewDN = null;

            // The user must have registered callback function.
            Debug.Assert(_callbackRoutine.QueryForConnection != null);

            // The user registers the QUERYFORCONNECTION callback.
            if (_callbackRoutine.QueryForConnection != null)
            {
                if (NewDNPtr != IntPtr.Zero)
                {
                    NewDN = LdapPal.PtrToString(NewDNPtr);
                }

                var target = new StringBuilder();
                target.Append(Marshal.PtrToStringUni(HostNamePtr));
                target.Append(':');
                target.Append(PortNumber);
                var identifier = new LdapDirectoryIdentifier(target.ToString());

                NetworkCredential cred = ProcessSecAuthIdentity(SecAuthIdentity);
                LdapConnection tempReferralConnection = null;

                // If ReferralFromConnection handle is valid.
                if (ReferralFromConnection != IntPtr.Zero)
                {
                    lock (LdapConnection.s_objectLock)
                    {
                        // Make sure first whether we have saved it in the handle table before
                        WeakReference reference = (WeakReference)(LdapConnection.s_handleTable[ReferralFromConnection]);
                        if (reference != null && reference.IsAlive)
                        {
                            // Save this before and object has not been garbage collected yet.
                            tempReferralConnection = (LdapConnection)reference.Target;
                        }
                        else
                        {
                            if (reference != null)
                            {
                                // Connection has been garbage collected, we need to remove this one.
                                LdapConnection.s_handleTable.Remove(ReferralFromConnection);
                            }

                            // We don't have it yet, construct a new one.
                            tempReferralConnection = new LdapConnection(((LdapDirectoryIdentifier)(_connection.Directory)), _connection.GetCredential(), _connection.AuthType, ReferralFromConnection);

                            // Save it to the handle table.
                            LdapConnection.s_handleTable.Add(ReferralFromConnection, new WeakReference(tempReferralConnection));
                        }
                    }
                }

                long tokenValue = (uint)CurrentUserToken->LowPart + (((long)CurrentUserToken->HighPart) << 32);

                LdapConnection con = _callbackRoutine.QueryForConnection(_connection, tempReferralConnection, NewDN, identifier, cred, tokenValue);
                if (con != null && con._ldapHandle != null && !con._ldapHandle.IsInvalid)
                {
                    bool success = AddLdapHandleRef(con);
                    if (success)
                    {
                        *ConnectionToUse = con._ldapHandle.DangerousGetHandle();
                    }
                }

                return 0;
            }

            // The user does not take ownership of the connection.
            return 1;
        }

        private unsafe Interop.BOOL ProcessNotifyConnection(IntPtr primaryConnection, IntPtr referralFromConnection, IntPtr newDNPtr, IntPtr hostNamePtr, IntPtr newConnection, int portNumber, SEC_WINNT_AUTH_IDENTITY_EX.Native* SecAuthIdentity, Luid* currentUser, int errorCodeFromBind)
        {
            if (newConnection != IntPtr.Zero && _callbackRoutine.NotifyNewConnection != null)
            {
                string newDN = null;
                if (newDNPtr != IntPtr.Zero)
                {
                    newDN = LdapPal.PtrToString(newDNPtr);
                }

                var target = new StringBuilder();
                target.Append(Marshal.PtrToStringUni(hostNamePtr));
                target.Append(':');
                target.Append(portNumber);
                var identifier = new LdapDirectoryIdentifier(target.ToString());

                NetworkCredential cred = ProcessSecAuthIdentity(SecAuthIdentity);
                LdapConnection tempNewConnection = null;
                LdapConnection tempReferralConnection = null;

                lock (LdapConnection.s_objectLock)
                {
                    // Check whether the referralFromConnection handle is valid.
                    if (referralFromConnection != IntPtr.Zero)
                    {
                        // Check whether we have save it in the handle table before.
                        WeakReference reference = (WeakReference)(LdapConnection.s_handleTable[referralFromConnection]);
                        if (reference != null && reference.Target is LdapConnection conn && conn._ldapHandle != null)
                        {
                            // Save this before and object has not been garbage collected yet.
                            tempReferralConnection = conn;
                        }
                        else
                        {
                            // Connection has been garbage collected, we need to remove this one.
                            if (reference != null)
                            {
                                LdapConnection.s_handleTable.Remove(referralFromConnection);
                            }

                            // We don't have it yet, construct a new one.
                            tempReferralConnection = new LdapConnection(((LdapDirectoryIdentifier)(_connection.Directory)), _connection.GetCredential(), _connection.AuthType, referralFromConnection);

                            // Save it to the handle table.
                            LdapConnection.s_handleTable.Add(referralFromConnection, new WeakReference(tempReferralConnection));
                        }
                    }

                    if (newConnection != IntPtr.Zero)
                    {
                        // Check whether we have save it in the handle table before.
                        WeakReference reference = (WeakReference)(LdapConnection.s_handleTable[newConnection]);
                        if (reference != null && reference.IsAlive && null != ((LdapConnection)reference.Target)._ldapHandle)
                        {
                            // Save this before and object has not been garbage collected yet.
                            tempNewConnection = (LdapConnection)reference.Target;
                        }
                        else
                        {
                            // Connection has been garbage collected, we need to remove this one.
                            if (reference != null)
                            {
                                LdapConnection.s_handleTable.Remove(newConnection);
                            }

                            // We don't have it yet, construct a new one.
                            tempNewConnection = new LdapConnection(identifier, cred, _connection.AuthType, newConnection);

                            // Save it to the handle table.
                            LdapConnection.s_handleTable.Add(newConnection, new WeakReference(tempNewConnection));
                        }
                    }
                }

                long tokenValue = (uint)currentUser->LowPart + (((long)currentUser->HighPart) << 32);
                bool value = _callbackRoutine.NotifyNewConnection(_connection, tempReferralConnection, newDN, identifier, tempNewConnection, cred, tokenValue, errorCodeFromBind);

                if (value)
                {
                    value = AddLdapHandleRef(tempNewConnection);
                    if (value)
                    {
                        tempNewConnection.NeedDispose = true;
                    }
                }

                return value ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
            }

            return Interop.BOOL.FALSE;
        }

        private int ProcessDereferenceConnection(IntPtr PrimaryConnection, IntPtr ConnectionToDereference)
        {
            if (ConnectionToDereference != IntPtr.Zero && _callbackRoutine.DereferenceConnection != null)
            {
                LdapConnection dereferenceConnection;
                WeakReference reference = null;

                // in most cases it should be in our table
                lock (LdapConnection.s_objectLock)
                {
                    reference = (WeakReference)(LdapConnection.s_handleTable[ConnectionToDereference]);
                }

                // not been added to the table before or it is garbage collected, we need to construct it
                if (reference == null || !reference.IsAlive)
                {
                    dereferenceConnection = new LdapConnection(((LdapDirectoryIdentifier)(_connection.Directory)), _connection.GetCredential(), _connection.AuthType, ConnectionToDereference);
                }
                else
                {
                    dereferenceConnection = (LdapConnection)reference.Target;
                    ReleaseLdapHandleRef(dereferenceConnection);
                }

                _callbackRoutine.DereferenceConnection(_connection, dereferenceConnection);

                // there is no need to remove the connection object from the handle table, as it will be removed automatically when
                // connection object is disposed.
            }

            return 1;
        }

        private unsafe NetworkCredential ProcessSecAuthIdentity(SEC_WINNT_AUTH_IDENTITY_EX.Native* SecAuthIdentit)
        {
            if (SecAuthIdentit == null)
            {
                return new NetworkCredential();
            }

            string user = Marshal.PtrToStringUni(SecAuthIdentit->user);
            string domain = Marshal.PtrToStringUni(SecAuthIdentit->domain);
            string password = Marshal.PtrToStringUni(SecAuthIdentit->password);

            return new NetworkCredential(user, password, domain);
        }

        private Interop.BOOL ProcessServerCertificate(IntPtr connection, IntPtr serverCert)
        {
            // If callback is not specified by user, it means the server certificate is accepted.
            bool value = true;
            if (_serverCertificateDelegate != null)
            {
                IntPtr certPtr = IntPtr.Zero;
                X509Certificate certificate = null;
                try
                {
                    Debug.Assert(serverCert != IntPtr.Zero);
                    certPtr = Marshal.ReadIntPtr(serverCert);
                    certificate = new X509Certificate(certPtr);
                }
                finally
                {
                    PALCertFreeCRLContext(certPtr);
                }

                value = _serverCertificateDelegate(_connection, certificate);
            }

            return value ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }

        private static bool AddLdapHandleRef(LdapConnection ldapConnection)
        {
            bool success = false;
            if (ldapConnection != null && ldapConnection._ldapHandle != null && !ldapConnection._ldapHandle.IsInvalid)
            {
                ldapConnection._ldapHandle.DangerousAddRef(ref success);
            }

            return success;
        }

        private static void ReleaseLdapHandleRef(LdapConnection ldapConnection)
        {
            if (ldapConnection != null && ldapConnection._ldapHandle != null && !ldapConnection._ldapHandle.IsInvalid)
            {
                ldapConnection._ldapHandle.DangerousRelease();
            }
        }
    }
}
