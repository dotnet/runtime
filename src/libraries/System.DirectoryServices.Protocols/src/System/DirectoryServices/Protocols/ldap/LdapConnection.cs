// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.DirectoryServices.Protocols
{
    internal delegate DirectoryResponse GetLdapResponseCallback(int messageId, LdapOperation operation, ResultAll resultType, TimeSpan requestTimeout, bool exceptionOnTimeOut);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate Interop.BOOL QUERYCLIENTCERT(IntPtr Connection, IntPtr trusted_CAs, IntPtr* certificateHandle);

    public partial class LdapConnection : DirectoryConnection, IDisposable
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

        internal static readonly object s_objectLock = new object();
        internal static readonly Hashtable s_handleTable = new Hashtable();
        private static readonly Hashtable s_asyncResultTable = Hashtable.Synchronized(new Hashtable());
        private static readonly ManualResetEvent s_waitHandle = new ManualResetEvent(false);
        private static readonly LdapPartialResultsProcessor s_partialResultsProcessor = new LdapPartialResultsProcessor(s_waitHandle);

        private AuthType _connectionAuthType = AuthType.Negotiate;
        internal bool _needDispose = true;
        internal ConnectionHandle _ldapHandle;
        internal bool _disposed;
        private bool _bounded;
        private bool _needRebind;
        private bool _connected;
        internal QUERYCLIENTCERT _clientCertificateRoutine;

        public LdapConnection(string server) : this(new LdapDirectoryIdentifier(server))
        {
        }

        public LdapConnection(LdapDirectoryIdentifier identifier) : this(identifier, null, AuthType.Negotiate)
        {
        }

        public LdapConnection(LdapDirectoryIdentifier identifier, NetworkCredential credential) : this(identifier, credential, AuthType.Negotiate)
        {
        }

        public unsafe LdapConnection(LdapDirectoryIdentifier identifier, NetworkCredential credential, AuthType authType)
        {
            _directoryIdentifier = identifier;
            _directoryCredential = (credential != null) ? new NetworkCredential(credential.UserName, credential.Password, credential.Domain) : null;

            _connectionAuthType = authType;

            if (authType < AuthType.Anonymous || authType > AuthType.Kerberos)
            {
                throw new InvalidEnumArgumentException(nameof(authType), (int)authType, typeof(AuthType));
            }

            // Throw if user wants to do anonymous bind but specifies credentials.
            if (AuthType == AuthType.Anonymous && (_directoryCredential != null && (!string.IsNullOrEmpty(_directoryCredential.Password) || !string.IsNullOrEmpty(_directoryCredential.UserName))))
            {
                throw new ArgumentException(SR.InvalidAuthCredential);
            }

            Init();
            SessionOptions = new LdapSessionOptions(this);
            _clientCertificateRoutine = new QUERYCLIENTCERT(ProcessClientCertificate);
        }

        internal unsafe LdapConnection(LdapDirectoryIdentifier identifier, NetworkCredential credential, AuthType authType, IntPtr handle)
        {
            _directoryIdentifier = identifier;
            _needDispose = false;
            _ldapHandle = new ConnectionHandle(handle, _needDispose);
            _directoryCredential = credential;
            _connectionAuthType = authType;
            SessionOptions = new LdapSessionOptions(this);
            _clientCertificateRoutine = new QUERYCLIENTCERT(ProcessClientCertificate);
        }

        ~LdapConnection() => Dispose(false);

        public override TimeSpan Timeout
        {
            get => _connectionTimeOut;
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

                _connectionTimeOut = value;
            }
        }

        public AuthType AuthType
        {
            get => _connectionAuthType;
            set
            {
                if (value < AuthType.Anonymous || value > AuthType.Kerberos)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(AuthType));
                }

                // If the change is made after we have bound to the server and value is really
                // changed, set the flag to indicate the need to do rebind.
                if (_bounded && (value != _connectionAuthType))
                {
                    _needRebind = true;
                }

                _connectionAuthType = value;
            }
        }

        public LdapSessionOptions SessionOptions { get; }

        public override NetworkCredential Credential
        {
            set
            {
                if (_bounded && !SameCredential(_directoryCredential, value))
                {
                    _needRebind = true;
                }

                _directoryCredential = (value != null) ? new NetworkCredential(value.UserName, value.Password, value.Domain) : null;
            }
        }

        public bool AutoBind { get; set; } = true;

        internal bool NeedDispose
        {
            get => _needDispose;
            set
            {
                if (_ldapHandle != null)
                {
                    _ldapHandle._needDispose = value;
                }

                _needDispose = value;
            }
        }

        internal void Init()
        {
            string hostname = null;
            string[] servers = ((LdapDirectoryIdentifier)_directoryIdentifier)?.Servers;
            if (servers != null && servers.Length != 0)
            {
                var temp = new StringBuilder(200);
                for (int i = 0; i < servers.Length; i++)
                {
                    if (servers[i] != null)
                    {
                        temp.Append(servers[i]);
                        if (i < servers.Length - 1)
                        {
                            temp.Append(' ');
                        }
                    }
                }

                if (temp.Length != 0)
                {
                    hostname = temp.ToString();
                }
            }

            InternalInitConnectionHandle(hostname);

            // Create a WeakReference object with the target of ldapHandle and put it into our handle table.
            lock (s_objectLock)
            {
                if (s_handleTable[_ldapHandle.DangerousGetHandle()] != null)
                {
                    s_handleTable.Remove(_ldapHandle.DangerousGetHandle());
                }

                s_handleTable.Add(_ldapHandle.DangerousGetHandle(), new WeakReference(this));
            }
        }

        public override DirectoryResponse SendRequest(DirectoryRequest request)
        {
            // No request specific timeout is specified, use the connection timeout.
            return SendRequest(request, _connectionTimeOut);
        }

        public DirectoryResponse SendRequest(DirectoryRequest request, TimeSpan requestTimeout)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request is DsmlAuthRequest)
            {
                throw new NotSupportedException(SR.DsmlAuthRequestNotSupported);
            }

            int messageID = 0;
            int error = SendRequestHelper(request, ref messageID);

            LdapOperation operation = LdapOperation.LdapSearch;
            if (request is DeleteRequest)
            {
                operation = LdapOperation.LdapDelete;
            }
            else if (request is AddRequest)
            {
                operation = LdapOperation.LdapAdd;
            }
            else if (request is ModifyRequest)
            {
                operation = LdapOperation.LdapModify;
            }
            else if (request is SearchRequest)
            {
                operation = LdapOperation.LdapSearch;
            }
            else if (request is ModifyDNRequest)
            {
                operation = LdapOperation.LdapModifyDn;
            }
            else if (request is CompareRequest)
            {
                operation = LdapOperation.LdapCompare;
            }
            else if (request is ExtendedRequest)
            {
                operation = LdapOperation.LdapExtendedRequest;
            }

            if (error == 0 && messageID != -1)
            {
                ValueTask<DirectoryResponse> vt = ConstructResponseAsync(messageID, operation, ResultAll.LDAP_MSG_ALL, requestTimeout, true, sync: true);
                Debug.Assert(vt.IsCompleted);
                return vt.GetAwaiter().GetResult();
            }
            else
            {
                if (error == 0)
                {
                    // Success code but message is -1, unexpected.
                    error = LdapPal.GetLastErrorFromConnection(_ldapHandle);
                }

                throw ConstructException(error, operation);
            }
        }

        public IAsyncResult BeginSendRequest(DirectoryRequest request, PartialResultProcessing partialMode, AsyncCallback callback, object state)
        {
            return BeginSendRequest(request, _connectionTimeOut, partialMode, callback, state);
        }

        public IAsyncResult BeginSendRequest(DirectoryRequest request, TimeSpan requestTimeout, PartialResultProcessing partialMode, AsyncCallback callback, object state)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (partialMode < PartialResultProcessing.NoPartialResultSupport || partialMode > PartialResultProcessing.ReturnPartialResultsAndNotifyCallback)
            {
                throw new InvalidEnumArgumentException(nameof(partialMode), (int)partialMode, typeof(PartialResultProcessing));
            }

            if (partialMode != PartialResultProcessing.NoPartialResultSupport && !(request is SearchRequest))
            {
                throw new NotSupportedException(SR.PartialResultsNotSupported);
            }

            if (partialMode == PartialResultProcessing.ReturnPartialResultsAndNotifyCallback && callback == null)
            {
                throw new ArgumentException(SR.CallBackIsNull, nameof(callback));
            }

            int messageID = 0;
            int error = SendRequestHelper(request, ref messageID);

            LdapOperation operation = LdapOperation.LdapSearch;
            if (request is DeleteRequest)
            {
                operation = LdapOperation.LdapDelete;
            }
            else if (request is AddRequest)
            {
                operation = LdapOperation.LdapAdd;
            }
            else if (request is ModifyRequest)
            {
                operation = LdapOperation.LdapModify;
            }
            else if (request is SearchRequest)
            {
                operation = LdapOperation.LdapSearch;
            }
            else if (request is ModifyDNRequest)
            {
                operation = LdapOperation.LdapModifyDn;
            }
            else if (request is CompareRequest)
            {
                operation = LdapOperation.LdapCompare;
            }
            else if (request is ExtendedRequest)
            {
                operation = LdapOperation.LdapExtendedRequest;
            }

            if (error == 0 && messageID != -1)
            {
                if (partialMode == PartialResultProcessing.NoPartialResultSupport)
                {
                    var requestState = new LdapRequestState();
                    var asyncResult = new LdapAsyncResult(callback, state, false);

                    requestState._ldapAsync = asyncResult;
                    asyncResult._resultObject = requestState;

                    s_asyncResultTable.Add(asyncResult, messageID);

                    _ = ResponseCallback(ConstructResponseAsync(messageID, operation, ResultAll.LDAP_MSG_ALL, requestTimeout, true, sync: false), requestState);

                    static async Task ResponseCallback(ValueTask<DirectoryResponse> vt, LdapRequestState requestState)
                    {
                        try
                        {
                            DirectoryResponse response = await vt.ConfigureAwait(false);
                            requestState._response = response;
                        }
                        catch (Exception e)
                        {
                            requestState._exception = e;
                            requestState._response = null;
                        }

                        // Signal waitable object, indicate operation completed and fire callback.
                        requestState._ldapAsync._manualResetEvent.Set();
                        requestState._ldapAsync._completed = true;

                        if (requestState._ldapAsync._callback != null && !requestState._abortCalled)
                        {
                            requestState._ldapAsync._callback(requestState._ldapAsync);
                        }
                    }

                    return asyncResult;
                }
                else
                {
                    // the user registers to retrieve partial results
                    bool partialCallback = partialMode == PartialResultProcessing.ReturnPartialResultsAndNotifyCallback;

                    var asyncResult = new LdapPartialAsyncResult(messageID, callback, state, true, this, partialCallback, requestTimeout);
                    s_partialResultsProcessor.Add(asyncResult);

                    return asyncResult;
                }
            }

            if (error == 0)
            {
                // Success code but message is -1, unexpected.
                error = LdapPal.GetLastErrorFromConnection(_ldapHandle);
            }

            throw ConstructException(error, operation);
        }

        public void Abort(IAsyncResult asyncResult)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            if (!(asyncResult is LdapAsyncResult))
            {
                throw new ArgumentException(SR.Format(SR.NotReturnedAsyncResult, nameof(asyncResult)));
            }

            int messageId;

            LdapAsyncResult result = (LdapAsyncResult)asyncResult;
            if (!result._partialResults)
            {
                if (!s_asyncResultTable.Contains(asyncResult))
                {
                    throw new ArgumentException(SR.InvalidAsyncResult);
                }

                messageId = (int)(s_asyncResultTable[asyncResult]);

                // remove the asyncResult from our connection table
                s_asyncResultTable.Remove(asyncResult);
            }
            else
            {
                s_partialResultsProcessor.Remove((LdapPartialAsyncResult)asyncResult);
                messageId = ((LdapPartialAsyncResult)asyncResult)._messageID;
            }

            // Cancel the request.
            LdapPal.CancelDirectoryAsyncOperation(_ldapHandle, messageId);

            LdapRequestState resultObject = result._resultObject;
            if (resultObject != null)
            {
                resultObject._abortCalled = true;
            }
        }

        public PartialResultsCollection GetPartialResults(IAsyncResult asyncResult)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            if (!(asyncResult is LdapAsyncResult))
            {
                throw new ArgumentException(SR.Format(SR.NotReturnedAsyncResult, nameof(asyncResult)));
            }

            if (!(asyncResult is LdapPartialAsyncResult))
            {
                throw new InvalidOperationException(SR.NoPartialResults);
            }

            return s_partialResultsProcessor.GetPartialResults((LdapPartialAsyncResult)asyncResult);
        }

        public DirectoryResponse EndSendRequest(IAsyncResult asyncResult)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            if (!(asyncResult is LdapAsyncResult))
            {
                throw new ArgumentException(SR.Format(SR.NotReturnedAsyncResult, nameof(asyncResult)));
            }

            LdapAsyncResult result = (LdapAsyncResult)asyncResult;

            if (!result._partialResults)
            {
                // Not a partial results.
                if (!s_asyncResultTable.Contains(asyncResult))
                {
                    throw new ArgumentException(SR.InvalidAsyncResult);
                }

                // Remove the asyncResult from our connection table.
                s_asyncResultTable.Remove(asyncResult);

                asyncResult.AsyncWaitHandle.WaitOne();

                if (result._resultObject._exception != null)
                {
                    throw result._resultObject._exception;
                }

                return result._resultObject._response;
            }

            // Deal with partial results.
            s_partialResultsProcessor.NeedCompleteResult((LdapPartialAsyncResult)asyncResult);
            asyncResult.AsyncWaitHandle.WaitOne();

            return s_partialResultsProcessor.GetCompleteResult((LdapPartialAsyncResult)asyncResult);
        }

        private int SendRequestHelper(DirectoryRequest request, ref int messageID)
        {
            IntPtr serverControlArray = IntPtr.Zero;
            LdapControl[] managedServerControls = null;
            IntPtr clientControlArray = IntPtr.Zero;
            LdapControl[] managedClientControls = null;

            var ptrToFree = new ArrayList();
            LdapMod[] modifications = null;
            IntPtr modArray = IntPtr.Zero;
            int addModCount = 0;

            BerVal berValuePtr = null;

            IntPtr searchAttributes = IntPtr.Zero;
            int attributeCount = 0;

            int error = 0;

            // Connect to the server first if have not done so.
            if (!_connected)
            {
                Connect();
                _connected = true;
            }

            // Bind if user has not turned off automatic bind, have not done so or there is a need
            // to do rebind, also connectionless LDAP does not need to do bind.
            if (AutoBind && (!_bounded || _needRebind) && ((LdapDirectoryIdentifier)Directory).Connectionless != true)
            {
                Debug.WriteLine("rebind occurs\n");
                Bind();
            }

            try
            {
                IntPtr tempPtr = IntPtr.Zero;

                // Build server control.
                managedServerControls = BuildControlArray(request.Controls, true);
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

                // build client control
                managedClientControls = BuildControlArray(request.Controls, false);
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

                if (request is DeleteRequest)
                {
                    // It is an delete operation.
                    error = LdapPal.DeleteDirectoryEntry(_ldapHandle, ((DeleteRequest)request).DistinguishedName, serverControlArray, clientControlArray, ref messageID);
                }
                else if (request is ModifyDNRequest)
                {
                    // It is a modify dn operation
                    error = LdapPal.RenameDirectoryEntry(
                        _ldapHandle,
                        ((ModifyDNRequest)request).DistinguishedName,
                        ((ModifyDNRequest)request).NewName,
                        ((ModifyDNRequest)request).NewParentDistinguishedName,
                        ((ModifyDNRequest)request).DeleteOldRdn ? 1 : 0,
                        serverControlArray, clientControlArray, ref messageID);
                }
                else if (request is CompareRequest compareRequest)
                {
                    // It is a compare request.
                    DirectoryAttribute assertion = compareRequest.Assertion;
                    if (assertion == null)
                    {
                        throw new ArgumentException(SR.WrongAssertionCompare);
                    }

                    if (assertion.Count != 1)
                    {
                        throw new ArgumentException(SR.WrongNumValuesCompare);
                    }

                    // Process the attribute.
                    string stringValue = null;
                    if (assertion[0] is byte[] byteArray)
                    {
                        if (byteArray != null && byteArray.Length != 0)
                        {
                            berValuePtr = new BerVal
                            {
                                bv_len = byteArray.Length,
                                bv_val = Marshal.AllocHGlobal(byteArray.Length)
                            };
                            Marshal.Copy(byteArray, 0, berValuePtr.bv_val, byteArray.Length);
                        }
                    }
                    else
                    {
                        stringValue = assertion[0].ToString();
                    }

                    // It is a compare request.
                    error = LdapPal.CompareDirectoryEntries(
                        _ldapHandle,
                        ((CompareRequest)request).DistinguishedName,
                        assertion.Name,
                        stringValue,
                        berValuePtr,
                        serverControlArray, clientControlArray, ref messageID);
                }
                else if (request is AddRequest || request is ModifyRequest)
                {
                    // Build the attributes.
                    if (request is AddRequest)
                    {
                        modifications = BuildAttributes(((AddRequest)request).Attributes, ptrToFree);
                    }
                    else
                    {
                        modifications = BuildAttributes(((ModifyRequest)request).Modifications, ptrToFree);
                    }

                    addModCount = (modifications == null ? 1 : modifications.Length + 1);
                    modArray = Utility.AllocHGlobalIntPtrArray(addModCount);
                    int modStructSize = Marshal.SizeOf(typeof(LdapMod));
                    int i = 0;
                    for (i = 0; i < addModCount - 1; i++)
                    {
                        IntPtr controlPtr = Marshal.AllocHGlobal(modStructSize);
                        Marshal.StructureToPtr(modifications[i], controlPtr, false);
                        tempPtr = (IntPtr)((long)modArray + IntPtr.Size * i);
                        Marshal.WriteIntPtr(tempPtr, controlPtr);
                    }
                    tempPtr = (IntPtr)((long)modArray + IntPtr.Size * i);
                    Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);

                    if (request is AddRequest)
                    {
                        error = LdapPal.AddDirectoryEntry(
                            _ldapHandle,
                            ((AddRequest)request).DistinguishedName,
                            modArray,
                            serverControlArray, clientControlArray, ref messageID);
                    }
                    else
                    {
                        error = LdapPal.ModifyDirectoryEntry(
                            _ldapHandle,
                            ((ModifyRequest)request).DistinguishedName,
                            modArray,
                            serverControlArray, clientControlArray, ref messageID);
                    }
                }
                else if (request is ExtendedRequest extendedRequest)
                {
                    string name = extendedRequest.RequestName;
                    byte[] val = extendedRequest.RequestValue;

                    // process the requestvalue
                    if (val != null && val.Length != 0)
                    {
                        berValuePtr = new BerVal()
                        {
                            bv_len = val.Length,
                            bv_val = Marshal.AllocHGlobal(val.Length)
                        };
                        Marshal.Copy(val, 0, berValuePtr.bv_val, val.Length);
                    }

                    error = LdapPal.ExtendedDirectoryOperation(
                        _ldapHandle,
                        name,
                        berValuePtr,
                        serverControlArray, clientControlArray, ref messageID);
                }
                else if (request is SearchRequest searchRequest)
                {
                    // Process the filter.
                    object filter = searchRequest.Filter;
                    if (filter != null)
                    {
                        // LdapConnection only supports ldap filter.
                        if (filter is XmlDocument)
                        {
                            throw new ArgumentException(SR.InvalidLdapSearchRequestFilter);
                        }
                    }

                    string searchRequestFilter = (string)filter;

                    // Process the attributes.
                    attributeCount = (searchRequest.Attributes == null ? 0 : searchRequest.Attributes.Count);
                    if (attributeCount != 0)
                    {
                        searchAttributes = Utility.AllocHGlobalIntPtrArray(attributeCount + 1);
                        int i = 0;
                        for (i = 0; i < attributeCount; i++)
                        {
                            IntPtr controlPtr = LdapPal.StringToPtr(searchRequest.Attributes[i]);
                            tempPtr = (IntPtr)((long)searchAttributes + IntPtr.Size * i);
                            Marshal.WriteIntPtr(tempPtr, controlPtr);
                        }

                        tempPtr = (IntPtr)((long)searchAttributes + IntPtr.Size * i);
                        Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);
                    }

                    // Process the scope.
                    int searchScope = (int)searchRequest.Scope;

                    // Process the timelimit.
                    int searchTimeLimit = (int)(searchRequest.TimeLimit.Ticks / TimeSpan.TicksPerSecond);

                    // Process the alias.
                    DereferenceAlias searchAliases = SessionOptions.DerefAlias;
                    SessionOptions.DerefAlias = searchRequest.Aliases;

                    try
                    {
                        error = LdapPal.SearchDirectory(
                            _ldapHandle,
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
                        // Revert back.
                        SessionOptions.DerefAlias = searchAliases;
                    }
                }
                else
                {
                    throw new NotSupportedException(SR.InvliadRequestType);
                }

                // The asynchronous call itself timeout, this actually means that we time out the
                // LDAP_OPT_SEND_TIMEOUT specified in the session option wldap32 does not differentiate
                // that, but the application caller actually needs this information to determin what to
                // do with the error code
                if (error == (int)LdapError.TimeOut)
                {
                    error = (int)LdapError.SendTimeOut;
                }

                return error;
            }
            finally
            {
                GC.KeepAlive(modifications);

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
                    // Release the memory from the heap.
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
                            {
                                Marshal.FreeHGlobal(managedClientControls[i].ldctl_value.bv_val);
                            }
                        }
                    }
                }

                if (modArray != IntPtr.Zero)
                {
                    // release the memory from the heap
                    for (int i = 0; i < addModCount - 1; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(modArray, IntPtr.Size * i);
                        if (tempPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(tempPtr);
                        }
                    }

                    Marshal.FreeHGlobal(modArray);
                }

                // Free the pointers.
                for (int x = 0; x < ptrToFree.Count; x++)
                {
                    IntPtr tempPtr = (IntPtr)ptrToFree[x];
                    Marshal.FreeHGlobal(tempPtr);
                }

                if (berValuePtr != null && berValuePtr.bv_val != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(berValuePtr.bv_val);
                }

                if (searchAttributes != IntPtr.Zero)
                {
                    for (int i = 0; i < attributeCount; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(searchAttributes, IntPtr.Size * i);
                        if (tempPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(tempPtr);
                        }
                    }

                    Marshal.FreeHGlobal(searchAttributes);
                }
            }
        }

        private unsafe Interop.BOOL ProcessClientCertificate(IntPtr ldapHandle, IntPtr CAs, IntPtr* certificate)
        {
            int count = ClientCertificates == null ? 0 : ClientCertificates.Count;
            if (count == 0 && SessionOptions._clientCertificateDelegate == null)
            {
                return Interop.BOOL.FALSE;
            }

            // If the user specify certificate through property and not though option, we don't need to check the certificate authority.
            if (SessionOptions._clientCertificateDelegate == null)
            {
                *certificate = ClientCertificates[0].Handle;
                return Interop.BOOL.TRUE;
            }

            // Processing the certificate authority.
            var list = new ArrayList();
            if (CAs != IntPtr.Zero)
            {
                SecPkgContext_IssuerListInfoEx trustedCAs = (SecPkgContext_IssuerListInfoEx)Marshal.PtrToStructure(CAs, typeof(SecPkgContext_IssuerListInfoEx));
                int issuerNumber = trustedCAs.cIssuers;
                for (int i = 0; i < issuerNumber; i++)
                {
                    IntPtr tempPtr = (IntPtr)((long)trustedCAs.aIssuers + Marshal.SizeOf(typeof(CRYPTOAPI_BLOB)) * i);
                    CRYPTOAPI_BLOB info = (CRYPTOAPI_BLOB)Marshal.PtrToStructure(tempPtr, typeof(CRYPTOAPI_BLOB));
                    int dataLength = info.cbData;

                    byte[] context = new byte[dataLength];
                    Marshal.Copy(info.pbData, context, 0, dataLength);
                    list.Add(context);
                }
            }

            byte[][] certAuthorities = null;
            if (list.Count != 0)
            {
                certAuthorities = new byte[list.Count][];
                for (int i = 0; i < list.Count; i++)
                {
                    certAuthorities[i] = (byte[])list[i];
                }
            }

            X509Certificate cert = SessionOptions._clientCertificateDelegate(this, certAuthorities);
            if (cert != null)
            {
                *certificate = cert.Handle;
                return Interop.BOOL.TRUE;
            }

            *certificate = IntPtr.Zero;
            return Interop.BOOL.FALSE;
        }

        private void Connect()
        {
            //Ccurrently ldap does not accept more than one certificate.
            if (ClientCertificates.Count > 1)
            {
                throw new InvalidOperationException(SR.InvalidClientCertificates);
            }

            // Set the certificate callback routine here if user adds the certifcate to the certificate collection.
            if (ClientCertificates.Count != 0)
            {
                int certError = LdapPal.SetClientCertOption(_ldapHandle, LdapOption.LDAP_OPT_CLIENT_CERTIFICATE, _clientCertificateRoutine);
                if (certError != (int)ResultCode.Success)
                {
                    if (LdapErrorMappings.IsLdapError(certError))
                    {
                        string certerrorMessage = LdapErrorMappings.MapResultCode(certError);
                        throw new LdapException(certError, certerrorMessage);
                    }

                    throw new LdapException(certError);
                }

                // When certificate is specified, automatic bind is disabled.
                AutoBind = false;
            }

            // Set the LDAP_OPT_AREC_EXCLUSIVE flag if necessary.
            if (((LdapDirectoryIdentifier)Directory).FullyQualifiedDnsHostName && !_setFQDNDone)
            {
                SessionOptions.SetFqdnRequired();
                _setFQDNDone = true;
            }

            int error = InternalConnectToServer();

            // Failed, throw an exception.
            if (error != (int)ResultCode.Success)
            {
                if (LdapErrorMappings.IsLdapError(error))
                {
                    string errorMessage = LdapErrorMappings.MapResultCode(error);
                    throw new LdapException(error, errorMessage);
                }

                throw new LdapException(error);
            }
        }

        public void Bind() => BindHelper(_directoryCredential, needSetCredential: false);

        public void Bind(NetworkCredential newCredential) => BindHelper(newCredential, needSetCredential: true);

        private void BindHelper(NetworkCredential newCredential, bool needSetCredential)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            // Throw if user wants to do anonymous bind but specifies credentials.
            if (AuthType == AuthType.Anonymous && (newCredential != null && (!string.IsNullOrEmpty(newCredential.Password) || string.IsNullOrEmpty(newCredential.UserName))))
            {
                throw new InvalidOperationException(SR.InvalidAuthCredential);
            }

            // Set the credential.
            NetworkCredential tempCredential;
            if (needSetCredential)
            {
                _directoryCredential = tempCredential = (newCredential != null ? new NetworkCredential(newCredential.UserName, newCredential.Password, newCredential.Domain) : null);
            }
            else
            {
                tempCredential = _directoryCredential;
            }

            // Connect to the server first.
            if (!_connected)
            {
                Connect();
                _connected = true;
            }

            // Bind to the server.
            string username;
            string domainName;
            string password;
            if (tempCredential != null && tempCredential.UserName.Length == 0 && tempCredential.Password.Length == 0 && tempCredential.Domain.Length == 0)
            {
                // Default credentials.
                username = null;
                domainName = null;
                password = null;
            }
            else
            {
                username = tempCredential?.UserName;
                domainName = tempCredential?.Domain;
                password = tempCredential?.Password;
            }

            int error;
            if (AuthType == AuthType.Anonymous)
            {
                error = LdapPal.BindToDirectory(_ldapHandle, null, null);
            }
            else if (AuthType == AuthType.Basic)
            {
                var tempDomainName = new StringBuilder(100);
                if (domainName != null && domainName.Length != 0)
                {
                    tempDomainName.Append(domainName);
                    tempDomainName.Append('\\');
                }

                tempDomainName.Append(username);
                error = LdapPal.BindToDirectory(_ldapHandle, tempDomainName.ToString(), password);
            }
            else
            {
                var cred = new SEC_WINNT_AUTH_IDENTITY_EX()
                {
                    version = Interop.SEC_WINNT_AUTH_IDENTITY_VERSION,
                    length = Marshal.SizeOf(typeof(SEC_WINNT_AUTH_IDENTITY_EX)),
                    flags = Interop.SEC_WINNT_AUTH_IDENTITY_UNICODE
                };
                if (AuthType == AuthType.Kerberos)
                {
                    cred.packageList = Interop.MICROSOFT_KERBEROS_NAME_W;
                    cred.packageListLength = cred.packageList.Length;
                }

                if (tempCredential != null)
                {
                    cred.user = username;
                    cred.userLength = (username == null ? 0 : username.Length);
                    cred.domain = domainName;
                    cred.domainLength = (domainName == null ? 0 : domainName.Length);
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

                error = InternalBind(tempCredential, cred, method);
            }

            // Failed, throw exception.
            if (error != (int)ResultCode.Success)
            {
                if (Utility.IsResultCode((ResultCode)error))
                {
                    string errorMessage = OperationErrorMappings.MapResultCode(error);
                    throw new DirectoryOperationException(null, errorMessage);
                }
                else if (LdapErrorMappings.IsLdapError(error))
                {
                    string errorMessage = LdapErrorMappings.MapResultCode(error);
                    string serverErrorMessage = SessionOptions.ServerErrorMessage;
                    if (!string.IsNullOrEmpty(serverErrorMessage))
                    {
                        throw new LdapException(error, errorMessage, serverErrorMessage);
                    }

                    throw new LdapException(error, errorMessage);
                }

                throw new LdapException(error);
            }

            // We successfully bound to the server.
            _bounded = true;

            // Rebind has been done.
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
                // We need to remove the handle from the handle table.
                lock (s_objectLock)
                {
                    if (_ldapHandle != null)
                    {
                        s_handleTable.Remove(_ldapHandle.DangerousGetHandle());
                    }
                }
            }

            // Close the ldap connection.
            if (_needDispose && _ldapHandle != null && !_ldapHandle.IsInvalid)
            {
                _ldapHandle.Dispose();
            }

            _ldapHandle = null;
            _disposed = true;
        }

        internal static LdapControl[] BuildControlArray(DirectoryControlCollection controls, bool serverControl)
        {
            LdapControl[] managedControls = null;

            if (controls != null && controls.Count != 0)
            {
                var controlList = new ArrayList();
                foreach (DirectoryControl col in controls)
                {
                    if (serverControl == true)
                    {
                        if (col.ServerSide)
                        {
                            controlList.Add(col);
                        }
                    }
                    else if (!col.ServerSide)
                    {
                        controlList.Add(col);
                    }
                }

                if (controlList.Count != 0)
                {
                    int count = controlList.Count;
                    managedControls = new LdapControl[count];

                    for (int i = 0; i < count; i++)
                    {
                        managedControls[i] = new LdapControl()
                        {
                            // Get the control type.
                            ldctl_oid = LdapPal.StringToPtr(((DirectoryControl)controlList[i]).Type),

                            // Get the control cricality.
                            ldctl_iscritical = ((DirectoryControl)controlList[i]).IsCritical
                        };

                        // Get the control value.
                        DirectoryControl tempControl = (DirectoryControl)controlList[i];
                        byte[] byteControlValue = tempControl.GetValue();
                        if (byteControlValue == null || byteControlValue.Length == 0)
                        {
                            // Treat the control value as null.
                            managedControls[i].ldctl_value = new BerVal
                            {
                                bv_len = 0,
                                bv_val = IntPtr.Zero
                            };
                        }
                        else
                        {
                            managedControls[i].ldctl_value = new BerVal
                            {
                                bv_len = byteControlValue.Length,
                                bv_val = Marshal.AllocHGlobal(sizeof(byte) * byteControlValue.Length)
                            };
                            Marshal.Copy(byteControlValue, 0, managedControls[i].ldctl_value.bv_val, managedControls[i].ldctl_value.bv_len);
                        }
                    }
                }
            }

            return managedControls;
        }

        internal static LdapMod[] BuildAttributes(CollectionBase directoryAttributes, ArrayList ptrToFree)
        {
            LdapMod[] attributes = null;

            if (directoryAttributes != null && directoryAttributes.Count != 0)
            {
                var encoder = new UTF8Encoding();
                DirectoryAttributeModificationCollection modificationCollection = null;
                DirectoryAttributeCollection attributeCollection = null;

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
                    // Get the managed attribute first.
                    DirectoryAttribute modAttribute;
                    if (attributeCollection != null)
                    {
                        modAttribute = attributeCollection[i];
                    }
                    else
                    {
                        modAttribute = modificationCollection[i];
                    }

                    attributes[i] = new LdapMod();

                    // Write the operation type.
                    if (modAttribute is DirectoryAttributeModification)
                    {
                        attributes[i].type = (int)((DirectoryAttributeModification)modAttribute).Operation;
                    }
                    else
                    {
                        attributes[i].type = (int)DirectoryAttributeOperation.Add;
                    }

                    // We treat all the values as binary
                    attributes[i].type |= LDAP_MOD_BVALUES;

                    // Write the attribute name.
                    attributes[i].attribute = LdapPal.StringToPtr(modAttribute.Name);

                    // Write the values.
                    int valuesCount = 0;
                    BerVal[] berValues = null;
                    if (modAttribute.Count > 0)
                    {
                        valuesCount = modAttribute.Count;
                        berValues = new BerVal[valuesCount];
                        for (int j = 0; j < valuesCount; j++)
                        {
                            byte[] byteArray;
                            if (modAttribute[j] is string)
                            {
                                byteArray = encoder.GetBytes((string)modAttribute[j]);
                            }
                            else if (modAttribute[j] is Uri)
                            {
                                byteArray = encoder.GetBytes(((Uri)modAttribute[j]).ToString());
                            }
                            else
                            {
                                byteArray = (byte[])modAttribute[j];
                            }

                            berValues[j] = new BerVal()
                            {
                                bv_len = byteArray.Length,
                                bv_val = Marshal.AllocHGlobal(byteArray.Length)
                            };

                            // need to free the memory allocated on the heap when we are done
                            ptrToFree.Add(berValues[j].bv_val);
                            Marshal.Copy(byteArray, 0, berValues[j].bv_val, berValues[j].bv_len);
                        }
                    }

                    attributes[i].values = Utility.AllocHGlobalIntPtrArray(valuesCount + 1);
                    int structSize = Marshal.SizeOf(typeof(BerVal));
                    IntPtr controlPtr;
                    IntPtr tempPtr;

                    int m;
                    for (m = 0; m < valuesCount; m++)
                    {
                        controlPtr = Marshal.AllocHGlobal(structSize);

                        // Need to free the memory allocated on the heap when we are done.
                        ptrToFree.Add(controlPtr);
                        Marshal.StructureToPtr(berValues[m], controlPtr, false);
                        tempPtr = (IntPtr)((long)attributes[i].values + IntPtr.Size * m);
                        Marshal.WriteIntPtr(tempPtr, controlPtr);
                    }
                    tempPtr = (IntPtr)((long)attributes[i].values + IntPtr.Size * m);
                    Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);
                }
            }

            return attributes;
        }

        internal async ValueTask<DirectoryResponse> ConstructResponseAsync(int messageId, LdapOperation operation, ResultAll resultType, TimeSpan requestTimeOut, bool exceptionOnTimeOut, bool sync)
        {
            var timeout = new LDAP_TIMEVAL()
            {
                tv_sec = (int)(requestTimeOut.Ticks / TimeSpan.TicksPerSecond)
            };
            IntPtr ldapResult = IntPtr.Zero;
            DirectoryResponse response = null;

            IntPtr requestName = IntPtr.Zero;
            IntPtr requestValue = IntPtr.Zero;

            IntPtr entryMessage;

            bool needAbandon = true;

            // processing for the partial results retrieval
            if (resultType != ResultAll.LDAP_MSG_ALL)
            {
                // we need to have 0 timeout as we are polling for the results and don't want to wait
                timeout.tv_sec = 0;
                timeout.tv_usec = 0;

                if (resultType == ResultAll.LDAP_MSG_POLLINGALL)
                {
                    resultType = ResultAll.LDAP_MSG_ALL;
                }

                // when doing partial results retrieving, if ldap_result failed, we don't do ldap_abandon here.
                needAbandon = false;
            }

            int error;
            if (sync)
            {
                error = LdapPal.GetResultFromAsyncOperation(_ldapHandle, messageId, (int)resultType, timeout, ref ldapResult);
            }
            else
            {
                timeout.tv_sec = 0;
                timeout.tv_usec = 0;
                int iterationDelay = 1;
                // Underlying native libraries don't support callback-based function, so we will instead use polling and
                // use a Stopwatch to track the timeout manually.
                Stopwatch watch = Stopwatch.StartNew();
                while (true)
                {
                    error = LdapPal.GetResultFromAsyncOperation(_ldapHandle, messageId, (int)resultType, timeout, ref ldapResult);
                    if (error != 0 || (requestTimeOut != Threading.Timeout.InfiniteTimeSpan && watch.Elapsed > requestTimeOut))
                    {
                        break;
                    }
                    await Task.Delay(Math.Min(iterationDelay, 100)).ConfigureAwait(false);
                    if (iterationDelay < 100)
                    {
                        iterationDelay *= 2;
                    }
                }
                watch.Stop();
            }

            if (error != -1 && error != 0)
            {
                // parsing the result
                int serverError = 0;
                try
                {
                    int resultError = 0;
                    string responseDn = null;
                    string responseMessage = null;
                    Uri[] responseReferral = null;
                    DirectoryControl[] responseControl = null;

                    // ldap_parse_result skips over messages of type LDAP_RES_SEARCH_ENTRY and LDAP_RES_SEARCH_REFERRAL
                    if (error != (int)LdapResult.LDAP_RES_SEARCH_ENTRY && error != (int)LdapResult.LDAP_RES_REFERRAL)
                    {
                        resultError = ConstructParsedResult(ldapResult, ref serverError, ref responseDn, ref responseMessage, ref responseReferral, ref responseControl);
                    }

                    if (resultError == 0)
                    {
                        resultError = serverError;

                        if (error == (int)LdapResult.LDAP_RES_ADD)
                        {
                            response = new AddResponse(responseDn, responseControl, (ResultCode)resultError, responseMessage, responseReferral);
                        }
                        else if (error == (int)LdapResult.LDAP_RES_MODIFY)
                        {
                            response = new ModifyResponse(responseDn, responseControl, (ResultCode)resultError, responseMessage, responseReferral);
                        }
                        else if (error == (int)LdapResult.LDAP_RES_DELETE)
                        {
                            response = new DeleteResponse(responseDn, responseControl, (ResultCode)resultError, responseMessage, responseReferral);
                        }
                        else if (error == (int)LdapResult.LDAP_RES_MODRDN)
                        {
                            response = new ModifyDNResponse(responseDn, responseControl, (ResultCode)resultError, responseMessage, responseReferral);
                        }
                        else if (error == (int)LdapResult.LDAP_RES_COMPARE)
                        {
                            response = new CompareResponse(responseDn, responseControl, (ResultCode)resultError, responseMessage, responseReferral);
                        }
                        else if (error == (int)LdapResult.LDAP_RES_EXTENDED)
                        {
                            response = new ExtendedResponse(responseDn, responseControl, (ResultCode)resultError, responseMessage, responseReferral);
                            if (resultError == (int)ResultCode.Success)
                            {
                                resultError = LdapPal.ParseExtendedResult(_ldapHandle, ldapResult, ref requestName, ref requestValue, 0 /*not free it*/);
                                if (resultError == 0)
                                {
                                    string name = null;
                                    if (requestName != IntPtr.Zero)
                                    {
                                        name = LdapPal.PtrToString(requestName);
                                    }

                                    BerVal val = null;
                                    byte[] requestValueArray = null;
                                    if (requestValue != IntPtr.Zero)
                                    {
                                        val = new BerVal();
                                        Marshal.PtrToStructure(requestValue, val);
                                        if (val.bv_len != 0 && val.bv_val != IntPtr.Zero)
                                        {
                                            requestValueArray = new byte[val.bv_len];
                                            Marshal.Copy(val.bv_val, requestValueArray, 0, val.bv_len);
                                        }
                                    }

                                    ((ExtendedResponse)response).ResponseName = name;
                                    ((ExtendedResponse)response).ResponseValue = requestValueArray;
                                }
                            }
                        }
                        else if (error == (int)LdapResult.LDAP_RES_SEARCH_RESULT ||
                               error == (int)LdapResult.LDAP_RES_SEARCH_ENTRY ||
                               error == (int)LdapResult.LDAP_RES_REFERRAL)
                        {
                            response = new SearchResponse(responseDn, responseControl, (ResultCode)resultError, responseMessage, responseReferral);

                            //set the flag here so our partial result processor knows whether the search is done or not
                            if (error == (int)LdapResult.LDAP_RES_SEARCH_RESULT)
                            {
                                ((SearchResponse)response).searchDone = true;
                            }

                            SearchResultEntryCollection searchResultEntries = new SearchResultEntryCollection();
                            SearchResultReferenceCollection searchResultReferences = new SearchResultReferenceCollection();

                            // parsing the resultentry
                            entryMessage = LdapPal.GetFirstEntryFromResult(_ldapHandle, ldapResult);

                            int entrycount = 0;
                            while (entryMessage != IntPtr.Zero)
                            {
                                SearchResultEntry entry = ConstructEntry(entryMessage);
                                if (entry != null)
                                {
                                    searchResultEntries.Add(entry);
                                }

                                entrycount++;
                                entryMessage = LdapPal.GetNextEntryFromResult(_ldapHandle, entryMessage);
                            }

                            // Parse the reference.
                            IntPtr referenceMessage = LdapPal.GetFirstReferenceFromResult(_ldapHandle, ldapResult);

                            while (referenceMessage != IntPtr.Zero)
                            {
                                SearchResultReference reference = ConstructReference(referenceMessage);
                                if (reference != null)
                                {
                                    searchResultReferences.Add(reference);
                                }

                                referenceMessage = LdapPal.GetNextReferenceFromResult(_ldapHandle, referenceMessage);
                            }

                            ((SearchResponse)response).Entries = searchResultEntries;
                            ((SearchResponse)response).References = searchResultReferences;
                        }

                        if (resultError != (int)ResultCode.Success && resultError != (int)ResultCode.CompareFalse && resultError != (int)ResultCode.CompareTrue && resultError != (int)ResultCode.Referral && resultError != (int)ResultCode.ReferralV2)
                        {
                            // Throw operation exception.
                            if (Utility.IsResultCode((ResultCode)resultError))
                            {
                                throw new DirectoryOperationException(response, OperationErrorMappings.MapResultCode(resultError));
                            }
                            else
                            {
                                // This should not occur.
                                throw new DirectoryOperationException(response);
                            }
                        }

                        return response;
                    }
                    else
                    {
                        // Fall through, throw the exception beow.
                        error = resultError;
                    }
                }
                finally
                {
                    if (requestName != IntPtr.Zero)
                    {
                        LdapPal.FreeMemory(requestName);
                    }

                    if (requestValue != IntPtr.Zero)
                    {
                        LdapPal.FreeMemory(requestValue);
                    }

                    if (ldapResult != IntPtr.Zero)
                    {
                        LdapPal.FreeMessage(ldapResult);
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
                        // Client side timeout.
                        error = (int)LdapError.TimeOut;
                    }
                    else
                    {
                        // If we don't throw exception on time out (notification search for example), we
                        // just return an empty response.
                        return null;
                    }
                }
                else
                {
                    error = LdapPal.GetLastErrorFromConnection(_ldapHandle);
                }

                // Abandon the request.
                if (needAbandon)
                {
                    LdapPal.CancelDirectoryAsyncOperation(_ldapHandle, messageId);
                }
            }

            // Throw the proper exception here.
            throw ConstructException(error, operation);
        }

        internal unsafe int ConstructParsedResult(IntPtr ldapResult, ref int serverError, ref string responseDn, ref string responseMessage, ref Uri[] responseReferral, ref DirectoryControl[] responseControl)
        {
            IntPtr dn = IntPtr.Zero;
            IntPtr message = IntPtr.Zero;
            IntPtr referral = IntPtr.Zero;
            IntPtr control = IntPtr.Zero;

            try
            {
                int resultError = LdapPal.ParseResult(_ldapHandle, ldapResult, ref serverError, ref dn, ref message, ref referral, ref control, 0 /* not free it */);

                if (resultError == 0)
                {
                    // Parse the dn.
                    responseDn = LdapPal.PtrToString(dn);

                    // Parse the message.
                    responseMessage = LdapPal.PtrToString(message);

                    // Parse the referral.
                    if (referral != IntPtr.Zero)
                    {
                        char** tempPtr = (char**)referral;
                        char* singleReferral = tempPtr[0];
                        int i = 0;
                        var referralList = new ArrayList();
                        while (singleReferral != null)
                        {
                            string s = LdapPal.PtrToString((IntPtr)singleReferral);
                            referralList.Add(s);

                            i++;
                            singleReferral = tempPtr[i];
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

                    // Parse the control.
                    if (control != IntPtr.Zero)
                    {
                        int i = 0;
                        IntPtr tempControlPtr = control;
                        IntPtr singleControl = Marshal.ReadIntPtr(tempControlPtr, 0);
                        var controlList = new ArrayList();
                        while (singleControl != IntPtr.Zero)
                        {
                            DirectoryControl directoryControl = ConstructControl(singleControl);
                            controlList.Add(directoryControl);

                            i++;
                            singleControl = Marshal.ReadIntPtr(tempControlPtr, i * IntPtr.Size);
                        }

                        responseControl = new DirectoryControl[controlList.Count];
                        controlList.CopyTo(responseControl);
                    }
                }
                else
                {
                    // we need to take care of one special case, when can't connect to the server, ldap_parse_result fails with local error
                    if (resultError == (int)LdapError.LocalError)
                    {
                        int tmpResult = LdapPal.ResultToErrorCode(_ldapHandle, ldapResult, 0 /* not free it */);
                        if (tmpResult != 0)
                        {
                            resultError = tmpResult;
                        }
                    }
                }

                return resultError;
            }
            finally
            {
                if (dn != IntPtr.Zero)
                {
                    LdapPal.FreeMemory(dn);
                }

                if (message != IntPtr.Zero)
                {
                    LdapPal.FreeMemory(message);
                }

                if (referral != IntPtr.Zero)
                {
                    LdapPal.FreeValue(referral);
                }

                if (control != IntPtr.Zero)
                {
                    LdapPal.FreeDirectoryControls(control);
                }
            }
        }

        internal SearchResultEntry ConstructEntry(IntPtr entryMessage)
        {
            IntPtr dn = IntPtr.Zero;
            IntPtr attribute = IntPtr.Zero;
            IntPtr address = IntPtr.Zero;

            try
            {
                // Get the dn.
                string entryDn = null;
                dn = LdapPal.GetDistinguishedName(_ldapHandle, entryMessage);
                if (dn != IntPtr.Zero)
                {
                    entryDn = LdapPal.PtrToString(dn);
                    LdapPal.FreeMemory(dn);
                    dn = IntPtr.Zero;
                }

                SearchResultEntry resultEntry = new SearchResultEntry(entryDn);
                SearchResultAttributeCollection attributes = resultEntry.Attributes;

                // Get attributes.
                attribute = LdapPal.GetFirstAttributeFromEntry(_ldapHandle, entryMessage, ref address);

                int tempcount = 0;
                while (attribute != IntPtr.Zero)
                {
                    DirectoryAttribute attr = ConstructAttribute(entryMessage, attribute);
                    attributes.Add(attr.Name, attr);

                    LdapPal.FreeMemory(attribute);
                    tempcount++;
                    attribute = LdapPal.GetNextAttributeFromResult(_ldapHandle, entryMessage, address);
                }

                if (address != IntPtr.Zero)
                {
                    BerPal.FreeBerElement(address, 0);
                    address = IntPtr.Zero;
                }

                return resultEntry;
            }
            finally
            {
                if (dn != IntPtr.Zero)
                {
                    LdapPal.FreeMemory(dn);
                }

                if (attribute != IntPtr.Zero)
                {
                    LdapPal.FreeMemory(attribute);
                }

                if (address != IntPtr.Zero)
                {
                    BerPal.FreeBerElement(address, 0);
                }
            }
        }

        internal DirectoryAttribute ConstructAttribute(IntPtr entryMessage, IntPtr attributeName)
        {
            var attribute = new DirectoryAttribute()
            {
                _isSearchResult = true
            };

            string name = LdapPal.PtrToString(attributeName);
            attribute.Name = name;
            IntPtr valuesArray = LdapPal.GetValuesFromAttribute(_ldapHandle, entryMessage, name);
            try
            {
                if (valuesArray != IntPtr.Zero)
                {
                    int count = 0;
                    IntPtr tempPtr = Marshal.ReadIntPtr(valuesArray, IntPtr.Size * count);
                    while (tempPtr != IntPtr.Zero)
                    {
                        BerVal bervalue = new BerVal();
                        Marshal.PtrToStructure(tempPtr, bervalue);
                        byte[] byteArray;
                        if (bervalue.bv_len > 0 && bervalue.bv_val != IntPtr.Zero)
                        {
                            byteArray = new byte[bervalue.bv_len];
                            Marshal.Copy(bervalue.bv_val, byteArray, 0, bervalue.bv_len);
                            attribute.Add(byteArray);
                        }

                        count++;
                        tempPtr = Marshal.ReadIntPtr(valuesArray, IntPtr.Size * count);
                    }
                }
            }
            finally
            {
                if (valuesArray != IntPtr.Zero)
                {
                    LdapPal.FreeAttributes(valuesArray);
                }
            }

            return attribute;
        }

        internal SearchResultReference ConstructReference(IntPtr referenceMessage)
        {
            IntPtr referenceArray = IntPtr.Zero;
            int error = LdapPal.ParseReference(_ldapHandle, referenceMessage, ref referenceArray);

            try
            {
                if (error == 0)
                {
                    var referralList = new ArrayList();
                    IntPtr tempPtr;
                    int count = 0;
                    if (referenceArray != IntPtr.Zero)
                    {
                        tempPtr = Marshal.ReadIntPtr(referenceArray, IntPtr.Size * count);
                        while (tempPtr != IntPtr.Zero)
                        {
                            string s = LdapPal.PtrToString(tempPtr);
                            referralList.Add(s);

                            count++;
                            tempPtr = Marshal.ReadIntPtr(referenceArray, IntPtr.Size * count);
                        }

                        LdapPal.FreeValue(referenceArray);
                        referenceArray = IntPtr.Zero;
                    }

                    if (referralList.Count > 0)
                    {
                        Uri[] uris = new Uri[referralList.Count];
                        for (int i = 0; i < referralList.Count; i++)
                        {
                            uris[i] = new Uri((string)referralList[i]);
                        }

                        return new SearchResultReference(uris);
                    }
                }
            }
            finally
            {
                if (referenceArray != IntPtr.Zero)
                {
                    LdapPal.FreeValue(referenceArray);
                }
            }

            return null;
        }

        private DirectoryException ConstructException(int error, LdapOperation operation)
        {
            DirectoryResponse response = null;

            if (Utility.IsResultCode((ResultCode)error))
            {
                if (operation == LdapOperation.LdapAdd)
                {
                    response = new AddResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                }
                else if (operation == LdapOperation.LdapModify)
                {
                    response = new ModifyResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                }
                else if (operation == LdapOperation.LdapDelete)
                {
                    response = new DeleteResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                }
                else if (operation == LdapOperation.LdapModifyDn)
                {
                    response = new ModifyDNResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                }
                else if (operation == LdapOperation.LdapCompare)
                {
                    response = new CompareResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                }
                else if (operation == LdapOperation.LdapSearch)
                {
                    response = new SearchResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                }
                else if (operation == LdapOperation.LdapExtendedRequest)
                {
                    response = new ExtendedResponse(null, null, (ResultCode)error, OperationErrorMappings.MapResultCode(error), null);
                }

                string errorMessage = OperationErrorMappings.MapResultCode(error);
                return new DirectoryOperationException(response, errorMessage);
            }
            else
            {
                if (LdapErrorMappings.IsLdapError(error))
                {
                    string errorMessage = LdapErrorMappings.MapResultCode(error);
                    string serverErrorMessage = SessionOptions.ServerErrorMessage;
                    if (!string.IsNullOrEmpty(serverErrorMessage))
                    {
                        throw new LdapException(error, errorMessage, serverErrorMessage);
                    }

                    return new LdapException(error, errorMessage);
                }

                return new LdapException(error);
            }
        }

        private static DirectoryControl ConstructControl(IntPtr controlPtr)
        {
            LdapControl control = new LdapControl();
            Marshal.PtrToStructure(controlPtr, control);

            Debug.Assert(control.ldctl_oid != IntPtr.Zero);
            string controlType = LdapPal.PtrToString(control.ldctl_oid);

            byte[] bytes = new byte[control.ldctl_value.bv_len];
            Marshal.Copy(control.ldctl_value.bv_val, bytes, 0, control.ldctl_value.bv_len);

            bool criticality = control.ldctl_iscritical;

            return new DirectoryControl(controlType, bytes, criticality, true);
        }

        private static bool SameCredential(NetworkCredential oldCredential, NetworkCredential newCredential)
        {
            if (oldCredential == null && newCredential == null)
            {
                return true;
            }
            else if (oldCredential == null && newCredential != null)
            {
                return false;
            }
            else if (oldCredential != null && newCredential == null)
            {
                return false;
            }
            else
            {
                if (oldCredential.Domain == newCredential.Domain &&
                    oldCredential.UserName == newCredential.UserName &&
                    oldCredential.Password == newCredential.Password)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
