// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
  Type:  AssemblyNameHelpers
**
==============================================================*/

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace System.Reflection.Runtime.Assemblies
{
    internal static class AssemblyNameHelpers
    {
        private const int PUBLIC_KEY_TOKEN_LEN = 8;

        public static String ComputeDisplayName(AssemblyName a)
        {
            if (a.Name == String.Empty)
                throw new FileLoadException();

            StringBuilder sb = new StringBuilder();
            if (a.Name != null)
            {
                sb.AppendQuoted(a.Name);
            }

            if (a.Version != null)
            {
                Version canonicalizedVersion = a.Version.CanonicalizeVersion();
                if (canonicalizedVersion.Major != ushort.MaxValue)
                {
                    sb.Append(", Version=");
                    sb.Append(canonicalizedVersion.Major);

                    if(canonicalizedVersion.Minor != ushort.MaxValue)
                    {
                        sb.Append('.');
                        sb.Append(canonicalizedVersion.Minor);

                        if(canonicalizedVersion.Build != ushort.MaxValue)
                        {
                            sb.Append('.');
                            sb.Append(canonicalizedVersion.Build);

                            if(canonicalizedVersion.Revision != ushort.MaxValue)
                            {
                                sb.Append('.');
                                sb.Append(canonicalizedVersion.Revision);
                            }
                        }
                    }
                }
            }

            String cultureName = a.CultureName;
            if (cultureName != null)
            {
                if (cultureName == String.Empty)
                    cultureName = "neutral";
                sb.Append(", Culture=");
                sb.AppendQuoted(cultureName);
            }

            byte[] pkt = a.GetPublicKeyToken();
            if (pkt != null)
            {
                if (pkt.Length > PUBLIC_KEY_TOKEN_LEN)
                    throw new ArgumentException();

                sb.Append(", PublicKeyToken=");
                if (pkt.Length == 0)
                    sb.Append("null");
                else
                {
                    foreach (byte b in pkt)
                    {
                        sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                    }
                }
            }

            if (0 != (a.Flags & AssemblyNameFlags.Retargetable))
                sb.Append(", Retargetable=Yes");

            AssemblyContentType contentType = a.ContentType;
            if (contentType == AssemblyContentType.WindowsRuntime)
                sb.Append(", ContentType=WindowsRuntime");

            // NOTE: By design (desktop compat) AssemblyName.FullName and ToString() do not include ProcessorArchitecture.

            return sb.ToString();
        }

        private static void AppendQuoted(this StringBuilder sb, String s)
        {
            bool needsQuoting = false;
            const char quoteChar = '\"';

            //@todo: App-compat: You can use double or single quotes to quote a name, and Fusion (or rather the IdentityAuthority) picks one
            // by some algorithm. Rather than guess at it, I'll just use double-quote consistently.
            if (s != s.Trim() || s.Contains("\"") || s.Contains("\'"))
                needsQuoting = true;

            if (needsQuoting)
                sb.Append(quoteChar);

            for (int i = 0; i < s.Length; i++)
            {
                bool addedEscape = false;
                foreach (KeyValuePair<char, String> kv in AssemblyNameLexer.EscapeSequences)
                {
                    String escapeReplacement = kv.Value;
                    if (!(s[i] == escapeReplacement[0]))
                        continue;
                    if ((s.Length - i) < escapeReplacement.Length)
                        continue;
                    String prefix = s.Substring(i, escapeReplacement.Length);
                    if (prefix == escapeReplacement)
                    {
                        sb.Append('\\');
                        sb.Append(kv.Key);
                        addedEscape = true;
                    }
                }

                if (!addedEscape)
                    sb.Append(s[i]);
            }

            if (needsQuoting)
                sb.Append(quoteChar);
        }

        public static Version CanonicalizeVersion(this Version version)
        {
            ushort major = (ushort)version.Major;
            ushort minor = (ushort)version.Minor;
            ushort build = (ushort)version.Build;
            ushort revision = (ushort)version.Revision;

            if (major == version.Major && minor == version.Minor && build == version.Build && revision == version.Revision)
                return version;

            return new Version(major, minor, build, revision);
        }

        internal static AssemblyContentType ExtractAssemblyContentType(this AssemblyNameFlags flags)
        {
            return (AssemblyContentType)((((int)flags) >> 9) & 0x7);
        }

        internal static ProcessorArchitecture ExtractProcessorArchitecture(this AssemblyNameFlags flags)
        {
            return (ProcessorArchitecture)((((int)flags) >> 4) & 0x7);
        }

        public static AssemblyNameFlags ExtractAssemblyNameFlags(this AssemblyNameFlags combinedFlags)
        {
            return combinedFlags & unchecked((AssemblyNameFlags)0xFFFFF10F);
        }

        internal static AssemblyNameFlags CombineAssemblyNameFlags(AssemblyNameFlags flags, AssemblyContentType contentType, ProcessorArchitecture processorArchitecture)
        {
            return (AssemblyNameFlags)(((int)flags) | (((int)contentType) << 9) | ((int)processorArchitecture << 4));
        }
    }
}