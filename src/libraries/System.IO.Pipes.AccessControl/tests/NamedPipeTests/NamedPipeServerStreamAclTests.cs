// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class NamedPipeServerStreamAclTests : PipeServerStreamAclTestBase
    {
        private const int DefaultNumberOfServerInstances = 1;
        private const PipeTransmissionMode DefaultPipeTransmissionMode = PipeTransmissionMode.Byte;
        private const PipeOptions DefaultPipeOptions = PipeOptions.None;

        [Fact]
        public void Create_NullSecurity()
        {
            CreateNamedPipe(GetRandomName(), expectedSecurity: null).Dispose();
            CreateNamedPipe(GetRandomName(), expectedSecurity: null, options: PipeOptions.WriteThrough).Dispose();
            CreateNamedPipe(GetRandomName(), expectedSecurity: null, options: PipeOptions.Asynchronous).Dispose();
        }

        [Theory]
        [InlineData((PipeOptions)(-1))]
        [InlineData((PipeOptions)1)]
        [InlineData((PipeOptions)int.MaxValue)]
        public void Create_InvalidOptions(PipeOptions options)
        {
            Assert.Throws<ArgumentOutOfRangeException>("options", () =>
            {
                CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), options: options).Dispose();
            });
        }

        [Theory]
        [InlineData(PipeOptions.None)]
        [InlineData(PipeOptions.Asynchronous)]
        [InlineData(PipeOptions.WriteThrough)]
        [InlineData(PipeOptions.Asynchronous | PipeOptions.WriteThrough)]
        public void Create_ValidOptions(PipeOptions options)
        {
            CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), options: options).Dispose();
        }

        // Creating a pipe with CurrentUserOnly should be allowed only when the passed pipeSecurity is null.
        [Fact]
        public void Create_NullSecurity_PipeOptionsCurrentUserOnly()
        {
            using NamedPipeServerStream pipe = CreateNamedPipe(GetRandomName(), null, options: PipeOptions.CurrentUserOnly);
            PipeSecurity actualSecurity = pipe.GetAccessControl();
            PipeSecurity expectedSecurity = GetPipeSecurityForCurrentUserOnly();
            VerifyPipeSecurity(expectedSecurity, actualSecurity);
        }

        // We do not allow using PipeOptions.CurrentUserOnly and passing a PipeSecurity object at the same time,
        // because the Create method will force the usage of a custom PipeSecurity instance assigned to the
        // current user with full control allowed
        [Fact]
        public void Create_ValidSecurity_PipeOptionsCurrentUserOnly()
        {
            Assert.Throws<ArgumentException>("pipeSecurity", () =>
            {
                CreateNamedPipe(GetRandomName(), GetBasicPipeSecurity(), options: PipeOptions.CurrentUserOnly);
            });
        }

        [Fact]
        public void Create_InvalidName()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                CreateNamedPipe(pipeName: "", GetBasicPipeSecurity());
            });

            Assert.Throws<ArgumentNullException>("pipeName", () =>
            {
                CreateNamedPipe(pipeName: null, GetBasicPipeSecurity());
            });

            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () =>
            {
                CreateNamedPipe(pipeName: "anonymous", GetBasicPipeSecurity());
            });
        }

        [Theory]
        [MemberData(nameof(Create_InvalidPipeDirection_MemberData))]
        public void Create_InvalidPipeDirection(PipeDirection direction)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), direction: direction).Dispose();
            });
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        [InlineData(255)]
        [InlineData(int.MaxValue)]
        public void Create_InvalidMaxNumberOfServerInstances(int maxNumberOfServerInstances)
        {
            Assert.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () =>
            {
                CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), maxNumberOfServerInstances: maxNumberOfServerInstances).Dispose();
            });
        }

        [Theory]
        [InlineData(-1)] // We interpret -1 as MaxAllowedServerInstances.
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(254)]
        public void Create_ValidMaxNumberOfServerInstances(int instances)
        {
            CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), maxNumberOfServerInstances: instances).Dispose();
        }

        [Theory]
        [InlineData((PipeTransmissionMode)(-1))]
        [InlineData((PipeTransmissionMode)2)]
        public void Create_InvalidTransmissionMode(PipeTransmissionMode transmissionMode)
        {
            Assert.Throws<ArgumentOutOfRangeException>("transmissionMode", () =>
            {
                CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), transmissionMode: transmissionMode).Dispose();
            });
        }

        [Theory]
        [MemberData(nameof(Create_InvalidBufferSize_MemberData))]
        public void Create_InvalidInBufferSize(int inBufferSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), inBufferSize: inBufferSize).Dispose();
            });
        }

        [Theory]
        [MemberData(nameof(Create_InvalidBufferSize_MemberData))]
        public void Create_InvalidOutBufferSize(int outBufferSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), outBufferSize: outBufferSize).Dispose();
            });
        }

        [Theory]
        [MemberData(nameof(Create_InvalidInheritability_MemberData))]
        public void Create_InvalidInheritability(HandleInheritability inheritability)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), inheritability: inheritability).Dispose();
            });
        }

        [Theory]
        [InlineData(PipeAccessRights.Read)]
        [InlineData(PipeAccessRights.ReadExtendedAttributes)]
        [InlineData(PipeAccessRights.ReadAttributes)]
        [InlineData(PipeAccessRights.ReadPermissions)]
        [InlineData(PipeAccessRights.Write)]
        [InlineData(PipeAccessRights.WriteExtendedAttributes)]
        [InlineData(PipeAccessRights.WriteAttributes)]
        public void Create_InvalidAdditionalAccessRights(PipeAccessRights additionalAccessRights)
        {
            // GetBasicPipeSecurity returns an object created with PipeAccessRights.ReadWrite as default. This enum is formed by:
            //     - PipeAccessRights.Read: This enum is formed by:
            //         - PipeAccessRights.ReadData | PipeAccessRights.ReadExtendedAttributes | PipeAccessRights.ReadAttributes | PipeAccessRights.ReadPermissions
            //     - PipeAccessRights.Write: This enum is formed by:
            //         - PipeAccessRights.WriteData | PipeAccessRights.WriteExtendedAttributes | PipeAccessRights.WriteAttributes

            // additionalAccessRights gets bitwise merged with the 'dwOpenMode' parameter we pass to CreateNamedPipeW.
            // This parameter can acquire any of the values described here: https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-createnamedpipea
            // It's particularly important to mention that two of the accepted values collide with the value of two PipeAccessRights enum values:
            // - ReadData (0x1): Same value as PIPE_ACCESS_INBOUND
            // - WriteData (0x2): Same value as PIPE_ACCESS_OUTBOUND

            // Any other value will throw with the message 'The parameter is incorrect.'
            Assert.Throws<IOException>(() =>
            {
                Create_AdditionalAccessRights(additionalAccessRights).Dispose();
            });
        }

        [Theory]
        [InlineData(PipeAccessRights.CreateNewInstance)]
        [InlineData(PipeAccessRights.Delete)]
        public void Create_WindowsNotAcceptedAdditionalAccessRights(PipeAccessRights additionalAccessRights)
        {
            // Exception message: "The parameter is incorrect."
            // Neither CreateNewInstance (0x4) nor Delete (0x10000) collide with any of the dwOpenMode values that get into the bitwise combination:
            // PipeOptions, PipeDirection, Interop.Kernel32.FileOperations.FILE_FLAG_FIRST_PIPE_INSTANCE
            // But Windows does not accept them anyway
            Assert.Throws<IOException>(() =>
            {
                Create_AdditionalAccessRights(additionalAccessRights).Dispose();
            });
        }

        [Fact]
        public void Create_NotEnoughPrivilegesAdditionalAccessRights()
        {
            // Exception message: "A required privilege is not held by the client"
            Assert.Throws<IOException>(() =>
            {
                Create_AdditionalAccessRights(PipeAccessRights.AccessSystemSecurity).Dispose();
            });
        }

        [Theory]
        [InlineData(PipeAccessRights.ReadData)]
        [InlineData(PipeAccessRights.WriteData)]
        [InlineData(PipeAccessRights.ChangePermissions)]
        [InlineData(PipeAccessRights.TakeOwnership)]
        public void Create_ValidAdditionalAccessRights(PipeAccessRights additionalAccessRights)
        {
            using var pipe = Create_AdditionalAccessRights(additionalAccessRights);

            // This contains the rights added to BasicPipeSecurity plus the one we are testing
            PipeSecurity expectedPipeSecurity = GetPipeSecurity(WellKnownSidType.BuiltinUsersSid, additionalAccessRights | PipeAccessRights.ReadWrite, AccessControlType.Allow);

            // additional should be applied to the pipe, so actual should be identical to expected
            PipeSecurity actualPipeSecurity = pipe.GetAccessControl();

            VerifyPipeSecurity(expectedPipeSecurity, actualPipeSecurity);
        }

        private NamedPipeServerStream Create_AdditionalAccessRights(PipeAccessRights additionalAccessRights)
        {
            // GetBasicPipeSecurity returns an object created with PipeAccessRights.ReadWrite as default
            PipeSecurity initialPipeSecurity = GetBasicPipeSecurity();
            return CreateNamedPipe(GetRandomName(), initialPipeSecurity, additionalAccessRights: additionalAccessRights);
        }

        public static IEnumerable<object[]> Create_ValidParameters_MemberData() =>
            from options in new[] { PipeOptions.None, PipeOptions.Asynchronous, PipeOptions.WriteThrough }
            from direction in new[] { PipeDirection.In, PipeDirection.Out, PipeDirection.InOut }
            from transmissionMode in new[] { PipeTransmissionMode.Byte, PipeTransmissionMode.Message }
            from inheritability in new[] { HandleInheritability.None, HandleInheritability.Inheritable }
            from inBufferSize in new[] { 0, 1 }
            from outBufferSize in new[] { 0, 1 }
            from maxNumberOfServerInstances in new[] { -1, 1, 254 }
            from rights in s_combinedPipeAccessRights
            from controlType in new[] { AccessControlType.Allow, AccessControlType.Deny }
            select new object[] { options, direction, transmissionMode, inheritability, inBufferSize, outBufferSize, maxNumberOfServerInstances, rights, controlType };

        [Theory]
        [MemberData(nameof(Create_ValidParameters_MemberData))]
        public void Create_ValidParameters(PipeOptions options, PipeDirection direction, PipeTransmissionMode transmissionMode, HandleInheritability inheritability, int inBufferSize, int outBufferSize, int maxNumberOfServerInstances, PipeAccessRights rights, AccessControlType controlType)
        {
            if (controlType != AccessControlType.Deny && (rights & ~PipeAccessRights.Synchronize) != 0)
            {
                PipeSecurity security = GetPipeSecurity(WellKnownSidType.BuiltinUsersSid, rights, controlType);
                CreateAndVerifyNamedPipe(GetRandomName(), security, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, inheritability, 0).Dispose();
            }
        }

        private NamedPipeServerStream CreateAndVerifyNamedPipe(
            string pipeName,
            PipeSecurity expectedSecurity,
            PipeDirection direction = DefaultPipeDirection,
            int maxNumberOfServerInstances = DefaultNumberOfServerInstances,
            PipeTransmissionMode transmissionMode = DefaultPipeTransmissionMode,
            PipeOptions options = DefaultPipeOptions,
            int inBufferSize = DefaultBufferSize,
            int outBufferSize = DefaultBufferSize,
            HandleInheritability inheritability = DefaultInheritability,
            PipeAccessRights additionalAccessRights = 0)
        {
            NamedPipeServerStream pipe = CreateNamedPipe(pipeName, expectedSecurity, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, inheritability, additionalAccessRights);

            if (expectedSecurity != null)
            {
                PipeSecurity actualSecurity = pipe.GetAccessControl();
                VerifyPipeSecurity(expectedSecurity, actualSecurity);
            }
            return pipe;
        }

        private NamedPipeServerStream CreateNamedPipe(
            string pipeName,
            PipeSecurity expectedSecurity,
            PipeDirection direction = DefaultPipeDirection,
            int maxNumberOfServerInstances = DefaultNumberOfServerInstances,
            PipeTransmissionMode transmissionMode = DefaultPipeTransmissionMode,
            PipeOptions options = DefaultPipeOptions,
            int inBufferSize = DefaultBufferSize,
            int outBufferSize = DefaultBufferSize,
            HandleInheritability inheritability = DefaultInheritability,
            PipeAccessRights additionalAccessRights = 0)
        {
            NamedPipeServerStream pipe = NamedPipeServerStreamAcl.Create(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, expectedSecurity, inheritability, additionalAccessRights);
            Assert.NotNull(pipe);
            return pipe;
        }

        // This is the code we use in the Create method called by the NamedPipeServerStream constructor
        private PipeSecurity GetPipeSecurityForCurrentUserOnly()
        {
            PipeSecurity security = new PipeSecurity();

            using WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            SecurityIdentifier identifier = currentIdentity.Owner;
            PipeAccessRule rule = new PipeAccessRule(identifier, PipeAccessRights.FullControl, AccessControlType.Allow);
            security.AddAccessRule(rule);
            security.SetOwner(identifier);

            return security;
        }
    }
}
