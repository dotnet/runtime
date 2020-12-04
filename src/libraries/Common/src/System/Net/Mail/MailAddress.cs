// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Text;

namespace System.Net.Mail
{
    internal class MailAddress
    {
        public MailAddress(string address)
        {
            MailAddressParser.TryParseAddress(address, out ParseAddressInfo _, throwExceptionIfFail: true);
        }

        internal MailAddress(string displayName, string localPart, string domain, Encoding? displayNameEncoding)
        {
        }
    }
}
