// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class MultipartFormDataContent : MultipartContent
    {
        private const string formData = "form-data";

        public MultipartFormDataContent()
            : base(formData)
        {
        }

        public MultipartFormDataContent(string boundary)
            : base(formData, boundary)
        {
        }

        public override void Add(HttpContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            content.Headers.ContentDisposition ??= new ContentDispositionHeaderValue(formData);

            base.Add(content);
        }

        public void Add(HttpContent content, string name)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            AddInternal(content, name, null);
        }

        public void Add(HttpContent content, string name, string fileName)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            AddInternal(content, name, fileName);
        }

        private void AddInternal(HttpContent content, string name, string? fileName)
        {
            if (content.Headers.ContentDisposition == null)
            {
                ContentDispositionHeaderValue header = new ContentDispositionHeaderValue(formData);
                header.Name = name;
                header.FileName = fileName;
                header.FileNameStar = fileName;

                content.Headers.ContentDisposition = header;
            }
            base.Add(content);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            // Only skip the original protected virtual SerializeToStreamAsync if this
            // isn't a derived type that may have overridden the behavior.
            GetType() == typeof(MultipartFormDataContent) ? SerializeToStreamAsyncCore(stream, context, cancellationToken) :
            base.SerializeToStreamAsync(stream, context, cancellationToken);
    }
}
