// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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
