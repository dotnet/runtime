// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices.AccountManagement;

namespace AccountManagementUnitTests
{
    [DirectoryRdnPrefix("CN")]
    [DirectoryObjectClass("User")]
    public class ExtendedUserPrincipal : UserPrincipal, IExtendedPrincipalTest
    {
        public ExtendedUserPrincipal(PrincipalContext context)
            : base(context)
        {
        }

        // Implement the overloaded search method FindByIdentity.
        public static new ExtendedUserPrincipal FindByIdentity(PrincipalContext context,
                                                       string identityValue)
        {
            return (ExtendedUserPrincipal)FindByIdentityWithType(context,
                                                         typeof(ExtendedUserPrincipal),
                                                         identityValue);
        }

        [DirectoryProperty("jpegPhoto")]
        public byte[] ByteArrayExtension
        {
            get
            {
                return (byte[])this.ExtensionGet("jpegPhoto")[0];
            }

            set
            {
                this.ExtensionSet("jpegPhoto", value);
            }
        }

        public object ObjectExtension
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }

        public object[] ObjectArrayExtension
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
