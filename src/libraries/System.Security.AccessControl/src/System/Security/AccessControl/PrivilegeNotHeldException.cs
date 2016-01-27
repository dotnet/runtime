// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace System.Security.AccessControl
{
    public sealed class PrivilegeNotHeldException : UnauthorizedAccessException
    {
        private readonly string _privilegeName = null;

        public PrivilegeNotHeldException()
            : base(SR.PrivilegeNotHeld_Default)
        {
        }

        public PrivilegeNotHeldException(string privilege)
            : base(string.Format(CultureInfo.CurrentCulture, SR.PrivilegeNotHeld_Named, privilege))
        {
            _privilegeName = privilege;
        }

        public PrivilegeNotHeldException(string privilege, Exception inner)
            : base(string.Format(CultureInfo.CurrentCulture, SR.PrivilegeNotHeld_Named, privilege), inner)
        {
            _privilegeName = privilege;
        }

        public string PrivilegeName
        {
            get { return _privilegeName; }
        }
    }
}
