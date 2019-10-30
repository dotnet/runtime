// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Claims
{
    /// <summary>
    /// Defines the claim types that are supported by the framework.
    /// </summary>
    public static class ClaimTypes
    {
        internal const string ClaimTypeNamespace = "http://schemas.microsoft.com/ws/2008/06/identity/claims";

        public const string AuthenticationInstant = ClaimTypeNamespace + "/authenticationinstant";
        public const string AuthenticationMethod = ClaimTypeNamespace + "/authenticationmethod";
        public const string CookiePath = ClaimTypeNamespace + "/cookiepath";
        public const string DenyOnlyPrimarySid = ClaimTypeNamespace + "/denyonlyprimarysid";
        public const string DenyOnlyPrimaryGroupSid = ClaimTypeNamespace + "/denyonlyprimarygroupsid";
        public const string DenyOnlyWindowsDeviceGroup = ClaimTypeNamespace + "/denyonlywindowsdevicegroup";
        public const string Dsa = ClaimTypeNamespace + "/dsa";
        public const string Expiration = ClaimTypeNamespace + "/expiration";
        public const string Expired = ClaimTypeNamespace + "/expired";
        public const string GroupSid = ClaimTypeNamespace + "/groupsid";
        public const string IsPersistent = ClaimTypeNamespace + "/ispersistent";
        public const string PrimaryGroupSid = ClaimTypeNamespace + "/primarygroupsid";
        public const string PrimarySid = ClaimTypeNamespace + "/primarysid";
        public const string Role = ClaimTypeNamespace + "/role";
        public const string SerialNumber = ClaimTypeNamespace + "/serialnumber";
        public const string UserData = ClaimTypeNamespace + "/userdata";
        public const string Version = ClaimTypeNamespace + "/version";
        public const string WindowsAccountName = ClaimTypeNamespace + "/windowsaccountname";
        public const string WindowsDeviceClaim = ClaimTypeNamespace + "/windowsdeviceclaim";
        public const string WindowsDeviceGroup = ClaimTypeNamespace + "/windowsdevicegroup";
        public const string WindowsUserClaim = ClaimTypeNamespace + "/windowsuserclaim";
        public const string WindowsFqbnVersion = ClaimTypeNamespace + "/windowsfqbnversion";
        public const string WindowsSubAuthority = ClaimTypeNamespace + "/windowssubauthority";


        //
        // From System.IdentityModel.Claims
        //
        internal const string ClaimType2005Namespace = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims";

        public const string Anonymous = ClaimType2005Namespace + "/anonymous";
        public const string Authentication = ClaimType2005Namespace + "/authentication";
        public const string AuthorizationDecision = ClaimType2005Namespace + "/authorizationdecision";
        public const string Country = ClaimType2005Namespace + "/country";
        public const string DateOfBirth = ClaimType2005Namespace + "/dateofbirth";
        public const string Dns = ClaimType2005Namespace + "/dns";
        public const string DenyOnlySid = ClaimType2005Namespace + "/denyonlysid"; // NOTE: shown as 'Deny only group SID' on the ADFSv2 UI!
        public const string Email = ClaimType2005Namespace + "/emailaddress";
        public const string Gender = ClaimType2005Namespace + "/gender";
        public const string GivenName = ClaimType2005Namespace + "/givenname";
        public const string Hash = ClaimType2005Namespace + "/hash";
        public const string HomePhone = ClaimType2005Namespace + "/homephone";
        public const string Locality = ClaimType2005Namespace + "/locality";
        public const string MobilePhone = ClaimType2005Namespace + "/mobilephone";
        public const string Name = ClaimType2005Namespace + "/name";
        public const string NameIdentifier = ClaimType2005Namespace + "/nameidentifier";
        public const string OtherPhone = ClaimType2005Namespace + "/otherphone";
        public const string PostalCode = ClaimType2005Namespace + "/postalcode";
        public const string Rsa = ClaimType2005Namespace + "/rsa";
        public const string Sid = ClaimType2005Namespace + "/sid";
        public const string Spn = ClaimType2005Namespace + "/spn";
        public const string StateOrProvince = ClaimType2005Namespace + "/stateorprovince";
        public const string StreetAddress = ClaimType2005Namespace + "/streetaddress";
        public const string Surname = ClaimType2005Namespace + "/surname";
        public const string System = ClaimType2005Namespace + "/system";
        public const string Thumbprint = ClaimType2005Namespace + "/thumbprint";
        public const string Upn = ClaimType2005Namespace + "/upn";
        public const string Uri = ClaimType2005Namespace + "/uri";
        public const string Webpage = ClaimType2005Namespace + "/webpage";
        public const string X500DistinguishedName = ClaimType2005Namespace + "/x500distinguishedname";

        internal const string ClaimType2009Namespace = "http://schemas.xmlsoap.org/ws/2009/09/identity/claims";
        public const string Actor = ClaimType2009Namespace + "/actor";
    }
}
