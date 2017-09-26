// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http
{
    internal partial class AuthenticationHelper
    {
        public const string Basic = "Basic";

        public static bool TrySetBasicAuthToken(HttpRequestMessage request, ICredentials credentials)
        {
            NetworkCredential credential = credentials.GetCredential(request.RequestUri, Basic);
            if (credential == null)
            {
                return false;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue(Basic, GetBasicTokenForCredential(credential));
            return true;
        }

        public static string GetBasicTokenForCredential(NetworkCredential credential)
        {
            if (credential.UserName.IndexOf(':') != -1)
            {
                // TODO #23135: What's the right way to handle this?
                throw new NotImplementedException($"Basic auth: can't handle ':' in username \"{credential.UserName}\"");
            }

            string userPass = credential.UserName + ":" + credential.Password;
            if (!string.IsNullOrEmpty(credential.Domain))
            {
                if (credential.Domain.IndexOf(':') != -1)
                {
                    // TODO #23135: What's the right way to handle this?
                    throw new NotImplementedException($"Basic auth: can't handle ':' in domain \"{credential.Domain}\"");
                }

                userPass = credential.Domain + "\\" + userPass;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(userPass));
        }
    }
}
