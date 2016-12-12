// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Text;
    using System.Diagnostics;
    using System.Security.Authentication;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;

    public delegate LdapConnection QueryForConnectionCallback(LdapConnection primaryConnection, LdapConnection referralFromConnection, string newDistinguishedName, LdapDirectoryIdentifier identifier, NetworkCredential credential, long currentUserToken);
    public delegate bool NotifyOfNewConnectionCallback(LdapConnection primaryConnection, LdapConnection referralFromConnection, string newDistinguishedName, LdapDirectoryIdentifier identifier, LdapConnection newConnection, NetworkCredential credential, long currentUserToken, int errorCodeFromBind);
    public delegate void DereferenceConnectionCallback(LdapConnection primaryConnection, LdapConnection connectionToDereference);
    public delegate X509Certificate QueryClientCertificateCallback(LdapConnection connection, byte[][] trustedCAs);
    public delegate bool VerifyServerCertificateCallback(LdapConnection connection, X509Certificate certificate);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int QUERYFORCONNECTIONInternal(IntPtr Connection, IntPtr ReferralFromConnection, IntPtr NewDNPtr, string HostName, int PortNumber, SEC_WINNT_AUTH_IDENTITY_EX SecAuthIdentity, Luid CurrentUserToken, ref IntPtr ConnectionToUse);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate bool NOTIFYOFNEWCONNECTIONInternal(IntPtr Connection, IntPtr ReferralFromConnection, IntPtr NewDNPtr, string HostName, IntPtr NewConnection, int PortNumber, SEC_WINNT_AUTH_IDENTITY_EX SecAuthIdentity, Luid CurrentUser, int ErrorCodeFromBind);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int DEREFERENCECONNECTIONInternal(IntPtr Connection, IntPtr ConnectionToDereference);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate bool VERIFYSERVERCERT(IntPtr Connection, IntPtr pServerCert);

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
        private SecurityProtocol _securityProtocol;
        private CipherAlgorithmType _identifier;
        private int _strength;
        private HashAlgorithmType _hashAlgorithm;
        private int _hashStrength;
        private int _keyExchangeAlgorithm;
        private int _exchangeStrength;

        internal SecurityPackageContextConnectionInformation()
        {
        }

        public SecurityProtocol Protocol
        {
            get
            {
                return _securityProtocol;
            }
        }

        public CipherAlgorithmType AlgorithmIdentifier
        {
            get
            {
                return _identifier;
            }
        }

        public int CipherStrength
        {
            get
            {
                return _strength;
            }
        }

        public HashAlgorithmType Hash
        {
            get
            {
                return _hashAlgorithm;
            }
        }

        public int HashStrength
        {
            get
            {
                return _hashStrength;
            }
        }

        public int KeyExchangeAlgorithm
        {
            get
            {
                return _keyExchangeAlgorithm;
            }
        }

        public int ExchangeStrength
        {
            get
            {
                return _exchangeStrength;
            }
        }
    }

    public sealed class ReferralCallback
    {
        private QueryForConnectionCallback _query;
        private NotifyOfNewConnectionCallback _notify;
        private DereferenceConnectionCallback _dereference;

        public ReferralCallback()
        {
            Utility.CheckOSVersion();
        }

        public QueryForConnectionCallback QueryForConnection
        {
            get
            {
                return _query;
            }
            set
            {
                _query = value;
            }
        }

        public NotifyOfNewConnectionCallback NotifyNewConnection
        {
            get
            {
                return _notify;
            }
            set
            {
                _notify = value;
            }
        }

        public DereferenceConnectionCallback DereferenceConnection
        {
            get
            {
                return _dereference;
            }
            set
            {
                _dereference = value;
            }
        }
    }

    internal struct SecurityHandle
    {
        public IntPtr Lower;
        public IntPtr Upper;
    }

    public class LdapSessionOptions
    {
        private LdapConnection _connection = null;
        private ReferralCallback _callbackRoutine = new ReferralCallback();
        internal QueryClientCertificateCallback clientCertificateDelegate = null;
        private VerifyServerCertificateCallback _serverCertificateDelegate = null;

        private QUERYFORCONNECTIONInternal _queryDelegate = null;
        private NOTIFYOFNEWCONNECTIONInternal _notifiyDelegate = null;
        private DEREFERENCECONNECTIONInternal _dereferenceDelegate = null;
        private VERIFYSERVERCERT _serverCertificateRoutine = null;

        internal LdapSessionOptions(LdapConnection connection)
        {
            _connection = connection;
            _queryDelegate = new QUERYFORCONNECTIONInternal(ProcessQueryConnection);
            _notifiyDelegate = new NOTIFYOFNEWCONNECTIONInternal(ProcessNotifyConnection);
            _dereferenceDelegate = new DEREFERENCECONNECTIONInternal(ProcessDereferenceConnection);
            _serverCertificateRoutine = new VERIFYSERVERCERT(ProcessServerCertificate);
        }

        public ReferralChasingOptions ReferralChasing
        {
            get
            {
                int result = GetIntValueHelper(LdapOption.LDAP_OPT_REFERRALS);

                if (result == 1)
                    return ReferralChasingOptions.All;
                else
                    return (ReferralChasingOptions)result;
            }
            set
            {
                if (((value) & (~ReferralChasingOptions.All)) != 0)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(ReferralChasingOptions));

                SetIntValueHelper(LdapOption.LDAP_OPT_REFERRALS, (int)value);
            }
        }

        public bool SecureSocketLayer
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_SSL);

                if (outValue == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                int temp;
                if (value)
                    temp = 1;
                else
                    temp = 0;

                SetIntValueHelper(LdapOption.LDAP_OPT_SSL, temp);
            }
        }

        public int ReferralHopLimit
        {
            get
            {
                return GetIntValueHelper(LdapOption.LDAP_OPT_REFERRAL_HOP_LIMIT);
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.ValidValue), "value");

                SetIntValueHelper(LdapOption.LDAP_OPT_REFERRAL_HOP_LIMIT, value);
            }
        }

        public int ProtocolVersion
        {
            get
            {
                return GetIntValueHelper(LdapOption.LDAP_OPT_VERSION);
            }
            set
            {
                SetIntValueHelper(LdapOption.LDAP_OPT_VERSION, value);
            }
        }

        public string HostName
        {
            get
            {
                return GetStringValueHelper(LdapOption.LDAP_OPT_HOST_NAME, false);
            }
            set
            {
                SetStringValueHelper(LdapOption.LDAP_OPT_HOST_NAME, value);
            }
        }

        public string DomainName
        {
            get
            {
                return GetStringValueHelper(LdapOption.LDAP_OPT_DNSDOMAIN_NAME, true);
            }
            set
            {
                SetStringValueHelper(LdapOption.LDAP_OPT_DNSDOMAIN_NAME, value);
            }
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
                // we don't do validation to the dirsync flag here as underneath API does not check for it and we don't want to put
                // unnecessary limitation on it.

                SetIntValueHelper(LdapOption.LDAP_OPT_GETDSNAME_FLAGS, (int)value);
            }
        }

        public bool HostReachable
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_HOST_REACHABLE);

                if (outValue == 1)
                    return true;
                else
                    return false;
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
                    throw new ArgumentException(Res.GetString(Res.NoNegativeTime), "value");
                }

                // prevent integer overflow
                if (value.TotalSeconds > Int32.MaxValue)
                {
                    throw new ArgumentException(Res.GetString(Res.TimespanExceedMax), "value");
                }

                int seconds = (int)(value.Ticks / TimeSpan.TicksPerSecond);

                SetIntValueHelper(LdapOption.LDAP_OPT_PING_KEEP_ALIVE, (int)seconds);
            }
        }

        public int PingLimit
        {
            get
            {
                return GetIntValueHelper(LdapOption.LDAP_OPT_PING_LIMIT);
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.ValidValue), "value");

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
                    throw new ArgumentException(Res.GetString(Res.NoNegativeTime), "value");
                }

                // prevent integer overflow
                if (value.TotalMilliseconds > Int32.MaxValue)
                {
                    throw new ArgumentException(Res.GetString(Res.TimespanExceedMax), "value");
                }

                int milliseconds = (int)(value.Ticks / TimeSpan.TicksPerMillisecond);

                SetIntValueHelper(LdapOption.LDAP_OPT_PING_WAIT_TIME, (int)milliseconds);
            }
        }

        public bool AutoReconnect
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_AUTO_RECONNECT);

                if (outValue == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                int temp;
                if (value)
                    temp = 1;
                else
                    temp = 0;

                SetIntValueHelper(LdapOption.LDAP_OPT_AUTO_RECONNECT, temp);
            }
        }

        public int SspiFlag
        {
            get
            {
                return GetIntValueHelper(LdapOption.LDAP_OPT_SSPI_FLAGS);
            }
            set
            {
                SetIntValueHelper(LdapOption.LDAP_OPT_SSPI_FLAGS, value);
            }
        }

        public SecurityPackageContextConnectionInformation SslInformation
        {
            get
            {
                if (_connection.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                SecurityPackageContextConnectionInformation secInfo = new SecurityPackageContextConnectionInformation();
                int error = Wldap32.ldap_get_option_secInfo(_connection.ldapHandle, LdapOption.LDAP_OPT_SSL_INFO, secInfo);
                ErrorChecking.CheckAndSetLdapError(error);

                return secInfo;
            }
        }

        public object SecurityContext
        {
            get
            {
                if (_connection.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                SecurityHandle tempHandle = new SecurityHandle();

                int error = Wldap32.ldap_get_option_sechandle(_connection.ldapHandle, LdapOption.LDAP_OPT_SECURITY_CONTEXT, ref tempHandle);
                ErrorChecking.CheckAndSetLdapError(error);

                return tempHandle;
            }
        }

        public bool Signing
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_SIGN);

                if (outValue == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                int temp;
                if (value)
                    temp = 1;
                else
                    temp = 0;

                SetIntValueHelper(LdapOption.LDAP_OPT_SIGN, temp);
            }
        }

        public bool Sealing
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_ENCRYPT);

                if (outValue == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                int temp;
                if (value)
                    temp = 1;
                else
                    temp = 0;

                SetIntValueHelper(LdapOption.LDAP_OPT_ENCRYPT, temp);
            }
        }

        public string SaslMethod
        {
            get
            {
                return GetStringValueHelper(LdapOption.LDAP_OPT_SASL_METHOD, true);
            }
            set
            {
                SetStringValueHelper(LdapOption.LDAP_OPT_SASL_METHOD, value);
            }
        }

        public bool RootDseCache
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_ROOTDSE_CACHE);

                if (outValue == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                int temp;
                if (value)
                    temp = 1;
                else
                    temp = 0;

                SetIntValueHelper(LdapOption.LDAP_OPT_ROOTDSE_CACHE, temp);
            }
        }

        public bool TcpKeepAlive
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_TCP_KEEPALIVE);

                if (outValue == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                int temp;
                if (value)
                    temp = 1;
                else
                    temp = 0;

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
                    throw new ArgumentException(Res.GetString(Res.NoNegativeTime), "value");
                }

                // prevent integer overflow
                if (value.TotalSeconds > Int32.MaxValue)
                {
                    throw new ArgumentException(Res.GetString(Res.TimespanExceedMax), "value");
                }

                int seconds = (int)(value.Ticks / TimeSpan.TicksPerSecond);

                SetIntValueHelper(LdapOption.LDAP_OPT_SEND_TIMEOUT, (int)seconds);
            }
        }

        public ReferralCallback ReferralCallback
        {
            get
            {
                if (_connection.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return _callbackRoutine;
            }
            set
            {
                if (_connection.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                ReferralCallback tempCallback = new ReferralCallback();

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

                // set the callback property
                _callbackRoutine = value;
            }
        }

        public QueryClientCertificateCallback QueryClientCertificate
        {
            get
            {
                if (_connection.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return clientCertificateDelegate;
            }
            set
            {
                if (_connection.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (value != null)
                {
                    int certerror = Wldap32.ldap_set_option_clientcert(_connection.ldapHandle, LdapOption.LDAP_OPT_CLIENT_CERTIFICATE, _connection.clientCertificateRoutine);
                    if (certerror != (int)ResultCode.Success)
                    {
                        if (Utility.IsLdapError((LdapError)certerror))
                        {
                            string certerrorMessage = LdapErrorMappings.MapResultCode(certerror);
                            throw new LdapException(certerror, certerrorMessage);
                        }
                        else
                            throw new LdapException(certerror);
                    }
                    // certificate callback is specified, automatic bind is disabled
                    _connection.automaticBind = false;
                }
                clientCertificateDelegate = value;
            }
        }

        public VerifyServerCertificateCallback VerifyServerCertificate
        {
            get
            {
                if (_connection.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return _serverCertificateDelegate;
            }
            set
            {
                if (_connection.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (value != null)
                {
                    int error = Wldap32.ldap_set_option_servercert(_connection.ldapHandle, LdapOption.LDAP_OPT_SERVER_CERTIFICATE, _serverCertificateRoutine);
                    ErrorChecking.CheckAndSetLdapError(error);
                }
                _serverCertificateDelegate = value;
            }
        }

        internal string ServerErrorMessage
        {
            get
            {
                return GetStringValueHelper(LdapOption.LDAP_OPT_SERVER_ERROR, true);
            }
        }

        internal DereferenceAlias DerefAlias
        {
            get
            {
                return (DereferenceAlias)GetIntValueHelper(LdapOption.LDAP_OPT_DEREF);
            }
            set
            {
                SetIntValueHelper(LdapOption.LDAP_OPT_DEREF, (int)value);
            }
        }

        internal bool FQDN
        {
            set
            {
                // set the value to true
                SetIntValueHelper(LdapOption.LDAP_OPT_AREC_EXCLUSIVE, 1);
            }
        }

        public void FastConcurrentBind()
        {
            if (_connection.disposed)
                throw new ObjectDisposedException(GetType().Name);

            int inValue = 1;
            // bump up the protocol version
            ProtocolVersion = 3;
            // do the fast concurrent bind
            int error = Wldap32.ldap_set_option_int(_connection.ldapHandle, LdapOption.LDAP_OPT_FAST_CONCURRENT_BIND, ref inValue);
            //we only throw PlatformNotSupportedException when we get parameter error and os is win2k3 below which does not support fast concurrent bind
            if (error == (int)LdapError.ParameterError && !Utility.IsWin2k3AboveOS)
                throw new PlatformNotSupportedException(Res.GetString(Res.ConcurrentBindNotSupport));
            else
                ErrorChecking.CheckAndSetLdapError(error);
        }

        public unsafe void StartTransportLayerSecurity(DirectoryControlCollection controls)
        {
            IntPtr serverControlArray = (IntPtr)0;
            LdapControl[] managedServerControls = null;
            IntPtr clientControlArray = (IntPtr)0;
            LdapControl[] managedClientControls = null;
            IntPtr ldapResult = (IntPtr)0;
            IntPtr referral = (IntPtr)0;

            int serverError = 0;
            Uri[] responseReferral = null;

            if (Utility.IsWin2kOS)
                throw new PlatformNotSupportedException(Res.GetString(Res.TLSNotSupported));

            if (_connection.disposed)
                throw new ObjectDisposedException(GetType().Name);

            try
            {
                IntPtr controlPtr = (IntPtr)0;
                IntPtr tempPtr = (IntPtr)0;

                // build server control
                managedServerControls = _connection.BuildControlArray(controls, true);
                int structSize = Marshal.SizeOf(typeof(LdapControl));
                if (managedServerControls != null)
                {
                    serverControlArray = Utility.AllocHGlobalIntPtrArray(managedServerControls.Length + 1);
                    for (int i = 0; i < managedServerControls.Length; i++)
                    {
                        controlPtr = Marshal.AllocHGlobal(structSize);
                        Marshal.StructureToPtr(managedServerControls[i], controlPtr, false);
                        tempPtr = (IntPtr)((long)serverControlArray + Marshal.SizeOf(typeof(IntPtr)) * i);
                        Marshal.WriteIntPtr(tempPtr, controlPtr);
                    }

                    tempPtr = (IntPtr)((long)serverControlArray + Marshal.SizeOf(typeof(IntPtr)) * managedServerControls.Length);
                    Marshal.WriteIntPtr(tempPtr, (IntPtr)0);
                }

                // build client control
                managedClientControls = _connection.BuildControlArray(controls, false);
                if (managedClientControls != null)
                {
                    clientControlArray = Utility.AllocHGlobalIntPtrArray(managedClientControls.Length + 1);
                    for (int i = 0; i < managedClientControls.Length; i++)
                    {
                        controlPtr = Marshal.AllocHGlobal(structSize);
                        Marshal.StructureToPtr(managedClientControls[i], controlPtr, false);
                        tempPtr = (IntPtr)((long)clientControlArray + Marshal.SizeOf(typeof(IntPtr)) * i);
                        Marshal.WriteIntPtr(tempPtr, controlPtr);
                    }
                    tempPtr = (IntPtr)((long)clientControlArray + Marshal.SizeOf(typeof(IntPtr)) * managedClientControls.Length);
                    Marshal.WriteIntPtr(tempPtr, (IntPtr)0);
                }

                int error = Wldap32.ldap_start_tls(_connection.ldapHandle, ref serverError, ref ldapResult, serverControlArray, clientControlArray);
                if (ldapResult != (IntPtr)0)
                {
                    // parsing the referral                          
                    int resulterror = Wldap32.ldap_parse_result_referral(_connection.ldapHandle, ldapResult, (IntPtr)0, (IntPtr)0, (IntPtr)0, ref referral, (IntPtr)0, 0 /* not free it */);
                    if (resulterror == 0)
                    {
                        // parsing referral                        
                        if (referral != (IntPtr)0)
                        {
                            char** referralPtr = (char**)referral;
                            char* singleReferral = referralPtr[0];
                            int i = 0;
                            ArrayList referralList = new ArrayList();
                            while (singleReferral != null)
                            {
                                string s = Marshal.PtrToStringUni((IntPtr)singleReferral);
                                referralList.Add(s);

                                i++;
                                singleReferral = referralPtr[i];
                            }

                            // free heap memory
                            if (referral != (IntPtr)0)
                            {
                                Wldap32.ldap_value_free(referral);
                                referral = (IntPtr)0;
                            }

                            if (referralList.Count > 0)
                            {
                                responseReferral = new Uri[referralList.Count];
                                for (int j = 0; j < referralList.Count; j++)
                                    responseReferral[j] = new Uri((string)referralList[j]);
                            }
                        }
                    }
                }

                if (error != (int)ResultCode.Success)
                {
                    string errorMessage = Res.GetString(Res.DefaultLdapError);
                    if (Utility.IsResultCode((ResultCode)error))
                    {
                        //If the server failed request for whatever reason, the ldap_start_tls returns LDAP_OTHER
                        // and the ServerReturnValue will contain the error code from the server.   
                        if (error == (int)ResultCode.Other)
                            error = serverError;

                        errorMessage = OperationErrorMappings.MapResultCode(error);
                        ExtendedResponse response = new ExtendedResponse(null, null, (ResultCode)error, errorMessage, responseReferral);
                        response.name = "1.3.6.1.4.1.1466.20037";
                        throw new TlsOperationException(response);
                    }
                    else if (Utility.IsLdapError((LdapError)error))
                    {
                        errorMessage = LdapErrorMappings.MapResultCode(error);
                        throw new LdapException(error, errorMessage);
                    }
                }
            }
            finally
            {
                if (serverControlArray != (IntPtr)0)
                {
                    //release the memory from the heap
                    for (int i = 0; i < managedServerControls.Length; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(serverControlArray, Marshal.SizeOf(typeof(IntPtr)) * i);
                        if (tempPtr != (IntPtr)0)
                            Marshal.FreeHGlobal(tempPtr);
                    }
                    Marshal.FreeHGlobal(serverControlArray);
                }

                if (managedServerControls != null)
                {
                    for (int i = 0; i < managedServerControls.Length; i++)
                    {
                        if (managedServerControls[i].ldctl_oid != (IntPtr)0)
                            Marshal.FreeHGlobal(managedServerControls[i].ldctl_oid);

                        if (managedServerControls[i].ldctl_value != null)
                        {
                            if (managedServerControls[i].ldctl_value.bv_val != (IntPtr)0)
                                Marshal.FreeHGlobal(managedServerControls[i].ldctl_value.bv_val);
                        }
                    }
                }

                if (clientControlArray != (IntPtr)0)
                {
                    // release the memor from the heap
                    for (int i = 0; i < managedClientControls.Length; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(clientControlArray, Marshal.SizeOf(typeof(IntPtr)) * i);
                        if (tempPtr != (IntPtr)0)
                            Marshal.FreeHGlobal(tempPtr);
                    }
                    Marshal.FreeHGlobal(clientControlArray);
                }

                if (managedClientControls != null)
                {
                    for (int i = 0; i < managedClientControls.Length; i++)
                    {
                        if (managedClientControls[i].ldctl_oid != (IntPtr)0)
                            Marshal.FreeHGlobal(managedClientControls[i].ldctl_oid);

                        if (managedClientControls[i].ldctl_value != null)
                        {
                            if (managedClientControls[i].ldctl_value.bv_val != (IntPtr)0)
                                Marshal.FreeHGlobal(managedClientControls[i].ldctl_value.bv_val);
                        }
                    }
                }

                if (referral != (IntPtr)0)
                    Wldap32.ldap_value_free(referral);
            }
        }

        public void StopTransportLayerSecurity()
        {
            if (Utility.IsWin2kOS)
                throw new PlatformNotSupportedException(Res.GetString(Res.TLSNotSupported));

            if (_connection.disposed)
                throw new ObjectDisposedException(GetType().Name);

            byte result = Wldap32.ldap_stop_tls(_connection.ldapHandle);
            if (result == 0)
                // caller needs to close this ldap connection
                throw new TlsOperationException(null, Res.GetString(Res.TLSStopFailure));
        }

        private int GetIntValueHelper(LdapOption option)
        {
            if (_connection.disposed)
                throw new ObjectDisposedException(GetType().Name);

            int outValue = 0;
            int error = Wldap32.ldap_get_option_int(_connection.ldapHandle, option, ref outValue);

            ErrorChecking.CheckAndSetLdapError(error);
            return outValue;
        }

        private void SetIntValueHelper(LdapOption option, int value)
        {
            if (_connection.disposed)
                throw new ObjectDisposedException(GetType().Name);

            int temp = value;
            int error = Wldap32.ldap_set_option_int(_connection.ldapHandle, option, ref temp);

            ErrorChecking.CheckAndSetLdapError(error);
        }

        private string GetStringValueHelper(LdapOption option, bool releasePtr)
        {
            if (_connection.disposed)
                throw new ObjectDisposedException(GetType().Name);

            IntPtr outValue = new IntPtr(0);
            int error = Wldap32.ldap_get_option_ptr(_connection.ldapHandle, option, ref outValue);
            ErrorChecking.CheckAndSetLdapError(error);

            string s = null;
            if (outValue != (IntPtr)0)
                s = Marshal.PtrToStringUni(outValue);

            if (releasePtr)
                Wldap32.ldap_memfree(outValue);

            return s;
        }

        private void SetStringValueHelper(LdapOption option, string value)
        {
            if (_connection.disposed)
                throw new ObjectDisposedException(GetType().Name);

            IntPtr inValue = new IntPtr(0);
            if (value != null)
            {
                inValue = Marshal.StringToHGlobalUni(value);
            }

            try
            {
                int error = Wldap32.ldap_set_option_ptr(_connection.ldapHandle, option, ref inValue);
                ErrorChecking.CheckAndSetLdapError(error);
            }
            finally
            {
                if (inValue != (IntPtr)0)
                    Marshal.FreeHGlobal(inValue);
            }
        }

        private void ProcessCallBackRoutine(ReferralCallback tempCallback)
        {
            LdapReferralCallback value = new LdapReferralCallback();
            value.sizeofcallback = Marshal.SizeOf(typeof(LdapReferralCallback));
            value.query = tempCallback.QueryForConnection == null ? null : _queryDelegate;
            value.notify = tempCallback.NotifyNewConnection == null ? null : _notifiyDelegate;
            value.dereference = tempCallback.DereferenceConnection == null ? null : _dereferenceDelegate;

            int error = Wldap32.ldap_set_option_referral(_connection.ldapHandle, LdapOption.LDAP_OPT_REFERRAL_CALLBACK, ref value);
            ErrorChecking.CheckAndSetLdapError(error);
        }

        private int ProcessQueryConnection(IntPtr PrimaryConnection, IntPtr ReferralFromConnection, IntPtr NewDNPtr, string HostName, int PortNumber, SEC_WINNT_AUTH_IDENTITY_EX SecAuthIdentity, Luid CurrentUserToken, ref IntPtr ConnectionToUse)
        {
            ConnectionToUse = IntPtr.Zero;
            string NewDN = null;

            // user must have registered callback function
            Debug.Assert(_callbackRoutine.QueryForConnection != null);

            // user registers the QUERYFORCONNECTION callback
            if (_callbackRoutine.QueryForConnection != null)
            {
                if (NewDNPtr != (IntPtr)0)
                    NewDN = Marshal.PtrToStringUni(NewDNPtr);
                StringBuilder target = new StringBuilder();
                target.Append(HostName);
                target.Append(":");
                target.Append(PortNumber);
                LdapDirectoryIdentifier identifier = new LdapDirectoryIdentifier(target.ToString());
                NetworkCredential cred = ProcessSecAuthIdentity(SecAuthIdentity);
                LdapConnection tempReferralConnection = null;
                WeakReference reference = null;

                // if referrafromconnection handle is valid
                if (ReferralFromConnection != (IntPtr)0)
                {
                    lock (LdapConnection.objectLock)
                    {
                        //make sure first whether we have saved it in the handle table before
                        reference = (WeakReference)(LdapConnection.handleTable[ReferralFromConnection]);
                        if (reference != null && reference.IsAlive)
                        {
                            // save this before and object has not been garbage collected yet.
                            tempReferralConnection = (LdapConnection)reference.Target;
                        }
                        else
                        {
                            if (reference != null)
                            {
                                // connection has been garbage collected, we need to remove this one
                                LdapConnection.handleTable.Remove(ReferralFromConnection);
                            }
                            // we don't have it yet, construct a new one
                            tempReferralConnection = new LdapConnection(((LdapDirectoryIdentifier)(_connection.Directory)), _connection.GetCredential(), _connection.AuthType, ReferralFromConnection);

                            // save it to the handle table
                            LdapConnection.handleTable.Add(ReferralFromConnection, new WeakReference(tempReferralConnection));
                        }
                    }
                }

                long tokenValue = (long)((uint)CurrentUserToken.LowPart + (((long)CurrentUserToken.HighPart) << 32));

                LdapConnection con = _callbackRoutine.QueryForConnection(_connection, tempReferralConnection, NewDN, identifier, cred, tokenValue);
                if (null != con && null != con.ldapHandle && !con.ldapHandle.IsInvalid)
                {
                    bool success = AddLdapHandleRef(con);
                    if (success)
                    {
                        ConnectionToUse = con.ldapHandle.DangerousGetHandle();
                    }
                }
                return 0;
            }
            else
            {
                // user does not take ownership of the connection
                return 1;
            }
        }

        private bool ProcessNotifyConnection(IntPtr PrimaryConnection, IntPtr ReferralFromConnection, IntPtr NewDNPtr, string HostName, IntPtr NewConnection, int PortNumber, SEC_WINNT_AUTH_IDENTITY_EX SecAuthIdentity, Luid CurrentUser, int ErrorCodeFromBind)
        {
            string NewDN = null;
            if (NewConnection != (IntPtr)0 && _callbackRoutine.NotifyNewConnection != null)
            {
                if (NewDNPtr != (IntPtr)0)
                    NewDN = Marshal.PtrToStringUni(NewDNPtr);
                StringBuilder target = new StringBuilder();
                target.Append(HostName);
                target.Append(":");
                target.Append(PortNumber);
                LdapDirectoryIdentifier identifier = new LdapDirectoryIdentifier(target.ToString());
                NetworkCredential cred = ProcessSecAuthIdentity(SecAuthIdentity);
                LdapConnection tempNewConnection = null;
                LdapConnection tempReferralConnection = null;
                WeakReference reference = null;

                lock (LdapConnection.objectLock)
                {
                    // if referrafromconnection handle is valid
                    if (ReferralFromConnection != (IntPtr)0)
                    {
                        //check whether we have save it in the handle table before
                        reference = (WeakReference)(LdapConnection.handleTable[ReferralFromConnection]);
                        if (reference != null && reference.IsAlive && null != ((LdapConnection)reference.Target).ldapHandle)
                        {
                            // save this before and object has not been garbage collected yet.
                            tempReferralConnection = (LdapConnection)reference.Target;
                        }
                        else
                        {
                            // connection has been garbage collected, we need to remove this one
                            if (reference != null)
                                LdapConnection.handleTable.Remove(ReferralFromConnection);

                            // we don't have it yet, construct a new one
                            tempReferralConnection = new LdapConnection(((LdapDirectoryIdentifier)(_connection.Directory)), _connection.GetCredential(), _connection.AuthType, ReferralFromConnection);
                            // save it to the handle table
                            LdapConnection.handleTable.Add(ReferralFromConnection, new WeakReference(tempReferralConnection));
                        }
                    }

                    if (NewConnection != (IntPtr)0)
                    {
                        //check whether we have save it in the handle table before
                        reference = (WeakReference)(LdapConnection.handleTable[NewConnection]);
                        if (reference != null && reference.IsAlive && null != ((LdapConnection)reference.Target).ldapHandle)
                        {
                            // save this before and object has not been garbage collected yet.
                            tempNewConnection = (LdapConnection)reference.Target;
                        }
                        else
                        {
                            // connection has been garbage collected, we need to remove this one
                            if (reference != null)
                                LdapConnection.handleTable.Remove(NewConnection);

                            // we don't have it yet, construct a new one
                            tempNewConnection = new LdapConnection(identifier, cred, _connection.AuthType, NewConnection);
                            // save it to the handle table
                            LdapConnection.handleTable.Add(NewConnection, new WeakReference(tempNewConnection));
                        }
                    }
                }
                long tokenValue = (long)((uint)CurrentUser.LowPart + (((long)CurrentUser.HighPart) << 32));

                bool value = _callbackRoutine.NotifyNewConnection(_connection, tempReferralConnection, NewDN, identifier, tempNewConnection, cred, tokenValue, ErrorCodeFromBind);

                if (value)
                {
                    value = AddLdapHandleRef(tempNewConnection);
                    if (value)
                    {
                        tempNewConnection.NeedDispose = true;
                    }
                }
                return value;
            }
            else
            {
                return false;
            }
        }

        private int ProcessDereferenceConnection(IntPtr PrimaryConnection, IntPtr ConnectionToDereference)
        {
            if (ConnectionToDereference != (IntPtr)0 && _callbackRoutine.DereferenceConnection != null)
            {
                LdapConnection dereferenceConnection = null;
                WeakReference reference = null;

                // in most cases it should be in our table
                lock (LdapConnection.objectLock)
                {
                    reference = (WeakReference)(LdapConnection.handleTable[ConnectionToDereference]);
                }

                // not been added to the table before or it is garbage collected, we need to construct it
                if (reference == null || !reference.IsAlive)
                    dereferenceConnection = new LdapConnection(((LdapDirectoryIdentifier)(_connection.Directory)), _connection.GetCredential(), _connection.AuthType, ConnectionToDereference);
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

        private NetworkCredential ProcessSecAuthIdentity(SEC_WINNT_AUTH_IDENTITY_EX SecAuthIdentit)
        {
            if (SecAuthIdentit == null)
                return new NetworkCredential();
            else
            {
                string user = SecAuthIdentit.user;
                string domain = SecAuthIdentit.domain;
                string password = SecAuthIdentit.password;

                return new NetworkCredential(user, password, domain);
            }
        }

        private bool ProcessServerCertificate(IntPtr Connection, IntPtr pServerCert)
        {
            // if callback is not specified by user, it means server certificate is accepted
            bool value = true;
            if (_serverCertificateDelegate != null)
            {
                IntPtr certPtr = (IntPtr)0;
                X509Certificate certificate = null;
                try
                {
                    Debug.Assert(pServerCert != (IntPtr)0);
                    certPtr = Marshal.ReadIntPtr(pServerCert);
                    certificate = new X509Certificate(certPtr);
                }
                finally
                {
                    Wldap32.CertFreeCRLContext(certPtr);
                }
                value = _serverCertificateDelegate(_connection, certificate);
            }
            return value;
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        private static bool AddLdapHandleRef(LdapConnection ldapConnection)
        {
            bool success = false;
            if (null != ldapConnection && null != ldapConnection.ldapHandle && !ldapConnection.ldapHandle.IsInvalid)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    ldapConnection.ldapHandle.DangerousAddRef(ref success);
                }
            }
            return success;
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        private static void ReleaseLdapHandleRef(LdapConnection ldapConnection)
        {
            if (null != ldapConnection && null != ldapConnection.ldapHandle && !ldapConnection.ldapHandle.IsInvalid)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    ldapConnection.ldapHandle.DangerousRelease();
                }
            }
        }
    }
}
