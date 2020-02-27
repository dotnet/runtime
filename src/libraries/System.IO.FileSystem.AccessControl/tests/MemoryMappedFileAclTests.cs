// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using Xunit;

namespace System.IO
{
    public class MemoryMappedFileAclTests : AclTestBase
    {
        // CreateFromFile

        [Fact]
        public void CreateFromFile_FileStreamNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("fileStream", () => MemoryMappedFileAcl.CreateFromFile(null, CreateUniqueMapName(), StandardCapacity, MemoryMappedFileAccess.ReadWriteExecute, GetSecurity(), HandleInheritability.None, false));
        }

        [Fact]
        public void CreateFromFile_MapNameNull()
        {
            using TempFile file = new TempFile(GetTestFilePath(), StandardCapacity);
            using MemoryMappedFile mmf = CreateFromFile(file, mapName: null);
            Assert.NotNull(mmf);
        }

        [Fact]
        public void CreateFromFile_MapNameEmpty()
        {
            using TempFile file = new TempFile(GetTestFilePath(), StandardCapacity);
            Assert.Throws<ArgumentException>(() => CreateFromFile(file, string.Empty));
        }

        [Fact]
        public void CreateFromFile_CapacityNegative()
        {
            using TempFile file = new TempFile(GetTestFilePath(), StandardCapacity);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => CreateFromFile(file, capacity: -1));
        }

        [Fact]
        public void CreateFromFile_CapacityZero_EmptyFile()
        {
            using TempFile file = new TempFile(GetTestFilePath(), 0);
            Assert.Throws<ArgumentException>(() => CreateFromFile(file, capacity: 0));
        }

        [Fact]
        public void CreateFromFile_CapacityLowerThanFileSize()
        {
            using TempFile file = new TempFile(GetTestFilePath(), StandardCapacity);
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateFromFile(file, capacity: 2048));
        }

        [Theory]
        [InlineData((MemoryMappedFileAccess)(-1))]
        [InlineData((MemoryMappedFileAccess)(6))]
        public void CreateFromFile_AccessOutOfRange(MemoryMappedFileAccess access)
        {
            using TempFile file = new TempFile(GetTestFilePath(), StandardCapacity);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => CreateFromFile(file, access: access));
        }

        [Fact]
        public void CreateFromFile_AccessWriteForbidden()
        {
            using TempFile file = new TempFile(GetTestFilePath(), StandardCapacity);
            AssertExtensions.Throws<ArgumentException>("access", () => CreateFromFile(file, access: MemoryMappedFileAccess.Write));
        }

        [Fact]
        public void CreateFromFile_AccessRead_CapacityGreaterThanFileSize()
        {
            using TempFile file = new TempFile(GetTestFilePath(), 0);
            Assert.Throws<ArgumentException>(() => CreateFromFile(file, access: MemoryMappedFileAccess.Read));
        }

        [Fact]
        public void CreateFromFile_SecurityNull()
        {
            using TempFile file = new TempFile(GetTestFilePath(), StandardCapacity);
            AssertExtensions.Throws<ArgumentNullException>("memoryMappedFileSecurity", () => CreateFromFile(file, security: null));
        }

        [Theory]
        [InlineData((HandleInheritability)(-1))]
        [InlineData((HandleInheritability)(2))]
        public void CreateFromFile_InheritabilityOutOfRange(HandleInheritability inheritability)
        {
            using TempFile file = new TempFile(GetTestFilePath(), StandardCapacity);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => CreateFromFile(file, inheritability: inheritability));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CreateFromFile_RespectLeaveOpen(bool leaveOpen)
        {
            using TempFile file = new TempFile(GetTestFilePath(), StandardCapacity);

            using FileStream fileStream = GetFileStreamWithSecurity(file.Path); // Created with full control
            Assert.True(fileStream.CanRead);
            Assert.True(fileStream.CanWrite);

            MemoryMappedFile mmf = CreateFromFile(fileStream, leaveOpen: leaveOpen);
            mmf.Dispose();

            Assert.Equal(leaveOpen, fileStream.CanRead);
            Assert.Equal(leaveOpen, fileStream.CanWrite);
        }

        [Fact]
        public void CreateFromFile_DisposeFileStream_BeforeOpeningMemoryMappedFile()
        {
            using TempFile file = new TempFile(GetTestFilePath(), StandardCapacity);

            using FileStream fileStream = GetFileStreamWithSecurity(file.Path);

            using MemoryMappedFile mmf = CreateFromFile(fileStream);
            
            fileStream.Dispose(); // Close it before accessing the memory mapped file data

            using (MemoryMappedViewAccessor mmva = mmf.CreateViewAccessor(0, 1, MemoryMappedFileAccess.ReadWrite))
            {
                byte b = mmva.ReadByte(0);
            }

            using (MemoryMappedViewStream mmvs = mmf.CreateViewStream(0, 1, MemoryMappedFileAccess.ReadWrite))
            {
                int b = mmvs.ReadByte();
            }
            
        }

        [Theory]
        //// //[InlineData(MemoryMappedFileRights.AccessSystemSecurity)] // mmf.GetAccessControl() excludes this right
        [InlineData(MemoryMappedFileRights.ChangePermissions)]
        [InlineData(MemoryMappedFileRights.CopyOnWrite)]
        [InlineData(MemoryMappedFileRights.Delete)]
        [InlineData(MemoryMappedFileRights.Execute)]
        [InlineData(MemoryMappedFileRights.FullControl)]
        [InlineData(MemoryMappedFileRights.Read)]
        [InlineData(MemoryMappedFileRights.ReadExecute)]
        [InlineData(MemoryMappedFileRights.ReadPermissions)]
        [InlineData(MemoryMappedFileRights.ReadWrite)]
        [InlineData(MemoryMappedFileRights.ReadWriteExecute)]
        [InlineData(MemoryMappedFileRights.TakeOwnership)]
        [InlineData(MemoryMappedFileRights.Write)]
        public void CreateFromFile_VerifySecurities(MemoryMappedFileRights mmfRights)
        {
            string path = GetTestFilePath();
            FileInfo fileInfo = new FileInfo(path);

            FileSystemRights fsRights = FileSystemRights.FullControl;
            AccessControlType controlType = AccessControlType.Allow;
            WellKnownSidType sid = WellKnownSidType.BuiltinUsersSid;
            SecurityIdentifier identity = new SecurityIdentifier(sid, null);

            FileSystemAccessRule fsAccessRule = new FileSystemAccessRule(identity, fsRights, controlType);
            AccessRule<MemoryMappedFileRights> mmfAccessRule = new AccessRule<MemoryMappedFileRights>(identity, mmfRights, controlType);

            FileSecurity expectedFileSecurity = new FileSecurity();
            MemoryMappedFileSecurity expectedMmfSecurity = new MemoryMappedFileSecurity();

            expectedFileSecurity.AddAccessRule(fsAccessRule);
            expectedMmfSecurity.AddAccessRule(mmfAccessRule);

            // FileInfo.Create will create the file
            // Notice the fsRights are the same that were passed to fsAccessRule
            using (FileStream fileStream = fileInfo.Create(FileMode.OpenOrCreate, fsRights, FileShare.ReadWrite, 4096, FileOptions.None, expectedFileSecurity))
            {
                using MemoryMappedFile mmf = MemoryMappedFileAcl.CreateFromFile(fileStream, CreateUniqueMapName(), StandardCapacity, MemoryMappedFileAccess.ReadWriteExecute, expectedMmfSecurity, HandleInheritability.None, leaveOpen: true);
                VerifyAccessSecurity(expectedFileSecurity, fileStream.GetAccessControl());
                VerifyAccessSecurity(expectedMmfSecurity, mmf.GetAccessControl());
            }

            fileInfo.Delete();
        }


        // CreateNew

        [Fact]
        public void CreateNew_MapNameEmpty()
        {
            Assert.Throws<ArgumentException>(() => CreateNew(mapName: string.Empty));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void CreateNew_CapacityOutOfRange(long capacity)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => CreateNew(capacity: capacity));
        }

        [ConditionalFact(typeof(MemoryMappedFileAclTests), nameof(IsAddressSpaceWithIntPtrSize4))]
        public void CreateNew_CapacityLargerThanLogicalAddressSpace()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => CreateNew(capacity: long.MaxValue));
        }

        [Theory]
        [InlineData((MemoryMappedFileAccess)(-1))]
        [InlineData((MemoryMappedFileAccess)(6))]
        public void CreateNew_AccessOutOfRange(MemoryMappedFileAccess access)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => CreateNew(access: access));
        }

        [Fact]
        public void CreateNew_AccessWriteNotAllowed()
        {
            AssertExtensions.Throws<ArgumentException>("access", () => CreateNew(access: MemoryMappedFileAccess.Write));
        }

        [Theory]
        [InlineData((MemoryMappedFileOptions)(-1))]
        [InlineData((MemoryMappedFileOptions)(1))]
        [InlineData((MemoryMappedFileOptions)(67108865))]
        public void CreateNew_OptionsOutOfRange(MemoryMappedFileOptions options)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => CreateNew(options: options));
        }

        [Theory]
        [InlineData((HandleInheritability)(-1))]
        [InlineData((HandleInheritability)(2))]
        public void CreateNew_InheritabilityOutOfRange(HandleInheritability inheritability)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => CreateNew(inheritability: inheritability));
        }

        [Fact]
        public void CreateNew_SecurityNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("memoryMappedFileSecurity", () => CreateNew(security: null));
        }

        [Fact]
        public void CreateNew_MapNameNull()
        {
            using MemoryMappedFile mmf = CreateNew(mapName: null);
            Assert.NotNull(mmf);
        }

        [Theory]
        // [InlineData(MemoryMappedFileRights.AccessSystemSecurity)] // mmf.GetAccessControl() excludes this right
        [InlineData(MemoryMappedFileRights.ChangePermissions)]
        [InlineData(MemoryMappedFileRights.CopyOnWrite)]
        [InlineData(MemoryMappedFileRights.Delete)]
        [InlineData(MemoryMappedFileRights.Execute)]
        [InlineData(MemoryMappedFileRights.FullControl)]
        [InlineData(MemoryMappedFileRights.Read)]
        [InlineData(MemoryMappedFileRights.ReadExecute)]
        [InlineData(MemoryMappedFileRights.ReadPermissions)]
        [InlineData(MemoryMappedFileRights.ReadWrite)]
        [InlineData(MemoryMappedFileRights.ReadWriteExecute)]
        [InlineData(MemoryMappedFileRights.TakeOwnership)]
        [InlineData(MemoryMappedFileRights.Write)]
        public void CreateNew_VerifySecurity(MemoryMappedFileRights rights)
        {
            string path = GetTestFilePath();
            using TempFile file = new TempFile(path, StandardCapacity);
            FileInfo fileInfo = new FileInfo(file.Path);

            AccessControlType controlType = AccessControlType.Allow;
            WellKnownSidType sid = WellKnownSidType.BuiltinUsersSid;
            SecurityIdentifier identity = new SecurityIdentifier(sid, null);

            AccessRule<MemoryMappedFileRights> accessRule
                = new AccessRule<MemoryMappedFileRights>(identity, rights, controlType);

            MemoryMappedFileSecurity expectedSecurity = new MemoryMappedFileSecurity();

            expectedSecurity.AddAccessRule(accessRule);

            using MemoryMappedFile mmf = MemoryMappedFileAcl.CreateNew(CreateUniqueMapName(), StandardCapacity, MemoryMappedFileAccess.ReadWriteExecute, MemoryMappedFileOptions.None, expectedSecurity, HandleInheritability.None);

            VerifyAccessSecurity(expectedSecurity, mmf.GetAccessControl());
        }


        // CreateOrOpen

        [Fact]
        public void CreateOrOpen_MapNameNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("mapName", () => CreateOrOpen(mapName: null));
        }

        [Fact]
        public void CreateOrOpen_MapNameEmpty()
        {
            Assert.Throws<ArgumentException>(() => CreateOrOpen(mapName: string.Empty));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void CreateOrOpen_CapacityOutOfRange(long capacity)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => CreateOrOpen(capacity: capacity));
        }

        [ConditionalFact(typeof(MemoryMappedFileAclTests), nameof(MemoryMappedFileAclTests.IsAddressSpaceWithIntPtrSize4))]
        public void CreateOrOpen_CapacityLargerThanLogicalAddressSpace()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => CreateOrOpen(capacity: long.MaxValue));
        }

        [Theory]
        [InlineData((MemoryMappedFileAccess)(-1))]
        [InlineData((MemoryMappedFileAccess)(6))]
        public void CreateOrOpen_AccessOutOfRange(MemoryMappedFileAccess access)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => CreateOrOpen(access: access));
        }

        [Theory]
        [InlineData((MemoryMappedFileOptions)(-1))]
        [InlineData((MemoryMappedFileOptions)(1))]
        [InlineData((MemoryMappedFileOptions)(67108865))]
        public void CreateOrOpen_OptionsOutOfRange(MemoryMappedFileOptions options)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => CreateOrOpen(options: options));
        }

        [Theory]
        [InlineData((HandleInheritability)(-1))]
        [InlineData((HandleInheritability)(2))]
        public void CreateOrOpen_InheritabilityOutOfRange(HandleInheritability inheritability)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => CreateOrOpen(inheritability: inheritability));
        }

        [Fact]
        public void CreateOrOpen_SecurityNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("memoryMappedFileSecurity", () => CreateOrOpen(security: null));
        }

        [Fact]
        public void CreateOrOpen_FileNotFound()
        {
            AssertExtensions.Throws<ArgumentException>("access", () => CreateOrOpen(access: MemoryMappedFileAccess.Write));
        }

        [Theory]
        //[InlineData(MemoryMappedFileRights.AccessSystemSecurity)] // mmf.GetAccessControl() excludes this right
        [InlineData(MemoryMappedFileRights.ChangePermissions)]
        [InlineData(MemoryMappedFileRights.CopyOnWrite)]
        [InlineData(MemoryMappedFileRights.Delete)]
        [InlineData(MemoryMappedFileRights.Execute)]
        [InlineData(MemoryMappedFileRights.FullControl)]
        [InlineData(MemoryMappedFileRights.Read)]
        [InlineData(MemoryMappedFileRights.ReadExecute)]
        [InlineData(MemoryMappedFileRights.ReadPermissions)]
        [InlineData(MemoryMappedFileRights.ReadWrite)]
        [InlineData(MemoryMappedFileRights.ReadWriteExecute)]
        [InlineData(MemoryMappedFileRights.TakeOwnership)]
        [InlineData(MemoryMappedFileRights.Write)]
        public void CreateOrOpen_VerifySecurity(MemoryMappedFileRights rights)
        {
            string path = GetTestFilePath();
            using TempFile file = new TempFile(path, StandardCapacity);
            FileInfo fileInfo = new FileInfo(file.Path);

            AccessControlType controlType = AccessControlType.Allow;
            WellKnownSidType sid = WellKnownSidType.BuiltinUsersSid;
            SecurityIdentifier identity = new SecurityIdentifier(sid, null);

            AccessRule<MemoryMappedFileRights> accessRule
                = new AccessRule<MemoryMappedFileRights>(identity, rights, controlType);

            MemoryMappedFileSecurity expectedSecurity = new MemoryMappedFileSecurity();

            expectedSecurity.AddAccessRule(accessRule);

            using MemoryMappedFile mmf = MemoryMappedFileAcl.CreateOrOpen(CreateUniqueMapName(), StandardCapacity, MemoryMappedFileAccess.ReadWriteExecute, MemoryMappedFileOptions.None, expectedSecurity, HandleInheritability.None);

            VerifyAccessSecurity(expectedSecurity, mmf.GetAccessControl());
        }


        // CreateFromFile

        private MemoryMappedFile CreateFromFile(
            TempFile file,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            HandleInheritability inheritability = HandleInheritability.None,
            bool leaveOpen = false)
        {
            return CreateFromFile(file, CreateUniqueMapName(), GetSecurity(), capacity, access, inheritability, leaveOpen);
        }

        private MemoryMappedFile CreateFromFile(
            TempFile file,
            MemoryMappedFileSecurity security,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            HandleInheritability inheritability = HandleInheritability.None,
            bool leaveOpen = false)
        {
            return CreateFromFile(file, CreateUniqueMapName(), security, capacity, access, inheritability, leaveOpen);
        }

        private MemoryMappedFile CreateFromFile(
            TempFile file,
            string mapName,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            HandleInheritability inheritability = HandleInheritability.None,
            bool leaveOpen = false)
        {
            return CreateFromFile(file, mapName, GetSecurity(), capacity, access, inheritability, leaveOpen);
        }

        private MemoryMappedFile CreateFromFile(
            TempFile file,
            string mapName,
            MemoryMappedFileSecurity security,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            HandleInheritability inheritability = HandleInheritability.None,
            bool leaveOpen = false)
        {
            using FileStream fileStream = GetFileStreamWithSecurity(file.Path);
            return CreateFromFile(fileStream, mapName, security, capacity, access, inheritability, leaveOpen);
        }

        private MemoryMappedFile CreateFromFile(
            FileStream fileStream,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            HandleInheritability inheritability = HandleInheritability.None,
            bool leaveOpen = false)
        {
            return CreateFromFile(fileStream, CreateUniqueMapName(), GetSecurity(), capacity, access, inheritability, leaveOpen);
        }

        private MemoryMappedFile CreateFromFile(
            FileStream fileStream,
            string mapName,
            MemoryMappedFileSecurity security,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            HandleInheritability inheritability = HandleInheritability.None,
            bool leaveOpen = false)
        {
            MemoryMappedFile mmf = MemoryMappedFileAcl.CreateFromFile(fileStream, mapName, capacity, access, security, inheritability, leaveOpen);
            if (security != null)
            {
                VerifyAccessSecurity(security, mmf.GetAccessControl());
            }
            return mmf;
        }


        // CreateNew

        private MemoryMappedFile CreateNew(
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            MemoryMappedFileOptions options = MemoryMappedFileOptions.None,
            HandleInheritability inheritability = HandleInheritability.None)
        {
            return CreateNew(CreateUniqueMapName(), GetSecurity(), capacity, access, options, inheritability);
        }

        private MemoryMappedFile CreateNew(
            MemoryMappedFileSecurity security,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            MemoryMappedFileOptions options = MemoryMappedFileOptions.None,
            HandleInheritability inheritability = HandleInheritability.None)
        {
            return CreateNew(CreateUniqueMapName(), security, capacity, access, options, inheritability);
        }

        private MemoryMappedFile CreateNew(
            string mapName,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            MemoryMappedFileOptions options = MemoryMappedFileOptions.None,
            HandleInheritability inheritability = HandleInheritability.None)
        {
            return CreateNew(mapName, GetSecurity(), capacity, access, options, inheritability);
        }

        private MemoryMappedFile CreateNew(
            string mapName,
            MemoryMappedFileSecurity security,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            MemoryMappedFileOptions options = MemoryMappedFileOptions.None,
            HandleInheritability inheritability = HandleInheritability.None)
        {
            MemoryMappedFile mmf = MemoryMappedFileAcl.CreateNew(mapName, capacity, access, options, security, inheritability);
            if (security != null)
            {
                VerifyAccessSecurity(security, mmf.GetAccessControl());
            }
            return mmf;
        }


        // CreateOrOpen

        private MemoryMappedFile CreateOrOpen(
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            MemoryMappedFileOptions options = MemoryMappedFileOptions.None,
            HandleInheritability inheritability = HandleInheritability.None)
        {
            return CreateOrOpen(CreateUniqueMapName(), GetSecurity(), capacity, access, options, inheritability);
        }

        private MemoryMappedFile CreateOrOpen(
             MemoryMappedFileSecurity security,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            MemoryMappedFileOptions options = MemoryMappedFileOptions.None,
            HandleInheritability inheritability = HandleInheritability.None)
        {
            return CreateOrOpen(CreateUniqueMapName(), security, capacity, access, options, inheritability);
        }

        private MemoryMappedFile CreateOrOpen(
            string mapName,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            MemoryMappedFileOptions options = MemoryMappedFileOptions.None,
            HandleInheritability inheritability = HandleInheritability.None)
        {
            return CreateOrOpen(mapName, GetSecurity(), capacity, access, options, inheritability);
        }

        private MemoryMappedFile CreateOrOpen(
            string mapName,
            MemoryMappedFileSecurity security,
            long capacity = StandardCapacity,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWriteExecute,
            MemoryMappedFileOptions options = MemoryMappedFileOptions.None,
            HandleInheritability inheritability = HandleInheritability.None)
        {
            MemoryMappedFile mmf = MemoryMappedFileAcl.CreateOrOpen(mapName, capacity, access, options, security, inheritability);
            if (security != null)
            {
                VerifyAccessSecurity(security, mmf.GetAccessControl());
            }
            return mmf;
        }


        // Private shared

        private const long StandardCapacity = 4096;

        private static bool IsAddressSpaceWithIntPtrSize4 => IntPtr.Size == 4;

        private static string CreateUniqueMapName() => Guid.NewGuid().ToString("N");

        private MemoryMappedFileSecurity GetSecurity() => GetSecurity(MemoryMappedFileRights.FullControl, AccessControlType.Allow);

        private MemoryMappedFileSecurity GetSecurity(MemoryMappedFileRights rights, AccessControlType controlType) => GetSecurity(WellKnownSidType.BuiltinUsersSid, rights, controlType);

        private MemoryMappedFileSecurity GetSecurity(WellKnownSidType sid, MemoryMappedFileRights rights, AccessControlType controlType)
        {
            var security = new MemoryMappedFileSecurity();

            var identity = new SecurityIdentifier(sid, null);
            var accessRule = new AccessRule<MemoryMappedFileRights>(identity, rights, controlType);
            security.AddAccessRule(accessRule);

            return security;
        }

        // If the FileStream object is not created using the FileSystemAcl extension method that takes a FileSecurity object,
        // then attempting to create a MemoryMappedFile object using the MemoryMappedFileAcl extension method will throw UnauthorizedException.
        private FileStream GetFileStreamWithSecurity(
            string path,
            FileSystemRights rights = FileSystemRights.FullControl,
            AccessControlType controlType = AccessControlType.Allow)
        {
            var fileSecurity = new FileSecurity();
            var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var fileAccessRule = new FileSystemAccessRule(identity, rights, controlType);
            fileSecurity.AddAccessRule(fileAccessRule);

            FileInfo fileInfo = new FileInfo(path);

            return fileInfo.Create(FileMode.OpenOrCreate, FileSystemRights.FullControl, FileShare.ReadWrite, 4096, FileOptions.None, fileSecurity);
        }
    }
}
