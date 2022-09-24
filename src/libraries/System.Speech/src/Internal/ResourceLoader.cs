// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace System.Speech.Internal
{
    internal class ResourceLoader
    {
        #region Internal Methods

        /// <summary>
        /// Load a file either from a local network or from the Internet.
        /// </summary>
        internal Stream LoadFile(Uri uri, out string mimeType, out Uri baseUri, out string localPath)
        {
            localPath = null;

            {
                Stream stream = null;

                // Check for a local file
                if (!uri.IsAbsoluteUri || uri.IsFile)
                {
                    // Local file
                    string file = uri.IsAbsoluteUri ? uri.LocalPath : uri.OriginalString;
                    try
                    {
                        stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                    catch
                    {
                        if (Directory.Exists(file))
                        {
                            throw new InvalidOperationException(SR.Get(SRID.CannotReadFromDirectory, file));
                        }
                        throw;
                    }
                    baseUri = null;
                }
                else
                {
                    try
                    {
                        // http:// Load the data from the web
                        stream = DownloadData(uri, out baseUri);
                    }
                    catch (WebException e)
                    {
                        throw new IOException(e.Message, e);
                    }
                }
                mimeType = null;
                return stream;
            }
        }

        /// <summary>
        /// Release a file from a cache if any
        /// </summary>
        internal void UnloadFile(string localPath)
        {
        }

        internal Stream LoadFile(Uri uri, out string localPath, out Uri redirectedUri)
        {
            string mediaTypeUnused;
            return LoadFile(uri, out mediaTypeUnused, out redirectedUri, out localPath);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Download data from the web.
        /// Set the redirectUri as the location of the file could be redirected in ASP pages.
        /// </summary>
        private static Stream DownloadData(Uri uri, out Uri redirectedUri)
        {
            // Create an HttpClient that uses default credentials
            using (HttpClient client = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true }))
            {
                // Get the socket to close after the conenction
                client.DefaultRequestHeaders.ConnectionClose = true;
                // Get the message
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, uri);

                // Get the response
#if NET5_0_OR_GREATER
                HttpResponseMessage response = client.Send(message);
#else
                HttpResponseMessage response = Task.Run(() => client.SendAsync(message)).Result;
#endif

                // Ensure we got a success status code
                response.EnsureSuccessStatusCode();

                // Set redirectedUri
                redirectedUri = response.RequestMessage.RequestUri;

                // Get the stream containing content returned by the server.
                return response.Content.ReadAsStream();
            }
        }

        #endregion

        #region Private Fields

        #endregion
    }
}
