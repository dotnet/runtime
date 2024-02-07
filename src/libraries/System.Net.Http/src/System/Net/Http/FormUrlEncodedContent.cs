// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class FormUrlEncodedContent : ByteArrayContent
    {
        public FormUrlEncodedContent(
            IEnumerable<KeyValuePair<
                #nullable disable
                string, string
                #nullable restore
            >> nameValueCollection)
            : base(GetContentByteArray(nameValueCollection))
        {
            Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        }

        private static byte[] GetContentByteArray(IEnumerable<KeyValuePair<string?, string?>> nameValueCollection)
        {
            ArgumentNullException.ThrowIfNull(nameValueCollection);

            // Encode and concatenate data
            var builder = new ValueStringBuilder(stackalloc char[256]);

            foreach (KeyValuePair<string?, string?> pair in nameValueCollection)
            {
                if (builder.Length > 0)
                {
                    builder.Append('&');
                }

                Encode(ref builder, pair.Key);
                builder.Append('=');
                Encode(ref builder, pair.Value);
            }

            // We know the encoded length because the input is all ASCII.
            byte[] bytes = new byte[builder.Length];
            HttpRuleParser.DefaultHttpEncoding.GetBytes(builder.AsSpan(), bytes);
            builder.Dispose();
            return bytes;
        }

        private static void Encode(ref ValueStringBuilder builder, string? data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                int charsWritten;
                while (!Uri.TryEscapeDataString(data, builder.RawChars.Slice(builder.Length), out charsWritten))
                {
                    builder.EnsureCapacity(builder.Capacity + 1);
                }

                // Escape spaces as '+'.
                if (data.Contains(' '))
                {
                    ReadOnlySpan<char> escapedChars = builder.RawChars.Slice(builder.Length, charsWritten);

                    while (true)
                    {
                        int indexOfEscapedSpace = escapedChars.IndexOf("%20", StringComparison.Ordinal);
                        if (indexOfEscapedSpace < 0)
                        {
                            builder.Append(escapedChars);
                            break;
                        }

                        builder.Append(escapedChars.Slice(0, indexOfEscapedSpace));
                        builder.Append('+');
                        escapedChars = escapedChars.Slice(indexOfEscapedSpace + 3); // Skip "%20"
                    }
                }
                else
                {
                    builder.Length += charsWritten;
                }
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            // Only skip the original protected virtual SerializeToStreamAsync if this
            // isn't a derived type that may have overridden the behavior.
            GetType() == typeof(FormUrlEncodedContent) ? SerializeToStreamAsyncCore(stream, cancellationToken) :
            base.SerializeToStreamAsync(stream, context, cancellationToken);

        internal override Stream? TryCreateContentReadStream() =>
            GetType() == typeof(FormUrlEncodedContent) ? CreateMemoryStreamForByteArray() : // type check ensures we use possible derived type's CreateContentReadStreamAsync override
            null;
    }
}
