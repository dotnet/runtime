// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    /// <summary>
    /// <para>
    ///     Implements basic sending and receiving of network commands.
    ///     Handles generic parsing of server responses and provides
    ///     a pipeline sequencing mechanism for sending the commands to the server.
    /// </para>
    /// </summary>
    internal class CommandStream : NetworkStreamWrapper
    {
        private static readonly AsyncCallback s_writeCallbackDelegate = new AsyncCallback(WriteCallback);
        private static readonly AsyncCallback s_readCallbackDelegate = new AsyncCallback(ReadCallback);

        private bool _recoverableFailure;

        //
        // Active variables used for the command state machine
        //

        protected WebRequest? _request;
        protected bool _isAsync;
        private bool _aborted;

        protected PipelineEntry[]? _commands;
        protected int _index;
        private bool _doRead;
        private bool _doSend;
        private ResponseDescription? _currentResponseDescription;
        protected string? _abortReason;

        internal CommandStream(NetworkStream client)
            : base(client)
        {
            _decoder = _encoding.GetDecoder();
        }

        internal virtual void Abort(Exception e)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "closing control Stream");

            lock (this)
            {
                if (_aborted)
                    return;
                _aborted = true;
            }

            try
            {
                base.Close(0);
            }
            finally
            {
                if (e != null)
                {
                    InvokeRequestCallback(e);
                }
                else
                {
                    InvokeRequestCallback(null);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this);

            InvokeRequestCallback(null);

            // Do not call base.Dispose(bool), which would close the web request.
            // This stream effectively should be a wrapper around a web
            // request that does not own the web request.
        }

        protected void InvokeRequestCallback(object? obj)
        {
            WebRequest? webRequest = _request;
            if (webRequest != null)
            {
                FtpWebRequest ftpWebRequest = (FtpWebRequest)webRequest;
                ftpWebRequest.RequestCallback(obj);
            }
        }

        internal bool RecoverableFailure
        {
            get
            {
                return _recoverableFailure;
            }
        }

        protected void MarkAsRecoverableFailure()
        {
            if (_index <= 1)
            {
                _recoverableFailure = true;
            }
        }

        internal Stream? SubmitRequest(WebRequest request, bool isAsync, bool readInitalResponseOnConnect)
        {
            ClearState();
            PipelineEntry[]? commands = BuildCommandsList(request);
            InitCommandPipeline(request, commands, isAsync);
            if (readInitalResponseOnConnect)
            {
                _doSend = false;
                _index = -1;
            }
            return ContinueCommandPipeline();
        }

        protected virtual void ClearState()
        {
            InitCommandPipeline(null, null, false);
        }

        protected virtual PipelineEntry[]? BuildCommandsList(WebRequest request)
        {
            return null;
        }

        protected static Exception GenerateException(string message, WebExceptionStatus status, Exception? innerException)
        {
            return new WebException(
                            message,
                            innerException,
                            status,
                            null /* no response */ );
        }

        protected static Exception GenerateException(FtpStatusCode code, string? statusDescription, Exception? innerException)
        {
            return new WebException(SR.Format(SR.net_ftp_servererror, NetRes.GetWebStatusCodeString(code, statusDescription)),
                                    innerException, WebExceptionStatus.ProtocolError, null);
        }

        protected void InitCommandPipeline(WebRequest? request, PipelineEntry[]? commands, bool isAsync)
        {
            _commands = commands;
            _index = 0;
            _request = request;
            _aborted = false;
            _doRead = true;
            _doSend = true;
            _currentResponseDescription = null;
            _isAsync = isAsync;
            _recoverableFailure = false;
            _abortReason = string.Empty;
        }

        internal void CheckContinuePipeline()
        {
            if (_isAsync)
                return;
            try
            {
                ContinueCommandPipeline();
            }
            catch (Exception e)
            {
                Abort(e);
            }
        }

        ///     Pipelined command resolution.
        ///     How this works:
        ///     A list of commands that need to be sent to the FTP server are spliced together into an array,
        ///     each command such STOR, PORT, etc, is sent to the server, then the response is parsed into a string,
        ///     with the response, the delegate is called, which returns an instruction (either continue, stop, or read additional
        ///     responses from server).
        protected Stream? ContinueCommandPipeline()
        {
            // In async case, The BeginWrite can actually result in a
            // series of synchronous completions that eventually close
            // the connection. So we need to save the members that
            // we need to access, since they may not be valid after
            // BeginWrite returns
            bool isAsync = _isAsync;
            while (_index < _commands!.Length)
            {
                if (_doSend)
                {
                    if (_index < 0)
                        throw new InternalException();

                    byte[] sendBuffer = Encoding.GetBytes(_commands[_index].Command);

                    if (NetEventSource.Log.IsEnabled())
                    {
                        string sendCommand = _commands[_index].Command.Substring(0, _commands[_index].Command.Length - 2);
                        if (_commands[_index].HasFlag(PipelineEntryFlags.DontLogParameter))
                        {
                            int index = sendCommand.IndexOf(' ');
                            if (index != -1)
                                sendCommand = string.Concat(sendCommand.AsSpan(0, index), " ********");
                        }
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Sending command {sendCommand}");
                    }

                    try
                    {
                        if (isAsync)
                        {
                            BeginWrite(sendBuffer, 0, sendBuffer.Length, s_writeCallbackDelegate, this);
                        }
                        else
                        {
                            Write(sendBuffer, 0, sendBuffer.Length);
                        }
                    }
                    catch (IOException)
                    {
                        MarkAsRecoverableFailure();
                        throw;
                    }
                    catch
                    {
                        throw;
                    }

                    if (isAsync)
                    {
                        return null;
                    }
                }

                Stream? stream = null;
                bool isReturn = PostSendCommandProcessing(ref stream);
                if (isReturn)
                {
                    return stream;
                }
            }

            lock (this)
            {
                Close();
            }

            return null;
        }

        private bool PostSendCommandProcessing(ref Stream? stream)
        {
            if (_doRead)
            {
                // In async case, the next call can actually result in a
                // series of synchronous completions that eventually close
                // the connection. So we need to save the members that
                // we need to access, since they may not be valid after the
                // next call returns
                bool isAsync = _isAsync;
                int index = _index;
                PipelineEntry[] commands = _commands!;

                try
                {
                    ResponseDescription? response = ReceiveCommandResponse();
                    if (isAsync)
                    {
                        return true;
                    }
                    _currentResponseDescription = response;
                }
                catch
                {
                    // If we get an exception on the QUIT command (which is
                    // always the last command), ignore the final exception
                    // and continue with the pipeline regardlss of sync/async
                    if (index < 0 || index >= commands!.Length ||
                        commands[index].Command != "QUIT\r\n")
                        throw;
                }
            }
            return PostReadCommandProcessing(ref stream);
        }

        private bool PostReadCommandProcessing(ref Stream? stream)
        {
            if (_index >= _commands!.Length)
                return false;

            // Set up front to prevent a race condition on result == PipelineInstruction.Pause
            _doSend = false;
            _doRead = false;

            PipelineInstruction result;
            PipelineEntry? entry;
            if (_index == -1)
                entry = null;
            else
                entry = _commands[_index];

            // Final QUIT command may get exceptions since the connection
            // may be already closed by the server. So there is no response
            // to process, just advance the pipeline to continue.
            if (_currentResponseDescription == null && entry!.Command == "QUIT\r\n")
                result = PipelineInstruction.Advance;
            else
                result = PipelineCallback(entry, _currentResponseDescription, false, ref stream);

            if (result == PipelineInstruction.Abort)
            {
                Exception exception;
                if (_abortReason != string.Empty)
                    exception = new WebException(_abortReason);
                else
                    exception = GenerateException(SR.net_ftp_protocolerror, WebExceptionStatus.ServerProtocolViolation, null);
                Abort(exception);
                throw exception;
            }
            else if (result == PipelineInstruction.Advance)
            {
                _currentResponseDescription = null;
                _doSend = true;
                _doRead = true;
                _index++;
            }
            else if (result == PipelineInstruction.Pause)
            {
                // PipelineCallback did an async operation and will have to re-enter again.
                // Hold on for now.
                return true;
            }
            else if (result == PipelineInstruction.GiveStream)
            {
                // We will have another response coming, don't send
                _currentResponseDescription = null;
                _doRead = true;
                if (_isAsync)
                {
                    // If they block in the requestcallback we should still continue the pipeline
                    ContinueCommandPipeline();
                    InvokeRequestCallback(stream);
                }
                return true;
            }
            else if (result == PipelineInstruction.Reread)
            {
                // Another response is expected after this one
                _currentResponseDescription = null;
                _doRead = true;
            }
            return false;
        }

        internal enum PipelineInstruction
        {
            Abort,          // aborts the pipeline
            Advance,        // advances to the next pipelined command
            Pause,          // Let async callback to continue the pipeline
            Reread,         // rereads from the command socket
            GiveStream,     // returns with open data stream, let stream close to continue
        }

        [Flags]
        internal enum PipelineEntryFlags
        {
            UserCommand = 0x1,
            GiveDataStream = 0x2,
            CreateDataConnection = 0x4,
            DontLogParameter = 0x8
        }

        internal sealed class PipelineEntry
        {
            internal PipelineEntry(string command)
            {
                Command = command;
            }
            internal PipelineEntry(string command, PipelineEntryFlags flags)
            {
                Command = command;
                Flags = flags;
            }
            internal bool HasFlag(PipelineEntryFlags flags)
            {
                return (Flags & flags) != 0;
            }
            internal string Command;
            internal PipelineEntryFlags Flags;
        }

        protected virtual PipelineInstruction PipelineCallback(PipelineEntry? entry, ResponseDescription? response, bool timeout, ref Stream? stream)
        {
            return PipelineInstruction.Abort;
        }

        //
        // I/O callback methods
        //

        private static void ReadCallback(IAsyncResult asyncResult)
        {
            ReceiveState state = (ReceiveState)asyncResult.AsyncState!;
            try
            {
                Stream stream = (Stream)state.Connection;
                int bytesRead = 0;
                try
                {
                    bytesRead = stream.EndRead(asyncResult);
                    if (bytesRead == 0)
                        state.Connection.CloseSocket();
                }
                catch (IOException)
                {
                    state.Connection.MarkAsRecoverableFailure();
                    throw;
                }
                catch
                {
                    throw;
                }

                state.Connection.ReceiveCommandResponseCallback(state, bytesRead);
            }
            catch (Exception e)
            {
                state.Connection.Abort(e);
            }
        }

        private static void WriteCallback(IAsyncResult asyncResult)
        {
            CommandStream connection = (CommandStream)asyncResult.AsyncState!;
            try
            {
                try
                {
                    connection.EndWrite(asyncResult);
                }
                catch (IOException)
                {
                    connection.MarkAsRecoverableFailure();
                    throw;
                }
                catch
                {
                    throw;
                }
                Stream? stream = null;
                if (connection.PostSendCommandProcessing(ref stream))
                    return;
                connection.ContinueCommandPipeline();
            }
            catch (Exception e)
            {
                connection.Abort(e);
            }
        }

        //
        // Read parsing methods and privates
        //

        private string _buffer = string.Empty;
        private Encoding _encoding = Encoding.UTF8;
        private Decoder _decoder;

        protected Encoding Encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                _encoding = value;
                _decoder = _encoding.GetDecoder();
            }
        }

        /// <summary>
        /// This function is implemented in a derived class to determine whether a response is valid, and when it is complete.
        /// </summary>
        protected virtual bool CheckValid(ResponseDescription response, ref int validThrough, ref int completeLength)
        {
            return false;
        }

        /// <summary>
        /// Kicks off an asynchronous or sync request to receive a response from the server.
        /// Uses the Encoding <code>encoding</code> to transform the bytes received into a string to be
        /// returned in the GeneralResponseDescription's StatusDescription field.
        /// </summary>
        private ResponseDescription? ReceiveCommandResponse()
        {
            // These are the things that will be needed to maintain state.
            ReceiveState state = new ReceiveState(this);

            try
            {
                // If a string of nonzero length was decoded from the buffered bytes after the last complete response, then we
                // will use this string as our first string to append to the response StatusBuffer, and we will
                // forego a Connection.Receive here.
                if (_buffer.Length > 0)
                {
                    ReceiveCommandResponseCallback(state, -1);
                }
                else
                {
                    int bytesRead;

                    try
                    {
                        if (_isAsync)
                        {
                            BeginRead(state.Buffer, 0, state.Buffer.Length, s_readCallbackDelegate, state);
                            return null;
                        }
                        else
                        {
                            bytesRead = Read(state.Buffer, 0, state.Buffer.Length);
                            if (bytesRead == 0)
                                CloseSocket();
                            ReceiveCommandResponseCallback(state, bytesRead);
                        }
                    }
                    catch (IOException)
                    {
                        MarkAsRecoverableFailure();
                        throw;
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                if (e is WebException)
                    throw;
                throw GenerateException(SR.net_ftp_receivefailure, WebExceptionStatus.ReceiveFailure, e);
            }
            return state.Resp;
        }

        /// <summary>
        /// ReceiveCommandResponseCallback is the main "while loop" of the ReceiveCommandResponse function family.
        /// In general, what is does is perform an EndReceive() to complete the previous retrieval of bytes from the
        /// server (unless it is using a buffered response)  It then processes what is received by using the
        /// implementing class's CheckValid() function, as described above. If the response is complete, it returns the single complete
        /// response in the GeneralResponseDescription created in BeginReceiveComamndResponse, and buffers the rest as described above.
        ///
        /// If the response is not complete, it issues another Connection.BeginReceive, with callback ReceiveCommandResponse2,
        /// so the action will continue at the next invocation of ReceiveCommandResponse2.
        /// </summary>
        private void ReceiveCommandResponseCallback(ReceiveState state, int bytesRead)
        {
            // completeLength will be set to a nonnegative number by CheckValid if the response is complete:
            // it will set completeLength to the length of a complete response.
            int completeLength = -1;

            while (true)
            {
                int validThrough = state.ValidThrough; // passed to checkvalid

                // If we have a Buffered response (ie data was received with the last response that was past the end of that response)
                // deal with it as if we had just received it now instead of actually doing another receive
                if (_buffer.Length > 0)
                {
                    // Append the string we got from the buffer, and flush it out.
                    state.Resp.StatusBuffer.Append(_buffer);
                    _buffer = string.Empty;

                    // invoke checkvalid.
                    if (!CheckValid(state.Resp, ref validThrough, ref completeLength))
                    {
                        throw GenerateException(SR.net_ftp_protocolerror, WebExceptionStatus.ServerProtocolViolation, null);
                    }
                }
                else // we did a Connection.BeginReceive.  Note that in this case, all bytes received are in the receive buffer (because bytes from
                     // the buffer were transferred there if necessary
                {
                    // this indicates the connection was closed.
                    if (bytesRead <= 0)
                    {
                        throw GenerateException(SR.net_ftp_protocolerror, WebExceptionStatus.ServerProtocolViolation, null);
                    }

                    // decode the bytes in the receive buffer into a string, append it to the statusbuffer, and invoke checkvalid.
                    // Decoder automatically takes care of caching partial codepoints at the end of a buffer.

                    char[] chars = new char[_decoder.GetCharCount(state.Buffer, 0, bytesRead)];
                    int numChars = _decoder.GetChars(state.Buffer, 0, bytesRead, chars, 0, false);

                    string szResponse = new string(chars, 0, numChars);

                    state.Resp.StatusBuffer.Append(szResponse);
                    if (!CheckValid(state.Resp, ref validThrough, ref completeLength))
                    {
                        throw GenerateException(SR.net_ftp_protocolerror, WebExceptionStatus.ServerProtocolViolation, null);
                    }

                    // If the response is complete, then determine how many characters are left over...these bytes need to be set into Buffer.
                    if (completeLength >= 0)
                    {
                        int unusedChars = state.Resp.StatusBuffer.Length - completeLength;
                        if (unusedChars > 0)
                        {
                            _buffer = szResponse.Substring(szResponse.Length - unusedChars, unusedChars);
                        }
                    }
                }

                // Now, in general, if the response is not complete, update the "valid through" length for the efficiency of checkValid,
                // and perform the next receive.
                // Note that there may NOT be bytes in the beginning of the receive buffer (even if there were partial characters left over after the
                // last encoding), because they get tracked in the Decoder.
                if (completeLength < 0)
                {
                    state.ValidThrough = validThrough;
                    try
                    {
                        if (_isAsync)
                        {
                            BeginRead(state.Buffer, 0, state.Buffer.Length, s_readCallbackDelegate, state);
                            return;
                        }
                        else
                        {
                            bytesRead = Read(state.Buffer, 0, state.Buffer.Length);
                            if (bytesRead == 0)
                                CloseSocket();
                            continue;
                        }
                    }
                    catch (IOException)
                    {
                        MarkAsRecoverableFailure();
                        throw;
                    }
                    catch
                    {
                        throw;
                    }
                }

                // The response is completed
                break;
            }

            // Otherwise, we have a complete response.
            string responseString = state.Resp.StatusBuffer.ToString();
            state.Resp.StatusDescription = responseString.Substring(0, completeLength);
            // Set the StatusDescription to the complete part of the response.  Note that the Buffer has already been taken care of above.

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Received response: {responseString.Substring(0, completeLength - 2)}");

            if (_isAsync)
            {
                // Tell who is listening what was received.
                if (state.Resp != null)
                {
                    _currentResponseDescription = state.Resp;
                }
                Stream? stream = null;
                if (PostReadCommandProcessing(ref stream))
                    return;
                ContinueCommandPipeline();
            }
        }
    } // class CommandStream

    /// <summary>
    /// Contains the parsed status line from the server
    /// </summary>
    internal sealed class ResponseDescription
    {
        internal const int NoStatus = -1;
        internal bool Multiline;

        internal int Status = NoStatus;
        internal string? StatusDescription;
        internal StringBuilder StatusBuffer = new StringBuilder();

        internal string? StatusCodeString;

        internal bool PositiveIntermediate { get { return (Status >= 100 && Status <= 199); } }
        internal bool PositiveCompletion { get { return (Status >= 200 && Status <= 299); } }
        internal bool TransientFailure { get { return (Status >= 400 && Status <= 499); } }
        internal bool PermanentFailure { get { return (Status >= 500 && Status <= 599); } }
        internal bool InvalidStatusCode { get { return (Status < 100 || Status > 599); } }
    }

    /// <summary>
    /// State information that is used during ReceiveCommandResponse()'s async operations
    /// </summary>
    internal sealed class ReceiveState
    {
        private const int bufferSize = 1024;

        internal ResponseDescription Resp;
        internal int ValidThrough;
        internal byte[] Buffer;
        internal CommandStream Connection;

        internal ReceiveState(CommandStream connection)
        {
            Connection = connection;
            Resp = new ResponseDescription();
            Buffer = new byte[bufferSize];  //1024
            ValidThrough = 0;
        }
    }
} // namespace System.Net
