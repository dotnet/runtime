// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 618
[assembly: System.Net.WebPermission(System.Security.Permissions.SecurityAction.RequestMinimum, Unrestricted = true)]
[assembly: System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, UnmanagedCode = true)]
[assembly: System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: System.Security.Permissions.EnvironmentPermission(System.Security.Permissions.SecurityAction.RequestMinimum, Unrestricted = true)]
[assembly: System.Net.NetworkInformation.NetworkInformationPermission(System.Security.Permissions.SecurityAction.RequestMinimum, Unrestricted = true)]

#pragma warning restore 618

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Net;
    using System.Collections;
    using System.ComponentModel;
    using System.Text;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Xml;
    using System.Threading;
    using System.Security.Cryptography.X509Certificates;
    using System.DirectoryServices;
    using System.Security.Permissions;

    internal delegate DirectoryResponse GetLdapResponseCallback(int messageId, LdapOperation operation, ResultAll resultType, TimeSpan requestTimeout, bool exceptionOnTimeOut);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate bool QUERYCLIENTCERT(IntPtr Connection, IntPtr trusted_CAs, ref IntPtr certificateHandle);

    public class LdapConnection : DirectoryConnection, IDisposable
    {
        internal enum LdapResult
        {
            LDAP_RES_SEARCH_RESULT = 0x65,
            LDAP_RES_SEARCH_ENTRY = 0x64,
            LDAP_RES_MODIFY = 0x67,
            LDAP_RES_ADD = 0x69,
            LDAP_RES_DELETE = 0x6b,
            LDAP_RES_MODRDN = 0x6d,
            LDAP_RES_COMPARE = 0x6f,
            LDAP_RES_REFERRAL = 0x73,
            LDAP_RES_EXTENDED = 0x78
        }

        private const int LDAP_MOD_BVALUES = 0x80;
        private AuthType _connectionAuthType = AuthType.Negotiate;
        private LdapSessionOptions _options = null;
        internal ConnectionHandle ldapHandle = null;
        internal bool disposed = false;
        private bool _bounded = false;
        private bool _needRebind = false;
        internal static Hashtable handleTable = null;
        internal static object objectLock = null;
        private GetLdapResponseCallback _fd = null;
        private static Hashtable s_asyncResultTable = null;
        private static LdapPartialResultsProcessor s_partialResultsProcessor = null;
        private static ManualResetEvent s_waitHandle = null;
        private static PartialResultsRetriever s_retriever = null;
        private bool _setFQDNDone = false;
        internal bool automaticBind = true;
        internal bool needDispose = true;
        private bool _connected = false;
        internal QUERYCLIENTCERT clientCertificateRoutine = null;

        static LdapConnection()
        {
            handleTable = new Hashtable();
            // initialize the lock
            objectLock = new Object();

            Hashtable tempAsyncTable = new Hashtable();
            s_asyncResultTable = Hashtable.Synchronized(tempAsyncTable);

            s_waitHandle = new ManualResetEvent(false);

            s_partialResultsProcessor = new LdapPartialResultsProcessor(s_waitHandle);

            s_retriever = new PartialResultsRetriever(s_waitHandle, s_partialResultsProcessor);
        }

        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted = true),
        ]
        public LdapConnection(string server) : this(new LdapDirectoryIdentifier(server))
        {
        }

        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted = true)
        ]
        public LdapConnection(LdapDirectoryIdentifier identifier) : this(identifier, null, AuthType.Negotiate)
        {
        }

        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted = true)
        ]
        public LdapConnection(LdapDirectoryIdentifier identifier, NetworkCredential credential) : this(identifier, credential, AuthType.Negotiate)
        {
        }

        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted = true),
            EnvironmentPermission(SecurityAction.Assert, Unrestricted = true),
            SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)
        ]
        public LdapConnection(LdapDirectoryIdentifier identifier, NetworkCredential credential, AuthType authType)
        {
            _fd = new GetLdapResponseCallback(ConstructResponse);
            directoryIdentifier = identifier;
            directoryCredential = (credential != null) ? new NetworkCredential(credential.UserName, credential.Password, credential.Domain) : null;

            _connectionAuthType = authType;

            if (authType < AuthType.Anonymous || authType > AuthType.Kerberos)
                throw new InvalidEnumArgumentException("authType", (int)authType, typeof(AuthType));

            // if user wants to do anonymous bind, but specifies credential, error out
            if (AuthType == AuthType.Anonymous && (directoryCredential != null && ((directoryCredential.Password != null && directoryCredential.Password.Length != 0) || (directoryCredential.UserName != null && directoryCredential.UserName.Length != 0))))
                throw new ArgumentException(Res.GetString(Res.InvalidAuthCredential));

            Init();
            _options = new LdapSessionOptions(this);
            clientCertificateRoutine = new QUERYCLIENTCERT(ProcessClientCertificate);
        }

        internal LdapConnection(LdapDirectoryIdentifier identifier, NetworkCredential credential, AuthType authType, IntPtr handle)
        {
            directoryIdentifier = identifier;
            needDispose = false;
            ldapHandle = new ConnectionHandle(handle, needDispose);
            directoryCredential = credential;
            _connectionAuthType = authType;
            _options = new LdapSessionOptions(this);
            clientCertificateRoutine = new QUERYCLIENTCERT(ProcessClientCertificate);
        }

        ~LdapConnection()
        {
            Dispose(false);
        }

        public override TimeSpan Timeout
        {
            get
            {
                return connectionTimeOut;
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException(Res.GetString(Res.NoNegativeTime), "value");
                }

                // prevent integer overflow
                if (value.TotalSeconds > Int32.MaxValue)
                    throw new ArgumentException(Res.GetString(Res.TimespanExceedMax), "value");

                connectionTimeOut = value;
            }
        }

        public AuthType AuthType
        {
            get
            {
                return _connectionAuthType;
            }
            set
            {
                if (value < AuthType.Anonymous || value > AuthType.Kerberos)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(AuthType));

                // if the change is made after we have bound to the server and value is really changed, set the flag to indicate the need to do rebind
                if (_bounded && (value != _connectionAuthType))
                {
                    _needRebind = true;
                }

                _connectionAuthType = value;
            }
        }

        public LdapSessionOptions SessionOptions
        {
            get
            {
                return _options;
            }
        }

        public override NetworkCredential Credential
        {
            [
                DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true),
                EnvironmentPermission(SecurityAction.Assert, Unrestricted = true),
                SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)
            ]
            set
            {
                if (_bounded && !SameCredential(directoryCredential, value))
                    _needRebind = true;

                directoryCredential = (value != null) ? new NetworkCredential(value.UserName, value.Password, value.Domain) : null;
            }
        }

        public bool AutoBind
        {
            get
            {
                return automaticBind;
            }
            set
            {
                automaticBind = value;
            }
        }

        internal bool NeedDispose
        {
            get
            {
                return needDispose;
            }
            set
            {
                if (null != ldapHandle)
                {
                    ldapHandle.needDispose = value;
                }
                needDispose = value;
            }
        }

        internal void Init()
        {
            string hostname = null;
            string[] servers = (directoryIdentifier == null ? null : ((LdapDirectoryIdentifier)directoryIdentifier).Servers);
            if (servers != null && servers.Length != 0)
            {
                StringBuilder temp = new StringBuilder(200);
                for (int i = 0; i < servers.Length; i++)
                {
                    if (servers[i] != null)
                    {
                        temp.Append(servers[i]);
                        if (i < servers.Length - 1)
                            temp.Append(" ");
                    }
                }
                if (temp.Length != 0)
                    hostname = temp.ToString();
            }

            // user wants to setup a connectionless session with server
            if (((LdapDirectoryIdentifier)directoryIdentifier).Connectionless == true)
            {
                ldapHandle = new ConnectionHandle(Wldap32.cldap_open(hostname, ((LdapDirectoryIdentifier)directoryIdentifier).PortNumber), needDispose);
            }
            else
            {
                ldapHandle = new ConnectionHandle(Wldap32.ldap_init(hostname, ((LdapDirectoryIdentifier)directoryIdentifier).PortNumber), needDispose);
            }

            // create a WeakReference object with the target of ldapHandle and put it into our handle table.
            lock (objectLock)
            {
                if (handleTable[ldapHandle.DangerousGetHandle()] != null)
                    handleTable.Remove(ldapHandle.DangerousGetHandle());

                handleTable.Add(ldapHandle.DangerousGetHandle(), new WeakReference(this));
            }
        }

        [
           DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
       ]
        public override DirectoryResponse SendRequest(DirectoryRequest request)
        {
            // no request specific timeout is specified, use the connection timeout
            return SendRequest(request, connectionTimeOut);
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public DirectoryResponse SendRequest(DirectoryRequest request, TimeSpan requestTimeout)
        {
            if (this.disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (request == null)
                throw new ArgumentNullException("request");

            if (request is DsmlAuthRequest)
                throw new NotSupportedException(Res.GetString(Res.DsmlAuthRequestNotSupported));

            int messageID = 0;
            int error = SendRequestHelper(request, ref messageID);

            LdapOperation operation = LdapOperation.LdapSearch;
            if (request is DeleteRequest)
                operation = LdapOperation.LdapDelete;
            else if (request is AddRequest)
                operation = LdapOperation.LdapAdd;
            else if (request is ModifyRequest)
                operation = LdapOperation.LdapModify;
            else if (request is SearchRequest)
                operation = LdapOperation.LdapSearch;
            else if (request is ModifyDNRequest)
                operation = LdapOperation.LdapModifyDn;
            else if (request is CompareRequest)
                operation = LdapOperation.LdapCompare;
            else if (request is ExtendedRequest)
                operation = LdapOperation.LdapExtendedRequest;

            if (error == 0 && messageID != -1)
            {
                return ConstructResponse(messageID, operation, ResultAll.LDAP_MSG_ALL, requestTimeout, true);
            }
            else
            {
                if (error == 0)
                {
                    // success code but message is -1, unexpected
                    error = Wldap32.LdapGetLastError();
                }

                throw ConstructException(error, operation);
            }
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public IAsyncResult BeginSendRequest(DirectoryRequest request, PartialResultProcessing partialMode, AsyncCallback callback, object state)
        {
            return BeginSendRequest(request, connectionTimeOut, partialMode, callback, state);
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public IAsyncResult BeginSendRequest(DirectoryRequest request, TimeSpan requestTimeout, PartialResultProcessing partialMode, AsyncCallback callback, object state)
        {
            int messageID = 0;
            int error = 0;

            if (this.disposed)
                throw new ObjectDisposedException(GetType().Name);

            // parameter validation
            if (request == null)
                throw new ArgumentNullException("request");

            if (partialMode < PartialResultProcessing.NoPartialResultSupport || partialMode > PartialResultProcessing.ReturnPartialResultsAndNotifyCallback)
                throw new InvalidEnumArgumentException("partialMode", (int)partialMode, typeof(PartialResultProcessing));

            if (partialMode != PartialResultProcessing.NoPartialResultSupport && !(request is SearchRequest))
                throw new NotSupportedException(Res.GetString(Res.PartialResultsNotSupported));

            if (partialMode == PartialResultProcessing.ReturnPartialResultsAndNotifyCallback && (callback == null))
                throw new ArgumentException(Res.GetString(Res.CallBackIsNull), "callback");

            error = SendRequestHelper(request, ref messageID);

            LdapOperation operation = LdapOperation.LdapSearch;
            if (request is DeleteRequest)
                operation = LdapOperation.LdapDelete;
            else if (request is AddRequest)
                operation = LdapOperation.LdapAdd;
            else if (request is ModifyRequest)
                operation = LdapOperation.LdapModify;
            else if (request is SearchRequest)
                operation = LdapOperation.LdapSearch;
            else if (request is ModifyDNRequest)
                operation = LdapOperation.LdapModifyDn;
            else if (request is CompareRequest)
                operation = LdapOperation.LdapCompare;
            else if (request is ExtendedRequest)
                operation = LdapOperation.LdapExtendedRequest;

            if (error == 0 && messageID != -1)
            {
                if (partialMode == PartialResultProcessing.NoPartialResultSupport)
                {
                    LdapRequestState rs = new LdapRequestState();
                    LdapAsyncResult asyncResult = new LdapAsyncResult(callback, state, false);

                    rs.ldapAsync = asyncResult;
                    asyncResult.resultObject = rs;

                    s_asyncResultTable.Add(asyncResult, messageID);

                    _fd.BeginInvoke(messageID, operation, ResultAll.LDAP_MSG_ALL, requestTimeout, true, new AsyncCallback(ResponseCallback), rs);

                    return (IAsyncResult)asyncResult;
                }
                else
                {
                    // the user registers to retrieve partial results
                    bool partialCallback = false;
                    if (partialMode == PartialResultProcessing.ReturnPartialResultsAndNotifyCallback)
                        partialCallback = true;
                    LdapPartialAsyncResult asyncResult = new LdapPartialAsyncResult(messageID, callback, state, true, this, partialCallback, requestTimeout);
                    s_partialResultsProcessor.Add(asyncResult);

                    return (IAsyncResult)asyncResult;
                }
            }
            else
            {
                if (error == 0)
                {
                    // success code but message is -1, unexpected
                    error = Wldap32.LdapGetLastError();
                }

                throw ConstructException(error, operation);
            }
        }

        private void ResponseCallback(IAsyncResult asyncResult)
        {
            LdapRequestState rs = (LdapRequestState)asyncResult.AsyncState;

            try
            {
                DirectoryResponse response = _fd.EndInvoke(asyncResult);
                rs.response = response;
            }
            catch (Exception e)
            {
                rs.exception = e;
                rs.response = null;
            }

            // signal waitable object, indicate operation completed and fire callback
            rs.ldapAsync.manualResetEvent.Set();
            rs.ldapAsync.completed = true;
            if (rs.ldapAsync.callback != null && !rs.abortCalled)
            {
                rs.ldapAsync.callback((IAsyncResult)rs.ldapAsync);
            }
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public void Abort(IAsyncResult asyncResult)
        {
            if (this.disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            if (!(asyncResult is LdapAsyncResult))
                throw new ArgumentException(Res.GetString(Res.NotReturnedAsyncResult, "asyncResult"));

            int messageId = -1;

            LdapAsyncResult result = (LdapAsyncResult)asyncResult;

            if (!result.partialResults)
            {
                if (!s_asyncResultTable.Contains(asyncResult))
                    throw new ArgumentException(Res.GetString(Res.InvalidAsyncResult));

                messageId = (int)(s_asyncResultTable[asyncResult]);

                // remove the asyncResult from our connection table
                s_asyncResultTable.Remove(asyncResult);
            }
            else
            {
                s_partialResultsProcessor.Remove((LdapPartialAsyncResult)asyncResult);
                messageId = ((LdapPartialAsyncResult)asyncResult).messageID;
            }

            // cancel the request
            Wldap32.ldap_abandon(ldapHandle, messageId);

            LdapRequestState rs = result.resultObject;
            if (rs != null)
                rs.abortCalled = true;
        }

        public PartialResultsCollection GetPartialResults(IAsyncResult asyncResult)
        {
            if (this.disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            if (!(asyncResult is LdapAsyncResult))
                throw new ArgumentException(Res.GetString(Res.NotReturnedAsyncResult, "asyncResult"));

            if (!(asyncResult is LdapPartialAsyncResult))
                throw new InvalidOperationException(Res.GetString(Res.NoPartialResults));

            return s_partialResultsProcessor.GetPartialResults((LdapPartialAsyncResult)asyncResult);
        }

        public DirectoryResponse EndSendRequest(IAsyncResult asyncResult)
        {
            if (this.disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            if (!(asyncResult is LdapAsyncResult))
                throw new ArgumentException(Res.GetString(Res.NotReturnedAsyncResult, "asyncResult"));

            LdapAsyncResult result = (LdapAsyncResult)asyncResult;

            if (!result.partialResults)
            {
                // not a partial results
                if (!s_asyncResultTable.Contains(asyncResult))
                    throw new ArgumentException(Res.GetString(Res.InvalidAsyncResult));

                // remove the asyncResult from our connection table
                s_asyncResultTable.Remove(asyncResult);

                asyncResult.AsyncWaitHandle.WaitOne();

                if (result.resultObject.exception != null)
                    throw result.resultObject.exception;
                else
                    return result.resultObject.response;
            }
            else
            {
                // deal with partial results
                s_partialResultsProcessor.NeedCompleteResult((LdapPartialAsyncResult)asyncResult);
                asyncResult.AsyncWaitHandle.WaitOne();

                return s_partialResultsProcessor.GetCompleteResult((LdapPartialAsyncResult)asyncResult);
            }
        }

        private int SendRequestHelper(DirectoryRequest request, ref int messageID)
        {
            IntPtr serverControlArray = (IntPtr)0;
            LdapControl[] managedServerControls = null;
            IntPtr clientControlArray = (IntPtr)0;
            LdapControl[] managedClientControls = null;

            string strValue = null;

            ArrayList ptrToFree = new ArrayList();
            LdapMod[] modifications = null;
            IntPtr modArray = (IntPtr)0;
            int addModCount = 0;

            berval berValuePtr = null;

            IntPtr searchAttributes = (IntPtr)0;
            DereferenceAlias searchAliases;
            int attributeCount = 0;

            int error = 0;

            // connect to the server first if have not done so
            if (!_connected)
            {
                Connect();
                _connected = true;
            }

            //do Bind if user has not turned off automatic bind, have not done so or there is a need to do rebind, also connectionless LDAP does not need to do bind
            if (AutoBind && (!_bounded || _needRebind) && ((LdapDirectoryIdentifier)Directory).Connectionless != true)
            {
                Debug.WriteLine("rebind occurs\n");
                Bind();
            }

            try
            {
                IntPtr controlPtr = (IntPtr)0;
                IntPtr tempPtr = (IntPtr)0;

                // build server control
                managedServerControls = BuildControlArray(request.Controls, true);
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
                managedClientControls = BuildControlArray(request.Controls, false);
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

                if (request is DeleteRequest)
                {
                    // it is an delete operation                      
                    error = Wldap32.ldap_delete_ext(ldapHandle, ((DeleteRequest)request).DistinguishedName, serverControlArray, clientControlArray, ref messageID);
                }
                else if (request is ModifyDNRequest)
                {
                    // it is a modify dn operation
                    error = Wldap32.ldap_rename(ldapHandle,
                                                 ((ModifyDNRequest)request).DistinguishedName,
                                                 ((ModifyDNRequest)request).NewName,
                                                 ((ModifyDNRequest)request).NewParentDistinguishedName,
                                                 ((ModifyDNRequest)request).DeleteOldRdn ? 1 : 0,
                                                 serverControlArray, clientControlArray, ref messageID);
                }
                else if (request is CompareRequest)
                {
                    // it is a compare request
                    DirectoryAttribute assertion = ((CompareRequest)request).Assertion;
                    if (assertion == null)
                        throw new ArgumentException(Res.GetString(Res.WrongAssertionCompare));

                    if (assertion.Count != 1)
                        throw new ArgumentException(Res.GetString(Res.WrongNumValuesCompare));

                    // process the attribute
                    byte[] byteArray = assertion[0] as byte[];
                    if (byteArray != null)
                    {
                        if (byteArray != null && byteArray.Length != 0)
                        {
                            berValuePtr = new berval();
                            berValuePtr.bv_len = byteArray.Length;
                            berValuePtr.bv_val = Marshal.AllocHGlobal(byteArray.Length);
                            Marshal.Copy(byteArray, 0, berValuePtr.bv_val, byteArray.Length);
                        }
                    }
                    else
                    {
                        strValue = assertion[0].ToString();
                    }

                    // it is a compare request
                    error = Wldap32.ldap_compare(ldapHandle,
                                                  ((CompareRequest)request).DistinguishedName,
                                                  assertion.Name,
                                                  strValue,
                                                  berValuePtr,
                                                  serverControlArray, clientControlArray, ref messageID);
                }
                else if (request is AddRequest || request is ModifyRequest)
                {
                    // build the attributes

                    if (request is AddRequest)
                        modifications = BuildAttributes(((AddRequest)request).Attributes, ptrToFree);
                    else
                        modifications = BuildAttributes(((ModifyRequest)request).Modifications, ptrToFree);

                    addModCount = (modifications == null ? 1 : modifications.Length + 1);
                    modArray = Utility.AllocHGlobalIntPtrArray(addModCount);
                    int modStructSize = Marshal.SizeOf(typeof(LdapMod));
                    int i = 0;
                    for (i = 0; i < addModCount - 1; i++)
                    {
                        controlPtr = Marshal.AllocHGlobal(modStructSize);
                        Marshal.StructureToPtr(modifications[i], controlPtr, false);
                        tempPtr = (IntPtr)((long)modArray + Marshal.SizeOf(typeof(IntPtr)) * i);
                        Marshal.WriteIntPtr(tempPtr, controlPtr);
                    }
                    tempPtr = (IntPtr)((long)modArray + Marshal.SizeOf(typeof(IntPtr)) * i);
                    Marshal.WriteIntPtr(tempPtr, (IntPtr)0);

                    if (request is AddRequest)
                    {
                        error = Wldap32.ldap_add(ldapHandle,
                                                  ((AddRequest)request).DistinguishedName,
                                                  modArray,
                                                  serverControlArray, clientControlArray, ref messageID);
                    }
                    else
                    {
                        error = Wldap32.ldap_modify(ldapHandle,
                                                     ((ModifyRequest)request).DistinguishedName,
                                                     modArray,
                                                     serverControlArray, clientControlArray, ref messageID);
                    }
                }
                else if (request is ExtendedRequest)
                {
                    string name = ((ExtendedRequest)request).RequestName;
                    byte[] val = ((ExtendedRequest)request).RequestValue;

                    // process the requestvalue
                    if (val != null && val.Length != 0)
                    {
                        berValuePtr = new berval();
                        berValuePtr.bv_len = val.Length;
                        berValuePtr.bv_val = Marshal.AllocHGlobal(val.Length);
                        Marshal.Copy(val, 0, berValuePtr.bv_val, val.Length);
                    }

                    error = Wldap32.ldap_extended_operation(ldapHandle,
                                                            name,
                                                            berValuePtr,
                                                            serverControlArray, clientControlArray, ref messageID);
                }
                else if (request is SearchRequest)
                {
                    // processing the filter
                    SearchRequest searchRequest = (SearchRequest)request;
                    object filter = searchRequest.Filter;
                    if (filter != null)
                    {
                        // LdapConnection only supports ldap filter
                        if (filter is XmlDocument)
                            throw new ArgumentException(Res.GetString(Res.InvalidLdapSearchRequestFilter));
                    }
                    string searchRequestFilter = (string)filter;

                    // processing the attributes
                    attributeCount = (searchRequest.Attributes == null ? 0 : searchRequest.Attributes.Count);
                    if (attributeCount != 0)
                    {
                        searchAttributes = Utility.AllocHGlobalIntPtrArray(attributeCount + 1);
                        int i = 0;
                        for (i = 0; i < attributeCount; i++)
                        {
                            controlPtr = Marshal.StringToHGlobalUni(searchRequest.Attributes[i]);
                            tempPtr = (IntPtr)((long)searchAttributes + Marshal.SizeOf(typeof(IntPtr)) * i);
                            Marshal.WriteIntPtr(tempPtr, controlPtr);
                        }
                        tempPtr = (IntPtr)((long)searchAttributes + Marshal.SizeOf(typeof(IntPtr)) * i);
                        Marshal.WriteIntPtr(tempPtr, (IntPtr)0);
                    }

                    // processing the scope
                    int searchScope = (int)searchRequest.Scope;

                    // processing the timelimit
                    int searchTimeLimit = (int)(searchRequest.TimeLimit.Ticks / TimeSpan.TicksPerSecond);

                    // processing the alias                    
                    searchAliases = _options.DerefAlias;
                    _options.DerefAlias = searchRequest.Aliases;

                    try
                    {
                        error = Wldap32.ldap_search(ldapHandle,
                                                     searchRequest.DistinguishedName,
                                                     searchScope,
                                                     searchRequestFilter,
                                                     searchAttributes,
                                                     searchRequest.TypesOnly,
                                                     serverControlArray,
                                                     clientControlArray,
                                                     searchTimeLimit,
                                                     searchRequest.SizeLimit,
                                                     ref messageID);
                    }
                    finally
                    {
                        // revert back
                        _options.DerefAlias = searchAliases;
                    }
                }
                else
                {
                    throw new NotSupportedException(Res.GetString(Res.InvliadRequestType));
                }

                // the asynchronous call itself timeout, this actually means that we time out the LDAP_OPT_SEND_TIMEOUT specified in the session option
                // wldap32 does not differentiate that, but the application caller actually needs this information to determin what to do with the error code
                if (error == (int)LdapError.TimeOut)
                    error = (int)LdapError.SendTimeOut;

                return error;
            }
            finally
            {
                GC.KeepAlive(modifications);

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
                    // release the memory from the heap
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

                if (modArray != (IntPtr)0)
                {
                    // release the memory from the heap
                    for (int i = 0; i < addModCount - 1; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(modArray, Marshal.SizeOf(typeof(IntPtr)) * i);
                        if (tempPtr != (IntPtr)0)
                            Marshal.FreeHGlobal(tempPtr);
                    }
                    Marshal.FreeHGlobal(modArray);
                }

                // free the pointers
                for (int x = 0; x < ptrToFree.Count; x++)
                {
                    IntPtr tempPtr = (IntPtr)ptrToFree[x];
                    Marshal.FreeHGlobal(tempPtr);
                }

                if (berValuePtr != null)
                {
                    if (berValuePtr.bv_val != (IntPtr)0)
                        Marshal.FreeHGlobal(berValuePtr.bv_val);
                }

                if (searchAttributes != (IntPtr)0)
                {
                    for (int i = 0; i < attributeCount; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(searchAttributes, Marshal.SizeOf(typeof(IntPtr)) * i);
                        if (tempPtr != (IntPtr)0)
                            Marshal.FreeHGlobal(tempPtr);
                    }
                    Marshal.FreeHGlobal(searchAttributes);
                }
            }
        }

        private bool ProcessClientCertificate(IntPtr ldapHandle, IntPtr CAs, ref IntPtr certificate)
        {
            ArrayList list = new ArrayList();
            byte[][] certAuthorities = null;
            int count = ClientCertificates == null ? 0 : ClientCertificates.Count;

            if (count == 0 && _options.clientCertificateDelegate == null)
            {
                return false;
            }

            // if user specify certificate through property and not though option, we don't need to check CA
            if (_options.clientCertificateDelegate == null)
            {
                certificate = ClientCertificates[0].Handle;
                return true;
            }

            // processing the CA
            if (CAs != (IntPtr)0)
            {
                SecPkgContext_IssuerListInfoEx trustedCAs = (SecPkgContext_IssuerListInfoEx)Marshal.PtrToStructure(CAs, typeof(SecPkgContext_IssuerListInfoEx));
                int issuerNumber = trustedCAs.cIssuers;
                IntPtr tempPtr = (IntPtr)0;
                for (int i = 0; i < issuerNumber; i++)
                {
                    tempPtr = (IntPtr)((long)trustedCAs.aIssuers + Marshal.SizeOf(typeof(CRYPTOAPI_BLOB)) * i);
                    CRYPTOAPI_BLOB info = (CRYPTOAPI_BLOB)Marshal.PtrToStructure(tempPtr, typeof(CRYPTOAPI_BLOB));
                    int dataLength = info.cbData;
                    byte[] context = new byte[dataLength];
                    Marshal.Copy(info.pbData, context, 0, dataLength);
                    list.Add(context);
                }
            }

            if (list.Count != 0)
            {
                certAuthorities = new byte[list.Count][];
                for (int i = 0; i < list.Count; i++)
                {
                    certAuthorities[i] = (byte[])list[i];
                }
            }

            X509Certificate cert = _options.clientCertificateDelegate(this, certAuthorities);
            if (cert != null)
            {
                certificate = cert.Handle;
                return true;
            }
            else
            {
                certificate = (IntPtr)0;
                return false;
            }
        }

        private void Connect()
        {
            int error = 0;
            string errorMessage;

            // currently ldap does not accept more than one certificate, so check here
            if (ClientCertificates.Count > 1)
                throw new InvalidOperationException(Res.GetString(Res.InvalidClientCertificates));

            // set the certificate callback routine here if user adds the certifcate to the certificate collection
            if (ClientCertificates.Count != 0)
            {
                int certerror = Wldap32.ldap_set_option_clientcert(ldapHandle, LdapOption.LDAP_OPT_CLIENT_CERTIFICATE, clientCertificateRoutine);
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
                // when certificate is specified, automatic bind is disabled
                automaticBind = false;
            }

            // set the LDAP_OPT_AREC_EXCLUSIVE flag if necessary
            if (((LdapDirectoryIdentifier)Directory).FullyQualifiedDnsHostName && !_setFQDNDone)
            {
                SessionOptions.FQDN = true;
                _setFQDNDone = true;
            }

            // connect explicitly to the server
            LDAP_TIMEVAL timeout = new LDAP_TIMEVAL();
            timeout.tv_sec = (int)(connectionTimeOut.Ticks / TimeSpan.TicksPerSecond);
            Debug.Assert(!ldapHandle.IsInvalid);
            error = Wldap32.ldap_connect(ldapHandle, timeout);
            // failed, throw exception
            if (error != (int)ResultCode.Success)
            {
                if (Utility.IsLdapError((LdapError)error))
                {
                    errorMessage = LdapErrorMappings.MapResultCode(error);
                    throw new LdapException(error, errorMessage);
                }
                else
                    throw new LdapException(error);
            }
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public void Bind()
        {
            BindHelper(directoryCredential, false /* no need to reset credential */);
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public void Bind(NetworkCredential newCredential)
        {
            BindHelper(newCredential, true /* need to reset credential */);
        }

        [
            EnvironmentPermission(SecurityAction.Assert, Unrestricted = true),
            SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)
        ]
        private void BindHelper(NetworkCredential newCredential, bool needSetCredential)
        {
            int error = 0;
            string errorMessage;
            string username;
            string domainname;
            string password;
            NetworkCredential tempCredential = null;

            // if already disposed, we need to throw exception
            if (this.disposed)
                throw new ObjectDisposedException(GetType().Name);

            // if user wants to do anonymous bind, but specifies credential, error out
            if (AuthType == AuthType.Anonymous && (newCredential != null && ((newCredential.Password != null && newCredential.Password.Length != 0) || (newCredential.UserName != null && newCredential.UserName.Length != 0))))
                throw new InvalidOperationException(Res.GetString(Res.InvalidAuthCredential));

            // set the credential
            if (needSetCredential)
                directoryCredential = tempCredential = (newCredential != null ? new NetworkCredential(newCredential.UserName, newCredential.Password, newCredential.Domain) : null);
            else
                tempCredential = directoryCredential;

            // connect to the server first
            if (!_connected)
            {
                Connect();
                _connected = true;
            }

            // bind to the server
            if (tempCredential != null && tempCredential.UserName.Length == 0 && tempCredential.Password.Length == 0 && tempCredential.Domain.Length == 0)
            {
                // default credential
                username = null;
                domainname = null;
                password = null;
            }
            else
            {
                username = (tempCredential == null) ? null : tempCredential.UserName;
                domainname = (tempCredential == null) ? null : tempCredential.Domain;
                password = (tempCredential == null) ? null : tempCredential.Password;
            }

            if (AuthType == AuthType.Anonymous)
                error = Wldap32.ldap_simple_bind_s(ldapHandle, null, null);
            else if (AuthType == AuthType.Basic)
            {
                StringBuilder tempdn = new StringBuilder(100);
                if (domainname != null && domainname.Length != 0)
                {
                    tempdn.Append(domainname);
                    tempdn.Append("\\");
                }
                tempdn.Append(username);
                error = Wldap32.ldap_simple_bind_s(ldapHandle, tempdn.ToString(), password);
            }
            else
            {
                SEC_WINNT_AUTH_IDENTITY_EX cred = new SEC_WINNT_AUTH_IDENTITY_EX();
                cred.version = Wldap32.SEC_WINNT_AUTH_IDENTITY_VERSION;
                cred.length = Marshal.SizeOf(typeof(SEC_WINNT_AUTH_IDENTITY_EX));
                cred.flags = Wldap32.SEC_WINNT_AUTH_IDENTITY_UNICODE;
                if (AuthType == AuthType.Kerberos)
                {
                    cred.packageList = Wldap32.MICROSOFT_KERBEROS_NAME_W;
                    cred.packageListLength = cred.packageList.Length;
                }

                if (tempCredential != null)
                {
                    cred.user = username;
                    cred.userLength = (username == null ? 0 : username.Length);
                    cred.domain = domainname;
                    cred.domainLength = (domainname == null ? 0 : domainname.Length);
                    cred.password = password;
                    cred.passwordLength = (password == null ? 0 : password.Length);
                }

                BindMethod method = BindMethod.LDAP_AUTH_NEGOTIATE;
                switch (AuthType)
                {
                    case AuthType.Negotiate:
                        method = BindMethod.LDAP_AUTH_NEGOTIATE;
                        break;
                    case AuthType.Kerberos:
                        method = BindMethod.LDAP_AUTH_NEGOTIATE;
                        break;
                    case AuthType.Ntlm:
                        method = BindMethod.LDAP_AUTH_NTLM;
                        break;
                    case AuthType.Digest:
                        method = BindMethod.LDAP_AUTH_DIGEST;
                        break;
                    case AuthType.Sicily:
                        method = BindMethod.LDAP_AUTH_SICILY;
                        break;
                    case AuthType.Dpa:
                        method = BindMethod.LDAP_AUTH_DPA;
                        break;
                    case AuthType.Msn:
                        method = BindMethod.LDAP_AUTH_MSN;
                        break;
                    case AuthType.External:
                        method = BindMethod.LDAP_AUTH_EXTERNAL;
                        break;
                }

                if ((tempCredential == null) && (AuthType == AuthType.External))
                {
                    error = Wldap32.ldap_bind_s(ldapHandle, null, null, method);
                }
                else
                {
                    error = Wldap32.ldap_bind_s(ldapHandle, null, cred, method);
                }
            }

            // failed, throw exception
            if (error != (int)ResultCode.Success)
            {
                if (Utility.IsResultCode((ResultCode)error))
                {
                    errorMessage = OperationErrorMappings.MapResultCode(error);
                    throw new DirectoryOperationException(null, errorMessage);
                }
                else if (Utility.IsLdapError((LdapError)error))
                {
                    errorMessage = LdapErrorMappings.MapResultCode(error);
                    string serverErrorMessage = _options.ServerErrorMessage;
                    if ((serverErrorMessage != null) && (serverErrorMessage.Length > 0))
                    {
                        throw new LdapException(error, errorMessage, serverErrorMessage);
                    }
                    else
                    {
                        throw new LdapException(error, errorMessage);
                    }
                }
                else
                    throw new LdapException(error);
            }

            // we successfully bind to the server
            _bounded = true;
            // rebind has been done
            _needRebind = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free other state (managed objects)                

                // we need to remove the handle from the handle table
                lock (objectLock)
                {
                    if (null != ldapHandle)
                    {
                        handleTable.Remove(ldapHandle.DangerousGetHandle());
                    }
                }
            }
            // free your own state (unmanaged objects)        	           

            // close the ldap connection
            if (needDispose && ldapHandle != null && !ldapHandle.IsInvalid)
                ldapHandle.Dispose();
            ldapHandle = null;

            disposed = true;

            Debug.WriteLine("Connection object is disposed\n");
        }

        internal LdapControl[] BuildControlArray(DirectoryControlCollection controls, bool serverControl)
        {
            int count = 0;
            LdapControl[] managedControls = null;

            if (controls != null && controls.Count != 0)
            {
                ArrayList controlList = new ArrayList();
                foreach (DirectoryControl col in controls)
                {
                    if (serverControl == true)
                    {
                        if (col.ServerSide)
                            controlList.Add(col);
                    }
                    else
                    {
                        if (!col.ServerSide)
                            controlList.Add(col);
                    }
                }
                if (controlList.Count != 0)
                {
                    count = controlList.Count;

                    managedControls = new LdapControl[count];

                    for (int i = 0; i < count; i++)
                    {
                        managedControls[i] = new LdapControl();
                        // get the control type
                        managedControls[i].ldctl_oid = Marshal.StringToHGlobalUni(((DirectoryControl)controlList[i]).Type);
                        //get the control cricality
                        managedControls[i].ldctl_iscritical = ((DirectoryControl)controlList[i]).IsCritical;
                        // get the control value
                        DirectoryControl tempControl = (DirectoryControl)controlList[i];
                        byte[] byteControlValue = tempControl.GetValue();
                        if (byteControlValue == null || byteControlValue.Length == 0)
                        {
                            // treat the control value as null
                            managedControls[i].ldctl_value = new berval();
                            managedControls[i].ldctl_value.bv_len = 0;
                            managedControls[i].ldctl_value.bv_val = (IntPtr)0;
                        }
                        else
                        {
                            managedControls[i].ldctl_value = new berval();
                            managedControls[i].ldctl_value.bv_len = byteControlValue.Length;
                            managedControls[i].ldctl_value.bv_val = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)) * managedControls[i].ldctl_value.bv_len);
                            Marshal.Copy(byteControlValue, 0, managedControls[i].ldctl_value.bv_val, managedControls[i].ldctl_value.bv_len);
                        }
                    }
                }
            }

            return managedControls;
        }

        internal LdapMod[] BuildAttributes(CollectionBase directoryAttributes, ArrayList ptrToFree)
        {
            LdapMod[] attributes = null;
            UTF8Encoding encoder = new UTF8Encoding();
            DirectoryAttributeCollection attributeCollection = null;
            DirectoryAttributeModificationCollection modificationCollection = null;
            DirectoryAttribute modAttribute = null;

            if (directoryAttributes != null && directoryAttributes.Count != 0)
            {
                if (directoryAttributes is DirectoryAttributeModificationCollection)
                {
                    modificationCollection = (DirectoryAttributeModificationCollection)directoryAttributes;
                }
                else
                {
                    attributeCollection = (DirectoryAttributeCollection)directoryAttributes;
                }

                attributes = new LdapMod[directoryAttributes.Count];
                for (int i = 0; i < directoryAttributes.Count; i++)
                {
                    // get the managed attribute first
                    if (attributeCollection != null)
                        modAttribute = attributeCollection[i];
                    else
                        modAttribute = modificationCollection[i];

                    attributes[i] = new LdapMod();

                    // operation type
                    if (modAttribute is DirectoryAttributeModification)
                    {
                        attributes[i].type = (int)((DirectoryAttributeModification)modAttribute).Operation;
                    }
                    else
                    {
                        attributes[i].type = (int)DirectoryAttributeOperation.Add;
                    }
                    // we treat all the values as binary
                    attributes[i].type |= (int)LDAP_MOD_BVALUES;

                    //attribute name
                    attributes[i].attribute = Marshal.StringToHGlobalUni(modAttribute.Name);

                    // values
                    int valuesCount = 0;
                    berval[] berValues = null;
                    if (modAttribute.Count > 0)
                    {
                        valuesCount = modAttribute.Count;
                        berValues = new berval[valuesCount];
                        for (int j = 0; j < valuesCount; j++)
                        {
                            byte[] byteArray = null;
                            if (modAttribute[j] is string)
                                byteArray = encoder.GetBytes((string)modAttribute[j]);
                            else if (modAttribute[j] is Uri)
                                byteArray = encoder.GetBytes(((Uri)modAttribute[j]).ToString());
                            else
                                byteArray = (byte[])modAttribute[j];

                            berValues[j] = new berval();
                            berValues[j].bv_len = byteArray.Length;
                            berValues[j].bv_val = Marshal.AllocHGlobal(berValues[j].bv_len);
                            // need to free the memory allocated on the heap when we are done
                            ptrToFree.Add(berValues[j].bv_val);
                            Marshal.Copy(byteArray, 0, berValues[j].bv_val, berValues[j].bv_len);
                        }
                    }

                    attributes[i].values = Utility.AllocHGlobalIntPtrArray(valuesCount + 1);
                    int structSize = Marshal.SizeOf(typeof(berval));
                    IntPtr controlPtr = (IntPtr)0;
                    IntPtr tempPtr = (IntPtr)0;
                    int m = 0;
                    for (m = 0; m < valuesCount; m++)
                    {
                        controlPtr = Marshal.AllocHGlobal(structSize);
                        // need to free the memory allocated on the heap when we are done
                        ptrToFree.Add(controlPtr);
                        Marshal.StructureToPtr(berValues[m], controlPtr, false);
                        tempPtr = (IntPtr)((long)attributes[i].values + Marshal.SizeOf(typeof(IntPtr)) * m);
                        Marshal.WriteIntPtr(tempPtr, controlPtr);
                    }
                    tempPtr = (IntPtr)((long)attributes[i].values + Marshal.SizeOf(typeof(IntPtr)) * m);
                    Marshal.WriteIntPtr(tempPtr, (IntPtr)0);
                }
            }

            return attributes;
        }

        internal DirectoryResponse ConstructResponse(int messageId, LdapOperation operation, ResultAll resultType, TimeSpan requestTimeOut, bool exceptionOnTimeOut)
        {
            int error;
            LDAP_TIMEVAL timeout = new LDAP_TIMEVAL();
            timeout.tv_sec = (int)(requestTimeOut.Ticks / TimeSpan.TicksPerSecond);
            IntPtr ldapResult = (IntPtr)0;
            DirectoryResponse response = null;

            IntPtr requestName = (IntPtr)0;
            IntPtr requestValue = (IntPtr)0;

            IntPtr entryMessage = (IntPtr)0;

            bool needAbandon = true;

            // processing for the partial results retrieval
            if (resultType != ResultAll.LDAP_MSG_ALL)
            {
                // we need to have 0 timeout as we are polling for the results and don't want to wait
                timeout.tv_sec = 0;
                timeout.tv_usec = 0;

                if (resultType == ResultAll.LDAP_MSG_POLLINGALL)
                    resultType = ResultAll.LDAP_MSG_ALL;

                // when doing partial results retrieving, if ldap_result failed, we don't do ldap_abandon here.
                needAbandon = false;
            }

            error = Wldap32.ldap_result(ldapHandle, messageId, (int)resultType, timeout, ref ldapResult);
            if (error != -1 && error != 0)
            {
                // parsing the result
                int serverError = 0;
                try
                {
                    int resulterror = 0;
                    string responseDn = null;
                    string responseMessage = null;
                    Uri[] responseReferral = null;
                    DirectoryControl[] responseControl = null;

                    // ldap_parse_result skips over messages of type LDAP_RES_SEARCH_ENTRY and LDAP_RES_SEARCH_REFERRAL
                    if (error != (int)LdapResult.LDAP_RES_SEARCH_ENTRY && error != (int)LdapResult.LDAP_RES_REFERRAL)
                        resulterror = ConstructParsedResult(ldapResult, ref serverError, ref responseDn, ref responseMessage, ref responseReferral, ref responseControl);

                    if (resulterror == 0)
                    {
                        resulterror = serverError;

                        if (error == (int)LdapResult.LDAP_RES_ADD)
                            response = new AddResponse(responseDn, responseControl, (ResultCode)resulterror, responseMessage, responseReferral);
                        else if (error == (int)LdapResult.LDAP_RES_MODIFY)
                            response = new ModifyResponse(responseDn, responseControl, (ResultCode)resulterror, responseMessage, responseReferral);
                        else if (error == (int)LdapResult.LDAP_RES_DELETE)
                            response = new DeleteResponse(responseDn, responseControl, (ResultCode)resulterror, responseMessage, responseReferral);
                        else if (error == (int)LdapResult.LDAP_RES_MODRDN)
                            response = new ModifyDNResponse(responseDn, responseControl, (ResultCode)resulterror, responseMessage, responseReferral);
                        else if (error == (int)LdapResult.LDAP_RES_COMPARE)
                            response = new CompareResponse(responseDn, responseControl, (ResultCode)resulterror, responseMessage, responseReferral);
                        else if (error == (int)LdapResult.LDAP_RES_EXTENDED)
                        {
                            response = new ExtendedResponse(responseDn, responseControl, (ResultCode)resulterror, responseMessage, responseReferral);
                            if (resulterror == (int)ResultCode.Success)
                            {
                                resulterror = Wldap32.ldap_parse_extended_result(ldapHandle, ldapResult, ref requestName, ref requestValue, 0 /*not free it*/);
                                if (resulterror == 0)
                                {
                                    string name = null;
                                    if (requestName != (IntPtr)0)
                                    {
                                        name = Marshal.PtrToStringUni(requestName);
                                    }

                                    berval val = null;
                                    byte[] requestValueArray = null;
                                    if (requestValue != (IntPtr)0)
                                    {
                                        val = new berval();
                                        Marshal.PtrToStructure(requestValue, val);
                                        if (val.bv_len != 0 && val.bv_val != (IntPtr)0)
                                        {
                                            requestValueArray = new byte[val.bv_len];
                                            Marshal.Copy(val.bv_val, requestValueArray, 0, val.bv_len);
                                        }
                                    }

                                    ((ExtendedResponse)response).name = name;
                                    ((ExtendedResponse)response).value = requestValueArray;
                                }
                            }
                        }
                        else if (error == (int)LdapResult.LDAP_RES_SEARCH_RESULT ||
                               error == (int)LdapResult.LDAP_RES_SEARCH_ENTRY ||
                               error == (int)LdapResult.LDAP_RES_REFERRAL)
                        {
                            response = new SearchResponse(responseDn, responseControl, (ResultCode)resulterror, responseMessage, responseReferral);

                            //set the flag here so our partial result processor knows whether the search is done or not
                            if (error == (int)LdapResult.LDAP_RES_SEARCH_RESULT)
                            {
                                ((SearchResponse)response).searchDone = true;
                            }

                            SearchResultEntryCollection searchResultEntries = new SearchResultEntryCollection();
                            SearchResultReferenceCollection searchResultReferences = new SearchResultReferenceCollection();

                            // parsing the resultentry
                            entryMessage = Wldap32.ldap_first_entry(ldapHandle, ldapResult);

                            int entrycount = 0;
                            while (entryMessage != (IntPtr)0)
                            {
                                SearchResultEntry entry = ConstructEntry(entryMessage);
                                if (entry != null)
                                    searchResultEntries.Add(entry);

                                entrycount++;
                                entryMessage = Wldap32.ldap_next_entry(ldapHandle, entryMessage);
                            }

                            // parsing the reference
                            IntPtr referenceMessage = Wldap32.ldap_first_reference(ldapHandle, ldapResult);

                            while (referenceMessage != (IntPtr)0)
                            {
                                SearchResultReference reference = ConstructReference(referenceMessage);
                                if (reference != null)
                                    searchResultReferences.Add(reference);

                                referenceMessage = Wldap32.ldap_next_reference(ldapHandle, referenceMessage);
                            }

                            ((SearchResponse)response).SetEntries(searchResultEntries);
                            ((SearchResponse)response).SetReferences(searchResultReferences);
                        }

                        if (resulterror != (int)ResultCode.Success && resulterror != (int)ResultCode.CompareFalse && resulterror != (int)ResultCode.CompareTrue && resulterror != (int)ResultCode.Referral && resulterror != (int)ResultCode.ReferralV2)
                        {
                            // throw operation exception                   
                            if (Utility.IsResultCode((ResultCode)resulterror))
                            {
                                throw new DirectoryOperationException(response, OperationErrorMappings.MapResultCode(resulterror));
                            }
                            else
                                // should not occur
                                throw new DirectoryOperationException(response);
                        }

                        return response;
                    }
                    else
                    {
                        // fall over, throw the exception beow
                        error = resulterror;
                    }
                }
                finally
                {
                    if (requestName != (IntPtr)0)
                        Wldap32.ldap_memfree(requestName);

                    if (requestValue != (IntPtr)0)
                        Wldap32.ldap_memfree(requestValue);

                    if (ldapResult != (IntPtr)0)
                    {
                        Wldap32.ldap_msgfree(ldapResult);
                    }
                }
            }
            else
            {
                // ldap_result failed
                if (error == 0)
                {
                    if (exceptionOnTimeOut)
                    {
                        // client side timeout                        
                        error = (int)LdapError.TimeOut;
                    }
                    else
                    {
                        // if we don't throw exception on time out (notification search for example), we just return empty resposne
                        return null;
                    }
                }
                else
                {
                    error = Wldap32.LdapGetLastError();
                }

                // abandon the request
                if (needAbandon)
                    Wldap32.ldap_abandon(ldapHandle, messageId);
            }

            // throw proper exception here            
            throw ConstructException(error, operation);
        }

        internal unsafe int ConstructParsedResult(IntPtr ldapResult, ref int serverError, ref string responseDn, ref string responseMessage, ref Uri[] responseReferral, ref DirectoryControl[] responseControl)
        {
            IntPtr dn = (IntPtr)0;
            IntPtr message = (IntPtr)0;
            IntPtr referral = (IntPtr)0;
            IntPtr control = (IntPtr)0;
            int resulterror = 0;

            try
            {
                resulterror = Wldap32.ldap_parse_result(ldapHandle, ldapResult, ref serverError, ref dn, ref message, ref referral, ref control, 0 /* not free it */);

                if (resulterror == 0)
                {
                    // parsing dn
                    responseDn = Marshal.PtrToStringUni(dn);

                    // parsing message
                    responseMessage = Marshal.PtrToStringUni(message);

                    // parsing referral                    
                    if (referral != (IntPtr)0)
                    {
                        char** tempPtr = (char**)referral;
                        char* singleReferral = tempPtr[0];
                        int i = 0;
                        ArrayList referralList = new ArrayList();
                        while (singleReferral != null)
                        {
                            string s = Marshal.PtrToStringUni((IntPtr)singleReferral);
                            referralList.Add(s);

                            i++;
                            singleReferral = tempPtr[i];
                        }

                        if (referralList.Count > 0)
                        {
                            responseReferral = new Uri[referralList.Count];
                            for (int j = 0; j < referralList.Count; j++)
                                responseReferral[j] = new Uri((string)referralList[j]);
                        }
                    }

                    // parsing control                                                
                    if (control != (IntPtr)0)
                    {
                        int i = 0;
                        IntPtr tempControlPtr = control;
                        IntPtr singleControl = Marshal.ReadIntPtr(tempControlPtr, 0);
                        ArrayList controlList = new ArrayList();
                        while (singleControl != (IntPtr)0)
                        {
                            DirectoryControl directoryControl = ConstructControl(singleControl);
                            controlList.Add(directoryControl);

                            i++;
                            singleControl = Marshal.ReadIntPtr(tempControlPtr, i * Marshal.SizeOf(typeof(IntPtr)));
                        }

                        responseControl = new DirectoryControl[controlList.Count];
                        controlList.CopyTo(responseControl);
                    }
                }
                else
                {
                    // we need to take care of one special case, when can't connect to the server, ldap_parse_result fails with local error 
                    if (resulterror == (int)LdapError.LocalError)
                    {
                        int tmpResult = Wldap32.ldap_result2error(ldapHandle, ldapResult, 0 /* not free it */);
                        if (tmpResult != 0)
                        {
                            resulterror = tmpResult;
                        }
                    }
                }
            }
            finally
            {
                if (dn != (IntPtr)0)
                {
                    Wldap32.ldap_memfree(dn);
                }

                if (message != (IntPtr)0)
                    Wldap32.ldap_memfree(message);

                if (referral != (IntPtr)0)
                    Wldap32.ldap_value_free(referral);

                if (control != (IntPtr)0)
                    Wldap32.ldap_controls_free(control);
            }

            return resulterror;
        }

        internal SearchResultEntry ConstructEntry(IntPtr entryMessage)
        {
            IntPtr dn = (IntPtr)0;
            string entryDn = null;
            IntPtr attribute = (IntPtr)0;
            IntPtr address = (IntPtr)0;
            SearchResultAttributeCollection attributes = null;

            try
            {
                // get the dn
                dn = Wldap32.ldap_get_dn(ldapHandle, entryMessage);
                if (dn != (IntPtr)0)
                {
                    entryDn = Marshal.PtrToStringUni(dn);
                    Wldap32.ldap_memfree(dn);
                    dn = (IntPtr)0;
                }

                SearchResultEntry resultEntry = new SearchResultEntry(entryDn);
                attributes = resultEntry.Attributes;

                // get attributes                
                attribute = Wldap32.ldap_first_attribute(ldapHandle, entryMessage, ref address);

                int tempcount = 0;
                while (attribute != (IntPtr)0)
                {
                    DirectoryAttribute attr = ConstructAttribute(entryMessage, attribute);
                    attributes.Add(attr.Name, attr);

                    Wldap32.ldap_memfree(attribute);
                    tempcount++;
                    attribute = Wldap32.ldap_next_attribute(ldapHandle, entryMessage, address);
                }

                if (address != (IntPtr)0)
                {
                    Wldap32.ber_free(address, 0);
                    address = (IntPtr)0;
                }

                return resultEntry;
            }
            finally
            {
                if (dn != (IntPtr)0)
                {
                    Wldap32.ldap_memfree(dn);
                }

                if (attribute != (IntPtr)0)
                    Wldap32.ldap_memfree(attribute);

                if (address != (IntPtr)0)
                {
                    Wldap32.ber_free(address, 0);
                }
            }
        }

        internal DirectoryAttribute ConstructAttribute(IntPtr entryMessage, IntPtr attributeName)
        {
            DirectoryAttribute attribute = new DirectoryAttribute();
            attribute.isSearchResult = true;

            // get name
            string name = Marshal.PtrToStringUni(attributeName);
            attribute.Name = name;

            // get values
            IntPtr valuesArray = Wldap32.ldap_get_values_len(ldapHandle, entryMessage, name);
            try
            {
                IntPtr tempPtr = (IntPtr)0;
                int count = 0;
                if (valuesArray != (IntPtr)0)
                {
                    tempPtr = Marshal.ReadIntPtr(valuesArray, Marshal.SizeOf(typeof(IntPtr)) * count);
                    while (tempPtr != (IntPtr)0)
                    {
                        berval bervalue = new berval();
                        Marshal.PtrToStructure(tempPtr, bervalue);
                        byte[] byteArray = null;
                        if (bervalue.bv_len > 0 && bervalue.bv_val != (IntPtr)0)
                        {
                            byteArray = new byte[bervalue.bv_len];
                            Marshal.Copy(bervalue.bv_val, byteArray, 0, bervalue.bv_len);
                            attribute.Add(byteArray);
                        }

                        count++;
                        tempPtr = Marshal.ReadIntPtr(valuesArray, Marshal.SizeOf(typeof(IntPtr)) * count);
                    }
                }
            }
            finally
            {
                if (valuesArray != (IntPtr)0)
                    Wldap32.ldap_value_free_len(valuesArray);
            }

            return attribute;
        }

        internal SearchResultReference ConstructReference(IntPtr referenceMessage)
        {
            SearchResultReference reference = null;
            ArrayList referralList = new ArrayList();
            Uri[] uris = null;
            IntPtr referenceArray = (IntPtr)0;

            int error = Wldap32.ldap_parse_reference(ldapHandle, referenceMessage, ref referenceArray);

            try
            {
                if (error == 0)
                {
                    IntPtr tempPtr = (IntPtr)0;
                    int count = 0;
                    if (referenceArray != (IntPtr)0)
                    {
                        tempPtr = Marshal.ReadIntPtr(referenceArray, Marshal.SizeOf(typeof(IntPtr)) * count);
                        while (tempPtr != (IntPtr)0)
                        {
                            string s = Marshal.PtrToStringUni(tempPtr);
                            referralList.Add(s);

                            count++;
                            tempPtr = Marshal.ReadIntPtr(referenceArray, Marshal.SizeOf(typeof(IntPtr)) * count);
                        }

                        Wldap32.ldap_value_free(referenceArray);
                        referenceArray = (IntPtr)0;
                    }

                    if (referralList.Count > 0)
                    {
                        uris = new Uri[referralList.Count];
                        for (int i = 0; i < referralList.Count; i++)
                        {
                            uris[i] = new Uri((string)referralList[i]);
                        }

                        reference = new SearchResultReference(uris);
                    }
                }
            }
            finally
            {
                if (referenceArray != (IntPtr)0)
                    Wldap32.ldap_value_free(referenceArray);
            }

            return reference;
        }

        private DirectoryException ConstructException(int error, LdapOperation operation)
        {
            DirectoryResponse response = null;

            if (Utility.IsResultCode((ResultCode)error))
            {
                if (operation == LdapOperation.LdapAdd)
                    response = new AddResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                else if (operation == LdapOperation.LdapModify)
                    response = new ModifyResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                else if (operation == LdapOperation.LdapDelete)
                    response = new DeleteResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                else if (operation == LdapOperation.LdapModifyDn)
                    response = new ModifyDNResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                else if (operation == LdapOperation.LdapCompare)
                    response = new CompareResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                else if (operation == LdapOperation.LdapSearch)
                    response = new SearchResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                else if (operation == LdapOperation.LdapExtendedRequest)
                    response = new ExtendedResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);

                string errorMessage = OperationErrorMappings.MapResultCode(error);
                return new DirectoryOperationException(response, errorMessage);
            }
            else
            {
                if (Utility.IsLdapError((LdapError)error))
                {
                    string errorMessage = LdapErrorMappings.MapResultCode(error);
                    string serverErrorMessage = _options.ServerErrorMessage;
                    if ((serverErrorMessage != null) && (serverErrorMessage.Length > 0))
                    {
                        throw new LdapException(error, errorMessage, serverErrorMessage);
                    }
                    else
                    {
                        return new LdapException(error, errorMessage);
                    }
                }
                else
                    return new LdapException(error);
            }
        }

        private DirectoryControl ConstructControl(IntPtr controlPtr)
        {
            LdapControl control = new LdapControl();
            Marshal.PtrToStructure(controlPtr, control);

            Debug.Assert(control.ldctl_oid != (IntPtr)0);
            string controlType = Marshal.PtrToStringUni(control.ldctl_oid);

            byte[] bytes = new byte[control.ldctl_value.bv_len];
            Marshal.Copy(control.ldctl_value.bv_val, bytes, 0, control.ldctl_value.bv_len);

            bool criticality = control.ldctl_iscritical;

            return new DirectoryControl(controlType, bytes, criticality, true);
        }

        private bool SameCredential(NetworkCredential oldCredential, NetworkCredential newCredential)
        {
            if (oldCredential == null && newCredential == null)
                return true;
            else if (oldCredential == null && newCredential != null)
                return false;
            else if (oldCredential != null && newCredential == null)
                return false;
            else
            {
                if (oldCredential.Domain == newCredential.Domain &&
                    oldCredential.UserName == newCredential.UserName &&
                    oldCredential.Password == newCredential.Password)
                    return true;
                else
                    return false;
            }
        }
    }
}
