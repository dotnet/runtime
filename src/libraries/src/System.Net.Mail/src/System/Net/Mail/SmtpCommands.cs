// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Net.Mime;
using System.Runtime.ExceptionServices;
using System.Text;

namespace System.Net.Mail
{
    internal static class CheckCommand
    {
        private static readonly AsyncCallback s_onReadLine = new AsyncCallback(OnReadLine);
        private static readonly AsyncCallback s_onWrite = new AsyncCallback(OnWrite);

        internal static IAsyncResult BeginSend(SmtpConnection conn, AsyncCallback callback, object state)
        {
            MultiAsyncResult multiResult = new MultiAsyncResult(conn, callback, state);
            multiResult.Enter();
            IAsyncResult writeResult = conn.BeginFlush(s_onWrite, multiResult);
            if (writeResult.CompletedSynchronously)
            {
                conn.EndFlush(writeResult);
                multiResult.Leave();
            }
            SmtpReplyReader reader = conn.Reader.GetNextReplyReader();
            multiResult.Enter();

            //this actually does a read on the stream.
            IAsyncResult result = reader.BeginReadLine(s_onReadLine, multiResult);
            if (result.CompletedSynchronously)
            {
                LineInfo info = reader.EndReadLine(result);
                if (!(multiResult.Result is Exception))
                    multiResult.Result = info;
                multiResult.Leave();
            }
            multiResult.CompleteSequence();
            return multiResult;
        }


        internal static object EndSend(IAsyncResult result, out string response)
        {
            object commandResult = MultiAsyncResult.End(result);
            if (commandResult is Exception e)
            {
                ExceptionDispatchInfo.Throw(e);
            }

            LineInfo info = (LineInfo)commandResult;
            response = info.Line;
            return info.StatusCode;
        }

        private static void OnReadLine(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                MultiAsyncResult multiResult = (MultiAsyncResult)result.AsyncState;
                try
                {
                    SmtpConnection conn = (SmtpConnection)multiResult.Context;
                    LineInfo info = conn.Reader.CurrentReader.EndReadLine(result);
                    if (!(multiResult.Result is Exception))
                        multiResult.Result = info;
                    multiResult.Leave();
                }
                catch (Exception e)
                {
                    multiResult.Leave(e);
                }
            }
        }

