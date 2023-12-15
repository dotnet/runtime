// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Security.AccessControl
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class PrivilegeNotHeldException : UnauthorizedAccessException
    {
        private readonly string? _privilegeName;

        public PrivilegeNotHeldException()
            : base(SR.PrivilegeNotHeld_Default)
        {
        }

        public PrivilegeNotHeldException(string? privilege)
            : base(SR.Format(SR.PrivilegeNotHeld_Named, privilege))
        {
            _privilegeName = privilege;
        }

        public PrivilegeNotHeldException(string? privilege, Exception? inner)
            : base(SR.Format(SR.PrivilegeNotHeld_Named, privilege), inner)
        {
            _privilegeName = privilege;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private PrivilegeNotHeldException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _privilegeName = info.GetString(nameof(PrivilegeName));
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(PrivilegeName), _privilegeName, typeof(string));
        }

        public string? PrivilegeName
        {
            get { return _privilegeName; }
        }
    }
}
