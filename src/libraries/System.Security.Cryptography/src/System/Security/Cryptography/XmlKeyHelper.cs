// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace System.Security.Cryptography
{
    internal static class XmlKeyHelper
    {
        internal static ParseState ParseDocument(string xmlString)
        {
            ArgumentNullException.ThrowIfNull(xmlString);

            try
            {
                return ParseState.ParseDocument(xmlString);
            }
            catch (Exception e)
            {
                throw new CryptographicException(SR.Cryptography_FromXmlParseError, e);
            }
        }

        internal static bool HasElement(ref ParseState state, string name)
        {
            return state.HasElement(name);
        }

        internal static byte[]? ReadCryptoBinary(ref ParseState state, string name, int sizeHint = -1)
        {
            string? value = state.GetValue(name);

            if (value == null)
            {
                return null;
            }

            if (value.Length == 0)
            {
                return Array.Empty<byte>();
            }

            if (sizeHint < 0)
            {
                return Convert.FromBase64String(value);
            }

            byte[] ret = new byte[sizeHint];

            if (Convert.TryFromBase64Chars(value.AsSpan(), ret, out int written))
            {
                if (written == sizeHint)
                {
                    return ret;
                }

                int shift = sizeHint - written;
                Buffer.BlockCopy(ret, 0, ret, shift, written);
                ret.AsSpan(0, shift).Clear();
                return ret;
            }

            // It didn't fit, so let FromBase64String figure out how big it should be.
            // This is almost certainly going to result in throwing from ImportParameters,
            // but that's where the exception belongs.
            //
            // Alternatively, this is where we get the exception that the base64 value was
            // corrupt.
            return Convert.FromBase64String(value);
        }

        internal static int ReadCryptoBinaryInt32(byte[] buf)
        {
            Debug.Assert(buf != null);
            int val = 0;
            int idx = Math.Max(0, buf.Length - sizeof(int));

            // This is like BinaryPrimitives.ReadBigEndianInt32, except it works
            // on trimmed inputs and skips to the end.
            //
            // This is compatible with what .NET Framework does (Utils.ConvertByteArrayToInt)
            for (; idx < buf.Length; idx++)
            {
                val <<= 8;
                val |= buf[idx];
            }

            return val;
        }

        internal static void WriteCryptoBinary(string name, int value, StringBuilder builder)
        {
            // .NET Framework compat
            if (value == 0)
            {
                byte single = 0;
                WriteCryptoBinary(name, new ReadOnlySpan<byte>(in single), builder);
                return;
            }

            Span<byte> valBuf = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(valBuf, value);

            // .NET Framework does write the counter value as CryptoBinary, so do the leading-byte trim here.

            int start = 0;

            // Guaranteed not to go out of bounds by the == 0 check above.
            while (valBuf[start] == 0)
            {
                start++;
            }

            WriteCryptoBinary(name, valBuf.Slice(start), builder);
        }

        internal static void WriteCryptoBinary(string name, ReadOnlySpan<byte> value, StringBuilder builder)
        {
            Debug.Assert(name.Length > 0);
            Debug.Assert(value.Length > 0);
            Debug.Assert(builder != null);

            builder.Append('<');
            builder.Append(name);
            builder.Append('>');

            int offset = 0;
            int length = value.Length;

            // If we wanted to produce a ds:CryptoBinary instead of an xml:base64Binary,
            // we'd skip all leading zeroes (increase offset, decrease length) before moving on

            const int StackChars = 256;
            const int ByteLimit = StackChars / 4 * 3;
            Span<char> base64 = stackalloc char[StackChars];

            while (length > 0)
            {
                int localLength = Math.Min(ByteLimit, length);

                if (!Convert.TryToBase64Chars(value.Slice(offset, localLength), base64, out int written))
                {
                    Debug.Fail($"Convert.TryToBase64Chars failed with {localLength} bytes to {StackChars} chars");
                    throw new CryptographicException();
                }

                builder.Append(base64.Slice(0, written));
                length -= localLength;
                offset += localLength;
            }

            builder.Append('<');
            builder.Append('/');
            builder.Append(name);
            builder.Append('>');
        }

        internal struct ParseState
        {
            private IEnumerable? _enumerable;
            private IEnumerator? _enumerator;
            private int _index;

            internal static ParseState ParseDocument(string xmlString)
            {
                object? rootElement = Functions.ParseDocument(xmlString);

                return new ParseState
                {
                    _enumerable = Functions.GetElements(rootElement),
                    _enumerator = null,
                    _index = -1,
                };
            }

            internal bool HasElement(string localName)
            {
                string? value = GetValue(localName);

                bool ret = value != null;

                if (ret)
                {
                    // Make it so that if GetValue is called on
                    // this name it'll advance into it correctly.
                    _index--;
                }

                return ret;
            }

            internal string? GetValue(string localName)
            {
                if (_enumerable == null)
                {
                    return null;
                }

                _enumerator ??= _enumerable.GetEnumerator();

                int origIdx = _index;
                int idx = origIdx;

                if (!_enumerator.MoveNext())
                {
                    idx = -1;
                    _enumerator = _enumerable.GetEnumerator();

                    if (!_enumerator.MoveNext())
                    {
                        _enumerable = null;
                        return null;
                    }
                }

                idx++;

                while (idx != origIdx)
                {
                    string? curName = Functions.GetLocalName(_enumerator.Current);

                    if (localName == curName)
                    {
                        _index = idx;
                        return Functions.GetValue(_enumerator.Current);
                    }

                    if (!_enumerator.MoveNext())
                    {
                        idx = -1;

                        if (origIdx < 0)
                        {
                            _enumerator = null;
                            return null;
                        }

                        _enumerator = _enumerable.GetEnumerator();

                        if (!_enumerator.MoveNext())
                        {
                            Debug.Fail("Original enumerator had elements, new one does not");
                            _enumerable = null;
                            return null;
                        }
                    }

                    idx++;
                }

                return null;
            }

            private static class Functions
            {
                private const string XmlLinqAssemblyString = ", System.Private.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";

                private static readonly Func<string, object> s_xDocumentCreate;
                private static readonly PropertyInfo s_docRootProperty;
                private static readonly MethodInfo s_getElementsMethod;
                private static readonly PropertyInfo s_elementNameProperty;
                private static readonly PropertyInfo s_elementValueProperty;
                private static readonly PropertyInfo s_nameNameProperty;

#pragma warning disable CA1810 // explicit static cctor
                static Functions()
                {
                    Type xDocument = Type.GetType("System.Xml.Linq.XDocument" + XmlLinqAssemblyString)!;
                    s_xDocumentCreate = xDocument.GetMethod(
                        "Parse",
                        BindingFlags.Static | BindingFlags.Public,
                        new[] { typeof(string) })!
                            .CreateDelegate<Func<string, object>>();

                    s_docRootProperty = xDocument.GetProperty("Root")!;

                    Type xElement = Type.GetType("System.Xml.Linq.XElement" + XmlLinqAssemblyString)!;
                    s_getElementsMethod = xElement.GetMethod(
                        "Elements",
                        BindingFlags.Instance | BindingFlags.Public,
                        Type.EmptyTypes)!;

                    s_elementNameProperty = xElement.GetProperty("Name")!;
                    s_elementValueProperty = xElement.GetProperty("Value")!;

                    Type xName= Type.GetType("System.Xml.Linq.XName" + XmlLinqAssemblyString)!;
                    s_nameNameProperty = xName.GetProperty("LocalName")!;
                }
#pragma warning restore CA1810

                internal static object? ParseDocument(string xmlString) =>
                    s_docRootProperty.GetValue(s_xDocumentCreate(xmlString));

                internal static IEnumerable? GetElements(object? element) =>
                    (IEnumerable?)s_getElementsMethod.Invoke(element, Array.Empty<object>());

                internal static string? GetLocalName(object? element) =>
                    (string?)s_nameNameProperty.GetValue(s_elementNameProperty.GetValue(element));

                internal static string? GetValue(object? element) =>
                    (string?)s_elementValueProperty.GetValue(element);
            }
        }
    }
}