        private static void OnWrite(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                MultiAsyncResult multiResult = (MultiAsyncResult)result.AsyncState;
                try
                {
                    SmtpConnection conn = (SmtpConnection)multiResult.Context;
                    conn.EndFlush(result);
                    multiResult.Leave();
                }
                catch (Exception e)
                {
                    multiResult.Leave(e);
                }
            }
        }

        internal static SmtpStatusCode Send(SmtpConnection conn, out string response)
        {
            conn.Flush();
            SmtpReplyReader reader = conn.Reader.GetNextReplyReader();
            LineInfo info = reader.ReadLine();
            response = info.Line;
            reader.Close();
            return info.StatusCode;
        }
    }

    internal static class ReadLinesCommand
    {
        private static readonly AsyncCallback s_onReadLines = new AsyncCallback(OnReadLines);
        private static readonly AsyncCallback s_onWrite = new AsyncCallback(OnWrite);

        internal static IAsyncResult BeginSend(SmtpConnection conn, AsyncCallback callback, object state)
        {
            MultiAsyncResult multiResult = new MultiAsyncResult(conn, callback, state);
            multiResult.Enter();
            IAsyncResult writeResult = conn.BeginFlush(s_onWrite, multiResult);
            if (writeResult.CompletedSynchronously)
            {
                conn.EndFlush(writeResult);
                multiResult.Leave();
            }
            SmtpReplyReader reader = conn.Reader.GetNextReplyReader();
            multiResult.Enter();
            IAsyncResult readLinesResult = reader.BeginReadLines(s_onReadLines, multiResult);
            if (readLinesResult.CompletedSynchronously)
            {
                LineInfo[] lines = conn.Reader.CurrentReader.EndReadLines(readLinesResult);
                if (!(multiResult.Result is Exception))
                    multiResult.Result = lines;
                multiResult.Leave();
            }
            multiResult.CompleteSequence();
            return multiResult;
        }

        internal static LineInfo[] EndSend(IAsyncResult result)
        {
            object commandResult = MultiAsyncResult.End(result);
            if (commandResult is Exception e)
            {
                ExceptionDispatchInfo.Throw(e);
            }
            return (LineInfo[])commandResult;
        }

        private static void OnReadLines(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                MultiAsyncResult multiResult = (MultiAsyncResult)result.AsyncState;
                try
                {
                    SmtpConnection conn = (SmtpConnection)multiResult.Context;
                    LineInfo[] lines = conn.Reader.CurrentReader.EndReadLines(result);
                    if (!(multiResult.Result is Exception))
                        multiResult.Result = lines;
                    multiResult.Leave();
                }
                catch (Exception e)
                {
                    multiResult.Leave(e);
                }
            }
        }

        private static void OnWrite(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                MultiAsyncResult multiResult = (MultiAsyncResult)result.AsyncState;
                try
                {
                    SmtpConnection conn = (SmtpConnection)multiResult.Context;
                    conn.EndFlush(result);
                    multiResult.Leave();
                }
                catch (Exception e)
                {
                    multiResult.Leave(e);
                }
            }
        }
        internal static LineInfo[] Send(SmtpConnection conn)
        {
            conn.Flush();
            return conn.Reader.GetNextReplyReader().ReadLines();
        }
    }

    internal static class AuthCommand
    {
        internal static IAsyncResult BeginSend(SmtpConnection conn, string type, string message, AsyncCallback callback, object state)
        {
            PrepareCommand(conn, type, message);
            return ReadLinesCommand.BeginSend(conn, callback, state);
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, string message, AsyncCallback callback, object state)
        {
            PrepareCommand(conn, message);
            return ReadLinesCommand.BeginSend(conn, callback, state);
        }

        private static LineInfo CheckResponse(LineInfo[] lines)
        {
            if (lines == null || lines.Length == 0)
            {
                throw new SmtpException(SR.SmtpAuthResponseInvalid);
            }
            System.Diagnostics.Debug.Assert(lines.Length == 1, "Did not expect more than one line response for auth command");
            return lines[0];
        }

        internal static LineInfo EndSend(IAsyncResult result)
        {
            return CheckResponse(ReadLinesCommand.EndSend(result));
        }
        private static void PrepareCommand(SmtpConnection conn, string type, string message)
        {
            conn.BufferBuilder.Append(SmtpCommands.Auth);
            conn.BufferBuilder.Append(type);
            conn.BufferBuilder.Append((byte)' ');
            conn.BufferBuilder.Append(message);
            conn.BufferBuilder.Append(SmtpCommands.CRLF);
        }

        private static void PrepareCommand(SmtpConnection conn, string message)
        {
            conn.BufferBuilder.Append(message);
            conn.BufferBuilder.Append(SmtpCommands.CRLF);
        }

        internal static LineInfo Send(SmtpConnection conn, string type, string message)
        {
            PrepareCommand(conn, type, message);
            return CheckResponse(ReadLinesCommand.Send(conn));
        }

        internal static LineInfo Send(SmtpConnection conn, string message)
        {
            PrepareCommand(conn, message);
            return CheckResponse(ReadLinesCommand.Send(conn));
        }
    }

    internal static class DataCommand
    {
        internal static IAsyncResult BeginSend(SmtpConnection conn, AsyncCallback callback, object state)
        {
            PrepareCommand(conn);
            return CheckCommand.BeginSend(conn, callback, state);
        }

        private static void CheckResponse(SmtpStatusCode statusCode, string serverResponse)
        {
            switch (statusCode)
            {
                case SmtpStatusCode.StartMailInput:
                    {
                        return;
                    }
                case SmtpStatusCode.LocalErrorInProcessing:
                case SmtpStatusCode.TransactionFailed:
                default:
                    {
                        if ((int)statusCode < 400)
                        {
                            throw new SmtpException(SR.net_webstatus_ServerProtocolViolation, serverResponse);
                        }

                        throw new SmtpException(statusCode, serverResponse, true);
                    }
            }
        }

        internal static void EndSend(IAsyncResult result)
        {
            string response;
            SmtpStatusCode statusCode = (SmtpStatusCode)CheckCommand.EndSend(result, out response);
            CheckResponse(statusCode, response);
        }

        private static void PrepareCommand(SmtpConnection conn)
        {
            if (conn.IsStreamOpen)
            {
                throw new InvalidOperationException(SR.SmtpDataStreamOpen);
            }

            conn.BufferBuilder.Append(SmtpCommands.Data);
        }

        internal static void Send(SmtpConnection conn)
        {
            PrepareCommand(conn);
            string response;
            SmtpStatusCode statusCode = CheckCommand.Send(conn, out response);
            CheckResponse(statusCode, response);
        }
    }

    internal static class DataStopCommand
    {
        private static void CheckResponse(SmtpStatusCode statusCode, string serverResponse)
        {
            switch (statusCode)
            {
                case SmtpStatusCode.Ok:
                    {
                        return;
                    }
                case SmtpStatusCode.ExceededStorageAllocation:
                case SmtpStatusCode.TransactionFailed:
                case SmtpStatusCode.LocalErrorInProcessing:
                case SmtpStatusCode.InsufficientStorage:
                default:
                    {
                        if ((int)statusCode < 400)
                        {
                            throw new SmtpException(SR.net_webstatus_ServerProtocolViolation, serverResponse);
                        }

                        throw new SmtpException(statusCode, serverResponse, true);
                    }
            }
        }

        private static void PrepareCommand(SmtpConnection conn)
        {
            if (conn.IsStreamOpen)
            {
                throw new InvalidOperationException(SR.SmtpDataStreamOpen);
            }

            conn.BufferBuilder.Append(SmtpCommands.DataStop);
        }
        internal static void Send(SmtpConnection conn)
        {
            PrepareCommand(conn);
            string response;
            SmtpStatusCode statusCode = CheckCommand.Send(conn, out response);
            CheckResponse(statusCode, response);
        }
    }

    internal static class EHelloCommand
    {
        internal static IAsyncResult BeginSend(SmtpConnection conn, string domain, AsyncCallback callback, object state)
        {
            PrepareCommand(conn, domain);
            return ReadLinesCommand.BeginSend(conn, callback, state);
        }

        private static string[] CheckResponse(LineInfo[] lines)
        {
            if (lines == null || lines.Length == 0)
            {
                throw new SmtpException(SR.SmtpEhloResponseInvalid);
            }
            if (lines[0].StatusCode != SmtpStatusCode.Ok)
            {
                if ((int)lines[0].StatusCode < 400)
                {
                    throw new SmtpException(SR.net_webstatus_ServerProtocolViolation, lines[0].Line);
                }

                throw new SmtpException(lines[0].StatusCode, lines[0].Line, true);
            }
            string[] extensions = new string[lines.Length - 1];
            for (int i = 1; i < lines.Length; i++)
            {
                extensions[i - 1] = lines[i].Line;
            }
            return extensions;
        }

        internal static string[] EndSend(IAsyncResult result)
        {
            return CheckResponse(ReadLinesCommand.EndSend(result));
        }
        private static void PrepareCommand(SmtpConnection conn, string domain)
        {
            if (conn.IsStreamOpen)
            {
                throw new InvalidOperationException(SR.SmtpDataStreamOpen);
            }

            conn.BufferBuilder.Append(SmtpCommands.EHello);
            conn.BufferBuilder.Append(domain);
            conn.BufferBuilder.Append(SmtpCommands.CRLF);
        }

        internal static string[] Send(SmtpConnection conn, string domain)
        {
            PrepareCommand(conn, domain);
            return CheckResponse(ReadLinesCommand.Send(conn));
        }
    }

    internal static class HelloCommand
    {
        internal static IAsyncResult BeginSend(SmtpConnection conn, string domain, AsyncCallback callback, object state)
        {
            PrepareCommand(conn, domain);
            return CheckCommand.BeginSend(conn, callback, state);
        }

        private static void CheckResponse(SmtpStatusCode statusCode, string serverResponse)
        {
            switch (statusCode)
            {
                case SmtpStatusCode.Ok:
                    {
                        return;
                    }
                default:
                    {
                        if ((int)statusCode < 400)
                        {
                            throw new SmtpException(SR.net_webstatus_ServerProtocolViolation, serverResponse);
                        }

                        throw new SmtpException(statusCode, serverResponse, true);
                    }
            }
        }

        internal static void EndSend(IAsyncResult result)
        {
            string response;
            SmtpStatusCode statusCode = (SmtpStatusCode)CheckCommand.EndSend(result, out response);
            CheckResponse(statusCode, response);
        }

        private static void PrepareCommand(SmtpConnection conn, string domain)
        {
            if (conn.IsStreamOpen)
            {
                throw new InvalidOperationException(SR.SmtpDataStreamOpen);
            }

            conn.BufferBuilder.Append(SmtpCommands.Hello);
            conn.BufferBuilder.Append(domain);
            conn.BufferBuilder.Append(SmtpCommands.CRLF);
        }

        internal static void Send(SmtpConnection conn, string domain)
        {
            PrepareCommand(conn, domain);
            string response;
            SmtpStatusCode statusCode = CheckCommand.Send(conn, out response);
            CheckResponse(statusCode, response);
        }
    }

    internal static class StartTlsCommand
    {
        internal static IAsyncResult BeginSend(SmtpConnection conn, AsyncCallback callback, object state)
        {
            PrepareCommand(conn);
            return CheckCommand.BeginSend(conn, callback, state);
        }

        private static void CheckResponse(SmtpStatusCode statusCode, string response)
        {
            switch (statusCode)
            {
                case SmtpStatusCode.ServiceReady:
                    {
                        return;
                    }

                case SmtpStatusCode.ClientNotPermitted:
                default:
                    {
                        if ((int)statusCode < 400)
                        {
                            throw new SmtpException(SR.net_webstatus_ServerProtocolViolation, response);
                        }

                        throw new SmtpException(statusCode, response, true);
                    }
            }
        }

        internal static void EndSend(IAsyncResult result)
        {
            string response;
            SmtpStatusCode statusCode = (SmtpStatusCode)CheckCommand.EndSend(result, out response);
            CheckResponse(statusCode, response);
        }

        private static void PrepareCommand(SmtpConnection conn)
        {
            if (conn.IsStreamOpen)
            {
                throw new InvalidOperationException(SR.SmtpDataStreamOpen);
            }

            conn.BufferBuilder.Append(SmtpCommands.StartTls);
            conn.BufferBuilder.Append(SmtpCommands.CRLF);
        }

        internal static void Send(SmtpConnection conn)
        {
            PrepareCommand(conn);
            string response;
            SmtpStatusCode statusCode = CheckCommand.Send(conn, out response);
            CheckResponse(statusCode, response);
        }
    }

    internal static class MailCommand
    {
        internal static IAsyncResult BeginSend(SmtpConnection conn, byte[] command, MailAddress from,
            bool allowUnicode, AsyncCallback callback, object state)
        {
            PrepareCommand(conn, command, from, allowUnicode);
            return CheckCommand.BeginSend(conn, callback, state);
        }

        private static void CheckResponse(SmtpStatusCode statusCode, string response)
        {
            switch (statusCode)
            {
                case SmtpStatusCode.Ok:
                    {
                        return;
                    }
                case SmtpStatusCode.ExceededStorageAllocation:
                case SmtpStatusCode.LocalErrorInProcessing:
                case SmtpStatusCode.InsufficientStorage:
                default:
                    {
                        if ((int)statusCode < 400)
                        {
                            throw new SmtpException(SR.net_webstatus_ServerProtocolViolation, response);
                        }

                        throw new SmtpException(statusCode, response, true);
                    }
            }
        }

        internal static void EndSend(IAsyncResult result)
        {
            string response;
            SmtpStatusCode statusCode = (SmtpStatusCode)CheckCommand.EndSend(result, out response);
            CheckResponse(statusCode, response);
        }

        private static void PrepareCommand(SmtpConnection conn, byte[] command, MailAddress from, bool allowUnicode)
        {
            if (conn.IsStreamOpen)
            {
                throw new InvalidOperationException(SR.SmtpDataStreamOpen);
            }
            conn.BufferBuilder.Append(command);
            string fromString = from.GetSmtpAddress(allowUnicode);
            conn.BufferBuilder.Append(fromString, allowUnicode);
            if (allowUnicode)
            {
                conn.BufferBuilder.Append(" BODY=8BITMIME SMTPUTF8");
            }
            conn.BufferBuilder.Append(SmtpCommands.CRLF);
        }

        internal static void Send(SmtpConnection conn, byte[] command, MailAddress from, bool allowUnicode)
        {
            PrepareCommand(conn, command, from, allowUnicode);
            string response;
            SmtpStatusCode statusCode = CheckCommand.Send(conn, out response);
            CheckResponse(statusCode, response);
        }
    }

    internal static class RecipientCommand
    {
        internal static IAsyncResult BeginSend(SmtpConnection conn, string to, AsyncCallback callback, object state)
        {
            PrepareCommand(conn, to);
            return CheckCommand.BeginSend(conn, callback, state);
        }

        private static bool CheckResponse(SmtpStatusCode statusCode, string response)
        {
            switch (statusCode)
            {
                case SmtpStatusCode.Ok:
                case SmtpStatusCode.UserNotLocalWillForward:
                    {
                        return true;
                    }
                case SmtpStatusCode.MailboxUnavailable:
                case SmtpStatusCode.UserNotLocalTryAlternatePath:
                case SmtpStatusCode.ExceededStorageAllocation:
                case SmtpStatusCode.MailboxNameNotAllowed:
                case SmtpStatusCode.MailboxBusy:
                case SmtpStatusCode.InsufficientStorage:
                    {
                        return false;
                    }
                default:
                    {
                        if ((int)statusCode < 400)
                        {
                            throw new SmtpException(SR.net_webstatus_ServerProtocolViolation, response);
                        }

                        throw new SmtpException(statusCode, response, true);
                    }
            }
        }

        internal static bool EndSend(IAsyncResult result, out string response)
        {
            SmtpStatusCode statusCode = (SmtpStatusCode)CheckCommand.EndSend(result, out response);
            return CheckResponse(statusCode, response);
        }

        private static void PrepareCommand(SmtpConnection conn, string to)
        {
            if (conn.IsStreamOpen)
            {
                throw new InvalidOperationException(SR.SmtpDataStreamOpen);
            }

            conn.BufferBuilder.Append(SmtpCommands.Recipient);
            conn.BufferBuilder.Append(to, true); // Unicode validation was done prior
            conn.BufferBuilder.Append(SmtpCommands.CRLF);
        }


        internal static bool Send(SmtpConnection conn, string to, out string response)
        {
            PrepareCommand(conn, to);
            SmtpStatusCode statusCode = CheckCommand.Send(conn, out response);
            return CheckResponse(statusCode, response);
        }
    }


    internal static class SmtpCommands
    {
        internal static readonly byte[] Auth = Encoding.ASCII.GetBytes("AUTH ");
        internal static readonly byte[] CRLF = Encoding.ASCII.GetBytes("\r\n");
        internal static readonly byte[] Data = Encoding.ASCII.GetBytes("DATA\r\n");
        internal static readonly byte[] DataStop = Encoding.ASCII.GetBytes("\r\n.\r\n");
        internal static readonly byte[] EHello = Encoding.ASCII.GetBytes("EHLO ");
        internal static readonly byte[] Expand = Encoding.ASCII.GetBytes("EXPN ");
        internal static readonly byte[] Hello = Encoding.ASCII.GetBytes("HELO ");
        internal static readonly byte[] Help = Encoding.ASCII.GetBytes("HELP");
        internal static readonly byte[] Mail = Encoding.ASCII.GetBytes("MAIL FROM:");
        internal static readonly byte[] Noop = Encoding.ASCII.GetBytes("NOOP\r\n");
        internal static readonly byte[] Quit = Encoding.ASCII.GetBytes("QUIT\r\n");
        internal static readonly byte[] Recipient = Encoding.ASCII.GetBytes("RCPT TO:");
        internal static readonly byte[] Reset = Encoding.ASCII.GetBytes("RSET\r\n");
        internal static readonly byte[] Send = Encoding.ASCII.GetBytes("SEND FROM:");
        internal static readonly byte[] SendAndMail = Encoding.ASCII.GetBytes("SAML FROM:");
        internal static readonly byte[] SendOrMail = Encoding.ASCII.GetBytes("SOML FROM:");
        internal static readonly byte[] Turn = Encoding.ASCII.GetBytes("TURN\r\n");
        internal static readonly byte[] Verify = Encoding.ASCII.GetBytes("VRFY ");
        internal static readonly byte[] StartTls = Encoding.ASCII.GetBytes("STARTTLS");
    }



    internal struct LineInfo
    {
        private string _line;
        private SmtpStatusCode _statusCode;

        internal LineInfo(SmtpStatusCode statusCode, string line)
        {
            _statusCode = statusCode;
            _line = line;
        }
        internal string Line
        {
            get
            {
                return _line;
            }
        }
        internal SmtpStatusCode StatusCode
        {
            get
            {
                return _statusCode;
            }
        }
    }
}
