// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.IsolatedStorage
{
    [ActiveIssue(18940, TargetFrameworkMonikers.UapAot)]
    public partial class IsolatedStorageFileStreamTests : IsoStorageTest
    {
        [Theory, MemberData(nameof(ValidStores))]
        public async Task DisposeAsync_MultipleInvokes_Idempotent(PresetScopes scope)
        {
            TestHelper.WipeStores();
            using (IsolatedStorageFile isf = GetPresetScope(scope))
            {
                IsolatedStorageFileStream isfs = isf.CreateFile("DisposeAsyncFile");
                await isfs.DisposeAsync();
                await isfs.DisposeAsync();
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public async Task DisposeAsync_FlushesAndCloses(PresetScopes scope)
        {
            TestHelper.WipeStores();
            using (IsolatedStorageFile isf = GetPresetScope(scope))
            {
                IsolatedStorageFileStream isfs = isf.CreateFile("DisposeAsyncFile");
                isfs.Write(new byte[100], 0, 100);
                await isfs.DisposeAsync();

                using (isfs = isf.OpenFile("DisposeAsyncFile", FileMode.Open))
                {
                    Assert.Equal(100, isfs.Length);
                }
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public async Task DisposeAsync_DerivedIsolatedStorageFileStream_DisposeInvoked(PresetScopes scope)
        {
            TestHelper.WipeStores();
            using (IsolatedStorageFile isf = GetPresetScope(scope))
            using (var isfs = new OverridesDisposeIsolatedStorageFileStream("DisposeAsyncFile", FileMode.Create))
            {
                Assert.False(isfs.DisposeInvoked);
                await isfs.DisposeAsync();
                Assert.True(isfs.DisposeInvoked);
            }
        }

        private sealed class OverridesDisposeIsolatedStorageFileStream : IsolatedStorageFileStream
        {
            public bool DisposeInvoked;
            public OverridesDisposeIsolatedStorageFileStream(string path, FileMode mode) : base(path, mode) { }
            protected override void Dispose(bool disposing)
            {
                DisposeInvoked = true;
                base.Dispose(disposing);
            }
        }
    }
}
