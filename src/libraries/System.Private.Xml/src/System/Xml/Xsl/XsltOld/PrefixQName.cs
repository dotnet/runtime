// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class PrefixQName
    {
        public string? Prefix;
        public string? Name;
        public string? Namespace;

        [MemberNotNull(nameof(Prefix))]
        internal void ClearPrefix()
        {
            Prefix = string.Empty;
        }

        [MemberNotNull(nameof(Prefix))]
        [MemberNotNull(nameof(Name))]
        internal void SetQName(string qname)
        {
            PrefixQName.ParseQualifiedName(qname, out Prefix, out Name);
        }

        //
        // Parsing qualified names
        //

        public static void ParseQualifiedName(string qname, out string prefix, out string local)
        {
            Debug.Assert(qname != null);
            prefix = string.Empty;
            local = string.Empty;

            // parse first NCName (prefix or local name)
            int position = ValidateNames.ParseNCName(qname);
            if (position == 0)
            {
                throw XsltException.Create(SR.Xslt_InvalidQName, qname);
            }
            local = qname.Substring(0, position);

            // not at the end -> parse ':' and the second NCName (local name)
            if (position < qname.Length)
            {
                if (qname[position] == ':')
                {
                    int startLocalNamePos = ++position;
                    prefix = local;
                    int len = ValidateNames.ParseNCName(qname, position);
                    position += len;
                    if (len == 0)
                    {
                        throw XsltException.Create(SR.Xslt_InvalidQName, qname);
                    }
                    local = qname.Substring(startLocalNamePos, len);
                }

                // still not at the end -> error
                if (position < qname.Length)
                {
                    throw XsltException.Create(SR.Xslt_InvalidQName, qname);
                }
            }
        }

        public static bool ValidatePrefix(string prefix)
        {
            if (prefix.Length == 0)
            {
                return false;
            }
            int endPos = ValidateNames.ParseNCName(prefix, 0);
            return endPos == prefix.Length;
        }
    }
}
