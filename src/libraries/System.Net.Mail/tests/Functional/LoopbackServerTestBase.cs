// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Mail.Tests;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Mail.Tests
{
    public enum SendMethod
    {
        Send,
        SendAsync,
        SendMailAsync
    }

    public interface ISendMethodProvider
    {
        static abstract SendMethod SendMethod { get; }
    }

    public struct SyncSendMethod : ISendMethodProvider
    {
        public static SendMethod SendMethod => SendMethod.Send;
    }

    public struct AsyncSendMethod : ISendMethodProvider
    {
        public static SendMethod SendMethod => SendMethod.SendAsync;
    }

    public struct SendMailAsyncMethod : ISendMethodProvider
    {
        public static SendMethod SendMethod => SendMethod.SendMailAsync;
    }

    public abstract class LoopbackServerTestBase<T> : IDisposable
        where T : ISendMethodProvider
    {
        protected LoopbackSmtpServer Server;
        protected ITestOutputHelper Output;

        public LoopbackServerTestBase(ITestOutputHelper output)
        {
            Output = output;
            Server = new LoopbackSmtpServer(Output);
        }

        private async Task<Exception?> SendMailInternal(SmtpClient client, MailMessage msg)
        {
            switch (T.SendMethod)
            {
                case SendMethod.Send:
                    try
                    {
                        client.Send(msg);
                        return null;
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }

                case SendMethod.SendAsync:
                    TaskCompletionSource<Exception?> tcs = new TaskCompletionSource<Exception?>();
                    SendCompletedEventHandler handler = null!;
                    handler = (s, e) =>
                    {
                        client.SendCompleted -= handler;

                        if (e.Error != null)
                        {
                            tcs.SetResult(e.Error);
                        }
                        else if (e.Cancelled)
                        {
                            tcs.SetResult(new OperationCanceledException("The operation was canceled."));
                        }
                        else
                        {
                            tcs.SetResult(null);
                        }
                    };
                    client.SendCompleted += handler;
                    client.SendAsync(msg, tcs);
                    return await tcs.Task;

                case SendMethod.SendMailAsync:
                    try
                    {
                        await client.SendMailAsync(msg);
                        return null;
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected async Task SendMail(SmtpClient client, MailMessage msg)
        {
            Exception? ex = await SendMailInternal(client, msg);
            Assert.Null(ex);
        }

        protected async Task<TException> SendMail<TException>(SmtpClient client, MailMessage msg) where TException : Exception
        {
            Exception? ex = await SendMailInternal(client, msg);

            if (T.SendMethod != SendMethod.Send && typeof(TException) != typeof(SmtpException))
            {
                ex = Assert.IsType<SmtpException>(ex).InnerException;
            }

            return Assert.IsType<TException>(ex);
        }

        public void Dispose()
        {
            Server?.Dispose();
        }
    }
}