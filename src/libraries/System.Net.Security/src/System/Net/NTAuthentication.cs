// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net
{
    internal sealed partial class NTAuthentication
    {
        internal bool IsConfidentialityFlag => (_contextFlags & ContextFlagsPal.Confidentiality) != 0;

        internal bool IsIntegrityFlag => (_contextFlags & (IsServer ? ContextFlagsPal.AcceptIntegrity : ContextFlagsPal.InitIntegrity)) != 0;

        internal bool IsMutualAuthFlag => (_contextFlags & ContextFlagsPal.MutualAuth) != 0;

        internal bool IsDelegationFlag => (_contextFlags & ContextFlagsPal.Delegate) != 0;

        internal bool IsIdentifyFlag => (_contextFlags & (IsServer ? ContextFlagsPal.AcceptIdentify : ContextFlagsPal.InitIdentify)) != 0;

        internal string? Spn => _spn;
    }
}
