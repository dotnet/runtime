// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net;

using System.Speech.Synthesis;


namespace System.Speech.Internal
{

    //*******************************************************************
    //
    // Public Types
    //
    //*******************************************************************

    internal class ResourceLoader
    {
        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        /// <summary>
        /// Load a file either from a local network or from the Internet.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="mimeType"></param>
        /// <param name="baseUri"></param>
        /// <param name="localPath"></param>
        internal Stream LoadFile (Uri uri, out string mimeType, out Uri baseUri, out string localPath)
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
                        stream = new FileStream (file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                    catch
                    {
                        if (Directory.Exists (file))
                        {
                            throw new InvalidOperationException (SR.Get (SRID.CannotReadFromDirectory, file));
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
                        stream = DownloadData (uri, out baseUri);
                    }
                    catch (WebException e)
                    {
                        throw new IOException (e.Message, e);
                    }
                }
                mimeType = null;
                return stream;
            }
        }

        /// <summary>
        /// Release a file from a cache if any
        /// </summary>
        /// <param name="localPath"></param>
        internal void UnloadFile (string localPath)
        {
        }

        internal Stream LoadFile (Uri uri, out string localPath, out Uri redirectedUri)
        {
            string mediaTypeUnused;
            return LoadFile (uri, out mediaTypeUnused, out redirectedUri, out localPath);
        }


        #endregion

        //*******************************************************************
        //
        // Private Methods
        //
        //*******************************************************************

        #region Private Methods

        /// <summary>
        /// Dowload data from the web. 
        /// Set the redirectUri as the location of the file could be redirected in ASP pages.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="redirectedUri"></param>
        /// <returns></returns>
        private static Stream DownloadData (Uri uri, out Uri redirectedUri)
        {
            // Create a request for the URL. 
            WebRequest request = WebRequest.Create (uri);

            // If required by the server, set the credentials. 
            request.Credentials = CredentialCache.DefaultCredentials;

            // Get the response. 
            using (HttpWebResponse response = (HttpWebResponse) request.GetResponse ())
            {
                // Get the stream containing content returned by the server. 
                using (Stream dataStream = response.GetResponseStream ())
                {
                    redirectedUri = response.ResponseUri;

                    // http:// Load the data from the web
                    using (WebClient client = new WebClient ())
                    {
                        client.UseDefaultCredentials = true;
                        return new MemoryStream (client.DownloadData (redirectedUri));
                    }
                }
            }
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields


        #endregion
    }
}
