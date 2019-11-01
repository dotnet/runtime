// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class HandleComparerTests
    {
        [Fact]
        public void CompareEntityHandles()
        {
            Assert.True(EntityHandle.Compare(new EntityHandle(0x02000001), new EntityHandle(0x02000002)) < 0);
            Assert.True(EntityHandle.Compare(new EntityHandle(0x02000002), new EntityHandle(0x02000001)) > 0);
            Assert.True(EntityHandle.Compare(new EntityHandle(0x02000001), new EntityHandle(0x02000001)) == 0);

            // order of different kinds of handles is undefined, we only guarantee that the result is the same as Equals
            Assert.True(EntityHandle.Compare(new EntityHandle(0x20000001), new EntityHandle(0x21000002)) < 0);
            Assert.True(EntityHandle.Compare(new EntityHandle(0x20000001), new EntityHandle(0x21000001)) < 0);
            Assert.True(EntityHandle.Compare(new EntityHandle(0x20000002), new EntityHandle(0x21000001)) < 0);

            // virtual tokens follow non-virtual:
            Assert.True(EntityHandle.Compare(new EntityHandle(0x82000001), new EntityHandle(0x02000002)) > 0);
            Assert.True(EntityHandle.Compare(new EntityHandle(0x02000002), new EntityHandle(0x82000001)) < 0);
            Assert.True(EntityHandle.Compare(new EntityHandle(0x82000001), new EntityHandle(0x82000001)) == 0);

            // make sure we won't overflow for extreme values:
            Assert.True(EntityHandle.Compare(new EntityHandle(0xffffffff), new EntityHandle(0x00000000)) > 0);
            Assert.True(EntityHandle.Compare(new EntityHandle(0x00000000), new EntityHandle(0xffffffff)) < 0);
            Assert.True(EntityHandle.Compare(new EntityHandle(0xfffffffe), new EntityHandle(0xffffffff)) < 0);
            Assert.True(EntityHandle.Compare(new EntityHandle(0xffffffff), new EntityHandle(0xfffffffe)) > 0);
            Assert.True(EntityHandle.Compare(new EntityHandle(0xffffffff), new EntityHandle(0xffffffff)) == 0);
        }

        [Fact]
        public void CompareHandles()
        {
            Assert.True(Handle.Compare(new Handle(0x02, 0x00000001), new Handle(0x02, 0x00000002)) < 0);
            Assert.True(Handle.Compare(new Handle(0x02, 0x00000002), new Handle(0x02, 0x00000001)) > 0);
            Assert.True(Handle.Compare(new Handle(0x02, 0x00000001), new Handle(0x02, 0x00000001)) == 0);

            // order of different kinds of handles is undefined, we only guarantee that the result is the same as Equals
            Assert.True(Handle.Compare(new Handle(0x20, 0x00000001), new Handle(0x21, 0x00000002)) < 0);
            Assert.True(Handle.Compare(new Handle(0x20, 0x00000001), new Handle(0x21, 0x00000001)) < 0);
            Assert.True(Handle.Compare(new Handle(0x20, 0x00000002), new Handle(0x21, 0x00000001)) < 0);

            // virtual tokens follow non-virtual:
            Assert.True(Handle.Compare(new Handle(0x82, 0x00000001), new Handle(0x02, 0x00000002)) > 0);
            Assert.True(Handle.Compare(new Handle(0x02, 0x00000002), new Handle(0x82, 0x00000001)) < 0);
            Assert.True(Handle.Compare(new Handle(0x82, 0x00000001), new Handle(0x82, 0x00000001)) == 0);

            // make sure we won't overflow for extreme values:
            Assert.True(Handle.Compare(new Handle(0xff, 0x01ffffff), new Handle(0x00, 0x00000000)) > 0);
            Assert.True(Handle.Compare(new Handle(0x00, 0x00000000), new Handle(0xff, 0x01ffffff)) < 0);
            Assert.True(Handle.Compare(new Handle(0xff, 0x01fffffe), new Handle(0xff, 0x01ffffff)) < 0);
            Assert.True(Handle.Compare(new Handle(0xff, 0x01ffffff), new Handle(0xff, 0x01fffffe)) > 0);
            Assert.True(Handle.Compare(new Handle(0xff, 0x01ffffff), new Handle(0xff, 0x01ffffff)) == 0);
        }
    }
}
