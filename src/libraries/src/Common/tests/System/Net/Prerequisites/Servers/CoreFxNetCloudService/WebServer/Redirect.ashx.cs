// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Web;

namespace WebServer
{
    /// <summary>
    /// Summary description for Redirect
    /// </summary>
    public class Redirect : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            int statusCode = 302;
            string statusCodeString = context.Request.QueryString["statuscode"];
            if (!string.IsNullOrEmpty(statusCodeString))
            {
                try
                {
                    statusCode = int.Parse(statusCodeString);
                    if (statusCode < 300 || statusCode > 307)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.StatusDescription = "Invalid redirect statuscode: " + statusCodeString;
                        return;
                    }
                }
                catch (Exception)
                {
                    context.Response.StatusCode = 500;
                    context.Response.StatusDescription = "Error parsing statuscode: " + statusCodeString;
                    return;
                }
            }

            string redirectUri = context.Request.QueryString["uri"];
            if (string.IsNullOrEmpty(redirectUri))
            {
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Missing redirection uri";
                return;
            }

            string hopsString = context.Request.QueryString["hops"];
            int hops = 1;
            if (!string.IsNullOrEmpty(hopsString))
            {
                try
                {
                    hops = int.Parse(hopsString);
                }
                catch (Exception)
                {
                    context.Response.StatusCode = 500;
                    context.Response.StatusDescription = "Error parsing hops: " + hopsString;
                    return;
                }
            }

            RequestHelper.AddResponseCookies(context);

            if (hops <= 1)
            {
                context.Response.Headers.Add("Location", redirectUri);
            }
            else
            {
                context.Response.Headers.Add(
                    "Location",
                    string.Format("/Redirect.ashx?uri={0}&hops={1}",
                    redirectUri,
                    hops - 1));
            }

            context.Response.StatusCode = statusCode;
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}
