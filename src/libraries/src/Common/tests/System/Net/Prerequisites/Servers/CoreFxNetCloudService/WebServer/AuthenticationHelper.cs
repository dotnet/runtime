// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace WebServer
{
    public static class AuthenticationHelper
    {
        public static bool HandleAuthentication(HttpContext context)
        {
            string authType = context.Request.QueryString["auth"];
            string user = context.Request.QueryString["user"];
            string password = context.Request.QueryString["password"];
            string domain = context.Request.QueryString["domain"];

            if (string.Equals("basic", authType, StringComparison.OrdinalIgnoreCase))
            {
                if (!HandleBasicAuthentication(context, user, password, domain))
                {
                    context.Response.End();
                    return false;
                }
            }
            else if (string.Equals("Negotiate", authType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("NTLM", authType, StringComparison.OrdinalIgnoreCase))
            {
                if (!HandleChallengeResponseAuthentication(context, authType, user, password, domain))
                {
                    context.Response.End();
                    return false;
                }
            }
            else if (authType != null)
            {
                context.Response.StatusCode = 501;
                context.Response.StatusDescription = "Unsupported auth type: " + authType;
                context.Response.End();
                return false;
            }

            return true;
        }

        private static bool HandleBasicAuthentication(HttpContext context, string user, string password, string domain)
        {
            const string WwwAuthenticateHeaderValue = "Basic realm=\"corefx-networking\"";

            string authHeader = context.Request.Headers["Authorization"];
            if (authHeader == null)
            {
                context.Response.StatusCode = 401;
                context.Response.Headers.Add("WWW-Authenticate", WwwAuthenticateHeaderValue);
                return false;
            }

            string[] split = authHeader.Split(new char[] { ' ' });
            if (split.Length < 2)
            {
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Invalid Authorization header: " + authHeader;
                return false;
            }

            if (!string.Equals("basic", split[0], StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Unsupported auth type: " + split[0];
                return false;
            }

            // Decode base64 username:password.
            byte[] bytes = Convert.FromBase64String(split[1]);
            string credential = Encoding.ASCII.GetString(bytes);
            string[] pair = credential.Split(new char[] { ':' });

            // Prefix "domain\" to username if domain is specified.
            if (domain != null)
            {
                user = domain + "\\" + user;
            }

            if (pair.Length != 2 || pair[0] != user || pair[1] != password)
            {
                context.Response.StatusCode = 401;
                context.Response.Headers.Add("WWW-Authenticate", WwwAuthenticateHeaderValue);
                return false;
            }

            // Success.
            return true;
        }
        private static bool HandleChallengeResponseAuthentication(
            HttpContext context,
            string authType,
            string user,
            string password,
            string domain)
        {
            string authHeader = context.Request.Headers["Authorization"];
            if (authHeader == null)
            {
                context.Response.StatusCode = 401;
                context.Response.Headers.Add("WWW-Authenticate", authType);
                return false;
            }

            // We don't fully support this authentication method.
            context.Response.StatusCode = 501;
            context.Response.StatusDescription = string.Format(
                "Attempt to use unsupported challenge/response auth type. {0}: {1}",
                authType,
                authHeader);
            return false;
        }
    }
}
