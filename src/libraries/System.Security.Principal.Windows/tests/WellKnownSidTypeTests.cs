// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Principal;
using Xunit;

public class WellKnownSidTypeTests
{
    public static bool AccountIsDomainJoined()
    {
        using (var identity = WindowsIdentity.GetCurrent())
            return identity.Owner.AccountDomainSid != null;
    }

    [ConditionalTheory(nameof(AccountIsDomainJoined))]
    [InlineData(WellKnownSidType.NullSid)]
    [InlineData(WellKnownSidType.WorldSid)]
    [InlineData(WellKnownSidType.LocalSid)]
    [InlineData(WellKnownSidType.CreatorOwnerSid)]
    [InlineData(WellKnownSidType.CreatorGroupSid)]
    [InlineData(WellKnownSidType.CreatorOwnerServerSid)]
    [InlineData(WellKnownSidType.CreatorGroupServerSid)]
    [InlineData(WellKnownSidType.NTAuthoritySid)]
    [InlineData(WellKnownSidType.DialupSid)]
    [InlineData(WellKnownSidType.NetworkSid)]
    [InlineData(WellKnownSidType.BatchSid)]
    [InlineData(WellKnownSidType.InteractiveSid)]
    [InlineData(WellKnownSidType.ServiceSid)]
    [InlineData(WellKnownSidType.AnonymousSid)]
    [InlineData(WellKnownSidType.ProxySid)]
    [InlineData(WellKnownSidType.EnterpriseControllersSid)]
    [InlineData(WellKnownSidType.SelfSid)]
    [InlineData(WellKnownSidType.AuthenticatedUserSid)]
    [InlineData(WellKnownSidType.RestrictedCodeSid)]
    [InlineData(WellKnownSidType.TerminalServerSid)]
    [InlineData(WellKnownSidType.RemoteLogonIdSid)]
    [InlineData(WellKnownSidType.LocalSystemSid)]
    [InlineData(WellKnownSidType.LocalServiceSid)]
    [InlineData(WellKnownSidType.NetworkServiceSid)]
    [InlineData(WellKnownSidType.BuiltinDomainSid)]
    [InlineData(WellKnownSidType.BuiltinAdministratorsSid)]
    [InlineData(WellKnownSidType.BuiltinUsersSid)]
    [InlineData(WellKnownSidType.BuiltinGuestsSid)]
    [InlineData(WellKnownSidType.BuiltinPowerUsersSid)]
    [InlineData(WellKnownSidType.BuiltinAccountOperatorsSid)]
    [InlineData(WellKnownSidType.BuiltinSystemOperatorsSid)]
    [InlineData(WellKnownSidType.BuiltinPrintOperatorsSid)]
    [InlineData(WellKnownSidType.BuiltinBackupOperatorsSid)]
    [InlineData(WellKnownSidType.BuiltinReplicatorSid)]
    [InlineData(WellKnownSidType.BuiltinPreWindows2000CompatibleAccessSid)]
    [InlineData(WellKnownSidType.BuiltinRemoteDesktopUsersSid)]
    [InlineData(WellKnownSidType.BuiltinNetworkConfigurationOperatorsSid)]
    [InlineData(WellKnownSidType.AccountAdministratorSid)]
    [InlineData(WellKnownSidType.AccountGuestSid)]
    [InlineData(WellKnownSidType.AccountKrbtgtSid)]
    [InlineData(WellKnownSidType.AccountDomainAdminsSid)]
    [InlineData(WellKnownSidType.AccountDomainUsersSid)]
    [InlineData(WellKnownSidType.AccountDomainGuestsSid)]
    [InlineData(WellKnownSidType.AccountComputersSid)]
    [InlineData(WellKnownSidType.AccountControllersSid)]
    [InlineData(WellKnownSidType.AccountCertAdminsSid)]
    [InlineData(WellKnownSidType.AccountSchemaAdminsSid)]
    [InlineData(WellKnownSidType.AccountEnterpriseAdminsSid)]
    [InlineData(WellKnownSidType.AccountPolicyAdminsSid)]
    [InlineData(WellKnownSidType.AccountRasAndIasServersSid)]
    [InlineData(WellKnownSidType.NtlmAuthenticationSid)]
    [InlineData(WellKnownSidType.DigestAuthenticationSid)]
    [InlineData(WellKnownSidType.SChannelAuthenticationSid)]
    [InlineData(WellKnownSidType.ThisOrganizationSid)]
    [InlineData(WellKnownSidType.OtherOrganizationSid)]
    [InlineData(WellKnownSidType.BuiltinIncomingForestTrustBuildersSid)]
    [InlineData(WellKnownSidType.BuiltinPerformanceMonitoringUsersSid)]
    [InlineData(WellKnownSidType.BuiltinPerformanceLoggingUsersSid)]
    [InlineData(WellKnownSidType.BuiltinAuthorizationAccessSid)]
    [InlineData(WellKnownSidType.WinBuiltinTerminalServerLicenseServersSid)]
    public void CanCreateSecurityIdentifierFromWellKnownSidType(WellKnownSidType sidType)
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var currentDomainSid = identity.Owner.AccountDomainSid;
            var wellKnownSidInstance = new SecurityIdentifier(sidType, currentDomainSid);

