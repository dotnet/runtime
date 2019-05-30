// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.Tests
{
    public class DirectoryEntryTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var entry = new DirectoryEntry();
            Assert.Equal(string.Empty, entry.Path);
            Assert.Null(entry.Username);
            Assert.Equal(AuthenticationTypes.Secure, entry.AuthenticationType);
            Assert.True(entry.UsePropertyCache);

            Assert.NotNull(entry.Children);
            Assert.NotNull(entry.Properties);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Path")]
        public void Ctor_Path(string path)
        {
            var entry = new DirectoryEntry(path);
            Assert.Equal(path ?? string.Empty, entry.Path);
            Assert.Null(entry.Username);
            Assert.Equal(AuthenticationTypes.Secure, entry.AuthenticationType);
            Assert.True(entry.UsePropertyCache);

            Assert.NotNull(entry.Children);
            Assert.NotNull(entry.Properties);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("", "", "")]
        [InlineData("Path", "UserName", "Password")]
        public void Ctor_Path_UserName_Password(string path, string userName, string password)
        {
            var entry = new DirectoryEntry(path, userName, password);
            Assert.Equal(path ?? string.Empty, entry.Path);
            Assert.Equal(userName, entry.Username);
            Assert.Equal(AuthenticationTypes.Secure, entry.AuthenticationType);
            Assert.True(entry.UsePropertyCache);

            Assert.NotNull(entry.Children);
            Assert.NotNull(entry.Properties);
        }

        [Theory]
        [InlineData(null, null, null, (AuthenticationTypes)int.MinValue)]
        [InlineData("", "", "", AuthenticationTypes.Anonymous)]
        [InlineData("Path", "UserName", "Password", AuthenticationTypes.None)]
        public void Ctor_Path_UserName_Password_AuthenticationType(string path, string userName, string password, AuthenticationTypes authenticationType)
        {
            var entry = new DirectoryEntry(path, userName, password, authenticationType);
            Assert.Equal(path ?? string.Empty, entry.Path);
            Assert.Equal(userName, entry.Username);
            Assert.Equal(authenticationType, entry.AuthenticationType);
            Assert.True(entry.UsePropertyCache);

            Assert.NotNull(entry.Children);
            Assert.NotNull(entry.Properties);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void Ctor_InvalidAdsObject_ThrowsArgumentException(object adsObject)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryEntry(adsObject));
        }

        [Theory]
        [InlineData(AuthenticationTypes.Secure)]
        [InlineData(AuthenticationTypes.Anonymous)]
        [InlineData((AuthenticationTypes)int.MinValue)]
        public void AuthenticationType_Set_GetReturnsExpected(AuthenticationTypes authenticationType)
        {
            var entry = new DirectoryEntry { AuthenticationType = authenticationType };
            Assert.Equal(authenticationType, entry.AuthenticationType);
        }

        [Fact]
        public void Dispose_InvokeMultipleTimes_Success()
        {
            var entry = new DirectoryEntry();
            entry.Dispose();
            entry.Dispose();
        }

        [Fact]
        public void Dispose_GetProperties_ThrowsObjectDisposedException()
        {
            var entry = new DirectoryEntry("path");
            entry.Dispose();

            Assert.Throws<ObjectDisposedException>(() => entry.Guid);
            Assert.Throws<ObjectDisposedException>(() => entry.Name);
            Assert.Throws<ObjectDisposedException>(() => entry.NativeGuid);
            Assert.Throws<ObjectDisposedException>(() => entry.NativeObject);
            Assert.Throws<ObjectDisposedException>(() => entry.ObjectSecurity);
            Assert.Throws<ObjectDisposedException>(() => entry.Parent);
            Assert.Throws<ObjectDisposedException>(() => entry.SchemaClassName);
            Assert.Throws<ObjectDisposedException>(() => entry.SchemaEntry);
        }

        [Fact]
        public void Dispose_InvokeMethods_ThrowsObjectDisposedException()
        {
            var entry = new DirectoryEntry("path");
            entry.Dispose();

            //Assert.Throws<ObjectDisposedException>(() => entry.CopyTo(new DirectoryEntry()));
        }

        [Fact]
        public void CopyTo_NullEntry_ThrowsNullReferenceException()
        {
            var entry = new DirectoryEntry("path");
            Assert.Throws<NullReferenceException>(() => entry.CopyTo(null));
            Assert.Throws<NullReferenceException>(() => entry.CopyTo(null, "newName"));
        }

        [Fact]
        public void CopyTo_DisposedEntry_ThrowsObjectDisposedException()
        {
            var entry = new DirectoryEntry("path");
            var disposedEntry = new DirectoryEntry();
            disposedEntry.Dispose();

            Assert.Throws<ObjectDisposedException>(() => entry.CopyTo(disposedEntry));
            Assert.Throws<ObjectDisposedException>(() => entry.CopyTo(disposedEntry, "newName"));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] 
        public void DeleteTree_NoObject_ThrowsCOMException()
        {
            var entry = new DirectoryEntry("path");
            Assert.Throws<COMException>(() => entry.DeleteTree());
        }

        [Fact]
        public void DeleteTree_DisposedObject_ObjectDisposedException()
        {
            var entry = new DirectoryEntry("path");
            entry.Dispose();
            Assert.Throws<ObjectDisposedException>(() => entry.DeleteTree());
        }

        [Fact]
        public void Invoke_DisposedObject_ObjectDisposedException()
        {
            var entry = new DirectoryEntry("path");
            entry.Dispose();
            Assert.Throws<ObjectDisposedException>(() => entry.Invoke(null, null));
        }

        [Fact]
        public void InvokeGet_DisposedObject_ObjectDisposedException()
        {
            var entry = new DirectoryEntry("path");
            entry.Dispose();
            Assert.Throws<ObjectDisposedException>(() => entry.InvokeGet(null));
        }

        [Fact]
        public void InvokeSet_DisposedObject_ObjectDisposedException()
        {
            var entry = new DirectoryEntry("path");
            entry.Dispose();
            Assert.Throws<ObjectDisposedException>(() => entry.InvokeSet(null));
        }

        [Fact]
        public void MoveTo_NullEntry_ThrowsNullReferenceException()
        {
            var entry = new DirectoryEntry("path");
            Assert.Throws<NullReferenceException>(() => entry.MoveTo(null));
            Assert.Throws<NullReferenceException>(() => entry.MoveTo(null, "newName"));
        }

        [Fact]
        public void MoveTo_DisposedEntry_ThrowsObjectDisposedException()
        {
            var entry = new DirectoryEntry("path");
            var disposedEntry = new DirectoryEntry();
            disposedEntry.Dispose();

            Assert.Throws<ObjectDisposedException>(() => entry.MoveTo(disposedEntry));
            Assert.Throws<ObjectDisposedException>(() => entry.MoveTo(disposedEntry, "newName"));
        }
        
        [Fact]
        public void Rename_Disposed_ThrowsObjectDisposedException()
        {
            var entry = new DirectoryEntry("path");
            entry.Dispose();

            Assert.Throws<ObjectDisposedException>(() => entry.Rename(null));
        }

        [Fact]
        public void RefreshCache_Disposed_ThrowsObjectDisposedException()
        {
            var entry = new DirectoryEntry("path");
            entry.Dispose();

            Assert.Throws<ObjectDisposedException>(() => entry.RefreshCache());
            Assert.Throws<ObjectDisposedException>(() => entry.RefreshCache(null));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "PNSE in UAP")]
        public void ObjectSecurity_Set_GetReturnsExpected()
        {
            var security = new ActiveDirectorySecurity();
            var entry = new DirectoryEntry { ObjectSecurity = security };
            Assert.Same(security, entry.ObjectSecurity);
        }

        [Fact]
        public void ObjectSecurity_SetNull_ThrowsArgumentnullExceptioN()
        {
            var entry = new DirectoryEntry();
            AssertExtensions.Throws<ArgumentNullException>("value", () => entry.ObjectSecurity = null);
        }
    }
}
