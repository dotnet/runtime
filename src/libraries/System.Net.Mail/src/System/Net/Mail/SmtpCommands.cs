// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Net.Mime;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mail
{
    internal static class CheckCommand
    {
        internal static LineInfo Send(SmtpConnection conn)
        {
            Task<LineInfo> task = SendAsync<SyncReadWriteAdapter>(conn);
            Debug.Assert(task.IsCompleted, "CheckCommand.SendAsync should be completed synchronously.");
            return task.GetAwaiter().GetResult();
        }

        internal static async Task<LineInfo> SendAsync<TIOAdapter>(SmtpConnection conn, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            await conn.FlushAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);
            return await conn.Reader!.GetNextReplyReader().ReadLineAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);
        }
    }

    internal static class ReadLinesCommand
    {
        internal static LineInfo[] Send(SmtpConnection conn)
        {
            Task<LineInfo[]> task = SendAsync<SyncReadWriteAdapter>(conn);
            Debug.Assert(task.IsCompleted, "ReadLinesCommand.SendAsync should be completed synchronously.");
            return task.GetAwaiter().GetResult();
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(SendAsync<AsyncReadWriteAdapter>(conn), callback, state);
        }

        internal static LineInfo[] EndSend(IAsyncResult asyncResult)
        {
            return TaskToAsyncResult.End<LineInfo[]>(asyncResult);
        }

        internal static async Task<LineInfo[]> SendAsync<TIOAdapter>(SmtpConnection conn, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            await conn.FlushAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);
            return await conn.Reader!.GetNextReplyReader().ReadLinesAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);
        }
    }

    internal static class AuthCommand
    {
        internal static LineInfo Send(SmtpConnection conn, string type, string message)
        {
            Task<LineInfo> task = SendAsync<SyncReadWriteAdapter>(conn, type, message);
            Debug.Assert(task.IsCompleted, "AuthCommand.SendAsync should be completed synchronously.");
            return task.GetAwaiter().GetResult();
        }

        internal static LineInfo Send(SmtpConnection conn, string? message)
        {
            Task<LineInfo> task = SendAsync<SyncReadWriteAdapter>(conn, message);
            Debug.Assert(task.IsCompleted, "AuthCommand.SendAsync should be completed synchronously.");
            return task.GetAwaiter().GetResult();
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, string type, string message, AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(SendAsync<AsyncReadWriteAdapter>(conn, type, message), callback, state);
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, string? message, AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(SendAsync<AsyncReadWriteAdapter>(conn, message), callback, state);
        }

        internal static LineInfo EndSend(IAsyncResult asyncResult)
        {
            return TaskToAsyncResult.End<LineInfo>(asyncResult);
        }

        internal static async Task<LineInfo> SendAsync<TIOAdapter>(SmtpConnection conn, string type, string message, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            PrepareCommand(conn, type, message);
            LineInfo[] lines = await ReadLinesCommand.SendAsync<TIOAdapter>(conn, cancellationToken).ConfigureAwait(false);
            return CheckResponse(lines);
        }

        internal static async Task<LineInfo> SendAsync<TIOAdapter>(SmtpConnection conn, string? message, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            PrepareCommand(conn, message);
            LineInfo[] lines = await ReadLinesCommand.SendAsync<TIOAdapter>(conn, cancellationToken).ConfigureAwait(false);
            return CheckResponse(lines);
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

        private static void PrepareCommand(SmtpConnection conn, string type, string message)
        {
            conn.BufferBuilder.Append(SmtpCommands.Auth);
            conn.BufferBuilder.Append(type);
            conn.BufferBuilder.Append((byte)' ');
            conn.BufferBuilder.Append(message);
            conn.BufferBuilder.Append(SmtpCommands.CRLF);
        }

        private static void PrepareCommand(SmtpConnection conn, string? message)
        {
            conn.BufferBuilder.Append(message);
            conn.BufferBuilder.Append(SmtpCommands.CRLF);
        }
    }

    internal static class DataCommand
    {
        internal static void Send(SmtpConnection conn)
        {
            Task task = SendAsync<SyncReadWriteAdapter>(conn);
            Debug.Assert(task.IsCompleted, "DataCommand.SendAsync should be completed synchronously.");
            task.GetAwaiter().GetResult();
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(SendAsync<AsyncReadWriteAdapter>(conn), callback, state);
        }

        internal static void EndSend(IAsyncResult asyncResult)
        {
            TaskToAsyncResult.End(asyncResult);
        }

        internal static async Task SendAsync<TIOAdapter>(SmtpConnection conn, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            PrepareCommand(conn);
            LineInfo info = await CheckCommand.SendAsync<TIOAdapter>(conn, cancellationToken).ConfigureAwait(false);
            CheckResponse(info.StatusCode, info.Line);
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

        private static void PrepareCommand(SmtpConnection conn)
        {
            if (conn.IsStreamOpen)
            {
                throw new InvalidOperationException(SR.SmtpDataStreamOpen);
            }

            conn.BufferBuilder.Append(SmtpCommands.Data);
        }
    }

    internal static class DataStopCommand
    {
        internal static void Send(SmtpConnection conn)
        {
            Task task = SendAsync<SyncReadWriteAdapter>(conn);
            Debug.Assert(task.IsCompleted, "DataStopCommand.SendAsync should be completed synchronously.");
            task.GetAwaiter().GetResult();
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(SendAsync<AsyncReadWriteAdapter>(conn), callback, state);
        }

        internal static void EndSend(IAsyncResult asyncResult)
        {
            TaskToAsyncResult.End(asyncResult);
        }

        internal static async Task SendAsync<TIOAdapter>(SmtpConnection conn, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            PrepareCommand(conn);
            LineInfo info = await CheckCommand.SendAsync<TIOAdapter>(conn, cancellationToken).ConfigureAwait(false);
            CheckResponse(info.StatusCode, info.Line);
        }

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
    }

    internal static class EHelloCommand
    {
        internal static string[] Send(SmtpConnection conn, string domain)
        {
            Task<string[]> task = SendAsync<SyncReadWriteAdapter>(conn, domain);
            Debug.Assert(task.IsCompleted, "EHelloCommand.SendAsync should be completed synchronously.");
            return task.GetAwaiter().GetResult();
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, string domain, AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(SendAsync<AsyncReadWriteAdapter>(conn, domain), callback, state);
        }

        internal static string[] EndSend(IAsyncResult asyncResult)
        {
            return TaskToAsyncResult.End<string[]>(asyncResult);
        }

        internal static async Task<string[]> SendAsync<TIOAdapter>(SmtpConnection conn, string domain, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            PrepareCommand(conn, domain);
            LineInfo[] lines = await ReadLinesCommand.SendAsync<TIOAdapter>(conn, cancellationToken).ConfigureAwait(false);
            return CheckResponse(lines);
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
    }

    internal static class HelloCommand
    {
        internal static void Send(SmtpConnection conn, string domain)
        {
            Task task = SendAsync<SyncReadWriteAdapter>(conn, domain);
            Debug.Assert(task.IsCompleted, "HelloCommand.SendAsync should be completed synchronously.");
            task.GetAwaiter().GetResult();
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, string domain, AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(SendAsync<AsyncReadWriteAdapter>(conn, domain), callback, state);
        }

        internal static void EndSend(IAsyncResult asyncResult)
        {
            TaskToAsyncResult.End(asyncResult);
        }

        internal static async Task SendAsync<TIOAdapter>(SmtpConnection conn, string domain, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            PrepareCommand(conn, domain);
            LineInfo info = await CheckCommand.SendAsync<TIOAdapter>(conn, cancellationToken).ConfigureAwait(false);
            CheckResponse(info.StatusCode, info.Line);
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
    }

    internal static class StartTlsCommand
    {
        internal static void Send(SmtpConnection conn)
        {
            Task task = SendAsync<SyncReadWriteAdapter>(conn);
            Debug.Assert(task.IsCompleted, "StartTlsCommand.SendAsync should be completed synchronously.");
            task.GetAwaiter().GetResult();
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(SendAsync<AsyncReadWriteAdapter>(conn), callback, state);
        }

        internal static void EndSend(IAsyncResult asyncResult)
        {
            TaskToAsyncResult.End(asyncResult);
        }

        internal static async Task SendAsync<TIOAdapter>(SmtpConnection conn, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            PrepareCommand(conn);
            LineInfo info = await CheckCommand.SendAsync<TIOAdapter>(conn, cancellationToken).ConfigureAwait(false);
            CheckResponse(info.StatusCode, info.Line);
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

        private static void PrepareCommand(SmtpConnection conn)
        {
            if (conn.IsStreamOpen)
            {
                throw new InvalidOperationException(SR.SmtpDataStreamOpen);
            }

            conn.BufferBuilder.Append(SmtpCommands.StartTls);
            conn.BufferBuilder.Append(SmtpCommands.CRLF);
        }
    }

    internal static class MailCommand
    {
        internal static void Send(SmtpConnection conn, ReadOnlySpan<byte> command, MailAddress from, bool allowUnicode)
        {
            Task task = SendAsync<SyncReadWriteAdapter>(conn, command, from, allowUnicode);
            Debug.Assert(task.IsCompleted, "MailCommand.SendAsync should be completed synchronously.");
            task.GetAwaiter().GetResult();
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, ReadOnlySpan<byte> command, MailAddress from, bool allowUnicode, AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(SendAsync<AsyncReadWriteAdapter>(conn, command, from, allowUnicode), callback, state);
        }

        internal static void EndSend(IAsyncResult asyncResult)
        {
            TaskToAsyncResult.End(asyncResult);
        }

        internal static Task SendAsync<TIOAdapter>(SmtpConnection conn, ReadOnlySpan<byte> command, MailAddress from, bool allowUnicode, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            PrepareCommand(conn, command, from, allowUnicode);
            return SendAndCheck(conn, cancellationToken);

            static async Task<LineInfo> SendAndCheck(SmtpConnection conn, CancellationToken cancellationToken)
            {
                LineInfo info = await CheckCommand.SendAsync<TIOAdapter>(conn, cancellationToken).ConfigureAwait(false);
                CheckResponse(info.StatusCode, info.Line);
                return info;
            }
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

        private static void PrepareCommand(SmtpConnection conn, ReadOnlySpan<byte> command, MailAddress from, bool allowUnicode)
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
    }

    internal static class RecipientCommand
    {
        internal static bool Send(SmtpConnection conn, string to, out string response)
        {
            Task<(bool success, string response)> task = SendAsync<SyncReadWriteAdapter>(conn, to);
            Debug.Assert(task.IsCompleted, "RecipientCommand.SendAsync should be completed synchronously.");
            (bool success, string r) = task.GetAwaiter().GetResult();
            response = r;
            return success;
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, string to, AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(SendAsync<AsyncReadWriteAdapter>(conn, to), callback, state);
        }

        internal static bool EndSend(IAsyncResult asyncResult, out string response)
        {
            (bool success, string r) = TaskToAsyncResult.End<(bool success, string response)>(asyncResult);
            response = r;
            return success;
        }

        internal static async Task<(bool success, string response)> SendAsync<TIOAdapter>(SmtpConnection conn, string to, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            PrepareCommand(conn, to);
            LineInfo info = await CheckCommand.SendAsync<TIOAdapter>(conn, cancellationToken).ConfigureAwait(false);
            return (CheckResponse(info.StatusCode, info.Line), info.Line);
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
    }

    internal static class QuitCommand
    {
        internal static void Send(SmtpConnection conn)
        {
            Task task = SendAsync<SyncReadWriteAdapter>(conn);
            Debug.Assert(task.IsCompleted, "QuitCommand.SendAsync should be completed synchronously.");
            task.GetAwaiter().GetResult();
        }

        internal static IAsyncResult BeginSend(SmtpConnection conn, AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(SendAsync<AsyncReadWriteAdapter>(conn), callback, state);
        }

        internal static void EndSend(IAsyncResult asyncResult)
        {
            TaskToAsyncResult.End(asyncResult);
        }

        internal static async Task SendAsync<TIOAdapter>(SmtpConnection conn, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            PrepareCommand(conn);
            await conn.FlushAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);
            // We don't read any response to match the synchronous behavior
        }

        private static void PrepareCommand(SmtpConnection conn)
        {
            if (conn.IsStreamOpen)
            {
                throw new InvalidOperationException(SR.SmtpDataStreamOpen);
            }

            conn.BufferBuilder.Append(SmtpCommands.Quit);
        }
    }

    internal static class SmtpCommands
    {
        internal static ReadOnlySpan<byte> Auth => "AUTH "u8;
        internal static ReadOnlySpan<byte> CRLF => "\r\n"u8;
        internal static ReadOnlySpan<byte> Data => "DATA\r\n"u8;
        internal static ReadOnlySpan<byte> DataStop => "\r\n.\r\n"u8;
        internal static ReadOnlySpan<byte> EHello => "EHLO "u8;
        internal static ReadOnlySpan<byte> Expand => "EXPN "u8;
        internal static ReadOnlySpan<byte> Hello => "HELO "u8;
        internal static ReadOnlySpan<byte> Help => "HELP"u8;
        internal static ReadOnlySpan<byte> Mail => "MAIL FROM:"u8;
        internal static ReadOnlySpan<byte> Noop => "NOOP\r\n"u8;
        internal static ReadOnlySpan<byte> Quit => "QUIT\r\n"u8;
        internal static ReadOnlySpan<byte> Recipient => "RCPT TO:"u8;
        internal static ReadOnlySpan<byte> Reset => "RSET\r\n"u8;
        internal static ReadOnlySpan<byte> Send => "SEND FROM:"u8;
        internal static ReadOnlySpan<byte> SendAndMail => "SAML FROM:"u8;
        internal static ReadOnlySpan<byte> SendOrMail => "SOML FROM:"u8;
        internal static ReadOnlySpan<byte> Turn => "TURN\r\n"u8;
        internal static ReadOnlySpan<byte> Verify => "VRFY "u8;
        internal static ReadOnlySpan<byte> StartTls => "STARTTLS"u8;
    }

    internal readonly struct LineInfo
    {
        internal LineInfo(SmtpStatusCode statusCode, string line)
        {
            StatusCode = statusCode;
            Line = line;
        }
        internal string Line { get; }
        internal SmtpStatusCode StatusCode { get; }
    }
}
