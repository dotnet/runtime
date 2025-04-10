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
        protected LoopbackSmtpServer Server { get; private set; }
        protected ITestOutputHelper Output { get; private set; }

        private SmtpClient _smtp;

        protected SmtpClient Smtp
        {
            get
            {
                return _smtp ??= Server.CreateClient();
            }
        }

        public LoopbackServerTestBase(ITestOutputHelper output)
        {
            Output = output;
            Server = new LoopbackSmtpServer(Output);
        }

        private Task<Exception?> SendMailInternal(MailMessage msg)
        {
            switch (T.SendMethod)
            {
                case SendMethod.Send:
                    try
                    {
                        Smtp.Send(msg);
                        return Task.FromResult<Exception>(null);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(ex);
                    }

                case SendMethod.SendAsync:
                    TaskCompletionSource<Exception?> tcs = new TaskCompletionSource<Exception?>();
                    SendCompletedEventHandler handler = null!;
                    handler = (s, e) =>
                    {
                        Smtp.SendCompleted -= handler;

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
                    Smtp.SendCompleted += handler;
                    try
                    {
                        Smtp.SendAsync(msg, tcs);
                        return tcs.Task;
                    }
                    catch (Exception ex)
                    {
                        Smtp.SendCompleted -= handler;
                        return Task.FromResult(ex);
                    }

                case SendMethod.SendMailAsync:
                    try
                    {
                        return Smtp.SendMailAsync(msg).ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                return t.Exception?.InnerException;
                            }
                            return null;
                        });
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(ex);
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected async Task SendMail(MailMessage msg)
        {
            Exception? ex = await SendMailInternal(msg);
            Assert.Null(ex);
        }

        protected async Task<TException> SendMail<TException>(MailMessage msg) where TException : Exception
        {
            Exception? ex = await SendMailInternal(msg);

            if (T.SendMethod != SendMethod.Send && typeof(TException) != typeof(SmtpException))
            {
                ex = Assert.IsType<SmtpException>(ex).InnerException;
            }

            return Assert.IsType<TException>(ex);
        }

        protected static string GetClientDomain() => IPGlobalProperties.GetIPGlobalProperties().HostName.Trim().ToLower();

        public virtual void Dispose()
        {
            _smtp?.Dispose();
            Server?.Dispose();
        }
    }
}