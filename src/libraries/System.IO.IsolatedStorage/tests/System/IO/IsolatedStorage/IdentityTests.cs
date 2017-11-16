// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    [ActiveIssue(18940, TargetFrameworkMonikers.UapAot)]
    public class IdentityTests : IsoStorageTest
    {
        private class TestStorage : IsolatedStorage
        {
            public TestStorage()
                : base()
            {
            }

            public TestStorage(IsolatedStorageScope scope)
                : this()
            {
                InitStore(scope, null);
            }

            public override void Remove()
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void IdentityThrowsInvalidOperation()
        {
            TestStorage storage = new TestStorage();
            Assert.Throws<InvalidOperationException>(() => storage.ApplicationIdentity);
            Assert.Throws<InvalidOperationException>(() => storage.AssemblyIdentity);
            Assert.Throws<InvalidOperationException>(() => storage.DomainIdentity);
        }

        [Theory,
            InlineData(IsolatedStorageScope.Application | IsolatedStorageScope.User),
            InlineData(IsolatedStorageScope.Application | IsolatedStorageScope.User | IsolatedStorageScope.Roaming)
            // https://github.com/dotnet/corefx/issues/12628
            // InlineData(IsolatedStorageScope.Application | IsolatedStorageScope.Machine)
            ]
        public void ApplicationIdentityIsSet(IsolatedStorageScope scope)
        {
            TestStorage storage = new TestStorage(scope);
            Assert.NotNull(storage.ApplicationIdentity);
            Assert.Throws<InvalidOperationException>(() => storage.AssemblyIdentity);
            Assert.Throws<InvalidOperationException>(() => storage.DomainIdentity);
        }

        [Theory,
            InlineData(IsolatedStorageScope.Assembly | IsolatedStorageScope.User),
            InlineData(IsolatedStorageScope.Assembly | IsolatedStorageScope.User | IsolatedStorageScope.Roaming)
            // https://github.com/dotnet/corefx/issues/12628
            // InlineData(IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine)
            ]
        public void AssemblyIdentityIsSet(IsolatedStorageScope scope)
        {
            TestStorage storage = new TestStorage(scope);
            Assert.NotNull(storage.AssemblyIdentity);
            Assert.Throws<InvalidOperationException>(() => storage.ApplicationIdentity);
            Assert.Throws<InvalidOperationException>(() => storage.DomainIdentity);
        }

        [Theory,
            InlineData(IsolatedStorageScope.Assembly | IsolatedStorageScope.User | IsolatedStorageScope.Domain),
            InlineData(IsolatedStorageScope.Assembly | IsolatedStorageScope.User | IsolatedStorageScope.Roaming | IsolatedStorageScope.Domain)
            // https://github.com/dotnet/corefx/issues/12628
            // InlineData(IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine | IsolatedStorageScope.Domain)
            ]
        public void DomainIdentityIsSet(IsolatedStorageScope scope)
        {
            TestStorage storage = new TestStorage(scope);
            Assert.NotNull(storage.AssemblyIdentity);
            Assert.NotNull(storage.DomainIdentity);
            Assert.Throws<InvalidOperationException>(() => storage.ApplicationIdentity);
        }
    }
}
