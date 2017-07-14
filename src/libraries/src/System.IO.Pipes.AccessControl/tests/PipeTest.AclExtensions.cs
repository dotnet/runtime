// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Pipes.Tests
{
    public abstract class PipeTest_AclExtensions : PipeTestBase
    {
        [Fact]
        public void GetAccessControl_NullPipeStream()
        {
            Assert.Throws<NullReferenceException>(() => PipesAclExtensions.GetAccessControl(null));
        }

        [Fact]
        [ActiveIssue(22271, TargetFrameworkMonikers.UapNotUapAot)]
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
        [ActiveIssue(22271, TargetFrameworkMonikers.UapNotUapAot)]
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
        [ActiveIssue(22271, TargetFrameworkMonikers.UapNotUapAot)]
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
        [ActiveIssue(22271, TargetFrameworkMonikers.UapNotUapAot)]
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
        [ActiveIssue(22271, TargetFrameworkMonikers.UapNotUapAot)]
        public void SetAccessControl_ConnectedStream()
        {
            using (var pair = CreateServerClientPair())
            {
                var security = new PipeSecurity();
                pair.readablePipe.SetAccessControl(security);
                pair.writeablePipe.SetAccessControl(security);
            }
        }
    }
}
