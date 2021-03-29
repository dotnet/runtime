// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace System.Xml
{
    internal sealed partial class XmlDownloadManager
    {
        internal Task<Stream> GetStreamAsync(Uri uri, ICredentials? credentials, IWebProxy? proxy)
        {
            if (uri.Scheme == "file")
            {
                Uri fileUri = uri;
                return Task.Run<Stream>(() => new FileStream(fileUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1, useAsync: true));
            }
            else
            {
                return GetNonFileStreamAsync(uri, credentials, proxy);
            }
        }

        private async Task<Stream> GetNonFileStreamAsync(Uri uri, ICredentials? credentials, IWebProxy? proxy)
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
            {
#pragma warning disable CA1416 // Validate platform compatibility, 'credentials' and 'proxy' will not be set for browser, so safe to suppress
                if (credentials != null)
                {
                    handler.Credentials = credentials;
                }
                if (proxy != null)
                {
                    handler.Proxy = proxy;
                }
#pragma warning restore CA1416

                using (Stream respStream = await client.GetStreamAsync(uri).ConfigureAwait(false))
                {
                    var result = new MemoryStream();
                    respStream.CopyTo(result);
                    result.Position = 0;
                    return result;
                }
            }
        }
    }
}
