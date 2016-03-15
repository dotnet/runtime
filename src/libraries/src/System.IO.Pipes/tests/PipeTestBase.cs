// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Get a unique pipe name that is guaranteed not to be in use elsewhere
        /// </summary>
        /// <returns></returns>
        protected static string GetUniquePipeName()
        {
            return Guid.NewGuid().ToString();
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
