// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Web;

namespace WebServer
{
    /// <summary>
    /// Summary description for Gzip
    /// </summary>
    public class GZip : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            string responseBody = "Sending GZIP compressed";

            context.Response.Headers.Add("Content-MD5", Convert.ToBase64String(ContentHelper.ComputeMD5Hash(responseBody)));
            context.Response.Headers.Add("Content-Encoding", "gzip");

            context.Response.ContentType = "text/plain";

            byte[] bytes = ContentHelper.GetGZipBytes(responseBody);
            context.Response.BinaryWrite(bytes);
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}