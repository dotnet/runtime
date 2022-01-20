// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    //
    // Parses an assembly name.
    //
    [System.Runtime.CompilerServices.ReflectionBlocked]
    public static class AssemblyNameParser
    {
        public static void Parse(AssemblyName blank, string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            RuntimeAssemblyName runtimeAssemblyName = Parse(s);
            runtimeAssemblyName.CopyToAssemblyName(blank);
        }

        public static RuntimeAssemblyName Parse(string s)
        {
            Debug.Assert(s != null);

            int indexOfNul = s.IndexOf((char)0);
            if (indexOfNul != -1)
                s = s.Substring(0, indexOfNul);
            if (s.Length == 0)
                throw new ArgumentException(SR.Format_StringZeroLength);

            AssemblyNameLexer lexer = new AssemblyNameLexer(s);

            // Name must come first.
            string name;
            AssemblyNameLexer.Token token = lexer.GetNext(out name);
            if (token != AssemblyNameLexer.Token.String)
                throw new FileLoadException(SR.InvalidAssemblyName);

            if (name == string.Empty || name.IndexOfAny(s_illegalCharactersInSimpleName) != -1)
                throw new FileLoadException(SR.InvalidAssemblyName);

            Version? version = null;
            string? cultureName = null;
            byte[]? pkt = null;
            AssemblyNameFlags flags = 0;

            LowLevelList<string> alreadySeen = new LowLevelList<string>();
            token = lexer.GetNext();
            while (token != AssemblyNameLexer.Token.End)
            {
                if (token != AssemblyNameLexer.Token.Comma)
                    throw new FileLoadException(SR.InvalidAssemblyName);
                string attributeName;

                token = lexer.GetNext(out attributeName);
                if (token != AssemblyNameLexer.Token.String)
                    throw new FileLoadException(SR.InvalidAssemblyName);
                token = lexer.GetNext();

                // Compat note: Inside AppX apps, the desktop CLR's AssemblyName parser skips past any elements that don't follow the "<Something>=<Something>" pattern.
                //  (when running classic Windows apps, such an illegal construction throws an exception as expected.)
                // Naturally, at least one app unwittingly takes advantage of this.
                if (token == AssemblyNameLexer.Token.Comma || token == AssemblyNameLexer.Token.End)
                    continue;

                if (token != AssemblyNameLexer.Token.Equals)
                    throw new FileLoadException(SR.InvalidAssemblyName);
                string attributeValue;
                token = lexer.GetNext(out attributeValue);
                if (token != AssemblyNameLexer.Token.String)
                    throw new FileLoadException(SR.InvalidAssemblyName);

                if (attributeName == string.Empty)
                    throw new FileLoadException(SR.InvalidAssemblyName);

                for (int i = 0; i < alreadySeen.Count; i++)
                {
                    if (alreadySeen[i].Equals(attributeName, StringComparison.OrdinalIgnoreCase))
                        throw new FileLoadException(SR.InvalidAssemblyName); // Cannot specify the same attribute twice.
                }
                alreadySeen.Add(attributeName);

                if (attributeName.Equals("Version", StringComparison.OrdinalIgnoreCase))
                {
                    version = ParseVersion(attributeValue);
                }

                if (attributeName.Equals("Culture", StringComparison.OrdinalIgnoreCase))
                {
                    cultureName = ParseCulture(attributeValue);
                }

                if (attributeName.Equals("PublicKeyToken", StringComparison.OrdinalIgnoreCase))
                {
                    pkt = ParsePKT(attributeValue);
                }

                if (attributeName.Equals("ProcessorArchitecture", StringComparison.OrdinalIgnoreCase))
                {
                    flags |= (AssemblyNameFlags)(((int)ParseProcessorArchitecture(attributeValue)) << 4);
                }

                if (attributeName.Equals("Retargetable", StringComparison.OrdinalIgnoreCase))
                {
                    if (attributeValue.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                        flags |= AssemblyNameFlags.Retargetable;
                    else if (attributeValue.Equals("No", StringComparison.OrdinalIgnoreCase))
                    {
                        // nothing to do
                    }
                    else
                        throw new FileLoadException(SR.InvalidAssemblyName);
                }

                if (attributeName.Equals("ContentType", StringComparison.OrdinalIgnoreCase))
                {
                    if (attributeValue.Equals("WindowsRuntime", StringComparison.OrdinalIgnoreCase))
                        flags |= (AssemblyNameFlags)(((int)AssemblyContentType.WindowsRuntime) << 9);
                    else
                        throw new FileLoadException(SR.InvalidAssemblyName);
                }

                // Desktop compat: If we got here, the attribute name is unknown to us. Ignore it (as long it's not duplicated.)
                token = lexer.GetNext();
            }
            return new RuntimeAssemblyName(name, version, cultureName, flags, pkt);
        }

        private static Version ParseVersion(string attributeValue)
        {
            string[] parts = attributeValue.Split('.');
            if (parts.Length > 4)
                throw new FileLoadException(SR.InvalidAssemblyName);
            ushort[] versionNumbers = new ushort[4];
            for (int i = 0; i < versionNumbers.Length; i++)
            {
                if (i >= parts.Length)
                    versionNumbers[i] = ushort.MaxValue;
                else
                {
                    // Desktop compat: TryParse is a little more forgiving than Fusion.
                    for (int j = 0; j < parts[i].Length; j++)
                    {
                        if (!char.IsDigit(parts[i][j]))
                            throw new FileLoadException(SR.InvalidAssemblyName);
                    }
                    if (!(ushort.TryParse(parts[i], out versionNumbers[i])))
                    {
                        throw new FileLoadException(SR.InvalidAssemblyName);
                    }
                }
            }

            if (versionNumbers[0] == ushort.MaxValue || versionNumbers[1] == ushort.MaxValue)
                throw new FileLoadException(SR.InvalidAssemblyName);
            if (versionNumbers[2] == ushort.MaxValue)
                return new Version(versionNumbers[0], versionNumbers[1]);
            if (versionNumbers[3] == ushort.MaxValue)
                return new Version(versionNumbers[0], versionNumbers[1], versionNumbers[2]);
            return new Version(versionNumbers[0], versionNumbers[1], versionNumbers[2], versionNumbers[3]);
        }

        private static string ParseCulture(string attributeValue)
        {
            if (attributeValue.Equals("Neutral", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }
            else
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(attributeValue); // Force a CultureNotFoundException if not a valid culture.
                return culture.Name;
            }
        }

        private static byte[] ParsePKT(string attributeValue)
        {
            if (attributeValue.Equals("null", StringComparison.OrdinalIgnoreCase) || attributeValue == string.Empty)
                return Array.Empty<byte>();

            if (attributeValue.Length != 8 * 2)
                throw new FileLoadException(SR.InvalidAssemblyName);

            byte[] pkt = new byte[8];
            int srcIndex = 0;
            for (int i = 0; i < 8; i++)
            {
                char hi = attributeValue[srcIndex++];
                char lo = attributeValue[srcIndex++];
                pkt[i] = (byte)((ParseHexNybble(hi) << 4) | ParseHexNybble(lo));
            }
            return pkt;
        }

        private static ProcessorArchitecture ParseProcessorArchitecture(string attributeValue)
        {
            if (attributeValue.Equals("msil", StringComparison.OrdinalIgnoreCase))
                return ProcessorArchitecture.MSIL;
            if (attributeValue.Equals("x86", StringComparison.OrdinalIgnoreCase))
                return ProcessorArchitecture.X86;
            if (attributeValue.Equals("ia64", StringComparison.OrdinalIgnoreCase))
                return ProcessorArchitecture.IA64;
            if (attributeValue.Equals("amd64", StringComparison.OrdinalIgnoreCase))
                return ProcessorArchitecture.Amd64;
            if (attributeValue.Equals("arm", StringComparison.OrdinalIgnoreCase))
                return ProcessorArchitecture.Arm;
            throw new FileLoadException(SR.InvalidAssemblyName);
        }

        private static byte ParseHexNybble(char c)
        {
            if (c >= '0' && c <= '9')
                return (byte)(c - '0');
            if (c >= 'a' && c <= 'f')
                return (byte)(c - 'a' + 10);
            if (c >= 'A' && c <= 'F')
                return (byte)(c - 'A' + 10);
            throw new FileLoadException(SR.InvalidAssemblyName);
        }

        private static readonly char[] s_illegalCharactersInSimpleName = { '/', '\\', ':' };
    }
}
