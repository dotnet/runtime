// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.IO;
using Xunit;

namespace System.ComponentModel.Design.Serialization.Tests
{
#pragma warning disable 0618
    public class SerializationStoreTests
    {
        [Fact]
        public void Dispose_Invoke_InvokesClose()
        {
            var store = new TestSerializationStore();
            ((IDisposable)store).Dispose();

            Assert.True(store.CalledClose);
        }

        [Fact]
        public void Dispose_InvokeNonDisposing_InvokesClose()
        {
            var store = new TestSerializationStore();
            store.DoDispose(disposing: false);

            Assert.False(store.CalledClose);
        }

        private class TestSerializationStore : SerializationStore
        {
            public bool CalledClose { get; set; }
            public void DoDispose(bool disposing) => base.Dispose(disposing);

            public override ICollection Errors => throw new NotImplementedException();
            public override void Close() => CalledClose = true;
            public override void Save(Stream stream) => throw new NotImplementedException();
        }
    }
#pragma warning restore 0618
}
