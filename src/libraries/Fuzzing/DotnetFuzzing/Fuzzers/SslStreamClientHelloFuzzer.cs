// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Security;
using System.Security.Authentication;

namespace DotnetFuzzing.Fuzzers
{
    internal sealed class SslStreamClientHelloFuzzer : IFuzzer
    {
        public string[] TargetAssemblies => ["System.Net.Security"];

        public string[] TargetCoreLibPrefixes => [];

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
            try
            {
                bytes.CopyTo(buffer);
                using MemoryStream ms = new MemoryStream(buffer, 0, bytes.Length);
                using SslStream sslStream = new SslStream(ms);
                sslStream.AuthenticateAsServerAsync((stream, clientHelloInfo, b, token) =>
                {
                    // This callback should be called when ClientHello is
                    // received, Since we don't parse any other TLS messages,
                    // we can terminate the handshake here. Fuzzing the rest of
                    // the handshake would fuzz the platform TLS implementation
                    // which is not instrumented and is out of scope for this
                    // fuzzer.
                    throw new MyCustomException();
                }, null).GetAwaiter().GetResult();
            }
            catch (Exception ex) when (ex is AuthenticationException or IOException or MyCustomException)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        internal class MyCustomException : Exception
        {
        }
    }
}
