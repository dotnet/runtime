// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class AnonymousPipeServerStreamAclTests : PipeServerStreamAclTestBase
    {
        [Fact]
        public void Create_NullSecurity()
        {
            CreateAndVerifyAnonymousPipe(expectedSecurity: null).Dispose();
        }

        [Fact]
        public void Create_NotSupportedPipeDirection()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                CreateAndVerifyAnonymousPipe(GetBasicPipeSecurity(), PipeDirection.InOut).Dispose();
            });
        }

        [Theory]
        [MemberData(nameof(Create_InvalidPipeDirection_MemberData))]
        public void Create_InvalidPipeDirection(PipeDirection direction)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyAnonymousPipe(GetBasicPipeSecurity(), direction).Dispose();
            });
        }

        [Theory]
        [MemberData(nameof(Create_InvalidInheritability_MemberData))]
        public void Create_InvalidInheritability(HandleInheritability inheritability)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyAnonymousPipe(GetBasicPipeSecurity(), inheritability: inheritability).Dispose();
            });
        }

        [Theory]
        [MemberData(nameof(Create_InvalidBufferSize_MemberData))]
        public void Create_InvalidBufferSize(int bufferSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyAnonymousPipe(GetBasicPipeSecurity(), bufferSize: bufferSize).Dispose();
            });
        }

        public static IEnumerable<object[]> Create_ValidParameters_MemberData() =>
            from direction in new[] { PipeDirection.In, PipeDirection.Out }
            from inheritability in new[] { HandleInheritability.None, HandleInheritability.Inheritable }
            from bufferSize in new[] { 0, 1 }
            select new object[] { direction, inheritability, bufferSize };

        [Theory]
        [MemberData(nameof(Create_ValidParameters_MemberData))]
        public void Create_ValidParameters(PipeDirection direction, HandleInheritability inheritability, int bufferSize)
        {
            CreateAndVerifyAnonymousPipe(GetBasicPipeSecurity(), direction, inheritability, bufferSize).Dispose();
        }

        public static IEnumerable<object[]> Create_CombineRightsAndAccessControl_MemberData() =>
            from rights in s_combinedPipeAccessRights
            from accessControl in new[] { AccessControlType.Allow, AccessControlType.Deny }
            select new object[] { rights, accessControl };

        // These tests match .NET Framework behavior
        [Theory]
        [MemberData(nameof(Create_CombineRightsAndAccessControl_MemberData))]
        public void Create_CombineRightsAndAccessControl(PipeAccessRights rights, AccessControlType accessControl)
        {
            // These are the only two rights that allow creating a pipe when using Allow
            if (accessControl == AccessControlType.Allow &&
                (rights == PipeAccessRights.FullControl || rights == PipeAccessRights.ReadWrite))
            {
                VerifyValidSecurity(rights, accessControl);
            }
            // Any other combination is not authorized
            else
            {
                PipeSecurity security = GetPipeSecurity(WellKnownSidType.BuiltinUsersSid, rights, accessControl);
                Assert.Throws<UnauthorizedAccessException>(() =>
                {
                    AnonymousPipeServerStreamAcl.Create(DefaultPipeDirection, DefaultInheritability, DefaultBufferSize, security).Dispose();
                });
            }
        }

        [Fact]
        public void Create_ValidBitwiseRightsSecurity()
        {
            // Synchronize gets removed from the bitwise combination,
            // but ReadWrite (an allowed right) should remain untouched
            VerifyValidSecurity(PipeAccessRights.ReadWrite | PipeAccessRights.Synchronize, AccessControlType.Allow);
        }

        private void VerifyValidSecurity(PipeAccessRights rights, AccessControlType accessControl)
        {
            PipeSecurity security = GetPipeSecurity(WellKnownSidType.BuiltinUsersSid, rights, accessControl);
            CreateAndVerifyAnonymousPipe(security).Dispose();
        }

        private AnonymousPipeServerStream CreateAndVerifyAnonymousPipe(
            PipeSecurity expectedSecurity,
            PipeDirection direction = DefaultPipeDirection,
            HandleInheritability inheritability = DefaultInheritability,
            int bufferSize = DefaultBufferSize)
        {
            AnonymousPipeServerStream pipe = AnonymousPipeServerStreamAcl.Create(direction, inheritability, bufferSize, expectedSecurity);
            Assert.NotNull(pipe);

            if (expectedSecurity != null)
            {
                PipeSecurity actualSecurity = pipe.GetAccessControl();
                VerifyPipeSecurity(expectedSecurity, actualSecurity);
            }

            return pipe;
        }

    }
}
