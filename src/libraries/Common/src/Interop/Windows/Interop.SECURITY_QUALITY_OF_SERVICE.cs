// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    /// <summary>
    /// <a href="https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-security_quality_of_service">SECURITY_QUALITY_OF_SERVICE</a> structure.
    ///  Used to support client impersonation. Client specifies this to a server to allow
    ///  it to impersonate the client.
    /// </summary>
    internal unsafe struct SECURITY_QUALITY_OF_SERVICE
    {
        public uint Length;
        public ImpersonationLevel ImpersonationLevel;
        public ContextTrackingMode ContextTrackingMode;
        public BOOLEAN EffectiveOnly;

        public unsafe SECURITY_QUALITY_OF_SERVICE(ImpersonationLevel impersonationLevel, ContextTrackingMode contextTrackingMode, bool effectiveOnly)
        {
            Length = (uint)sizeof(SECURITY_QUALITY_OF_SERVICE);
            ImpersonationLevel = impersonationLevel;
            ContextTrackingMode = contextTrackingMode;
            EffectiveOnly = effectiveOnly ? BOOLEAN.TRUE : BOOLEAN.FALSE;
        }
    }

    /// <summary>
    /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa379572.aspx">SECURITY_IMPERSONATION_LEVEL</a> enumeration values.
    ///  [SECURITY_IMPERSONATION_LEVEL]
    /// </summary>
    public enum ImpersonationLevel : uint
    {
        /// <summary>
        ///  The server process cannot obtain identification information about the client and cannot impersonate the client.
        ///  [SecurityAnonymous]
        /// </summary>
        Anonymous,

        /// <summary>
        ///  The server process can obtain identification information about the client, but cannot impersonate the client.
        ///  [SecurityIdentification]
        /// </summary>
        Identification,

        /// <summary>
        ///  The server process can impersonate the client's security context on it's local system.
        ///  [SecurityImpersonation]
        /// </summary>
        Impersonation,

        /// <summary>
        ///  The server process can impersonate the client's security context on remote systems.
        ///  [SecurityDelegation]
        /// </summary>
        Delegation
    }

    /// <summary>
    /// <a href="https://msdn.microsoft.com/en-us/library/cc234317.aspx">SECURITY_CONTEXT_TRACKING_MODE</a>
    /// </summary>
    public enum ContextTrackingMode : byte
    {
        /// <summary>
        ///  The server is given a snapshot of the client's security context.
        ///  [SECURITY_STATIC_TRACKING]
        /// </summary>
        Static = 0x00,

        /// <summary>
        ///  The server is continually updated with changes.
        ///  [SECURITY_DYNAMIC_TRACKING]
        /// </summary>
        Dynamic = 0x01
    }
}
