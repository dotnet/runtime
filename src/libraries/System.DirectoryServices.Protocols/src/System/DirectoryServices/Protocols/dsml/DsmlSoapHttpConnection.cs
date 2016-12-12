// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Xml;
    using System.IO;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Text;
    using System.Collections;
    using System.Threading;
    using System.Security.Permissions;
    using System.Net.NetworkInformation;

    public class DsmlSoapHttpConnection : DsmlSoapConnection
    {
        private HttpWebRequest _dsmlHttpConnection = null;
        private string _dsmlSoapAction = "\"#batchRequest\"";
        private AuthType _dsmlAuthType = AuthType.Negotiate;
        private string _dsmlSessionID = null;
        private Hashtable _httpConnectionTable = null;
        private string _debugResponse = null;

        [
           DirectoryServicesPermission(SecurityAction.Demand, Unrestricted = true)
        ]
        public DsmlSoapHttpConnection(Uri uri) : this(new DsmlDirectoryIdentifier(uri))
        {
        }

        [
           DirectoryServicesPermission(SecurityAction.Demand, Unrestricted = true),
           WebPermission(SecurityAction.Assert, Unrestricted = true)
        ]
        public DsmlSoapHttpConnection(DsmlDirectoryIdentifier identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            directoryIdentifier = identifier;
            _dsmlHttpConnection = (HttpWebRequest)WebRequest.Create(((DsmlDirectoryIdentifier)directoryIdentifier).ServerUri);
            Hashtable tempTable = new Hashtable();
            _httpConnectionTable = Hashtable.Synchronized(tempTable);
        }

        [
           DirectoryServicesPermission(SecurityAction.Demand, Unrestricted = true),
           EnvironmentPermission(SecurityAction.Assert, Unrestricted = true),
           SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)
        ]
        public DsmlSoapHttpConnection(DsmlDirectoryIdentifier identifier, NetworkCredential credential) : this(identifier)
        {
            directoryCredential = (credential != null) ? new NetworkCredential(credential.UserName, credential.Password, credential.Domain) : null;
        }

        [
           DirectoryServicesPermission(SecurityAction.Demand, Unrestricted = true)
        ]
        public DsmlSoapHttpConnection(DsmlDirectoryIdentifier identifier, NetworkCredential credential, AuthType authType) : this(identifier, credential)
        {
            AuthType = authType;
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
                if (value.TotalMilliseconds > Int32.MaxValue)
                    throw new ArgumentException(Res.GetString(Res.TimespanExceedMax), "value");

                connectionTimeOut = value;
            }
        }

        public string SoapActionHeader
        {
            get
            {
                return _dsmlSoapAction;
            }
            set
            {
                _dsmlSoapAction = value;
            }
        }

        public AuthType AuthType
        {
            get
            {
                return _dsmlAuthType;
            }
            set
            {
                if (value < AuthType.Anonymous || value > AuthType.Kerberos)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(AuthType));

                if ((value != AuthType.Anonymous) && (value != AuthType.Ntlm) && (value != AuthType.Basic) && (value != AuthType.Negotiate) && (value != AuthType.Digest))
                {
                    throw new ArgumentException(Res.GetString(Res.WrongAuthType, value), "value");
                }

                _dsmlAuthType = value;
            }
        }

        public override string SessionId
        {
            get
            {
                return _dsmlSessionID;
            }
        }

        private string ResponseString
        {
            get
            {
                return _debugResponse;
            }
        }

        [
           DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true),
           WebPermission(SecurityAction.Assert, Unrestricted = true),
           NetworkInformationPermission(SecurityAction.Assert, Unrestricted = true)
       ]
        public override void BeginSession()
        {
            // make sure a session isn't already active
            if (_dsmlSessionID != null)
            {
                throw new InvalidOperationException(Res.GetString(Res.SessionInUse));
            }

            try
            {
                //
                // Request
                //

                // Do the generic preparation of the request
                PrepareHttpWebRequest(_dsmlHttpConnection);

                // Get the stream we're going to write to
                StreamWriter reqWriter = GetWebRequestStreamWriter();

                try
                {
                    // write out the EndSession request
                    reqWriter.Write(DsmlConstants.SOAPEnvelopeBegin);

                    reqWriter.Write(DsmlConstants.SOAPHeaderBegin);
                    reqWriter.Write(DsmlConstants.SOAPBeginSession);                // BeginSession       
                    // other user provided soap headers
                    if (soapHeaders != null)
                        reqWriter.Write(soapHeaders.OuterXml);
                    reqWriter.Write(DsmlConstants.SOAPHeaderEnd);

                    reqWriter.Write(DsmlConstants.SOAPBodyBegin);
                    reqWriter.Write(new DsmlRequestDocument().ToXml().InnerXml);    // empty batchRequest
                    reqWriter.Write(DsmlConstants.SOAPBodyEnd);

                    reqWriter.Write(DsmlConstants.SOAPEnvelopeEnd);

                    // Close out the stream
                    reqWriter.Flush();
                }
                finally
                {
                    reqWriter.BaseStream.Close();
                    reqWriter.Close();
                }

                //
                // Response
                //

                // Actually send the request and get the response
                HttpWebResponse dsmlResponse = (HttpWebResponse)_dsmlHttpConnection.GetResponse();
                // N.B.: we deliberately permit any exception to bubble up to the caller                

                try
                {
                    // extract the sessionID from the successful response
                    _dsmlSessionID = ExtractSessionID(dsmlResponse);
                }
                finally
                {
                    dsmlResponse.Close();
                }
            }
            finally
            {
                // create a fresh HttpWebRequest for the next operation
                // (HttpWebRequest objects can only be used once)
                _dsmlHttpConnection = (HttpWebRequest)WebRequest.Create(((DsmlDirectoryIdentifier)directoryIdentifier).ServerUri);
            }
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true),
            WebPermission(SecurityAction.Assert, Unrestricted = true),
            NetworkInformationPermission(SecurityAction.Assert, Unrestricted = true)
        ]
        public override void EndSession()
        {
            // make sure we have a session
            if (_dsmlSessionID == null)
            {
                throw new InvalidOperationException(Res.GetString(Res.NoCurrentSession));
            }

            //
            // Request
            //

            try
            {
                try
                {
                    // Do the generic preparation of the request
                    PrepareHttpWebRequest(_dsmlHttpConnection);

                    // Get the stream we're going to write to
                    StreamWriter reqWriter = GetWebRequestStreamWriter();

                    try
                    {
                        // write out the EndSession request
                        reqWriter.Write(DsmlConstants.SOAPEnvelopeBegin);

                        reqWriter.Write(DsmlConstants.SOAPHeaderBegin);
                        reqWriter.Write(DsmlConstants.SOAPEndSession1);                // EndSession
                        reqWriter.Write(_dsmlSessionID);
                        reqWriter.Write(DsmlConstants.SOAPEndSession2);
                        if (soapHeaders != null)
                            reqWriter.Write(soapHeaders.OuterXml);
                        reqWriter.Write(DsmlConstants.SOAPHeaderEnd);

                        reqWriter.Write(DsmlConstants.SOAPBodyBegin);
                        reqWriter.Write(new DsmlRequestDocument().ToXml().InnerXml);    // empty batchRequest
                        reqWriter.Write(DsmlConstants.SOAPBodyEnd);

                        reqWriter.Write(DsmlConstants.SOAPEnvelopeEnd);

                        // Close out the stream
                        reqWriter.Flush();
                    }
                    finally
                    {
                        reqWriter.BaseStream.Close();
                        reqWriter.Close();
                    }

                    //
                    // Response
                    //

                    // Actually send the request and get the response
                    HttpWebResponse dsmlResponse = (HttpWebResponse)_dsmlHttpConnection.GetResponse();
                    // N.B.: we deliberately permit any exception to bubble up to the caller

                    dsmlResponse.Close();
                }
                catch (WebException e)
                {
                    // Null out dsmlSessionID if applicable and rethrow exception

                    // Based on the nature of the exception, we have to decide whether
                    // or not we're still in a session.  For example, if we failed because
                    // we couldn't connect to the server, the session must still be active.
                    // If the server failed the request (e.g., because it timed out the
                    // session and no longer recognizes our sessionID as value), the session
                    // is not active.
                    if ((e.Status != WebExceptionStatus.ConnectFailure) &&
                         (e.Status != WebExceptionStatus.NameResolutionFailure) &&
                         (e.Status != WebExceptionStatus.ProxyNameResolutionFailure) &&
                         (e.Status != WebExceptionStatus.SendFailure) &&
                         (e.Status != WebExceptionStatus.TrustFailure))
                    {
                        // It wasn't one of the exceptions where we failed to send the
                        // request to the server, so we have to assume the session may
                        // now be closed
                        _dsmlSessionID = null;
                    }

                    throw;
                }

                // Note, we don't actually process the contents of the response here.
                // If there was an error, the server would have returned a SOAP Fault, and we'd
                // have thrown an exception when we called GetResponse
                _dsmlSessionID = null;
            }
            finally
            {
                // create a fresh HttpWebRequest for the next operation
                // (HttpWebRequest objects can only be used once)
                _dsmlHttpConnection = (HttpWebRequest)WebRequest.Create(((DsmlDirectoryIdentifier)directoryIdentifier).ServerUri);
            }
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public override DirectoryResponse SendRequest(DirectoryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            DsmlRequestDocument doc = new DsmlRequestDocument();
            doc.Add(request);

            DsmlResponseDocument response = SendRequestHelper(doc.ToXml().InnerXml);

            if (response.Count == 0)
                throw new DsmlInvalidDocumentException(Res.GetString(Res.MissingResponse));

            DirectoryResponse result = response[0];

            if (result is DsmlErrorResponse)
            {
                // need to throw ErrorResponseException
                ErrorResponseException e = new ErrorResponseException((DsmlErrorResponse)result);
                throw e;
            }
            else
            {
                ResultCode error = result.ResultCode;
                if (error == ResultCode.Success || error == ResultCode.CompareFalse || error == ResultCode.CompareTrue || error == ResultCode.Referral || error == ResultCode.ReferralV2)
                    return result;
                else
                {
                    throw new DirectoryOperationException(result, OperationErrorMappings.MapResultCode((int)error));
                }
            }
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public DsmlResponseDocument SendRequest(DsmlRequestDocument request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            DsmlResponseDocument responseDoc = SendRequestHelper(request.ToXml().InnerXml);
            if (request.Count > 0 && responseDoc.Count == 0)
                throw new DsmlInvalidDocumentException(Res.GetString(Res.MissingResponse));

            return responseDoc;
        }

        [
            WebPermission(SecurityAction.Assert, Unrestricted = true),
            NetworkInformationPermission(SecurityAction.Assert, Unrestricted = true)
        ]
        private DsmlResponseDocument SendRequestHelper(string reqstring)
        {
            //
            // Request
            //                        
            StringBuilder requestBuffer = new StringBuilder(1024);

            try
            {
                // Do the generic preparation of the request
                PrepareHttpWebRequest(_dsmlHttpConnection);

                // Get the stream we're going to write to
                StreamWriter reqWriter = GetWebRequestStreamWriter();

                try
                {
                    // Begin the SOAP Envelope, attaching the sessionID if applicable
                    BeginSOAPRequest(ref requestBuffer);

                    // Write out the actual DSML v2 request
                    requestBuffer.Append(reqstring);

                    // Finish writing the SOAP Envelope
                    EndSOAPRequest(ref requestBuffer);

                    reqWriter.Write(requestBuffer.ToString());

                    // Close out the stream
                    reqWriter.Flush();
                }
                finally
                {
                    reqWriter.BaseStream.Close();
                    reqWriter.Close();
                }

                //
                // Response
                //

                // Actually send the request to the server, and retrieve the response
                // N.B.: we deliberately permit any exception to bubble up to the caller
                HttpWebResponse dsmlResponse = (HttpWebResponse)_dsmlHttpConnection.GetResponse();
                DsmlResponseDocument dsmlResponseDoc;

                try
                {
                    // Process the response
                    dsmlResponseDoc = new DsmlResponseDocument(
                                                       dsmlResponse,
                                                       "se:Envelope/se:Body/dsml:batchResponse"
                                                        );
                    _debugResponse = dsmlResponseDoc.ResponseString;
                }
                finally
                {
                    dsmlResponse.Close();
                }

                return dsmlResponseDoc;
            }
            finally
            {
                // create a fresh HttpWebRequest for the next operation
                // (HttpWebRequest objects can only be used once)
                _dsmlHttpConnection = (HttpWebRequest)WebRequest.Create(((DsmlDirectoryIdentifier)directoryIdentifier).ServerUri);
            }
        }

        [EnvironmentPermission(SecurityAction.Assert, Unrestricted = true)]
        private void PrepareHttpWebRequest(HttpWebRequest dsmlConnection)
        {
            // set the credentials appropriately
            if (directoryCredential == null)
            {
                // default credentials
                dsmlConnection.Credentials = CredentialCache.DefaultCredentials;
            }
            else
            {
                // use the caller's explicit credentials and authType
                string authenticationType = "negotiate";
                if (_dsmlAuthType == AuthType.Ntlm)
                    authenticationType = "NTLM";
                else if (_dsmlAuthType == AuthType.Basic)
                    authenticationType = "basic";
                else if (_dsmlAuthType == AuthType.Anonymous)
                    authenticationType = "anonymous";
                else if (_dsmlAuthType == AuthType.Digest)
                    authenticationType = "digest";

                CredentialCache ccache = new CredentialCache();
                ccache.Add(dsmlConnection.RequestUri, authenticationType, directoryCredential);
                dsmlConnection.Credentials = ccache;
            }

            // set the client certificates
            foreach (X509Certificate cert in ClientCertificates)
            {
                dsmlConnection.ClientCertificates.Add(cert);
            }

            // set the timeout
            if (connectionTimeOut.Ticks != 0)
            {
                dsmlConnection.Timeout = (int)(connectionTimeOut.Ticks / TimeSpan.TicksPerMillisecond);
            }

            // set the SOAPAction header
            if (_dsmlSoapAction != null)
            {
                WebHeaderCollection headers = dsmlConnection.Headers;
                headers.Set("SOAPAction", _dsmlSoapAction);
            }

            // set the other headers
            dsmlConnection.Method = DsmlConstants.HttpPostMethod;
        }

        private StreamWriter GetWebRequestStreamWriter()
        {
            Stream reqStream = _dsmlHttpConnection.GetRequestStream();
            // N.B.: we deliberately permit any exception to bubble up to the caller
            StreamWriter reqWriter = new StreamWriter(reqStream);

            return reqWriter;
        }

        /// <summary>
        /// This method writes the SOAP Envelope and Body to the stream.
        /// If also attaches the SOAP Header for the sessionID, if
        /// applicable.
        /// </summary>
        private void BeginSOAPRequest(ref StringBuilder buffer)
        {
            buffer.Append(DsmlConstants.SOAPEnvelopeBegin);

            if (_dsmlSessionID != null || soapHeaders != null)
            {
                buffer.Append(DsmlConstants.SOAPHeaderBegin);
                if (_dsmlSessionID != null)
                {
                    buffer.Append(DsmlConstants.SOAPSession1);
                    buffer.Append(_dsmlSessionID);
                    buffer.Append(DsmlConstants.SOAPSession2);
                }
                if (soapHeaders != null)
                {
                    buffer.Append(soapHeaders.OuterXml);
                }
                buffer.Append(DsmlConstants.SOAPHeaderEnd);
            }

            buffer.Append(DsmlConstants.SOAPBodyBegin);
        }

        /// <summary>
        /// This method writes the closing elements of the SOAP Envelope
        /// and Body to the stream.
        /// </summary>
        private void EndSOAPRequest(ref StringBuilder buffer)
        {
            buffer.Append(DsmlConstants.SOAPBodyEnd);
            buffer.Append(DsmlConstants.SOAPEnvelopeEnd);
        }

        /// <summary>
        /// This method extracts the sessionID value from the Session header
        /// of a response
        /// </summary>
        private string ExtractSessionID(HttpWebResponse resp)
        {
            // Transform the response into an XmlDocument
            Stream respStream = resp.GetResponseStream();
            StreamReader respReader = new StreamReader(respStream);

            try
            {
                XmlDocument respXml = new XmlDocument();

                try
                {
                    respXml.Load(respReader);
                }
                catch (XmlException)
                {
                    // The server didn't return well-formed XML to us
                    throw new DsmlInvalidDocumentException();
                }

                // Locate the SessionID attribute node
                XmlNamespaceManager ns = NamespaceUtils.GetDsmlNamespaceManager();
                XmlAttribute node = (XmlAttribute)respXml.SelectSingleNode("se:Envelope/se:Header/ad:Session/@ad:SessionID", ns);

                if (node == null)
                {
                    // try it without the namespace qualifier on the attribute, since default namespaces don't
                    // apply to attributes
                    node = (XmlAttribute)respXml.SelectSingleNode("se:Envelope/se:Header/ad:Session/@SessionID", ns);

                    if (node == null)
                    {
                        throw new DsmlInvalidDocumentException(Res.GetString(Res.NoSessionIDReturned));
                    }
                }

                return node.Value;
            }
            finally
            {
                respReader.Close();
            }
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true),
            WebPermission(SecurityAction.Assert, Unrestricted = true),
            NetworkInformationPermission(SecurityAction.Assert, Unrestricted = true)
        ]
        public IAsyncResult BeginSendRequest(DsmlRequestDocument request, AsyncCallback callback, object state)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            // construct the new httpwebrequest object
            HttpWebRequest asyncConnection = (HttpWebRequest)WebRequest.Create(((DsmlDirectoryIdentifier)directoryIdentifier).ServerUri);
            // Do the generic preparation of the request
            PrepareHttpWebRequest(asyncConnection);

            // construct the request string
            StringBuilder requestStringBuffer = new StringBuilder(1024);
            // Begin the SOAP Envelope, attaching the sessionID if applicable
            BeginSOAPRequest(ref requestStringBuffer);
            // append the document
            requestStringBuffer.Append(request.ToXml().InnerXml);
            // Finish writing the SOAP Envelope
            EndSOAPRequest(ref requestStringBuffer);

            // construct the state object
            RequestState rs = new RequestState();
            rs.request = asyncConnection;
            rs.requestString = requestStringBuffer.ToString();

            // construct the async object
            DsmlAsyncResult asyncResult = new DsmlAsyncResult(callback, state);
            asyncResult.resultObject = rs;
            // give hint about whether this is an empty request or not so later we could validate the response
            if (request.Count > 0)
                asyncResult.hasValidRequest = true;

            // result object needs to have a handle on the waitobject, so later it could wake up the EndSendRequest call
            rs.dsmlAsync = asyncResult;

            // add connection-async pair to our table
            _httpConnectionTable.Add(asyncResult, asyncConnection);

            // begin the requeststream call
            asyncConnection.BeginGetRequestStream(new AsyncCallback(RequestStreamCallback), rs);

            // return the asyncResult
            return (IAsyncResult)asyncResult;
        }

        private static void RequestStreamCallback(IAsyncResult asyncResult)
        {
            // get our state object back
            RequestState rs = (RequestState)asyncResult.AsyncState;
            HttpWebRequest request = rs.request;

            try
            {
                rs.requestStream = request.EndGetRequestStream(asyncResult);

                // get the request string            
                byte[] info = rs.encoder.GetBytes(rs.requestString);

                // begin asynchronous write
                rs.requestStream.BeginWrite(info, 0, info.Length, new AsyncCallback(WriteCallback), rs);
            }
            catch (Exception e)
            {
                // exception occurs, we need to catch it here, close the request stream and properly set the exception and call wakeup routine
                if (rs.requestStream != null)
                    rs.requestStream.Close();
                rs.exception = e;
                WakeupRoutine(rs);
            }
        }

        private static void WriteCallback(IAsyncResult asyncResult)
        {
            // get our state object back
            RequestState rs = (RequestState)asyncResult.AsyncState;
            try
            {
                rs.requestStream.EndWrite(asyncResult);

                // begin to get response
                rs.request.BeginGetResponse(new AsyncCallback(ResponseCallback), rs);
            }
            catch (Exception e)
            {
                // exception occurs, we need to catch it here and properly set the exception and call wakeup routine
                rs.exception = e;
                WakeupRoutine(rs);
            }
            finally
            {
                rs.requestStream.Close();
            }
        }

        private static void ResponseCallback(IAsyncResult asyncResult)
        {
            // get our state object back
            RequestState rs = (RequestState)asyncResult.AsyncState;
            WebResponse resp = null;

            try
            {
                resp = rs.request.EndGetResponse(asyncResult);

                rs.responseStream = resp.GetResponseStream();

                // begin to read
                rs.responseStream.BeginRead(rs.bufferRead, 0, RequestState.bufferSize, new AsyncCallback(ReadCallback), rs);
            }
            catch (Exception e)
            {
                // exception occurs, we need to catch it here, close the response stream and properly set the exception and call wakeup routine
                if (rs.responseStream != null)
                    rs.responseStream.Close();
                rs.exception = e;
                WakeupRoutine(rs);
            }
        }

        private static void ReadCallback(IAsyncResult asyncResult)
        {
            // get our state object back
            RequestState rs = (RequestState)asyncResult.AsyncState;
            int count = 0;
            string s = null;

            try
            {
                count = rs.responseStream.EndRead(asyncResult);
                if (count > 0)
                {
                    // still more to read                
                    s = rs.encoder.GetString(rs.bufferRead);
                    int val = Math.Min(s.Length, count);
                    rs.responseString.Append(s, 0, val);

                    rs.responseStream.BeginRead(rs.bufferRead, 0, RequestState.bufferSize, new AsyncCallback(ReadCallback), rs);
                }
                else
                {
                    // have retrieved all the response, close the stream
                    rs.responseStream.Close();

                    WakeupRoutine(rs);
                }
            }
            catch (Exception e)
            {
                // exception occurs, need to close the stream
                rs.responseStream.Close();

                // exception occurs, we need to catch it here and properly set the exception and call wakeup routine
                rs.exception = e;
                WakeupRoutine(rs);
            }
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public void Abort(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            if (!(asyncResult is DsmlAsyncResult))
                throw new ArgumentException(Res.GetString(Res.NotReturnedAsyncResult, "asyncResult"));

            if (!_httpConnectionTable.Contains(asyncResult))
                throw new ArgumentException(Res.GetString(Res.InvalidAsyncResult));

            HttpWebRequest request = (HttpWebRequest)(_httpConnectionTable[asyncResult]);

            // remove the asyncResult from our connection table
            _httpConnectionTable.Remove(asyncResult);

            // cancel the request
            request.Abort();

            DsmlAsyncResult result = (DsmlAsyncResult)asyncResult;
            result.resultObject.abortCalled = true;
        }

        public DsmlResponseDocument EndSendRequest(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            if (!(asyncResult is DsmlAsyncResult))
                throw new ArgumentException(Res.GetString(Res.NotReturnedAsyncResult, "asyncResult"));

            if (!_httpConnectionTable.Contains(asyncResult))
                throw new ArgumentException(Res.GetString(Res.InvalidAsyncResult));

            // remove the asyncResult from our connection table
            _httpConnectionTable.Remove(asyncResult);

            DsmlAsyncResult result = (DsmlAsyncResult)asyncResult;
            asyncResult.AsyncWaitHandle.WaitOne();

            // Process the response

            // see if any exception occurs, if yes, then throw the exception
            if (result.resultObject.exception != null)
                throw result.resultObject.exception;

            DsmlResponseDocument dsmlResponseDoc = new DsmlResponseDocument(
                                                       result.resultObject.responseString,
                                                       "se:Envelope/se:Body/dsml:batchResponse"
                                                       );
            _debugResponse = dsmlResponseDoc.ResponseString;

            // validate the response
            if (result.hasValidRequest && dsmlResponseDoc.Count == 0)
                throw new DsmlInvalidDocumentException(Res.GetString(Res.MissingResponse));

            return dsmlResponseDoc;
        }

        private static void WakeupRoutine(RequestState rs)
        {
            // signal waitable object, indicate operation completed and fire callback
            rs.dsmlAsync.manualResetEvent.Set();
            rs.dsmlAsync.completed = true;
            if (rs.dsmlAsync.callback != null && !rs.abortCalled)
            {
                rs.dsmlAsync.callback((IAsyncResult)rs.dsmlAsync);
            }
        }
    }
}
