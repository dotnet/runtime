// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Mail
{
    internal class MailAddress
    {
        public MailAddress(string address)
        {
            MailAddressParser.ParseAddress(address);
        }

        internal MailAddress(string displayName, string localPart, string domain)
        {
        }
    }
}
