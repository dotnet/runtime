// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security.Tests
{
    internal class UnixGssFakeNegotiateStream : NegotiateStream
    {
        private static Action<object> s_serverLoop = ServerLoop;
        private static Action<object> s_msgLoop = MessageLoop;
        private readonly UnixGssFakeStreamFramer _framer;
        private SafeGssContextHandle _context;
        private volatile int _dataMsgCount;

        public UnixGssFakeNegotiateStream(Stream innerStream) : base(innerStream)
        {
            _framer = new UnixGssFakeStreamFramer(innerStream);
            _dataMsgCount = 0;
        }

        public override Task AuthenticateAsServerAsync()
        {
            return Task.Factory.StartNew(s_serverLoop, this, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public Task PollMessageAsync(int count)
        {
            _dataMsgCount = count;
            return Task.Factory.StartNew(s_msgLoop, this, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public static void GetDefaultKerberosCredentials(string username, string password)
        {
            // Fetch a Kerberos TGT which gets saved in the default cache
            SafeGssCredHandle.Create(username, password, isNtlmOnly:false).Dispose();
        }

        private static void ServerLoop(object state)
        {
            UnixGssFakeNegotiateStream thisRef = (UnixGssFakeNegotiateStream)state;
            var header = new byte[5];
            bool handshakeDone = false;
            do
            {
                byte[] inBuf = thisRef._framer.ReadHandshakeFrame();
                byte[] outBuf = null;
                try
                {
                    SafeGssContextHandle context = thisRef._context; // workaround warning about a ref to a field on a MarshalByRefObject
                    handshakeDone = EstablishSecurityContext(ref context, inBuf, out outBuf);
                    thisRef._context = context;

                    thisRef._framer.WriteHandshakeFrame(outBuf, 0, outBuf.Length);
                }
                catch (Interop.NetSecurityNative.GssApiException e)
                {
                    thisRef._framer.WriteHandshakeFrame(e);
                    handshakeDone = true;
                }
            }
            while (!handshakeDone);
        }

        private static void MessageLoop(object state)
        {
            UnixGssFakeNegotiateStream thisRef = (UnixGssFakeNegotiateStream)state;
            while (thisRef._dataMsgCount > 0)
            {
                byte[] inBuf = thisRef._framer.ReadDataFrame();
                byte[] unwrapped = UnwrapMessage(thisRef._context, inBuf);
                byte[] outMsg = WrapMessage(thisRef._context, unwrapped);
                thisRef._framer.WriteDataFrame(outMsg, 0, outMsg.Length);
                thisRef._dataMsgCount--;
            }
        }

        private static bool EstablishSecurityContext(
            ref SafeGssContextHandle context,
            byte[] buffer,
            out byte[] outputBuffer)
        {
            outputBuffer = null;

            // EstablishSecurityContext is called multiple times in a session.
            // In each call, we need to pass the context handle from the previous call.
            // For the first call, the context handle will be null.
            if (context == null)
            {
                context = new SafeGssContextHandle();
            }

            Interop.NetSecurityNative.GssBuffer token = default(Interop.NetSecurityNative.GssBuffer);
            Interop.NetSecurityNative.Status status;

            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                status = Interop.NetSecurityNative.AcceptSecContext(out minorStatus,
                                                          ref context,
                                                          buffer,
                                                          (buffer == null) ? 0 : buffer.Length,
                                                          ref token);

                if ((status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE) && (status != Interop.NetSecurityNative.Status.GSS_S_CONTINUE_NEEDED))
                {
                    throw new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                }

                outputBuffer = token.ToByteArray();
            }
            finally
            {
                token.Dispose();
            }

            return status == Interop.NetSecurityNative.Status.GSS_S_COMPLETE;
        }

        private static byte[] UnwrapMessage(SafeGssContextHandle context, byte[] message)
        {
            Interop.NetSecurityNative.GssBuffer unwrapped = default(Interop.NetSecurityNative.GssBuffer);
            Interop.NetSecurityNative.Status status;

            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                status = Interop.NetSecurityNative.UnwrapBuffer(out minorStatus,
                    context, message, 0, message.Length, ref unwrapped);
                if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    throw new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                }

                return unwrapped.ToByteArray();
            }
            finally
            {
                unwrapped.Dispose();
            }
        }

        private static byte[] WrapMessage(SafeGssContextHandle context, byte[] message)
        {
            Interop.NetSecurityNative.GssBuffer wrapped = default(Interop.NetSecurityNative.GssBuffer);
            Interop.NetSecurityNative.Status status;

            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                status = Interop.NetSecurityNative.WrapBuffer(out minorStatus,
                    context, false, message, 0, message.Length, ref wrapped);
                if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    throw new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                }

                return wrapped.ToByteArray();
            }
            finally
            {
                wrapped.Dispose();
            }
        }
    }
}
