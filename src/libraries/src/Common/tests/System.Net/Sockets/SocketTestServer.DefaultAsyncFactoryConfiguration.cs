// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets.Tests
{
    // Each individual test must configure this class by defining s_implementationType within 
    // SocketTestServer.DefaultFactoryConfiguration.cs
    public abstract partial class SocketTestServer : IDisposable
    {
        protected const SocketImplementationType s_implementationType = SocketImplementationType.Async;
    }
}
