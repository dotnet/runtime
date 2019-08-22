// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.HPack;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace System.Net.Http.Unit.Tests.HPack
{
    public class HPackRoundtripTests
    {

        public static IEnumerable<object[]> TestHeaders()
        {
            yield return new object[] { new HttpRequestHeaders() { { "header", "value" } } };
            yield return new object[] { new HttpRequestHeaders() { { "header", new[] { "value1", "value2" } } } };
            yield return new object[] { new HttpRequestHeaders()
            {
                { "header-0", new[] { "value1", "value2" } },
                { "header-0", "value3" },
                { "header-1", "value1" },
                { "header-2", new[] { "value1", "value2" } },
            } };
        }

        [Theory, MemberData(nameof(TestHeaders))]
        public void HPack_HeaderEncodeDecodeRoundtrip_ShouldMatchOriginalInput(HttpHeaders headers)
        {
            Memory<byte> encoding = HPackEncode(headers);
            HttpHeaders decodedHeaders = HPackDecode(encoding);

            // Assert: decoded headers are structurally equal to original headers
            Assert.Equal(headers.Count(), decodedHeaders.Count());
            Assert.All(headers.Zip(decodedHeaders), pair =>
            {
                Assert.Equal(pair.First.Key, pair.Second.Key);
                Assert.Equal(pair.First.Value, pair.Second.Value);
            });
        }

        // adapted from Header serialization code in Http2Connection.cs
        private static Memory<byte> HPackEncode(HttpHeaders headers)
        {
            var buffer = new ArrayBuffer(4);
            FillAvailableSpaceWithOnes(buffer);

            foreach (KeyValuePair<HeaderDescriptor, string[]> header in headers.GetHeaderDescriptorsAndValues())
            {
                KnownHeader knownHeader = header.Key.KnownHeader;
                if (knownHeader != null)
                {
                    // For all other known headers, send them via their pre-encoded name and the associated value.
                    WriteBytes(knownHeader.Http2EncodedName);
                    string separator = null;
                    if (header.Value.Length > 1)
                    {
                        HttpHeaderParser parser = header.Key.Parser;
                        if (parser != null && parser.SupportsMultipleValues)
                        {
                            separator = parser.Separator;
                        }
                        else
                        {
                            separator = HttpHeaderParser.DefaultSeparator;
                        }
                    }

                    WriteLiteralHeaderValues(header.Value, separator);
                }
                else
                {
                    // The header is not known: fall back to just encoding the header name and value(s).
                    WriteLiteralHeader(header.Key.Name, header.Value);
                }
            }

            return buffer.ActiveMemory;

            void WriteBytes(ReadOnlySpan<byte> bytes)
            {
                if (bytes.Length > buffer.AvailableLength)
                {
                    buffer.EnsureAvailableSpace(bytes.Length);
                    FillAvailableSpaceWithOnes(buffer);
                }

                bytes.CopyTo(buffer.AvailableSpan);
                buffer.Commit(bytes.Length);
            }

            void WriteLiteralHeaderValues(string[] values, string separator)
            {
                int bytesWritten;
                while (!HPackEncoder.EncodeStringLiterals(values, separator, buffer.AvailableSpan, out bytesWritten))
                {
                    buffer.EnsureAvailableSpace(buffer.AvailableLength + 1);
                    FillAvailableSpaceWithOnes(buffer);
                }

                buffer.Commit(bytesWritten);
            }

            void WriteLiteralHeader(string name, string[] values)
            {
                int bytesWritten;
                while (!HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, values, HttpHeaderParser.DefaultSeparator, buffer.AvailableSpan, out bytesWritten))
                {
                    buffer.EnsureAvailableSpace(buffer.AvailableLength + 1);
                    FillAvailableSpaceWithOnes(buffer);
                }

                buffer.Commit(bytesWritten);
            }

            // force issues related to buffer not being zeroed out
            void FillAvailableSpaceWithOnes(ArrayBuffer buffer) => buffer.AvailableSpan.Fill(0xff);
        }

        // adapted from header deserialization code in Http2Connection.cs
        private static HttpHeaders HPackDecode(Memory<byte> memory)
        {
            var header = new HttpRequestHeaders();
            var hpackDecoder = new HPackDecoder(maxDynamicTableSize: 0, maxResponseHeadersLength: HttpHandlerDefaults.DefaultMaxResponseHeadersLength * 1024);

            hpackDecoder.Decode(memory.Span, true, ((_, name, value) => HeaderHandler(name, value)), null);

            return header;

            void HeaderHandler(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
            {
                if (!HeaderDescriptor.TryGet(name, out HeaderDescriptor descriptor))
                {
                    throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(name)));
                }

                string headerValue = descriptor.GetHeaderValue(value);

                header.TryAddWithoutValidation(descriptor, headerValue.Split(',').Select(x => x.Trim()));
            }
        }
    }
}
