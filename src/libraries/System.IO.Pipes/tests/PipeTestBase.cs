// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// The class that all Pipes tests will inherit from.
    /// 
    /// Contains methods and variables with code that is frequently repeated
    /// </summary>
    public class PipeTestBase
    {
        protected static byte[] sendBytes = new byte[] { 123, 234 };

        protected static void DoStreamOperations(PipeStream stream)
        {
            if (stream.CanWrite)
            {
                stream.Write(new byte[] { 123, 124 }, 0, 2);
            }
            if (stream.CanRead)
            {
                Assert.Equal(123, stream.ReadByte());
                Assert.Equal(124, stream.ReadByte());
            }
        }

        protected static void SuppressClientHandleFinalizationIfNetFramework(AnonymousPipeServerStream serverStream)
        {
            if (PlatformDetection.IsFullFramework)
            {
                // See https://github.com/dotnet/corefx/pull/1871.  When AnonymousPipeServerStream.GetClientHandleAsString()
                // is called, the assumption is that this string is going to be passed to another process, rather than wrapped
                // into a SafeHandle in the same process.  If it's wrapped into a SafeHandle in the same process, there are then
                // two SafeHandles that believe they own the same underlying handle, which leads to use-after-free and recycling
                // bugs.  AnonymousPipeServerStream incorrectly deals with this in desktop: it marks the SafeHandle as having
                // been exposed, but that then only prevents the disposal of AnonymousPipeServerStream from calling Dispose
                // on the SafeHandle... it doesn't prevent the SafeHandle itself from getting finalized, which leads to random
                // "The handle is invalid" or "Pipe is broken" errors at some later point when the handle is recycled and used
                // for another instance.  In core, this was addressed in 1871 by calling GC.SuppressFinalize(_clientHandle)
                // in GetClientHandleAsString.  For desktop, we work around this by suppressing the handle in this explicit call.
                GC.SuppressFinalize(serverStream.ClientSafePipeHandle);
            }
        }

        /// <summary>
        /// Represents a Server-Client pair where "readablePipe" refers to whichever
        /// of the two streams is defined with PipeDirection.In and "writeablePipe" is 
        /// defined with PipeDirection.Out. 
        /// </summary>
        /// <remarks>
        /// For tests where InOut is used, writeablePipe will refer to whichever pipe
        /// the tests should be treating as the one with PipeDirection.Out.
        /// </remarks>
        protected class ServerClientPair : IDisposable
        {
            public PipeStream readablePipe;
            public PipeStream writeablePipe;

            public void Dispose()
            {
                try
                {
                    if (readablePipe != null)
                        readablePipe.Dispose();
                }
                finally
                {
                    writeablePipe.Dispose();
                }
            }
        }

        /// <summary>
        /// Get a unique pipe name very unlikely to be in use elsewhere.
        /// </summary>
        /// <returns></returns>
        protected static string GetUniquePipeName()
        {
            if (PlatformDetection.IsInAppContainer)
            {
                return @"LOCAL\" + Path.GetRandomFileName();
            }
            else
            {
                return Path.GetRandomFileName();
            }
        }

        /// <summary>
        /// Virtual method to create a Server-Client PipeStream pair
        /// that the test methods can override and make use of.
        /// 
        /// The default (in PipeTest) will return a null ServerClientPair.
        /// </summary>
        protected virtual ServerClientPair CreateServerClientPair()
        {
            return null;
        }
    }
}
