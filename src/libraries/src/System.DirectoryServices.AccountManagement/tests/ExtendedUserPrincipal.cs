// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.AccountManagement.Tests
{
    [DirectoryRdnPrefix("CN")]
    [DirectoryObjectClass("User")]
    public class ExtendedUserPrincipal : UserPrincipal, IExtendedPrincipalTest
    {
        public ExtendedUserPrincipal(PrincipalContext context) : base(context) { }
        
        public static new ExtendedUserPrincipal FindByIdentity(PrincipalContext context,string identityValue)
        {
            return (ExtendedUserPrincipal)FindByIdentityWithType(context, typeof(ExtendedUserPrincipal), identityValue);
        }

        [DirectoryProperty("jpegPhoto")]
        public byte[] ByteArrayExtension
        {
            get => (byte[])ExtensionGet("jpegPhoto")[0];
            set => ExtensionSet("jpegPhoto", value);
        }

        public object ObjectExtension
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public object[] ObjectArrayExtension
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
