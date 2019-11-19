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
        private const PipeDirection DefaultPipeDirection = PipeDirection.InOut;
        private const int DefaultNumberOfServerInstances = 1;
        private const PipeTransmissionMode DefaultPipeTransmissionMode = PipeTransmissionMode.Byte;
        private const PipeOptions DefaultPipeOptions = PipeOptions.None;
        private const int DefaultInBufferSize = 1;
        private const int DefaultOutBufferSize = 1;
        private const HandleInheritability DefaultInheritability = HandleInheritability.None;
        private const PipeAccessRights DefaultAdditionalPipeAccessRights = 0;

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

        // Synchronize is handled in a special way inside the PipeAccessRuleInstance constructor when creating
        // the access mask. If Deny is specified, Synchronize gets removed from the rights.
        [Fact]
        public void Create_SynchronizeSecurity()
        {
            GetPipeSecurity(WellKnownSidType.BuiltinUsersSid, PipeAccessRights.Synchronize, AccessControlType.Allow);

            Assert.Throws<ArgumentException>("accessMask", () =>
            {
                GetPipeSecurity(WellKnownSidType.BuiltinUsersSid, PipeAccessRights.Synchronize, AccessControlType.Deny);
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
        [InlineData((PipeDirection)(int.MinValue))]
        [InlineData((PipeDirection)0)]
        [InlineData((PipeDirection)4)]
        [InlineData((PipeDirection)(int.MaxValue))]
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
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        public void Create_InvalidInBufferSize(int inBufferSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), inBufferSize: inBufferSize).Dispose();
            });
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        public void Create_InvalidOutBufferSize(int outBufferSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), outBufferSize: outBufferSize).Dispose();
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
                CreateAndVerifyNamedPipe(GetRandomName(), GetBasicPipeSecurity(), inheritability: inheritability).Dispose();
            });
        }

        // Two rights were excluded from this array:
        // - Synchronize has a special unit test case: Create_SynchronizeSecurity
        // - AccessSystemSecurity throws 'A required privilege is not held by the client'
        private static PipeAccessRights[] _mostRights = new[] { PipeAccessRights.ReadData, PipeAccessRights.WriteData, PipeAccessRights.CreateNewInstance, PipeAccessRights.ReadExtendedAttributes, PipeAccessRights.WriteExtendedAttributes, PipeAccessRights.ReadAttributes, PipeAccessRights.WriteAttributes, PipeAccessRights.Write, PipeAccessRights.Delete, PipeAccessRights.ReadPermissions, PipeAccessRights.Read, PipeAccessRights.ReadWrite, PipeAccessRights.ChangePermissions, PipeAccessRights.TakeOwnership, PipeAccessRights.FullControl };

        private static PipeAccessRights[] _bitWisePipeAccessRights = new[]
        {
            PipeAccessRights.ChangePermissions | PipeAccessRights.ReadPermissions | PipeAccessRights.WriteExtendedAttributes,
            PipeAccessRights.ReadData | PipeAccessRights.WriteData
        };

        //public static IEnumerable<object[]> Create_AdditionalAccessRights_MemberData() =>
        //    from rights in _mostRights
        //    select new object[] { rights };

        //[Theory]
        //[MemberData(nameof(Create_AdditionalAccessRights_MemberData))]
        //public void Create_AdditionalAccessRights(PipeAccessRights additionalAccessRights)
        //{
        //    PipeSecurity zeroRightsSecurity = GetPipeSecurity(WellKnownSidType.BuiltinUsersSid, 0, AccessControlType.Allow);

        //    PipeSecurity additionalSecurity = GetPipeSecurity(WellKnownSidType.BuiltinUsersSid, additionalAccessRights, AccessControlType.Allow);

        //    using NamedPipeServerStream pipe = CreateNamedPipe(GetRandomName(), zeroRightsSecurity, additionalAccessRights: additionalAccessRights);
        //    PipeSecurity actualSecurity = pipe.GetAccessControl();
        //    VerifyPipeSecurity(additionalSecurity, actualSecurity);
        //}

        private static IEnumerable<PipeAccessRights> _combinedPipeAccessRights = _mostRights.Concat(_bitWisePipeAccessRights);

        public static IEnumerable<object[]> Create_ValidParameters_MemberData() =>
            from options in new[] { PipeOptions.None, PipeOptions.Asynchronous, PipeOptions.WriteThrough }
            from direction in new[] { PipeDirection.In, PipeDirection.Out, PipeDirection.InOut }
            from transmissionMode in new[] { PipeTransmissionMode.Byte, PipeTransmissionMode.Message }
            from inheritability in new[] { HandleInheritability.None, HandleInheritability.Inheritable }
            from inBufferSize in new[] { 0, 1 }
            from outBufferSize in new[] { 0, 1 }
            from maxNumberOfServerInstances in new[] { -1, 1, 254 }
            from rights in _combinedPipeAccessRights
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
            int inBufferSize = DefaultInBufferSize,
            int outBufferSize = DefaultOutBufferSize,
            HandleInheritability inheritability = DefaultInheritability,
            PipeAccessRights additionalAccessRights = DefaultAdditionalPipeAccessRights)
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
            int inBufferSize = DefaultInBufferSize,
            int outBufferSize = DefaultOutBufferSize,
            HandleInheritability inheritability = DefaultInheritability,
            PipeAccessRights additionalAccessRights = DefaultAdditionalPipeAccessRights)
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
