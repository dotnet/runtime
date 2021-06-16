// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class AuthenticationHelper
    {
        public static bool HandleAuthentication(HttpContext context)
        {
            string authType = context.Request.Query["auth"];
            string user = context.Request.Query["user"];
            string password = context.Request.Query["password"];
            string domain = context.Request.Query["domain"];

            if (string.Equals("basic", authType, StringComparison.OrdinalIgnoreCase))
            {
                if (!HandleBasicAuthentication(context, user, password, domain))
                {
                    return false;
                }
            }
            else if (string.Equals("Negotiate", authType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("NTLM", authType, StringComparison.OrdinalIgnoreCase))
            {
                if (!HandleChallengeResponseAuthentication(context, authType, user, password, domain))
                {
                    return false;
                }
            }
            else if (authType != null)
            {
                context.Response.StatusCode = 501;
                context.Response.SetStatusDescription($"Unsupported auth type: {authType}");
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

            string[] split = authHeader.Split(new Char[] { ' ' });
            if (split.Length < 2)
            {
                context.Response.StatusCode = 500;
                context.Response.SetStatusDescription($"Invalid Authorization header: {authHeader}");
                return false;
            }

            if (!string.Equals("basic", split[0], StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 500;
                context.Response.SetStatusDescription($"Unsupported auth type: {split[0]}");
                return false;
            }

            // Decode base64 username:password.
            byte[] bytes = Convert.FromBase64String(split[1]);
            string credential = Encoding.ASCII.GetString(bytes);
            string[] pair = credential.Split(new Char[] { ':' });

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
            context.Response.SetStatusDescription(
                    $"Attempt to use unsupported challenge/response auth type. {authType}: {authHeader}");

            return false;
        }
    }
}
