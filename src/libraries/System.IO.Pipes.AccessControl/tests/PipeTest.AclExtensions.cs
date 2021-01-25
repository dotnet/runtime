// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public abstract class PipeTest_AclExtensions
    {
        [Fact]
        public void GetAccessControl_NullPipeStream()
        {
            Assert.Throws<NullReferenceException>(() => PipesAclExtensions.GetAccessControl(null));
        }

        [Fact]
        public void GetAccessControl_DisposedStream()
        {
            using (var pair = CreateServerClientPair())
            {
                pair.readablePipe.Dispose();
                Assert.Throws<ObjectDisposedException>(() => pair.readablePipe.GetAccessControl());

                pair.writeablePipe.Dispose();
                Assert.Throws<ObjectDisposedException>(() => pair.writeablePipe.GetAccessControl());
            }
        }

        [Fact]
        public void GetAccessControl_ConnectedStream()
        {
            using (var pair = CreateServerClientPair())
            {
                Assert.NotNull(pair.readablePipe.GetAccessControl());
                Assert.NotNull(pair.writeablePipe.GetAccessControl());
            }
        }

        [Fact]
        public void SetAccessControl_NullPipeStream()
        {
            Assert.Throws<NullReferenceException>(() => PipesAclExtensions.SetAccessControl(null, new PipeSecurity()));
        }

        [Fact]
        public void SetAccessControl_NullPipeSecurity()
        {
            using (var pair = CreateServerClientPair())
            {
                var stream = pair.readablePipe;
                Assert.Throws<ArgumentNullException>(() => PipesAclExtensions.SetAccessControl(stream, null));
                Assert.Throws<ArgumentNullException>(() => stream.SetAccessControl(null));

                stream = pair.writeablePipe;
                Assert.Throws<ArgumentNullException>(() => PipesAclExtensions.SetAccessControl(stream, null));
                Assert.Throws<ArgumentNullException>(() => stream.SetAccessControl(null));
            }
        }

        [Fact]
        public void SetAccessControl_DisposedStream()
        {
            using (var pair = CreateServerClientPair())
            {
                pair.readablePipe.Dispose();
                Assert.Throws<ObjectDisposedException>(() => pair.readablePipe.SetAccessControl(new PipeSecurity()));

                pair.writeablePipe.Dispose();
                Assert.Throws<ObjectDisposedException>(() => pair.writeablePipe.SetAccessControl(new PipeSecurity()));
            }
        }

        [Fact]
        public void SetAccessControl_ConnectedStream()
        {
            using (var pair = CreateServerClientPair())
            {
                var security = new PipeSecurity();
                pair.readablePipe.SetAccessControl(security);
                pair.writeablePipe.SetAccessControl(security);
            }
        }

        // This test matches .NET Framework behavior
        [Fact]
        public void PipeSecurity_VerifySynchronizeMasks()
        {
            var si = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            // This is a valid mask that should not throw
            new PipeAccessRule(si, PipeAccessRights.Synchronize, AccessControlType.Allow);

            Assert.Throws<ArgumentException>("accessMask", () =>
            {
                new PipeAccessRule(si, PipeAccessRights.Synchronize, AccessControlType.Deny);
            });
        }

        protected static string GetUniquePipeName() =>
            PlatformDetection.IsInAppContainer ? @"LOCAL\" + Path.GetRandomFileName() :
            Path.GetRandomFileName();

        protected abstract ServerClientPair CreateServerClientPair();

        protected class ServerClientPair : IDisposable
        {
            public PipeStream readablePipe;
            public PipeStream writeablePipe;

            public void Dispose()
            {
                try
                {
                    readablePipe?.Dispose();
                }
                finally
                {
                    writeablePipe.Dispose();
                }
            }
        }
    }
}
