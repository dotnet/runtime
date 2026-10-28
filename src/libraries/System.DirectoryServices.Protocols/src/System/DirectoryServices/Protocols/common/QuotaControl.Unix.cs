// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;

namespace System.DirectoryServices.Protocols
{
    public partial class QuotaControl : DirectoryControl
    {
        public SecurityIdentifier QuerySid
        {
            get => _sid == null ? null : throw new System.PlatformNotSupportedException(SR.QuotaControlNotSupported);
            set
            {
                if (value == null)
                {
                    _sid = null;
                }
                else
                {
                    throw new System.PlatformNotSupportedException(SR.QuotaControlNotSupported);
                }
            }
        }
    }
}
