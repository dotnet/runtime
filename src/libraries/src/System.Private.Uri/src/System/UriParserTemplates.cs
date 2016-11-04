// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System {
    
    public class HttpStyleUriParser: UriParser
    {
        public HttpStyleUriParser():base(UriParser.HttpUri.Flags)
        {
        }
    }

    public class FtpStyleUriParser: UriParser
    {
        public FtpStyleUriParser():base(UriParser.FtpUri.Flags)
        {
        }
    }

    public class FileStyleUriParser: UriParser
    {
        public FileStyleUriParser():base(UriParser.FileUri.Flags)
        {
        }
    }

    public class NewsStyleUriParser: UriParser
    {
        public NewsStyleUriParser():base(UriParser.NewsUri.Flags)
        {
        }
    }

    public class GopherStyleUriParser: UriParser
    {
        public GopherStyleUriParser():base(UriParser.GopherUri.Flags)
        {
        }
    }

    public class LdapStyleUriParser: UriParser
    {
        public LdapStyleUriParser():base(UriParser.LdapUri.Flags)
        {
        }
    }

    public class NetPipeStyleUriParser: UriParser
    {
        public NetPipeStyleUriParser():base(UriParser.NetPipeUri.Flags)
        {
        }
    }
    
    public class NetTcpStyleUriParser: UriParser
    {
        public NetTcpStyleUriParser():base(UriParser.NetTcpUri.Flags)
        {
        }
    }
}