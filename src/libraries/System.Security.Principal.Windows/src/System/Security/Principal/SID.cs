// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Security.Principal
{
    //
    // Identifier authorities
    //
    internal enum IdentifierAuthority : long
    {
        NullAuthority = 0,
        WorldAuthority = 1,
        LocalAuthority = 2,
        CreatorAuthority = 3,
        NonUniqueAuthority = 4,
        NTAuthority = 5,
        SiteServerAuthority = 6,
        InternetSiteAuthority = 7,
        ExchangeAuthority = 8,
        ResourceManagerAuthority = 9,
    }

    //
    // SID name usage
    //
    internal enum SidNameUse
    {
        User = 1,
        Group = 2,
        Domain = 3,
        Alias = 4,
        WellKnownGroup = 5,
        DeletedAccount = 6,
        Invalid = 7,
        Unknown = 8,
        Computer = 9,
    }

    //
    // Well-known SID types
    //
    public enum WellKnownSidType
    {
        /// <summary>Indicates a null SID.</summary>
        NullSid = 0,
        /// <summary>Indicates a SID that matches everyone.</summary>
        WorldSid = 1,
        /// <summary>Indicates a local SID.</summary>
        LocalSid = 2,
        /// <summary>Indicates a SID that matches the owner or creator of an object.</summary>
        CreatorOwnerSid = 3,
        /// <summary>Indicates a SID that matches the creator group of an object.</summary>
        CreatorGroupSid = 4,
        /// <summary>Indicates a creator owner server SID.</summary>
        CreatorOwnerServerSid = 5,
        /// <summary>Indicates a creator group server SID.</summary>
        CreatorGroupServerSid = 6,
        /// <summary>Indicates a SID for the Windows NT authority account.</summary>
        NTAuthoritySid = 7,
        /// <summary>Indicates a SID for a dial-up account.</summary>
        DialupSid = 8,
        /// <summary>Indicates a SID for a network account. This SID is added to the process of a token when it logs on across a network.</summary>
        NetworkSid = 9,
        /// <summary>Indicates a SID for a batch process. This SID is added to the process of a token when it logs on as a batch job.</summary>
        BatchSid = 10,
        /// <summary>Indicates a SID for an interactive account. This SID is added to the process of a token when it logs on interactively.</summary>
        InteractiveSid = 11,
        /// <summary>Indicates a SID for a service. This SID is added to the process of a token when it logs on as a service.</summary>
        ServiceSid = 12,
        /// <summary>Indicates a SID for the anonymous account.</summary>
        AnonymousSid = 13,
        /// <summary>Indicates a proxy SID.</summary>
        ProxySid = 14,
        /// <summary>Indicates a SID for an enterprise controller.</summary>
        EnterpriseControllersSid = 15,
        /// <summary>Indicates a SID for self.</summary>
        SelfSid = 16,
        /// <summary>Indicates a SID that matches any authenticated user.</summary>
        AuthenticatedUserSid = 17,
        /// <summary>Indicates a SID for restricted code.</summary>
        RestrictedCodeSid = 18,
        /// <summary>Indicates a SID that matches a terminal server account.</summary>
        TerminalServerSid = 19,
        /// <summary>Indicates a SID that matches remote logons.</summary>
        RemoteLogonIdSid = 20,
        /// <summary>Indicates a SID that matches logon IDs.</summary>
        LogonIdsSid = 21,
        /// <summary>Indicates a SID that matches the local system.</summary>
        LocalSystemSid = 22,
        /// <summary>Indicates a SID that matches a local service.</summary>
        LocalServiceSid = 23,
        /// <summary>Indicates a SID that matches a network service.</summary>
        NetworkServiceSid = 24,
        /// <summary>Indicates a SID that matches the domain account.</summary>
        BuiltinDomainSid = 25,
        /// <summary>Indicates a SID that matches the administrator group.</summary>
        BuiltinAdministratorsSid = 26,
        /// <summary>Indicates a SID that matches built-in user accounts.</summary>
        BuiltinUsersSid = 27,
        /// <summary>Indicates a SID that matches the guest account.</summary>
        BuiltinGuestsSid = 28,
        /// <summary>Indicates a SID that matches the power users group.</summary>
        BuiltinPowerUsersSid = 29,
        /// <summary>Indicates a SID that matches the account operators account.</summary>
        BuiltinAccountOperatorsSid = 30,
        /// <summary>Indicates a SID that matches the system operators group.</summary>
        BuiltinSystemOperatorsSid = 31,
        /// <summary>Indicates a SID that matches the print operators group.</summary>
        BuiltinPrintOperatorsSid = 32,
        /// <summary>Indicates a SID that matches the backup operators group.</summary>
        BuiltinBackupOperatorsSid = 33,
        /// <summary>Indicates a SID that matches the replicator account.</summary>
        BuiltinReplicatorSid = 34,
        /// <summary>Indicates a SID that matches pre-Windows 2000 compatible accounts.</summary>
        BuiltinPreWindows2000CompatibleAccessSid = 35,
        /// <summary>Indicates a SID that matches remote desktop users.</summary>
        BuiltinRemoteDesktopUsersSid = 36,
        /// <summary>Indicates a SID that matches the network operators group.</summary>
        BuiltinNetworkConfigurationOperatorsSid = 37,
        /// <summary>Indicates a SID that matches the account administrator's account.</summary>
        AccountAdministratorSid = 38,
        /// <summary>Indicates a SID that matches the account guest group.</summary>
        AccountGuestSid = 39,
        /// <summary>Indicates a SID that matches account Kerberos target group.</summary>
        AccountKrbtgtSid = 40,
        /// <summary>Indicates a SID that matches the account domain administrator group.</summary>
        AccountDomainAdminsSid = 41,
        /// <summary>Indicates a SID that matches the account domain users group.</summary>
        AccountDomainUsersSid = 42,
        /// <summary>Indicates a SID that matches the account domain guests group.</summary>
        AccountDomainGuestsSid = 43,
        /// <summary>Indicates a SID that matches the account computer group.</summary>
        AccountComputersSid = 44,
        /// <summary>Indicates a SID that matches the account controller group.</summary>
        AccountControllersSid = 45,
        /// <summary>Indicates a SID that matches the certificate administrators group.</summary>
        AccountCertAdminsSid = 46,
        /// <summary>Indicates a SID that matches the schema administrators group.</summary>
        AccountSchemaAdminsSid = 47,
        /// <summary>Indicates a SID that matches the enterprise administrators group.</summary>
        AccountEnterpriseAdminsSid = 48,
        /// <summary>Indicates a SID that matches the policy administrators group.</summary>
        AccountPolicyAdminsSid = 49,
        /// <summary>Indicates a SID that matches the RAS and IAS server account.</summary>
        AccountRasAndIasServersSid = 50,
        /// <summary>Indicates a SID present when the Microsoft NTLM authentication package authenticated the client.</summary>
        NtlmAuthenticationSid = 51,
        /// <summary>Indicates a SID present when the Microsoft Digest authentication package authenticated the client.</summary>
        DigestAuthenticationSid = 52,
        /// <summary>Indicates a SID present when the Secure Channel (SSL/TLS) authentication package authenticated the client.</summary>
        SChannelAuthenticationSid = 53,
        /// <summary>Indicates a SID present when the user authenticated from within the forest or across a trust that does not have the selective authentication option enabled. If this SID is present, then <see cref="OtherOrganizationSid"/> cannot be present.</summary>
        ThisOrganizationSid = 54,
        /// <summary>Indicates a SID present when the user authenticated across a forest with the selective authentication option enabled. If this SID is present, then <see cref="ThisOrganizationSid"/> cannot be present.</summary>
        OtherOrganizationSid = 55,
        /// <summary>Indicates a SID that allows a user to create incoming forest trusts. It is added to the token of users who are a member of the Incoming Forest Trust Builders built-in group in the root domain of the forest.</summary>
        BuiltinIncomingForestTrustBuildersSid = 56,
        /// <summary>Indicates a SID that matches the performance monitor user group.</summary>
        BuiltinPerformanceMonitoringUsersSid = 57,
        /// <summary>Indicates a SID that matches the performance log user group.</summary>
        BuiltinPerformanceLoggingUsersSid = 58,
        /// <summary>Indicates a SID that matches the Windows Authorization Access group.</summary>
        BuiltinAuthorizationAccessSid = 59,
        /// <summary>Indicates a SID is present in a server that can issue terminal server licenses.</summary>
        WinBuiltinTerminalServerLicenseServersSid = 60,
        [Obsolete("This member has been deprecated and is only maintained for backwards compatability. WellKnownSidType values greater than MaxDefined may be defined in future releases.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        MaxDefined = WinBuiltinTerminalServerLicenseServersSid,
        /// <summary>Indicates a SID that matches the distributed COM user group.</summary>
        WinBuiltinDCOMUsersSid = 61,
        /// <summary>Indicates a SID that matches the Internet built-in user group.</summary>
        WinBuiltinIUsersSid = 62,
        /// <summary>Indicates a SID that matches the Internet user group.</summary>
        WinIUserSid = 63,
        /// <summary>Indicates a SID that allows a user to use cryptographic operations. It is added to the token of users who are a member of the CryptoOperators built-in group. </summary>
        WinBuiltinCryptoOperatorsSid = 64,
        /// <summary>Indicates a SID that matches an untrusted label.</summary>
        WinUntrustedLabelSid = 65,
        /// <summary>Indicates a SID that matches an low level of trust label.</summary>
        WinLowLabelSid = 66,
        /// <summary>Indicates a SID that matches an medium level of trust label.</summary>
        WinMediumLabelSid = 67,
        /// <summary>Indicates a SID that matches a high level of trust label.</summary>
        WinHighLabelSid = 68,
        /// <summary>Indicates a SID that matches a system label.</summary>
        WinSystemLabelSid = 69,
        /// <summary>Indicates a SID that matches a write restricted code group.</summary>
        WinWriteRestrictedCodeSid = 70,
        /// <summary>Indicates a SID that matches a creator and owner rights group.</summary>
        WinCreatorOwnerRightsSid = 71,
        /// <summary>Indicates a SID that matches a cacheable principals group.</summary>
        WinCacheablePrincipalsGroupSid = 72,
        /// <summary>Indicates a SID that matches a non-cacheable principals group.</summary>
        WinNonCacheablePrincipalsGroupSid = 73,
        /// <summary>Indicates a SID that matches an enterprise wide read-only controllers group.</summary>
        WinEnterpriseReadonlyControllersSid = 74,
        /// <summary>Indicates a SID that matches an account read-only controllers group.</summary>
        WinAccountReadonlyControllersSid = 75,
        /// <summary>Indicates a SID that matches an event log readers group.</summary>
        WinBuiltinEventLogReadersGroup = 76,
        /// <summary>Indicates a SID that matches a read-only enterprise domain controller.</summary>
        WinNewEnterpriseReadonlyControllersSid = 77,
        /// <summary>Indicates a SID that matches the built-in DCOM certification services access group.</summary>
        WinBuiltinCertSvcDComAccessGroup = 78,
        /// <summary>Indicates a SID that matches the medium plus integrity label.</summary>
        WinMediumPlusLabelSid = 79,
        /// <summary>Indicates a SID that matches a local logon group.</summary>
        WinLocalLogonSid = 80,
        /// <summary>Indicates a SID that matches a console logon group.</summary>
        WinConsoleLogonSid = 81,
        /// <summary>Indicates a SID that matches a certificate for the given organization.</summary>
        WinThisOrganizationCertificateSid = 82,
        /// <summary>Indicates a SID that matches the application package authority.</summary>
        WinApplicationPackageAuthoritySid = 83,
        /// <summary>Indicates a SID that applies to all app containers.</summary>
        WinBuiltinAnyPackageSid = 84,
        /// <summary>Indicates a SID of Internet client capability for app containers.</summary>
        WinCapabilityInternetClientSid = 85,
        /// <summary>Indicates a SID of Internet client and server capability for app containers.</summary>
        WinCapabilityInternetClientServerSid = 86,
        /// <summary>Indicates a SID of private network client and server capability for app containers.</summary>
        WinCapabilityPrivateNetworkClientServerSid = 87,
        /// <summary>Indicates a SID for pictures library capability for app containers.</summary>
        WinCapabilityPicturesLibrarySid = 88,
        /// <summary>Indicates a SID for videos library capability for app containers.</summary>
        WinCapabilityVideosLibrarySid = 89,
        /// <summary>Indicates a SID for music library capability for app containers.</summary>
        WinCapabilityMusicLibrarySid = 90,
        /// <summary>Indicates a SID for documents library capability for app containers.</summary>
        WinCapabilityDocumentsLibrarySid = 91,
        /// <summary>Indicates a SID for shared user certificates capability for app containers.</summary>
        WinCapabilitySharedUserCertificatesSid = 92,
        /// <summary>Indicates a SID for Windows credentials capability for app containers.</summary>
        WinCapabilityEnterpriseAuthenticationSid = 93,
        /// <summary>Indicates a SID for removable storage capability for app containers.</summary>
        WinCapabilityRemovableStorageSid = 94
        // Note: Adding additional values require changes everywhere where the value above is used as the maximum defined WellKnownSidType value.
        // E.g. System.Security.Principal.SecurityIdentifier constructor
    }

    //
    // This class implements revision 1 SIDs
    // NOTE: The SecurityIdentifier class is immutable and must remain this way
    //
    public sealed class SecurityIdentifier : IdentityReference, IComparable<SecurityIdentifier>
    {
        #region Public Constants

        //
        // Identifier authority must be at most six bytes long
        //

        internal const long MaxIdentifierAuthority = 0xFFFFFFFFFFFF;

        //
        // Maximum number of subauthorities in a SID
        //

        internal const int MaxSubAuthorities = 15;

        //
        // Minimum length of a binary representation of a SID
        //

        public static readonly int MinBinaryLength = 1 + 1 + 6; // Revision (1) + subauth count (1) + identifier authority (6)

        //
        // Maximum length of a binary representation of a SID
        //

        public static readonly int MaxBinaryLength = 1 + 1 + 6 + MaxSubAuthorities * 4; // 4 bytes for each subauth

        #endregion

        #region Private Members

        //
        // Immutable properties of a SID
        //

        private IdentifierAuthority _identifierAuthority;
        // values guaranteed to be non-null on account of CreateFromParts()
        // method called by every constructor in this class.
        private int[] _subAuthorities;
        private byte[] _binaryForm;
        private SecurityIdentifier? _accountDomainSid;
        private bool _accountDomainSidInitialized;

        //
        // Computed attributes of a SID
        //

        private string? _sddlForm;

        #endregion

        #region Constructors

        //
        // Shared constructor logic
        // NOTE: subauthorities are really unsigned integers, but due to CLS
        //       lack of support for unsigned integers the caller must perform
        //       the typecast
        //

        [MemberNotNull(nameof(_binaryForm))]
        [MemberNotNull(nameof(_subAuthorities))]
        private void CreateFromParts(IdentifierAuthority identifierAuthority, ReadOnlySpan<int> subAuthorities)
        {
            //
            // Check the number of subauthorities passed in
            //
            if (subAuthorities.Length > MaxSubAuthorities)
            {
                throw new ArgumentOutOfRangeException(
                    "subAuthorities.Length",
                    subAuthorities.Length,
                    SR.Format(SR.IdentityReference_InvalidNumberOfSubauthorities, MaxSubAuthorities)
                );
            }

            //
            // Identifier authority is at most 6 bytes long
            //

            if (identifierAuthority < 0 || (long)identifierAuthority > MaxIdentifierAuthority)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(identifierAuthority),
                    identifierAuthority,
                    SR.IdentityReference_IdentifierAuthorityTooLarge
                );
            }

            //
            // Create a local copy of the data passed in
            //

            _identifierAuthority = identifierAuthority;
            _subAuthorities = subAuthorities.ToArray();

            //
            // Compute and store the binary form
            //
            // typedef struct _SID {
            //     UCHAR Revision;
            //     UCHAR SubAuthorityCount;
            //     SID_IDENTIFIER_AUTHORITY IdentifierAuthority;
            //     ULONG SubAuthority[ANYSIZE_ARRAY]
            // } SID, *PISID;
            //

            _binaryForm = new byte[1 + 1 + 6 + 4 * _subAuthorities.Length];

            //
            // First two bytes contain revision and subauthority count
            //

            _binaryForm[0] = Revision;
            _binaryForm[1] = (byte)_subAuthorities.Length;

            //
            // Identifier authority takes up 6 bytes
            //

            for (int i = 0; i < 6; i++)
            {
                _binaryForm[2 + i] = (byte)((((ulong)_identifierAuthority) >> ((5 - i) * 8)) & 0xFF);
            }

            //
            // Subauthorities go last, preserving big-endian representation
            //

            for (int i = 0; i < _subAuthorities.Length; i++)
            {
                for (byte shift = 0; shift < 4; shift += 1)
                {
                    _binaryForm[8 + 4 * i + shift] = unchecked((byte)(((ulong)_subAuthorities[i]) >> (shift * 8)));
                }
            }
        }

        [MemberNotNull(nameof(_binaryForm))]
        [MemberNotNull(nameof(_subAuthorities))]
        private void CreateFromBinaryForm(byte[] binaryForm, int offset)
        {
            ArgumentNullException.ThrowIfNull(binaryForm);

            //
            // Negative offsets are not allowed
            //

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            //
            // At least a minimum-size SID should fit in the buffer
            //

            if (binaryForm.Length - offset < SecurityIdentifier.MinBinaryLength)
            {
                throw new ArgumentOutOfRangeException(nameof(binaryForm), SR.ArgumentOutOfRange_ArrayTooSmall);
            }

            //
            // Extract the elements of a SID
            //

            if (binaryForm[offset] != Revision)
            {
                //
                // Revision is incorrect
                //

                throw new ArgumentException(SR.IdentityReference_InvalidSidRevision, nameof(binaryForm));
            }

            //
            // Insist on the correct number of subauthorities
            //
            int subAuthoritiesLength = binaryForm[offset + 1];
            if (subAuthoritiesLength > MaxSubAuthorities)
            {
                throw new ArgumentException(SR.Format(SR.IdentityReference_InvalidNumberOfSubauthorities, MaxSubAuthorities), nameof(binaryForm));
            }

            //
            // Make sure the buffer is big enough
            //

            int totalLength = 1 + 1 + 6 + 4 * subAuthoritiesLength;

            if (binaryForm.Length - offset < totalLength)
            {
                throw new ArgumentException(SR.ArgumentOutOfRange_ArrayTooSmall, nameof(binaryForm));
            }

            Span<int> subAuthorities = stackalloc int[MaxSubAuthorities];
            IdentifierAuthority authority = (IdentifierAuthority)(
                (((long)binaryForm[offset + 2]) << 40) +
                (((long)binaryForm[offset + 3]) << 32) +
                (((long)binaryForm[offset + 4]) << 24) +
                (((long)binaryForm[offset + 5]) << 16) +
                (((long)binaryForm[offset + 6]) << 8) +
                (((long)binaryForm[offset + 7]))
            );

            //
            // Subauthorities are represented in big-endian format
            //

            for (int i = 0; i < subAuthoritiesLength; i++)
            {
                subAuthorities[i] =
                    (int)(
                    (((uint)binaryForm[offset + 8 + 4 * i + 0]) << 0) +
                    (((uint)binaryForm[offset + 8 + 4 * i + 1]) << 8) +
                    (((uint)binaryForm[offset + 8 + 4 * i + 2]) << 16) +
                    (((uint)binaryForm[offset + 8 + 4 * i + 3]) << 24)
                );
            }

            CreateFromParts(
                authority,
                subAuthorities.Slice(0, subAuthoritiesLength)
            );

            return;
        }

        //
        // Constructs a SecurityIdentifier object from its string representation
        // Returns 'null' if string passed in is not a valid SID
        // NOTE: although there is a P/Invoke call involved in the implementation of this method,
        //       there is no security risk involved, so no security demand is being made.
        //


        public SecurityIdentifier(string sddlForm)
        {
            ArgumentNullException.ThrowIfNull(sddlForm);

            //
            // Call into the underlying O/S conversion routine
            //

            int error = Win32.CreateSidFromString(sddlForm, out byte[]? resultSid);

            if (error == Interop.Errors.ERROR_INVALID_SID)
            {
                throw new ArgumentException(SR.Argument_InvalidValue, nameof(sddlForm));
            }
            else if (error == Interop.Errors.ERROR_NOT_ENOUGH_MEMORY)
            {
                throw new OutOfMemoryException();
            }
            else if (error != Interop.Errors.ERROR_SUCCESS)
            {
                Debug.Fail($"Win32.CreateSidFromString returned unrecognized error {error}");
                throw new Win32Exception(error);
            }

            CreateFromBinaryForm(resultSid!, 0);
        }

        //
        // Constructs a SecurityIdentifier object from its binary representation
        //

        public SecurityIdentifier(byte[] binaryForm, int offset)
        {
            ArgumentNullException.ThrowIfNull(binaryForm);

            CreateFromBinaryForm(binaryForm, offset);
        }

        //
        // Constructs a SecurityIdentifier object from an IntPtr
        //

        public SecurityIdentifier(IntPtr binaryForm)
            : this(Win32.ConvertIntPtrSidToByteArraySid(binaryForm), 0)
        {
        }

        //
        // Constructs a well-known SID
        // The 'domainSid' parameter is optional and only used
        // by the well-known types that require it
        // NOTE: although there is a P/Invoke call involved in the implementation of this constructor,
        //       there is no security risk involved, so no security demand is being made.
        //


        public SecurityIdentifier(WellKnownSidType sidType, SecurityIdentifier? domainSid)
        {
            //
            // sidType must not be equal to LogonIdsSid
            //

            if (sidType == WellKnownSidType.LogonIdsSid)
            {
                throw new ArgumentException(SR.IdentityReference_CannotCreateLogonIdsSid, nameof(sidType));
            }

            //
            // sidType should not exceed the max defined value
            //

            if ((sidType < WellKnownSidType.NullSid) || (sidType > WellKnownSidType.WinCapabilityRemovableStorageSid))
            {
                throw new ArgumentException(SR.Argument_InvalidValue, nameof(sidType));
            }

            //
            // for sidType between 38 to 50, the domainSid parameter must be specified
            //
            int error;
            if ((sidType >= WellKnownSidType.AccountAdministratorSid) && (sidType <= WellKnownSidType.AccountRasAndIasServersSid))
            {
                if (domainSid == null)
                {
                    throw new ArgumentNullException(nameof(domainSid), SR.Format(SR.IdentityReference_DomainSidRequired, sidType));
                }

                //
                // verify that the domain sid is a valid windows domain sid
                // to do that we call GetAccountDomainSid and the return value should be the same as the domainSid
                //
                error = Win32.GetWindowsAccountDomainSid(domainSid, out SecurityIdentifier? resultDomainSid);

                if (error == Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new OutOfMemoryException();
                }
                else if (error == Interop.Errors.ERROR_NON_ACCOUNT_SID)
                {
                    // this means that the domain sid is not valid
                    throw new ArgumentException(SR.IdentityReference_NotAWindowsDomain, nameof(domainSid));
                }
                else if (error != Interop.Errors.ERROR_SUCCESS)
                {
                    Debug.Fail($"Win32.GetWindowsAccountDomainSid returned unrecognized error {error}");
                    throw new Win32Exception(error);
                }

                //
                // if domainSid is passed in as S-1-5-21-3-4-5-6,  the above api will return S-1-5-21-3-4-5 as the domainSid
                // Since these do not match S-1-5-21-3-4-5-6 is not a valid domainSid (wrong number of subauthorities)
                //
                if (resultDomainSid != domainSid)
                {
                    throw new ArgumentException(SR.IdentityReference_NotAWindowsDomain, nameof(domainSid));
                }
            }


            error = Win32.CreateWellKnownSid(sidType, domainSid, out byte[]? resultSid);

            if (error == Interop.Errors.ERROR_INVALID_PARAMETER)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly, combination of arguments used
                throw new ArgumentException(Marshal.GetPInvokeErrorMessage(error), "sidType/domainSid");
#pragma warning restore CS2208
            }
            else if (error != Interop.Errors.ERROR_SUCCESS)
            {
                Debug.Fail($"Win32.CreateWellKnownSid returned unrecognized error {error}");
                throw new Win32Exception(error);
            }

            CreateFromBinaryForm(resultSid!, 0);
        }

        internal SecurityIdentifier(IdentifierAuthority identifierAuthority, ReadOnlySpan<int> subAuthorities)
        {
            CreateFromParts(identifierAuthority, subAuthorities);
        }

#endregion

#region Static Properties

        //
        // Revision is always '1'
        //

        internal static byte Revision => 1;

#endregion

#region Non-static Properties

        //
        // This is for internal consumption only, hence it is marked 'internal'
        // Making this call public would require a deep copy of the data to
        // prevent the caller from messing with the internal representation.
        //

        internal byte[] BinaryForm => _binaryForm;

        internal IdentifierAuthority IdentifierAuthority => _identifierAuthority;

        internal int SubAuthorityCount => _subAuthorities.Length;

        public int BinaryLength => _binaryForm.Length;

        //
        // Returns the domain portion of a SID or null if the specified
        // SID is not an account SID
        // NOTE: although there is a P/Invoke call involved in the implementation of this method,
        //       there is no security risk involved, so no security demand is being made.
        //

        public SecurityIdentifier? AccountDomainSid
        {
            get
            {
                if (!_accountDomainSidInitialized)
                {
                    _accountDomainSid = GetAccountDomainSid();
                    _accountDomainSidInitialized = true;
                }

                return _accountDomainSid;
            }
        }

#endregion

#region Inherited properties and methods

        public override bool Equals([NotNullWhen(true)] object? o)
        {
            return (this == o as SecurityIdentifier); // invokes operator==
        }

        public bool Equals(SecurityIdentifier sid)
        {
            return (this == sid); // invokes operator==
        }

        public override int GetHashCode()
        {
            int hashCode = ((long)IdentifierAuthority).GetHashCode();
            for (int i = 0; i < SubAuthorityCount; i++)
            {
                hashCode ^= GetSubAuthority(i);
            }
            return hashCode;
        }

        public override string ToString()
        {
            if (_sddlForm == null)
            {
                //
                // Typecasting of _IdentifierAuthority to a ulong below is important, since
                // otherwise you would see this: "S-1-NTAuthority-32-544"
                //

                // length of buffer calculation
                // prefix = "S-1-".Length: 4;
                // authority: ulong.MaxValue.ToString("D") : 20;
                // subauth = MaxSubAuthorities * ( uint.MaxValue.ToString("D").Length + '-'.Length ): 15 * (10+1): 165;
                // max possible length = 4 + 20 + 165: 189
                Span<char> result = stackalloc char[189];
                result[0] = 'S';
                result[1] = '-';
                result[2] = '1';
                result[3] = '-';
                int length = 4;
                ((ulong)_identifierAuthority).TryFormat(result.Slice(length), out int written);
                length += written;
                int[] values = _subAuthorities;
                for (int index = 0; index < values.Length; index++)
                {
                    result[length] = '-';
                    length += 1;
                    ((uint)values[index]).TryFormat(result.Slice(length), out written);
                    length += written;
                }
                _sddlForm = result.Slice(0, length).ToString();
            }

            return _sddlForm;
        }

        public override string Value
        {
            get
            {
                return ToString().ToUpperInvariant();
            }
        }

        internal static bool IsValidTargetTypeStatic(Type targetType)
        {
            if (targetType == typeof(NTAccount))
            {
                return true;
            }
            else if (targetType == typeof(SecurityIdentifier))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool IsValidTargetType(Type targetType)
        {
            return IsValidTargetTypeStatic(targetType);
        }


        internal SecurityIdentifier? GetAccountDomainSid()
        {
            int error = Win32.GetWindowsAccountDomainSid(this, out SecurityIdentifier? resultSid);

            if (error == Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
            {
                throw new OutOfMemoryException();
            }
            else if (error == Interop.Errors.ERROR_NON_ACCOUNT_SID)
            {
                resultSid = null;
            }
            else if (error != Interop.Errors.ERROR_SUCCESS)
            {
                Debug.Fail($"Win32.GetWindowsAccountDomainSid returned unrecognized error {error}");
                throw new Win32Exception(error);
            }
            return resultSid;
        }


        public bool IsAccountSid()
        {
            if (!_accountDomainSidInitialized)
            {
                _accountDomainSid = GetAccountDomainSid();
                _accountDomainSidInitialized = true;
            }

            if (_accountDomainSid == null)
            {
                return false;
            }

            return true;
        }


        public override IdentityReference Translate(Type targetType)
        {
            ArgumentNullException.ThrowIfNull(targetType);

            if (targetType == typeof(SecurityIdentifier))
            {
                return this; // assumes SecurityIdentifier objects are immutable
            }
            else if (targetType == typeof(NTAccount))
            {
                IdentityReferenceCollection irSource = new IdentityReferenceCollection(1);
                irSource.Add(this);
                IdentityReferenceCollection irTarget;

                irTarget = SecurityIdentifier.Translate(irSource, targetType, true);

                return irTarget[0];
            }
            else
            {
                throw new ArgumentException(SR.IdentityReference_MustBeIdentityReference, nameof(targetType));
            }
        }

#endregion

#region Operators

        public static bool operator ==(SecurityIdentifier? left, SecurityIdentifier? right)
        {
            object? l = left;
            object? r = right;

            if (l == r)
            {
                return true;
            }
            else if (l == null || r == null)
            {
                return false;
            }
            else
            {
                return (left!.CompareTo(right) == 0);
            }
        }

        public static bool operator !=(SecurityIdentifier? left, SecurityIdentifier? right)
        {
            return !(left == right);
        }

#endregion

#region IComparable implementation

        public int CompareTo(SecurityIdentifier? sid)
        {
            ArgumentNullException.ThrowIfNull(sid);

            if (this.IdentifierAuthority < sid.IdentifierAuthority)
            {
                return -1;
            }

            if (this.IdentifierAuthority > sid.IdentifierAuthority)
            {
                return 1;
            }

            if (this.SubAuthorityCount < sid.SubAuthorityCount)
            {
                return -1;
            }

            if (this.SubAuthorityCount > sid.SubAuthorityCount)
            {
                return 1;
            }

            for (int i = 0; i < this.SubAuthorityCount; i++)
            {
                int diff = this.GetSubAuthority(i) - sid.GetSubAuthority(i);

                if (diff != 0)
                {
                    return diff;
                }
            }

            return 0;
        }

#endregion

#region Public Methods

        internal int GetSubAuthority(int index)
        {
            return _subAuthorities[index];
        }

        //
        // Determines whether this SID is a well-known SID of the specified type
        //
        // NOTE: although there is a P/Invoke call involved in the implementation of this method,
        //       there is no security risk involved, so no security demand is being made.
        //


        public bool IsWellKnown(WellKnownSidType type)
        {
            return Win32.IsWellKnownSid(this, type);
        }

        public void GetBinaryForm(byte[] binaryForm, int offset)
        {
            _binaryForm.CopyTo(binaryForm, offset);
        }

        //
        // NOTE: although there is a P/Invoke call involved in the implementation of this method,
        //       there is no security risk involved, so no security demand is being made.
        //


        public bool IsEqualDomainSid(SecurityIdentifier sid)
        {
            return Win32.IsEqualDomainSid(this, sid);
        }


        private static unsafe IdentityReferenceCollection TranslateToNTAccounts(IdentityReferenceCollection sourceSids, out bool someFailed)
        {
            ArgumentNullException.ThrowIfNull(sourceSids);

            if (sourceSids.Count == 0)
            {
                throw new ArgumentException(SR.Arg_EmptyCollection, nameof(sourceSids));
            }

            IntPtr[] SidArrayPtr = new IntPtr[sourceSids.Count];
            GCHandle[] HandleArray = new GCHandle[sourceSids.Count];
            SafeLsaPolicyHandle? LsaHandle = null;
            SafeLsaMemoryHandle? ReferencedDomainsPtr = null;
            SafeLsaMemoryHandle? NamesPtr = null;

            try
            {
                //
                // Pin all elements in the array of SIDs
                //

                int currentSid = 0;
                foreach (IdentityReference id in sourceSids)
                {
                    if (!(id is SecurityIdentifier sid))
                    {
                        throw new ArgumentException(SR.Argument_ImproperType, nameof(sourceSids));
                    }

                    HandleArray[currentSid] = GCHandle.Alloc(sid.BinaryForm, GCHandleType.Pinned);
                    SidArrayPtr[currentSid] = HandleArray[currentSid].AddrOfPinnedObject();
                    currentSid++;
                }

                //
                // Open LSA policy (for lookup requires it)
                //

                LsaHandle = Win32.LsaOpenPolicy(null, Interop.Advapi32.PolicyRights.POLICY_LOOKUP_NAMES);

                //
                // Perform the actual lookup
                //

                someFailed = false;
                uint ReturnCode;
                ReturnCode = Interop.Advapi32.LsaLookupSids(LsaHandle, sourceSids.Count, SidArrayPtr, out ReferencedDomainsPtr, out NamesPtr);

                //
                // Make a decision regarding whether it makes sense to proceed
                // based on the return code and the value of the forceSuccess argument
                //

                if (ReturnCode == Interop.StatusOptions.STATUS_NO_MEMORY ||
                    ReturnCode == Interop.StatusOptions.STATUS_INSUFFICIENT_RESOURCES)
                {
                    throw new OutOfMemoryException();
                }
                else if (ReturnCode == Interop.StatusOptions.STATUS_ACCESS_DENIED)
                {
                    throw new UnauthorizedAccessException();
                }
                else if (ReturnCode == Interop.StatusOptions.STATUS_NONE_MAPPED ||
                    ReturnCode == Interop.StatusOptions.STATUS_SOME_NOT_MAPPED)
                {
                    someFailed = true;
                }
                else if (ReturnCode != 0)
                {
                    uint win32ErrorCode = Interop.Advapi32.LsaNtStatusToWinError(ReturnCode);

                    Debug.Fail($"Interop.LsaLookupSids returned {win32ErrorCode}");
                    throw new Win32Exception(unchecked((int)win32ErrorCode));
                }


                NamesPtr.Initialize((uint)sourceSids.Count, (uint)sizeof(Interop.LSA_TRANSLATED_NAME));
                ReferencedDomainsPtr.InitializeReferencedDomainsList();

                //
                // Interpret the results and generate NTAccount objects
                //

                IdentityReferenceCollection Result = new IdentityReferenceCollection(sourceSids.Count);

                if (ReturnCode == 0 || ReturnCode == Interop.StatusOptions.STATUS_SOME_NOT_MAPPED)
                {
                    //
                    // Interpret the results and generate NT Account objects
                    //

                    Interop.LSA_REFERENCED_DOMAIN_LIST rdl = ReferencedDomainsPtr.Read<Interop.LSA_REFERENCED_DOMAIN_LIST>(0);
                    string[] ReferencedDomains = new string[rdl.Entries];

                    for (int i = 0; i < rdl.Entries; i++)
                    {
                        Interop.LSA_TRUST_INFORMATION* ti = (Interop.LSA_TRUST_INFORMATION*)rdl.Domains + i;
                        ReferencedDomains[i] = Marshal.PtrToStringUni(ti->Name.Buffer, ti->Name.Length / sizeof(char));
                    }

                    Interop.LSA_TRANSLATED_NAME[] translatedNames = new Interop.LSA_TRANSLATED_NAME[sourceSids.Count];
                    NamesPtr.ReadArray(0, translatedNames, 0, translatedNames.Length);

                    for (int i = 0; i < sourceSids.Count; i++)
                    {
                        Interop.LSA_TRANSLATED_NAME Ltn = translatedNames[i];

                        switch ((SidNameUse)Ltn.Use)
                        {
                            case SidNameUse.User:
                            case SidNameUse.Group:
                            case SidNameUse.Alias:
                            case SidNameUse.Computer:
                            case SidNameUse.WellKnownGroup:
                                string account = Marshal.PtrToStringUni(Ltn.Name.Buffer, Ltn.Name.Length / sizeof(char));
                                string domain = ReferencedDomains[Ltn.DomainIndex];
                                Result.Add(new NTAccount(domain, account));
                                break;

                            default:
                                someFailed = true;
                                Result.Add(sourceSids[i]);
                                break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < sourceSids.Count; i++)
                    {
                        Result.Add(sourceSids[i]);
                    }
                }

                return Result;
            }
            finally
            {
                for (int i = 0; i < sourceSids.Count; i++)
                {
                    if (HandleArray[i].IsAllocated)
                    {
                        HandleArray[i].Free();
                    }
                }

                LsaHandle?.Dispose();
                ReferencedDomainsPtr?.Dispose();
                NamesPtr?.Dispose();
            }
        }


        internal static IdentityReferenceCollection Translate(IdentityReferenceCollection sourceSids, Type targetType, bool forceSuccess)
        {
            IdentityReferenceCollection result = Translate(sourceSids, targetType, out bool someFailed);

            if (forceSuccess && someFailed)
            {
                IdentityReferenceCollection UnmappedIdentities = new IdentityReferenceCollection();

                foreach (IdentityReference id in result)
                {
                    if (id.GetType() != targetType)
                    {
                        UnmappedIdentities.Add(id);
                    }
                }

                throw new IdentityNotMappedException(SR.IdentityReference_IdentityNotMapped, UnmappedIdentities);
            }

            return result;
        }


        internal static IdentityReferenceCollection Translate(IdentityReferenceCollection sourceSids, Type targetType, out bool someFailed)
        {
            ArgumentNullException.ThrowIfNull(sourceSids);

            if (targetType == typeof(NTAccount))
            {
                return TranslateToNTAccounts(sourceSids, out someFailed);
            }

            throw new ArgumentException(SR.IdentityReference_MustBeIdentityReference, nameof(targetType));
        }
#endregion
    }
}
