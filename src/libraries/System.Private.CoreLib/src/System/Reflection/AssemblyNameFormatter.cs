// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace System.Reflection
{
    internal static class AssemblyNameFormatter
    {
        public static string ComputeDisplayName(string name, Version? version, string? cultureName, byte[]? pkt, AssemblyNameFlags flags = 0, AssemblyContentType contentType = 0)
        {
            const int PUBLIC_KEY_TOKEN_LEN = 8;
            Debug.Assert(name.Length != 0);

            StringBuilder sb = new StringBuilder();
            sb.AppendQuoted(name);

            if (version != null)
            {
                Version canonicalizedVersion = version.CanonicalizeVersion();
                if (canonicalizedVersion.Major != ushort.MaxValue)
                {
                    sb.Append(", Version=");
                    sb.Append(canonicalizedVersion.Major);

                    if (canonicalizedVersion.Minor != ushort.MaxValue)
                    {
                        sb.Append('.');
                        sb.Append(canonicalizedVersion.Minor);

                        if (canonicalizedVersion.Build != ushort.MaxValue)
                        {
                            sb.Append('.');
                            sb.Append(canonicalizedVersion.Build);

                            if (canonicalizedVersion.Revision != ushort.MaxValue)
                            {
                                sb.Append('.');
                                sb.Append(canonicalizedVersion.Revision);
                            }
                        }
                    }
                }
            }

            if (cultureName != null)
            {
                if (cultureName.Length == 0)
                    cultureName = "neutral";
                sb.Append(", Culture=");
                sb.AppendQuoted(cultureName);
            }

            if (pkt != null)
            {
                if (pkt.Length > PUBLIC_KEY_TOKEN_LEN)
                    throw new ArgumentException();

                sb.Append(", PublicKeyToken=");
                if (pkt.Length == 0)
                    sb.Append("null");
                else
                {
                    sb.Append(HexConverter.ToString(pkt, HexConverter.Casing.Lower));
                }
            }

            if (0 != (flags & AssemblyNameFlags.Retargetable))
                sb.Append(", Retargetable=Yes");

            if (contentType == AssemblyContentType.WindowsRuntime)
                sb.Append(", ContentType=WindowsRuntime");

            // NOTE: By design (desktop compat) AssemblyName.FullName and ToString() do not include ProcessorArchitecture.

            return sb.ToString();
        }

        private static void AppendQuoted(this StringBuilder sb, string s)
        {
            bool needsQuoting = false;
            const char quoteChar = '\"';

            // App-compat: You can use double or single quotes to quote a name, and Fusion (or rather the IdentityAuthority) picks one
            // by some algorithm. Rather than guess at it, we use double quotes consistently.
            if (s != s.Trim() || s.Contains('\"') || s.Contains('\''))
                needsQuoting = true;

            if (needsQuoting)
                sb.Append(quoteChar);

            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case '\\':
                    case ',':
                    case '=':
                    case '\'':
                    case '"':
                        sb.Append('\\');
                        break;
                    case '\t':
                        sb.Append("\\t");
                        continue;
                    case '\r':
                        sb.Append("\\r");
                        continue;
                    case '\n':
                        sb.Append("\\n");
                        continue;
                }

                sb.Append(s[i]);
            }

            if (needsQuoting)
                sb.Append(quoteChar);
        }

        private static Version CanonicalizeVersion(this Version version)
        {
            ushort major = (ushort)version.Major;
            ushort minor = (ushort)version.Minor;
            ushort build = (ushort)version.Build;
            ushort revision = (ushort)version.Revision;

            if (major == version.Major && minor == version.Minor && build == version.Build && revision == version.Revision)
                return version;

            return new Version(major, minor, build, revision);
        }
    }
}
