// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Diagnostics.Contracts;
using System.Text;

namespace System.Net.Http
{
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Justification = "Represents a multipart/* content. Even if a collection of HttpContent is stored, " +
        "suffix Collection is not appropriate.")]
    public class MultipartContent : HttpContent, IEnumerable<HttpContent>
    {
        #region Fields

        private const string CrLf = "\r\n";

        private static readonly int s_crlfLength = GetEncodedLength(CrLf);
        private static readonly int s_dashDashLength = GetEncodedLength("--");
        private static readonly int s_colonSpaceLength = GetEncodedLength(": ");
        private static readonly int s_commaSpaceLength = GetEncodedLength(", ");

        private readonly List<HttpContent> _nestedContent;
        private readonly string _boundary;

        #endregion Fields

        #region Construction

        public MultipartContent()
            : this("mixed", GetDefaultBoundary())
        { }

        public MultipartContent(string subtype)
            : this(subtype, GetDefaultBoundary())
        { }

        public MultipartContent(string subtype, string boundary)
        {
            if (string.IsNullOrWhiteSpace(subtype))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, nameof(subtype));
            }
            Contract.EndContractBlock();
            ValidateBoundary(boundary);

            _boundary = boundary;

            string quotedBoundary = boundary;
            if (!quotedBoundary.StartsWith("\"", StringComparison.Ordinal))
            {
                quotedBoundary = "\"" + quotedBoundary + "\"";
            }

            MediaTypeHeaderValue contentType = new MediaTypeHeaderValue("multipart/" + subtype);
            contentType.Parameters.Add(new NameValueHeaderValue(nameof(boundary), quotedBoundary));
            Headers.ContentType = contentType;

            _nestedContent = new List<HttpContent>();
        }

        private static void ValidateBoundary(string boundary)
        {
            // NameValueHeaderValue is too restrictive for boundary.
            // Instead validate it ourselves and then quote it.
            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, nameof(boundary));
            }

            // RFC 2046 Section 5.1.1
            // boundary := 0*69<bchars> bcharsnospace
            // bchars := bcharsnospace / " "
            // bcharsnospace := DIGIT / ALPHA / "'" / "(" / ")" / "+" / "_" / "," / "-" / "." / "/" / ":" / "=" / "?"
            if (boundary.Length > 70)
            {
                throw new ArgumentOutOfRangeException(nameof(boundary), boundary,
                    string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_content_field_too_long, 70));
            }
            // Cannot end with space.
            if (boundary.EndsWith(" ", StringComparison.Ordinal))
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, boundary), nameof(boundary));
            }
            Contract.EndContractBlock();

            const string AllowedMarks = @"'()+_,-./:=? ";

            foreach (char ch in boundary)
            {
                if (('0' <= ch && ch <= '9') || // Digit.
                    ('a' <= ch && ch <= 'z') || // alpha.
                    ('A' <= ch && ch <= 'Z') || // ALPHA.
                    (AllowedMarks.IndexOf(ch) >= 0)) // Marks.
                {
                    // Valid.
                }
                else
                {
                    throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, boundary), nameof(boundary));
                }
            }
        }

        private static string GetDefaultBoundary()
        {
            return Guid.NewGuid().ToString();
        }

        public virtual void Add(HttpContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            Contract.EndContractBlock();

            _nestedContent.Add(content);
        }

        #endregion Construction

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (HttpContent content in _nestedContent)
                {
                    content.Dispose();
                }
                _nestedContent.Clear();
            }
            base.Dispose(disposing);
        }

        #endregion Dispose

        #region IEnumerable<HttpContent> Members

        public IEnumerator<HttpContent> GetEnumerator()
        {
            return _nestedContent.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            return _nestedContent.GetEnumerator();
        }

        #endregion

        #region Serialization

        // for-each content
        //   write "--" + boundary
        //   for-each content header
        //     write header: header-value
        //   write content.CopyTo[Async]
        // write "--" + boundary + "--"
        // Can't be canceled directly by the user.  If the overall request is canceled 
        // then the stream will be closed an exception thrown.
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            Debug.Assert(stream != null);
            try
            {
                // Write start boundary.
                await EncodeStringToStreamAsync(stream, "--" + _boundary + CrLf).ConfigureAwait(false);

                // Write each nested content.
                var output = new StringBuilder();
                for (int contentIndex = 0; contentIndex < _nestedContent.Count; contentIndex++)
                {
                    output.Clear();
                    HttpContent content = _nestedContent[contentIndex];

                    // Add divider.
                    if (contentIndex != 0) // Write divider for all but the first content.
                    {
                        output.Append(CrLf + "--"); // const strings
                        output.Append(_boundary);
                        output.Append(CrLf);
                    }

                    // Add headers.
                    foreach (KeyValuePair<string, IEnumerable<string>> headerPair in content.Headers)
                    {
                        output.Append(headerPair.Key);
                        output.Append(": ");
                        string delim = string.Empty;
                        foreach (string value in headerPair.Value)
                        {
                            output.Append(delim);
                            output.Append(value);
                            delim = ", ";
                        }
                        output.Append(CrLf);
                    }
                    output.Append(CrLf); // Extra CRLF to end headers (even if there are no headers).

                    // Write divider, headers, and content.
                    await EncodeStringToStreamAsync(stream, output.ToString()).ConfigureAwait(false);
                    await content.CopyToAsync(stream).ConfigureAwait(false);
                }

                // Write footer boundary.
                await EncodeStringToStreamAsync(stream, CrLf + "--" + _boundary + "--" + CrLf).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Exception(NetEventSource.ComponentType.Http, this, nameof(SerializeToStreamAsync), ex);
                }
                throw;
            }
        }

        private static Task EncodeStringToStreamAsync(Stream stream, string input)
        {
            byte[] buffer = HttpRuleParser.DefaultHttpEncoding.GetBytes(input);
            return stream.WriteAsync(buffer, 0, buffer.Length);
        }

        protected internal override bool TryComputeLength(out long length)
        {
            int boundaryLength = GetEncodedLength(_boundary);

            long currentLength = 0;
            long internalBoundaryLength = s_crlfLength + s_dashDashLength + boundaryLength + s_crlfLength;

            // Start Boundary.
            currentLength += s_dashDashLength + boundaryLength + s_crlfLength;

            bool first = true;
            foreach (HttpContent content in _nestedContent)
            {
                if (first)
                {
                    first = false; // First boundary already written.
                }
                else
                {
                    // Internal Boundary.
                    currentLength += internalBoundaryLength;
                }

                // Headers.
                foreach (KeyValuePair<string, IEnumerable<string>> headerPair in content.Headers)
                {
                    currentLength += GetEncodedLength(headerPair.Key) + s_colonSpaceLength;

                    int valueCount = 0;
                    foreach (string value in headerPair.Value)
                    {
                        currentLength += GetEncodedLength(value);
                        valueCount++;
                    }
                    if (valueCount > 1)
                    {
                        currentLength += (valueCount - 1) * s_commaSpaceLength;
                    }

                    currentLength += s_crlfLength;
                }

                currentLength += s_crlfLength;

                // Content.
                long tempContentLength = 0;
                if (!content.TryComputeLength(out tempContentLength))
                {
                    length = 0;
                    return false;
                }
                currentLength += tempContentLength;
            }

            // Terminating boundary.
            currentLength += s_crlfLength + s_dashDashLength + boundaryLength + s_dashDashLength + s_crlfLength;

            length = currentLength;
            return true;
        }

        private static int GetEncodedLength(string input)
        {
            return HttpRuleParser.DefaultHttpEncoding.GetByteCount(input);
        }
        #endregion Serialization
    }
}
