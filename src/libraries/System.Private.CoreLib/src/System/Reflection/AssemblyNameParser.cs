// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection
{
    //
    // Parses an assembly name.
    //
    internal ref struct AssemblyNameParser
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

        // Token categories for the lexer.
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

        private ReadOnlySpan<char> _input;
        private int _index;

        private AssemblyNameParser(ReadOnlySpan<char> input)
        {
            if (input.Length == 0)
                throw new ArgumentException(SR.Format_StringZeroLength);

            _input = input;
            _index = 0;
        }

        public static AssemblyNameParts Parse(string name)
        {
            return new AssemblyNameParser(name).Parse();
        }

        public static AssemblyNameParts Parse(ReadOnlySpan<char> name)
        {
            return new AssemblyNameParser(name).Parse();
        }

        private void RecordNewSeenOrThrow(ref AttributeKind seenAttributes, AttributeKind newAttribute)
        {
            if ((seenAttributes & newAttribute) != 0)
            {
                ThrowInvalidAssemblyName();
            }
            seenAttributes |= newAttribute;
        }

        private AssemblyNameParts Parse()
        {
            // Name must come first.
            Token token = GetNextToken(out string name);
            if (token != Token.String)
                ThrowInvalidAssemblyName();

            if (string.IsNullOrEmpty(name) || name.AsSpan().IndexOfAny('/', '\\', ':') != -1)
                ThrowInvalidAssemblyName();

            Version? version = null;
            string? cultureName = null;
            byte[]? pkt = null;
            AssemblyNameFlags flags = 0;

            AttributeKind alreadySeen = default;
            token = GetNextToken();
            while (token != Token.End)
            {
                if (token != Token.Comma)
                    ThrowInvalidAssemblyName();

                token = GetNextToken(out string attributeName);
                if (token != Token.String)
                    ThrowInvalidAssemblyName();

                token = GetNextToken();
                if (token != Token.Equals)
                    ThrowInvalidAssemblyName();

                token = GetNextToken(out string attributeValue);
                if (token != Token.String)
                    ThrowInvalidAssemblyName();

                if (attributeName == string.Empty)
                    ThrowInvalidAssemblyName();

                if (attributeName.Equals("Version", StringComparison.OrdinalIgnoreCase))
                {
                    RecordNewSeenOrThrow(ref alreadySeen, AttributeKind.Version);
                    version = ParseVersion(attributeValue);
                }

                if (attributeName.Equals("Culture", StringComparison.OrdinalIgnoreCase))
                {
                    RecordNewSeenOrThrow(ref alreadySeen, AttributeKind.Culture);
                    cultureName = ParseCulture(attributeValue);
                }

                if (attributeName.Equals("PublicKey", StringComparison.OrdinalIgnoreCase))
                {
                    RecordNewSeenOrThrow(ref alreadySeen, AttributeKind.PublicKeyOrToken);
                    pkt = ParsePKT(attributeValue, isToken: false);
                    flags |= AssemblyNameFlags.PublicKey;
                }

                if (attributeName.Equals("PublicKeyToken", StringComparison.OrdinalIgnoreCase))
                {
                    RecordNewSeenOrThrow(ref alreadySeen, AttributeKind.PublicKeyOrToken);
                    pkt = ParsePKT(attributeValue, isToken: true);
                }

                if (attributeName.Equals("ProcessorArchitecture", StringComparison.OrdinalIgnoreCase))
                {
                    RecordNewSeenOrThrow(ref alreadySeen, AttributeKind.ProcessorArchitecture);
                    flags |= (AssemblyNameFlags)(((int)ParseProcessorArchitecture(attributeValue)) << 4);
                }

                if (attributeName.Equals("Retargetable", StringComparison.OrdinalIgnoreCase))
                {
                    RecordNewSeenOrThrow(ref alreadySeen, AttributeKind.Retargetable);
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
                        ThrowInvalidAssemblyName();
                    }
                }

                if (attributeName.Equals("ContentType", StringComparison.OrdinalIgnoreCase))
                {
                    RecordNewSeenOrThrow(ref alreadySeen, AttributeKind.ContentType);
                    if (attributeValue.Equals("WindowsRuntime", StringComparison.OrdinalIgnoreCase))
                    {
                        flags |= (AssemblyNameFlags)(((int)AssemblyContentType.WindowsRuntime) << 9);
                    }
                    else
                    {
                        ThrowInvalidAssemblyName();
                    }
                }

                // Desktop compat: If we got here, the attribute name is unknown to us. Ignore it.
                token = GetNextToken();
            }

            return new AssemblyNameParts(name, version, cultureName, flags, pkt);
        }

        private Version ParseVersion(string attributeValue)
        {
            string[] parts = attributeValue.Split('.');
            if (parts.Length > 4)
                ThrowInvalidAssemblyName();
            Span<ushort> versionNumbers = stackalloc ushort[4];
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
                            ThrowInvalidAssemblyName();
                    }
                    if (!(ushort.TryParse(parts[i], out versionNumbers[i])))
                    {
                        ThrowInvalidAssemblyName();
                    }
                }
            }

            if (versionNumbers[0] == ushort.MaxValue || versionNumbers[1] == ushort.MaxValue)
                ThrowInvalidAssemblyName();
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

            return attributeValue;
        }

        private byte[] ParsePKT(string attributeValue, bool isToken)
        {
            if (attributeValue.Equals("null", StringComparison.OrdinalIgnoreCase) || attributeValue == string.Empty)
                return Array.Empty<byte>();

            if (isToken && attributeValue.Length != 8 * 2)
                ThrowInvalidAssemblyName();

            byte[] pkt = new byte[attributeValue.Length / 2];
            int srcIndex = 0;
            for (int i = 0; i < pkt.Length; i++)
            {
                char hi = attributeValue[srcIndex++];
                char lo = attributeValue[srcIndex++];
                pkt[i] = (byte)((ParseHexNybble(hi) << 4) | ParseHexNybble(lo));
            }
            return pkt;
        }

        private ProcessorArchitecture ParseProcessorArchitecture(string attributeValue)
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
            ThrowInvalidAssemblyName();
            return default; // unreachable
        }

        private byte ParseHexNybble(char c)
        {
            int value = HexConverter.FromChar(c);
            if (value == 0xFF)
            {
                ThrowInvalidAssemblyName();
            }
            return (byte)value;
        }

        //
        // Return the next token in assembly name. If you expect the result to be Token.String,
        // use GetNext(out String) instead.
        //
        private Token GetNextToken()
        {
            return GetNextToken(out _);
        }

        private static bool IsWhiteSpace(char ch)
        {
            switch (ch)
            {
                case '\n':
                case '\r':
                case ' ':
                case '\t':
                    return true;
                default:
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char GetNextChar()
        {
            char ch;
            if (_index < _input.Length)
            {
                ch = _input[_index++];
                if (ch == '\0')
                {
                    ThrowInvalidAssemblyName();
                }
            }
            else
            {
                ch = '\0';
            }

            return ch;
        }

        //
        // Return the next token in assembly name. If the result is Token.String,
        // sets "tokenString" to the tokenized string.
        //
        private Token GetNextToken(out string tokenString)
        {
            tokenString = string.Empty;
            char c;

            while (true)
            {
                c = GetNextChar();
                switch (c)
                {
                    case ',':
                        return Token.Comma;
                    case '=':
                        return Token.Equals;
                    case '\0':
                        return Token.End;
                }

                if (!IsWhiteSpace(c))
                {
                    break;
                }
            }

            ValueStringBuilder sb = new ValueStringBuilder(stackalloc char[64]);

            char quoteChar = '\0';
            if (c == '\'' || c == '\"')
            {
                quoteChar = c;
                c = GetNextChar();
            }

            for (; ; )
            {
                if (c == 0)
                {
                    if (quoteChar != 0)
                    {
                        // EOS and unclosed quotes is an error
                        ThrowInvalidAssemblyName();
                    }
                    // Reached end of input and therefore of string
                    break;
                }

                if (quoteChar != 0 && c == quoteChar)
                    break;  // Terminate: Found closing quote of quoted string.

                if (quoteChar == 0 && (c == ',' || c == '='))
                {
                    _index--;
                    break;  // Terminate: Found start of a new ',' or '=' token.
                }

                if (quoteChar == 0 && (c == '\'' || c == '\"'))
                    ThrowInvalidAssemblyName();

                if (c == '\\')
                {
                    c = GetNextChar();

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
                            ThrowInvalidAssemblyName();
                            break; //unreachable
                    }
                }
                else
                {
                    sb.Append(c);
                }

                c = GetNextChar();
            }


            if (quoteChar == 0)
            {
                while (sb.Length > 0 && IsWhiteSpace(sb[sb.Length - 1]))
                    sb.Length--;
            }

            tokenString = sb.ToString();
            return Token.String;
        }

        [DoesNotReturn]
        private void ThrowInvalidAssemblyName()
            => throw new FileLoadException(SR.InvalidAssemblyName, _input.ToString());
    }
}
