// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;

using Newtonsoft.Json;

namespace WebServer
{
    public class Test : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            RequestInformation info = RequestInformation.Create(context.Request);

            string echoJson = info.SerializeToJson();

            // Compute MD5 hash to clients can verify the received data.
            MD5 md5 = MD5.Create();
            byte[] bytes = Encoding.ASCII.GetBytes(echoJson);
            var hash = md5.ComputeHash(bytes);
            string encodedHash = Convert.ToBase64String(hash);
            context.Response.Headers.Add("Content-MD5", encodedHash);

            RequestInformation newEcho = RequestInformation.DeSerializeFromJson(echoJson);
            context.Response.ContentType = "text/plain"; //"application/json";
            context.Response.Write(echoJson);
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
