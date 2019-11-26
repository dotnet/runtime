using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class AnonymousPipeServerStreamAclTests : PipeServerStreamAclTestBase
    {
        private const PipeDirection DefaultPipeDirection = PipeDirection.In;
        private const HandleInheritability DefaultInheritability = HandleInheritability.None;
        private const int DefaultBufferSize = 1;

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
        [InlineData((PipeDirection)(int.MinValue))]
        [InlineData((PipeDirection)0)]
        [InlineData((PipeDirection)4)]
        [InlineData((PipeDirection)(int.MaxValue))]
        public void Create_InvalidPipeDirection(PipeDirection direction)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyAnonymousPipe(GetBasicPipeSecurity(), direction).Dispose();
            });
        }

        [Theory]
        [InlineData((HandleInheritability)(int.MinValue))]
        [InlineData((HandleInheritability)(-1))]
        [InlineData((HandleInheritability)2)]
        [InlineData((HandleInheritability)(int.MaxValue))]
        public void Create_InvalidInheritability(HandleInheritability inheritability)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyAnonymousPipe(GetBasicPipeSecurity(), inheritability: inheritability).Dispose();
            });
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        public void Create_InvalidBufferSize(int bufferSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyAnonymousPipe(GetBasicPipeSecurity(), bufferSize: bufferSize).Dispose();
            });
        }

        public static IEnumerable<object[]> Create_ValidParameters_MemberData() =>
            from direction in new[] { PipeDirection.In, PipeDirection.Out }
            from inheritability in Enum.GetValues(typeof(HandleInheritability)).Cast<HandleInheritability>()
            from bufferSize in new[] { 0, 1 }
            select new object[] { direction, inheritability, bufferSize };

        [Theory]
        [MemberData(nameof(Create_ValidParameters_MemberData))]
        public void Create_ValidParameters(PipeDirection direction, HandleInheritability inheritability, int bufferSize)
        {
            CreateAndVerifyAnonymousPipe(GetBasicPipeSecurity(), direction, inheritability, bufferSize).Dispose();
        }

        public static IEnumerable<object[]> Create_CombineRightsAndAccessControl_MemberData() =>
            from rights in Enum.GetValues(typeof(PipeAccessRights)).Cast<PipeAccessRights>()
            from accessControl in new[] { AccessControlType.Allow, AccessControlType.Deny }
            select new object[] { rights, accessControl };

        // These tests match NetFX behavior
        [Theory]
        [MemberData(nameof(Create_CombineRightsAndAccessControl_MemberData))]
        public void Create_CombineRightsAndAccessControl(PipeAccessRights rights, AccessControlType accessControl)
        {
            // These are the two cases that create a valid pipe when using Allow
            if ((rights == PipeAccessRights.FullControl || rights == PipeAccessRights.ReadWrite) &&
                accessControl == AccessControlType.Allow)
            {
                VerifyValidSecurity(rights, accessControl);
            }
            // When creating the PipeAccessRule for the PipeSecurity, the PipeAccessRule constructor calls AccessMaskFromRights, which explicilty removes the Synchronize bit from rights when AccessControlType is Deny
            // and rights is not FullControl, so using Synchronize with Deny is not allowed
            else  if (rights == PipeAccessRights.Synchronize && accessControl == AccessControlType.Deny)
            {
                Assert.Throws<ArgumentException>("accessMask", () =>
                {
                    PipeSecurity security = GetPipeSecurity(WellKnownSidType.BuiltinUsersSid, PipeAccessRights.Synchronize, AccessControlType.Deny);
                });
            }
            // Any other case is not authorized
            else
            {
                PipeSecurity security = GetPipeSecurity(WellKnownSidType.BuiltinUsersSid, rights, accessControl);
                Assert.Throws<UnauthorizedAccessException>(() =>
                {
                    AnonymousPipeServerStreamAcl.Create(DefaultPipeDirection, DefaultInheritability, DefaultBufferSize, security).Dispose();
                });
            }
        }

        [Theory]
        [InlineData(PipeAccessRights.ReadWrite | PipeAccessRights.Synchronize, AccessControlType.Allow)]
        public void Create_ValidBitwiseRightsSecurity(PipeAccessRights rights, AccessControlType accessControl)
        {
            VerifyValidSecurity(rights, accessControl);
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
