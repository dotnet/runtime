// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal enum TRUSTED_INFORMATION_CLASS
        {
            TrustedDomainNameInformation = 1,
            TrustedControllersInformation,
            TrustedPosixOffsetInformation,
            TrustedPasswordInformation,
            TrustedDomainInformationBasic,
            TrustedDomainInformationEx,
            TrustedDomainAuthInformation,
            TrustedDomainFullInformation,
            TrustedDomainAuthInformationInternal,
            TrustedDomainFullInformationInternal,
            TrustedDomainInformationEx2Internal,
            TrustedDomainFullInformation2Internal
        }
    }
}
