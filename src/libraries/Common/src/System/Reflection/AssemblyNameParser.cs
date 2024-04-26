// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

#nullable enable

namespace System.Reflection
{
    /// <summary>
    /// Parses an assembly name.
    /// </summary>
    internal ref partial struct AssemblyNameParser
    {
        public readonly struct AssemblyNameParts
        {
            public AssemblyNameParts(string name, Version? version, string? cultureName, AssemblyNameFlags flags, byte[]? publicKeyOrToken)
            {
                _name = name;
                _version = version;
                _cultureName = cultureName;
                _flags = flags;
                _publicKeyOrToken = publicKeyOrToken;
            }

            public readonly string _name;
            public readonly Version? _version;
            public readonly string? _cultureName;
            public readonly AssemblyNameFlags _flags;
            public readonly byte[]? _publicKeyOrToken;
        }

        /// <summary>
        /// Token categories for the lexer.
        /// </summary>
        private enum Token
        {
            Equals = 1,
            Comma = 2,
            String = 3,
            End = 4,
        }

        private enum AttributeKind
        {
            Version = 1,
            Culture = 2,
            PublicKeyOrToken = 4,
            ProcessorArchitecture = 8,
            Retargetable = 16,
            ContentType = 32
        }

        private readonly ReadOnlySpan<char> _input;
        private int _index;

        private AssemblyNameParser(ReadOnlySpan<char> input)
        {
#if SYSTEM_PRIVATE_CORELIB
            if (input.Length == 0)
                throw new ArgumentException(SR.Format_StringZeroLength);
#else
            Debug.Assert(input.Length > 0);
#endif

            _input = input;
            _index = 0;
        }

#if SYSTEM_PRIVATE_CORELIB
        public static AssemblyNameParts Parse(string name) => Parse(name.AsSpan());

        public static AssemblyNameParts Parse(ReadOnlySpan<char> name)
        {
            AssemblyNameParser parser = new(name);
            AssemblyNameParts result = default;
            if (parser.TryParse(ref result))
            {
                return result;
            }
            throw new FileLoadException(SR.InvalidAssemblyName, name.ToString());
        }
#endif

        internal static bool TryParse(ReadOnlySpan<char> name, ref AssemblyNameParts parts)
        {
            AssemblyNameParser parser = new(name);
            return parser.TryParse(ref parts);
        }

        private static bool TryRecordNewSeen(scoped ref AttributeKind seenAttributes, AttributeKind newAttribute)
        {
            if ((seenAttributes & newAttribute) != 0)
            {
                return false;
            }
            seenAttributes |= newAttribute;
            return true;
        }

        private bool TryParse(ref AssemblyNameParts result)
        {
            // Name must come first.
            if (!TryGetNextToken(out string name, out Token token) || token != Token.String || string.IsNullOrEmpty(name))
                return false;

            Version? version = null;
            string? cultureName = null;
            byte[]? pkt = null;
            AssemblyNameFlags flags = 0;

            AttributeKind alreadySeen = default;
            if (!TryGetNextToken(out _, out token))
                return false;

            while (token != Token.End)
            {
                if (token != Token.Comma)
                    return false;

                if (!TryGetNextToken(out string attributeName, out token) || token != Token.String)
                    return false;

                if (!TryGetNextToken(out _, out token) || token != Token.Equals)
                    return false;

                if (!TryGetNextToken(out string attributeValue, out token) || token != Token.String)
                    return false;

                if (attributeName == string.Empty)
                    return false;

                if (IsAttribute(attributeName, "Version"))
                {
                    if (!TryRecordNewSeen(ref alreadySeen, AttributeKind.Version))
                    {
                        return false;
                    }
                    if (!TryParseVersion(attributeValue, ref version))
                    {
                        return false;
                    }
                }
                else if (IsAttribute(attributeName, "Culture"))
                {
                    if (!TryRecordNewSeen(ref alreadySeen, AttributeKind.Culture))
                    {
                        return false;
                    }
                    if (!TryParseCulture(attributeValue, out cultureName))
                    {
                        return false;
                    }
                }
                else if (IsAttribute(attributeName, "PublicKeyToken"))
                {
                    if (!TryRecordNewSeen(ref alreadySeen, AttributeKind.PublicKeyOrToken))
                    {
                        return false;
                    }
                    if (!TryParsePKT(attributeValue, isToken: true, out pkt))
                    {
                        return false;
                    }
                }
                else if (IsAttribute(attributeName, "PublicKey"))
                {
                    if (!TryRecordNewSeen(ref alreadySeen, AttributeKind.PublicKeyOrToken))
                    {
                        return false;
                    }
                    if (!TryParsePKT(attributeValue, isToken: false, out pkt))
                    {
                        return false;
                    }
                    flags |= AssemblyNameFlags.PublicKey;
                }
                else if (IsAttribute(attributeName, "ProcessorArchitecture"))
                {
                    if (!TryRecordNewSeen(ref alreadySeen, AttributeKind.ProcessorArchitecture))
                    {
                        return false;
                    }
                    if (!TryParseProcessorArchitecture(attributeValue, out ProcessorArchitecture arch))
                    {
                        return false;
                    }
                    flags |= (AssemblyNameFlags)(((int)arch) << 4);
                }
                else if (IsAttribute(attributeName, "Retargetable"))
                {
                    if (!TryRecordNewSeen(ref alreadySeen, AttributeKind.Retargetable))
                    {
                        return false;
                    }

                    if (attributeValue.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                    {
                        flags |= AssemblyNameFlags.Retargetable;
                    }
                    else if (attributeValue.Equals("No", StringComparison.OrdinalIgnoreCase))
                    {
                        // nothing to do
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (IsAttribute(attributeName, "ContentType"))
                {
                    if (!TryRecordNewSeen(ref alreadySeen, AttributeKind.ContentType))
                    {
                        return false;
                    }

                    if (attributeValue.Equals("WindowsRuntime", StringComparison.OrdinalIgnoreCase))
                    {
                        flags |= (AssemblyNameFlags)(((int)AssemblyContentType.WindowsRuntime) << 9);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // Desktop compat: If we got here, the attribute name is unknown to us. Ignore it.
                }

                if (!TryGetNextToken(out _, out token))
                {
                    return false;
                }
            }

            result = new AssemblyNameParts(name, version, cultureName, flags, pkt);
            return true;
        }

        private static bool IsAttribute(string candidate, string attributeKind)
            => candidate.Equals(attributeKind, StringComparison.OrdinalIgnoreCase);

        private static bool TryParseVersion(string attributeValue, ref Version? version)
        {
#if NET8_0_OR_GREATER
            ReadOnlySpan<char> attributeValueSpan = attributeValue;
            Span<Range> parts = stackalloc Range[5];
            parts = parts.Slice(0, attributeValueSpan.Split(parts, '.'));
#else
            string[] parts = attributeValue.Split('.');
#endif
            if (parts.Length is < 2 or > 4)
            {
                return false;
            }

            Span<ushort> versionNumbers = stackalloc ushort[4] { ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue };
            for (int i = 0; i < parts.Length; i++)
            {
                if (!ushort.TryParse(
#if NET8_0_OR_GREATER
                    attributeValueSpan[parts[i]],
#else
                    parts[i],
#endif
                    NumberStyles.None, NumberFormatInfo.InvariantInfo, out versionNumbers[i]))
                {
                    return false;
                }
            }

            if (versionNumbers[0] == ushort.MaxValue ||
                versionNumbers[1] == ushort.MaxValue)
            {
                return false;
            }

            version =
                versionNumbers[2] == ushort.MaxValue ? new Version(versionNumbers[0], versionNumbers[1]) :
                versionNumbers[3] == ushort.MaxValue ? new Version(versionNumbers[0], versionNumbers[1], versionNumbers[2]) :
                new Version(versionNumbers[0], versionNumbers[1], versionNumbers[2], versionNumbers[3]);

            return true;
        }

        private static bool TryParseCulture(string attributeValue, out string? result)
        {
            if (attributeValue.Equals("Neutral", StringComparison.OrdinalIgnoreCase))
            {
                result = "";
                return true;
            }

            result = attributeValue;
            return true;
        }

        private static bool TryParsePKT(string attributeValue, bool isToken, out byte[]? result)
        {
            if (attributeValue.Equals("null", StringComparison.OrdinalIgnoreCase) || attributeValue == string.Empty)
            {
                result = Array.Empty<byte>();
                return true;
            }

            if (attributeValue.Length % 2 != 0 || (isToken && attributeValue.Length != 8 * 2))
            {
                result = null;
                return false;
            }

            byte[] pkt = new byte[attributeValue.Length / 2];
            if (!HexConverter.TryDecodeFromUtf16(attributeValue.AsSpan(), pkt, out int _))
            {
                result = null;
                return false;
            }

            result = pkt;
            return true;
        }

        private static bool TryParseProcessorArchitecture(string attributeValue, out ProcessorArchitecture result)
        {
            result = attributeValue switch
            {
                _ when attributeValue.Equals("msil", StringComparison.OrdinalIgnoreCase) => ProcessorArchitecture.MSIL,
                _ when attributeValue.Equals("x86", StringComparison.OrdinalIgnoreCase) => ProcessorArchitecture.X86,
                _ when attributeValue.Equals("ia64", StringComparison.OrdinalIgnoreCase) => ProcessorArchitecture.IA64,
                _ when attributeValue.Equals("amd64", StringComparison.OrdinalIgnoreCase) => ProcessorArchitecture.Amd64,
                _ when attributeValue.Equals("arm", StringComparison.OrdinalIgnoreCase) => ProcessorArchitecture.Arm,
                _ when attributeValue.Equals("msil", StringComparison.OrdinalIgnoreCase) => ProcessorArchitecture.MSIL,
                _ => ProcessorArchitecture.None
            };
            return result != ProcessorArchitecture.None;
        }

        private static bool IsWhiteSpace(char ch)
            => ch is '\n' or '\r' or ' ' or '\t';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetNextChar(out char ch)
        {
            if (_index < _input.Length)
            {
                ch = _input[_index++];
                if (ch == '\0')
                {
                    return false;
                }
            }
            else
            {
                ch = '\0';
            }

            return true;
        }

        //
        // Return the next token in assembly name. If the result is Token.String,
        // sets "tokenString" to the tokenized string.
        //
        private bool TryGetNextToken(out string tokenString, out Token token)
        {
            tokenString = string.Empty;
            char c;

            while (true)
            {
                if (!TryGetNextChar(out c))
                {
                    token = default;
                    return false;
                }

                switch (c)
                {
                    case ',':
                    {
                        token = Token.Comma;
                        return true;
                    }
                    case '=':
                    {
                        token = Token.Equals;
                        return true;
                    }
                    case '\0':
                    {
                        token = Token.End;
                        return true;
                    }
                }

                if (!IsWhiteSpace(c))
                {
                    break;
                }
            }

            using ValueStringBuilder sb = new ValueStringBuilder(stackalloc char[64]);

            char quoteChar = '\0';
            if (c is '\'' or '\"')
            {
                quoteChar = c;
                if (!TryGetNextChar(out c))
                {
                    token = default;
                    return false;
                }
            }

            for (; ; )
            {
                if (c == 0)
                {
                    if (quoteChar != 0)
                    {
                        // EOS and unclosed quotes is an error
                        token = default;
                        return false;
                    }
                    // Reached end of input and therefore of string
                    break;
                }

                if (quoteChar != 0 && c == quoteChar)
                    break;  // Terminate: Found closing quote of quoted string.

                if (quoteChar == 0 && (c is ',' or '='))
                {
                    _index--;
                    break;  // Terminate: Found start of a new ',' or '=' token.
                }

                if (quoteChar == 0 && (c is '\'' or '\"'))
                {
                    token = default;
                    return false;
                }

                if (c is '\\')
                {
                    if (!TryGetNextChar(out c))
                    {
                        token = default;
                        return false;
                    }

                    switch (c)
                    {
                        case '\\':
                        case ',':
                        case '=':
                        case '\'':
                        case '"':
                            sb.Append(c);
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        default:
                            token = default;
                            return false;
                    }
                }
                else
                {
                    sb.Append(c);
                }

                if (!TryGetNextChar(out c))
                {
                    token = default;
                    return false;
                }
            }


            int length = sb.Length;
            if (quoteChar == 0)
            {
                while (length > 0 && IsWhiteSpace(sb[length - 1]))
                    length--;
            }

            tokenString = sb.AsSpan(0, length).ToString();
            token = Token.String;
            return true;
        }
    }
}