            Assert.True(wellKnownSidInstance.IsWellKnown(sidType));
        }
    }

    [ConditionalTheory(nameof(AccountIsDomainJoined))]
    [InlineData(WellKnownSidType.WinBuiltinDCOMUsersSid)]
    [InlineData(WellKnownSidType.WinBuiltinIUsersSid)]
    [InlineData(WellKnownSidType.WinIUserSid)]
    [InlineData(WellKnownSidType.WinBuiltinCryptoOperatorsSid)]
    [InlineData(WellKnownSidType.WinUntrustedLabelSid)]
    [InlineData(WellKnownSidType.WinLowLabelSid)]
    [InlineData(WellKnownSidType.WinMediumLabelSid)]
    [InlineData(WellKnownSidType.WinHighLabelSid)]
    [InlineData(WellKnownSidType.WinSystemLabelSid)]
    [InlineData(WellKnownSidType.WinWriteRestrictedCodeSid)]
    [InlineData(WellKnownSidType.WinCreatorOwnerRightsSid)]
    [InlineData(WellKnownSidType.WinCacheablePrincipalsGroupSid)]
    [InlineData(WellKnownSidType.WinNonCacheablePrincipalsGroupSid)]
    [InlineData(WellKnownSidType.WinEnterpriseReadonlyControllersSid)]
    [InlineData(WellKnownSidType.WinAccountReadonlyControllersSid)]
    [InlineData(WellKnownSidType.WinBuiltinEventLogReadersGroup)]
    [InlineData(WellKnownSidType.WinNewEnterpriseReadonlyControllersSid)]
    [InlineData(WellKnownSidType.WinBuiltinCertSvcDComAccessGroup)]
    [InlineData(WellKnownSidType.WinMediumPlusLabelSid)]
    // Test case for WinLocalLogonSid commented out, because of special Authority SID
    // Will require more specialized testing
    // [InlineData(WellKnownSidType.WinLocalLogonSid)]
    [InlineData(WellKnownSidType.WinConsoleLogonSid)]
    [InlineData(WellKnownSidType.WinThisOrganizationCertificateSid)]
    // Test case for WinApplicationPackageAuthoritySid commented out, because of special Authority SID
    // Will require more specialized testing
    // [InlineData(WellKnownSidType.WinApplicationPackageAuthoritySid)]
    [InlineData(WellKnownSidType.WinBuiltinAnyPackageSid)]
    [InlineData(WellKnownSidType.WinCapabilityInternetClientSid)]
    [InlineData(WellKnownSidType.WinCapabilityInternetClientServerSid)]
    [InlineData(WellKnownSidType.WinCapabilityPrivateNetworkClientServerSid)]
    [InlineData(WellKnownSidType.WinCapabilityPicturesLibrarySid)]
    [InlineData(WellKnownSidType.WinCapabilityVideosLibrarySid)]
    [InlineData(WellKnownSidType.WinCapabilityMusicLibrarySid)]
    [InlineData(WellKnownSidType.WinCapabilityDocumentsLibrarySid)]
    [InlineData(WellKnownSidType.WinCapabilitySharedUserCertificatesSid)]
    [InlineData(WellKnownSidType.WinCapabilityEnterpriseAuthenticationSid)]
    [InlineData(WellKnownSidType.WinCapabilityRemovableStorageSid)]
    public void CanCreateSecurityIdentifierFromWellKnownSidType_Netcoreapp(WellKnownSidType sidType)
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var currentDomainSid = identity.Owner.AccountDomainSid;
            var wellKnownSidInstance = new SecurityIdentifier(sidType, currentDomainSid);

            Assert.True(wellKnownSidInstance.IsWellKnown(sidType));
        }
    }

    [Theory]
    [InlineData((WellKnownSidType)(-1))]
    [InlineData((WellKnownSidType)((int)WellKnownSidType.WinCapabilityRemovableStorageSid + 1))]
    public void CreatingSecurityIdentifierOutsideWellKnownSidTypeDefinedRangeThrowsException(WellKnownSidType sidType)
    {
        using WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
        var currentDomainSid = currentIdentity.Owner.AccountDomainSid;
        AssertExtensions.Throws<ArgumentException>("sidType", () => new SecurityIdentifier(sidType, currentDomainSid));
    }

    [Fact]
    public void MaxDefinedHasLegacyValue()
    {
#pragma warning disable 0618
        Assert.Equal(WellKnownSidType.WinBuiltinTerminalServerLicenseServersSid, WellKnownSidType.MaxDefined);
#pragma warning restore 0618
    }
}
