// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;

namespace System.DirectoryServices.Protocols
{
    public partial class QuotaControl : DirectoryControl
    {
        public SecurityIdentifier QuerySid
        {
            get => _sid == null ? null : new SecurityIdentifier(_sid, 0);
            set
            {
                if (value == null)
                {
                    _sid = null;
                }
                else
                {
                    _sid = new byte[value.BinaryLength];
                    value.GetBinaryForm(_sid, 0);
                }
            }
        }
    }
}
