// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{

    //
    // This enum specifies the public options used to customize a hierarchical built-in parser.
    //
    [Flags]
    public enum GenericUriParserOptions
    {
        // A hierarchical URI, allows a userinfo, non empty Inet-based authority, path, query and fragment
        // The URI path gets aggressively compressed means dots, slashes and backslashes are unescaped,
        // backslashesare converted, and then it compresses the path. It also removes trailing dots,
        // empty segments and dots-only segments
        Default                         = 0x0,

        // Allows a free style authority that would terminate with '/'
        GenericAuthority           = 0x1,

        // Allows an empty authority foo:///
        AllowEmptyAuthority        = 0x2,

        // Disables a user info component, it implied in the case of GenericAuthority flag
        NoUserInfo                 = 0x4,

        // Disables a port component, it is implied in the case of GenericAuthority flag
        NoPort                     = 0x8,

        // Disables a query. A ? char is considered as part of the path and is escaped
        NoQuery                    = 0x10,

        // Disables a fragment. A # char is considered as part of the path or query and is escaped
        NoFragment                 = 0x20,

        // if false then converta \ to /, otherwise does this conversion for the Path component.
        DontConvertPathBackslashes = 0x40,

        // if false, then a/./b or a/.../b becomes a/b and /a/../b becomes /b
        DontCompressPath           = 0x80,

        // if false  then a/%2e./b  becomes a/../b and then usually compressed
        DontUnescapePathDotsAndSlashes= 0x100,

        // IDN hosts supported. if true then unicode hostname is converted to IDN host
        //  and vice versa
        Idn = 0x200,

        //  Iri strict parsing flag. Makes sense for Unicode. If true then string is
        //  normalized, bidi control characters are removed, unicode char limits are checked
        IriParsing = 0x400
    }

    public class GenericUriParser : UriParser
    {
        public GenericUriParser(GenericUriParserOptions options) : base(MapGenericParserOptions(options))
        {
        }

        private static UriSyntaxFlags MapGenericParserOptions(GenericUriParserOptions options)
        {
            //
            // Here we map public flags to internal ones
            // Note an instacne of this parser is always a "simple parser" since the class is sealed.
            //
            UriSyntaxFlags flags = DefaultGenericUriParserFlags;

            if ((options & GenericUriParserOptions.GenericAuthority) != 0)
            {
                // Disable some options that are not compatible with generic authority
                flags &= ~(UriSyntaxFlags.MayHaveUserInfo | UriSyntaxFlags.MayHavePort | UriSyntaxFlags.AllowUncHost | UriSyntaxFlags.AllowAnInternetHost);
                flags |= UriSyntaxFlags.AllowAnyOtherHost;
            }

            if ((options & GenericUriParserOptions.AllowEmptyAuthority) != 0)
            {
                flags |= UriSyntaxFlags.AllowEmptyHost;
            }

            if ((options & GenericUriParserOptions.NoUserInfo) != 0)
            {
                flags &= ~UriSyntaxFlags.MayHaveUserInfo;
            }

            if ((options & GenericUriParserOptions.NoPort) != 0)
            {
                flags &= ~UriSyntaxFlags.MayHavePort;
            }

            if ((options & GenericUriParserOptions.NoQuery) != 0)
            {
                flags &= ~UriSyntaxFlags.MayHaveQuery;
            }

            if ((options & GenericUriParserOptions.NoFragment) != 0)
            {
                flags &= ~UriSyntaxFlags.MayHaveFragment;
            }

            if ((options & GenericUriParserOptions.DontConvertPathBackslashes) != 0)
            {
                flags &= ~UriSyntaxFlags.ConvertPathSlashes;
            }

            if ((options & GenericUriParserOptions.DontCompressPath) != 0)
            {
                flags &= ~(UriSyntaxFlags.CompressPath | UriSyntaxFlags.CanonicalizeAsFilePath);
            }

            if ((options & GenericUriParserOptions.DontUnescapePathDotsAndSlashes) != 0)
            {
                flags &= ~UriSyntaxFlags.UnEscapeDotsAndSlashes;
            }

            if ((options & GenericUriParserOptions.Idn) != 0)
            {
                flags |= UriSyntaxFlags.AllowIdn;
            }

            if ((options & GenericUriParserOptions.IriParsing) != 0)
            {
                flags |= UriSyntaxFlags.AllowIriParsing;
            }

            return flags;
        }

        private const UriSyntaxFlags DefaultGenericUriParserFlags =
                                                                    UriSyntaxFlags.MustHaveAuthority |
                                                                    UriSyntaxFlags.MayHaveUserInfo |
                                                                    UriSyntaxFlags.MayHavePort |
                                                                    UriSyntaxFlags.MayHavePath |
                                                                    UriSyntaxFlags.MayHaveQuery |
                                                                    UriSyntaxFlags.MayHaveFragment |
                                                                    UriSyntaxFlags.AllowUncHost |
                                                                    UriSyntaxFlags.AllowAnInternetHost |
                                                                    UriSyntaxFlags.PathIsRooted |
                                                                    UriSyntaxFlags.ConvertPathSlashes |
                                                                    UriSyntaxFlags.CompressPath |
                                                                    UriSyntaxFlags.CanonicalizeAsFilePath |
                                                                    UriSyntaxFlags.UnEscapeDotsAndSlashes;
    }
}
