// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;

namespace Internal.Runtime.Binder
{
    internal static class TextualIdentityParser
    {
        public static string ToString(AssemblyIdentity pAssemblyIdentity, AssemblyIdentityFlags includeFlags)
        {
            if (string.IsNullOrEmpty(pAssemblyIdentity.SimpleName))
            {
                return string.Empty;
            }

            ValueStringBuilder textualIdentity = new ValueStringBuilder(256);

            AppendStringEscaped(ref textualIdentity, pAssemblyIdentity.SimpleName);

            if ((includeFlags & AssemblyIdentityFlags.IDENTITY_FLAG_VERSION) != 0)
            {
                AssemblyVersion version = pAssemblyIdentity.Version;
                textualIdentity.Append(", Version=");
                textualIdentity.AppendSpanFormattable(version.Major);
                textualIdentity.Append('.');
                textualIdentity.AppendSpanFormattable(version.Minor);
                textualIdentity.Append('.');
                textualIdentity.AppendSpanFormattable(version.Build);
                textualIdentity.Append('.');
                textualIdentity.AppendSpanFormattable(version.Revision);
            }

            if ((includeFlags & AssemblyIdentityFlags.IDENTITY_FLAG_CULTURE) != 0)
            {
                textualIdentity.Append(", Culture=");
                AppendStringEscaped(ref textualIdentity, pAssemblyIdentity.NormalizedCulture);
            }

            if ((includeFlags & AssemblyIdentityFlags.IDENTITY_FLAG_PUBLIC_KEY) != 0)
            {
                textualIdentity.Append(", PublicKey=");
                AppendBinary(ref textualIdentity, pAssemblyIdentity.PublicKeyOrTokenBLOB);
            }
            else if ((includeFlags & AssemblyIdentityFlags.IDENTITY_FLAG_PUBLIC_KEY_TOKEN) != 0)
            {
                textualIdentity.Append(", PublicKeyToken=");
                AppendBinary(ref textualIdentity, pAssemblyIdentity.PublicKeyOrTokenBLOB);
            }
            else if ((includeFlags & AssemblyIdentityFlags.IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL) != 0)
            {
                textualIdentity.Append(", PublicKeyToken=null");
            }

            if ((includeFlags & AssemblyIdentityFlags.IDENTITY_FLAG_PROCESSOR_ARCHITECTURE) != 0)
            {
                textualIdentity.Append(", processorArchitecture=");
                textualIdentity.Append(pAssemblyIdentity.ProcessorArchitecture switch
                {
                    PEKind.I386 => "x86",
                    PEKind.IA64 => "IA64",
                    PEKind.AMD64 => "AMD64",
                    PEKind.ARM => "ARM",
                    PEKind.MSIL => "MSIL",
                    _ => throw new UnreachableException()
                });
            }

            if ((includeFlags & AssemblyIdentityFlags.IDENTITY_FLAG_RETARGETABLE) != 0)
            {
                textualIdentity.Append(", Retargetable=Yes");
            }

            if ((includeFlags & AssemblyIdentityFlags.IDENTITY_FLAG_CONTENT_TYPE) != 0)
            {
                textualIdentity.Append($", ContentType={nameof(System.Reflection.AssemblyContentType.WindowsRuntime)}");
            }

            return textualIdentity.ToString();
        }

        private static void AppendStringEscaped(ref ValueStringBuilder vsb, string input)
        {
            Debug.Assert(input.Length > 0);

            bool fNeedQuotes = (input[0] is '\n' or '\r' or ' ' or '\t')
                || (input[^1] is '\n' or '\r' or ' ' or '\t');
            char quoteCharacter = '\"';

            ValueStringBuilder tmpString = new ValueStringBuilder(stackalloc char[256]);

            // Fusion textual identity compat: escape all non-quote characters even if quoted
            foreach (char ch in input)
            {
                switch (ch)
                {
                    case '\"':
                    case '\'':
                        if (fNeedQuotes && (quoteCharacter != ch))
                        {
                            tmpString.Append(ch);
                        }
                        else if (!fNeedQuotes)
                        {
                            fNeedQuotes = true;
                            quoteCharacter = (ch == '\"') ? '\'' : '\"';
                            tmpString.Append(ch);
                        }
                        else
                        {
                            tmpString.Append('\\');
                            tmpString.Append(ch);
                        }
                        break;

                    case '=':
                    case ',':
                    case '\\':
                        tmpString.Append('\\');
                        tmpString.Append(ch);
                        break;

                    case (char)9:
                        tmpString.Append("\\t");
                        break;

                    case (char)10:
                        tmpString.Append("\\n");
                        break;

                    case (char)13:
                        tmpString.Append("\\r");
                        break;

                    default:
                        tmpString.Append(ch);
                        break;
                }
            }

            if (fNeedQuotes)
            {
                vsb.Append(quoteCharacter);
                vsb.Append(tmpString.AsSpan());
                vsb.Append(quoteCharacter);
            }
            else
            {
                vsb.Append(tmpString.AsSpan());
            }

            tmpString.Dispose();
        }

        private static void AppendBinary(ref ValueStringBuilder vsb, ReadOnlySpan<byte> data)
        {
            vsb.EnsureCapacity(vsb.Length + data.Length * 2);
            HexConverter.EncodeToUtf16(data, vsb.RawChars[vsb.Length..], HexConverter.Casing.Lower);
        }
    }
}
