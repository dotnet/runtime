// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Xunit;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public class SafeWinHttpHandleTest
    {
        [ActiveIssue(13951)]
        [Fact]
        public void CreateAddRefDispose_HandleIsNotClosed()
        {
            var safeHandle = new FakeSafeWinHttpHandle(true);
            bool success = false;
            safeHandle.DangerousAddRef(ref success);
            Assert.True(success, "DangerousAddRef");
            safeHandle.Dispose();
            
            Assert.False(safeHandle.IsClosed, "closed");
            Assert.Equal(1, FakeSafeWinHttpHandle.HandlesOpen);
            
            // Clean up safeHandle to keep outstanding handles at zero.
            safeHandle.DangerousRelease();
        }

        [ActiveIssue(13951)]
        [Fact]
        public void CreateAddRefDisposeDispose_HandleIsNotClosed()
        {
            var safeHandle = new FakeSafeWinHttpHandle(true);
            bool success = false;
            safeHandle.DangerousAddRef(ref success);
            Assert.True(success, "DangerousAddRef");
            safeHandle.Dispose();
            safeHandle.Dispose();
            
            Assert.False(safeHandle.IsClosed, "closed");
            Assert.Equal(1, FakeSafeWinHttpHandle.HandlesOpen);
            
            // Clean up safeHandle to keep outstanding handles at zero.
            safeHandle.DangerousRelease();
        }

        [ActiveIssue(13951)]
        [Fact]
        public void CreateAddRefDisposeRelease_HandleIsClosed()
        {
            var safeHandle = new FakeSafeWinHttpHandle(true);
            bool success = false;
            safeHandle.DangerousAddRef(ref success);
            Assert.True(success, "DangerousAddRef");
            safeHandle.Dispose();
            safeHandle.DangerousRelease();
            
            Assert.True(safeHandle.IsClosed, "closed");
            Assert.Equal(0, FakeSafeWinHttpHandle.HandlesOpen);
        }

        [ActiveIssue(13951)]
        [Fact]
        public void CreateAddRefRelease_HandleIsNotClosed()
        {
            var safeHandle = new FakeSafeWinHttpHandle(true);
            bool success = false;
            safeHandle.DangerousAddRef(ref success);
            Assert.True(success, "DangerousAddRef");
            safeHandle.DangerousRelease();
            
            Assert.False(safeHandle.IsClosed, "closed");
            Assert.Equal(1, FakeSafeWinHttpHandle.HandlesOpen);
            
            // Clean up safeHandle to keep outstanding handles at zero.
            safeHandle.Dispose();
        }

        [ActiveIssue(13951)]
        [Fact]
        public void CreateAddRefReleaseDispose_HandleIsClosed()
        {
            var safeHandle = new FakeSafeWinHttpHandle(true);
            bool success = false;
            safeHandle.DangerousAddRef(ref success);
            Assert.True(success, "DangerousAddRef");
            safeHandle.DangerousRelease();
            safeHandle.Dispose();
            
            Assert.True(safeHandle.IsClosed, "closed");
            Assert.Equal(0, FakeSafeWinHttpHandle.HandlesOpen);
        }

        [ActiveIssue(13951)]
        [Fact]
        public void CreateDispose_HandleIsClosed()
        {
            var safeHandle = new FakeSafeWinHttpHandle(true);
            safeHandle.Dispose();
            
            Assert.True(safeHandle.IsClosed, "closed");
        }

        [ActiveIssue(13951)]
        [Fact]
        public void CreateDisposeDispose_HandleIsClosedAndSecondDisposeIsNoop()
        {
            var safeHandle = new FakeSafeWinHttpHandle(true);
            safeHandle.Dispose();
            safeHandle.Dispose();
            Assert.True(safeHandle.IsClosed, "closed");
        }

        [ActiveIssue(13951)]
        [Fact]
        public void CreateDisposeAddRef_ThrowsObjectDisposedException()
        {
            var safeHandle = new FakeSafeWinHttpHandle(true);
            safeHandle.Dispose();
            Assert.Throws<ObjectDisposedException>(() => 
                { bool ignore = false; safeHandle.DangerousAddRef(ref ignore); });
        }

        [ActiveIssue(13951)]
        [Fact]
        public void CreateDisposeRelease_ThrowsObjectDisposedException()
        {
            var safeHandle = new FakeSafeWinHttpHandle(true);
            safeHandle.Dispose();
            Assert.Throws<ObjectDisposedException>(() => safeHandle.DangerousRelease());
        }

        [ActiveIssue(13951)]
        [Fact]
        public void SetParentHandle_CreateParentCreateChildDisposeParent_ParentNotClosed()
        {
            var parentHandle = new FakeSafeWinHttpHandle(true);
            var childHandle = new FakeSafeWinHttpHandle(true);
            childHandle.SetParentHandle(parentHandle);
            parentHandle.Dispose();
            
            Assert.False(parentHandle.IsClosed, "closed");
            Assert.Equal(2, FakeSafeWinHttpHandle.HandlesOpen);
            
            // Clean up safeHandles to keep outstanding handles at zero.
            childHandle.Dispose();
        }

        [ActiveIssue(13951)]
        [Fact]
        public void SetParentHandle_CreateParentCreateChildDisposeParentDisposeChild_HandlesClosed()
        {
            var parentHandle = new FakeSafeWinHttpHandle(true);
            var childHandle = new FakeSafeWinHttpHandle(true);
            childHandle.SetParentHandle(parentHandle);
            parentHandle.Dispose();
            childHandle.Dispose();
            
            Assert.True(parentHandle.IsClosed, "closed");
            Assert.True(childHandle.IsClosed, "closed");
        }

        [ActiveIssue(13951)]
        [Fact]
        public void SetParentHandle_CreateParentCreateChildDisposeChildDisposeParent_HandlesClosed()
        {
            var parentHandle = new FakeSafeWinHttpHandle(true);
            var childHandle = new FakeSafeWinHttpHandle(true);
            childHandle.SetParentHandle(parentHandle);
            childHandle.Dispose();
            parentHandle.Dispose();
            
            Assert.True(parentHandle.IsClosed, "closed");
            Assert.True(childHandle.IsClosed, "closed");
        }
    }
}
