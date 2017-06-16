// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Net.Http.Managed
{
    internal static class BasicAuthenticationHelper
    {
        public static string GetBasicTokenForCredential(NetworkCredential credential)
        {
            if (credential.UserName.IndexOf(':') != -1)
            {
                // TODO: What's the right way to handle this?
                //              throw new NotImplementedException($"Basic auth: can't handle ':' in username \"{credential.UserName}\"");
            }

            string userPass = credential.UserName + ":" + credential.Password;
            if (!string.IsNullOrEmpty(credential.Domain))
            {
                if (credential.UserName.IndexOf(':') != -1)
                {
                    // TODO: What's the right way to handle this?
                    //                  throw new NotImplementedException($"Basic auth: can't handle ':' in domain \"{credential.Domain}\"");
                }

                userPass = credential.Domain + "\\" + userPass;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(userPass));
        }
    }
}
