// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    // Used to control whether absolute or relative URIs are used
    public enum UriKind
    {
        RelativeOrAbsolute = 0,
        Absolute = 1,
        Relative = 2
    }

    [Flags]
    public enum UriComponents
    {
        // Generic parts.
        // ATTN: The values must stay in sync with Uri.Flags.xxxNotCanonical
        Scheme = 0x1,
        UserInfo = 0x2,
        Host = 0x4,
        Port = 0x8,
        Path = 0x10,
        Query = 0x20,
        Fragment = 0x40,

        StrongPort = 0x80,
        NormalizedHost = 0x100,

        // This will also return respective delimiters for scheme, userinfo or port
        // Valid only for a single component requests.
        KeepDelimiter = 0x40000000,

        // This is used by GetObjectData and can also be used directly.
        // Works for both absolute and relative Uris
        SerializationInfoString = unchecked((int)0x80000000),

        // Shortcuts for general cases
        AbsoluteUri = Scheme | UserInfo | Host | Port | Path | Query | Fragment,
        HostAndPort = Host | StrongPort,                //includes port even if default
        StrongAuthority = UserInfo | Host | StrongPort, //includes port even if default
        SchemeAndServer = Scheme | Host | Port,
        HttpRequestUrl = Scheme | Host | Port | Path | Query,
        PathAndQuery = Path | Query,
    }
    public enum UriFormat
    {
        UriEscaped = 1,
        Unescaped = 2,      // Completely unescaped.
        SafeUnescaped = 3   // Canonical unescaped.  Allows same uri to be reconstructed from the output.
        // If the unescaped sequence results in a new escaped sequence, it will revert to the original sequence.

        // This value is reserved for the default ToString() format that is historically none of the above.
        // V1ToStringUnescape = 0x7FFF
    }

    internal enum ParsingError
    {
        // looks good
        None = 0,

        // These first errors indicate that the Uri cannot be absolute, but may be relative.
        BadFormat = 1,
        BadScheme = 2,
        BadAuthority = 3,
        EmptyUriString = 4,
        LastRelativeUriOkErrIndex = 4,

        // All higher error values are fatal, indicating that neither an absolute or relative
        // Uri could be generated.
        SchemeLimit = 5,
        SizeLimit = 6,
        MustRootedPath = 7,

        // derived class controlled
        BadHostName = 8,
        NonEmptyHost = 9, // unix only
        BadPort = 10,
        BadAuthorityTerminator = 11,

        // The user requested only a relative Uri, but an absolute Uri was parsed.
        CannotCreateRelative = 12
    }

    [Flags]
    internal enum UnescapeMode
    {
        CopyOnly = 0x0,                          // used for V1.0 ToString() compatibility mode only
        Escape = 0x1,                            // Only used by ImplicitFile, the string is already fully unescaped
        Unescape = 0x2,                          // Only used as V1.0 UserEscaped compatibility mode
        EscapeUnescape = Unescape | Escape,      // does both escaping control+reserved and unescaping of safe characters
        V1ToStringFlag = 0x4,                    // Only used as V1.0 ToString() compatibility mode, assumes DontEscape level also
        UnescapeAll = 0x8,                       // just unescape everything, leave bad escaped sequences as is
        UnescapeAllOrThrow = 0x10 | UnescapeAll, // just unescape everything plus throw on bad escaped sequences
    }
}
