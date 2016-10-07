// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;

namespace System.Net
{
    public class HttpListenerBasicIdentity : GenericIdentity
    {
        private string m_Password;

        public HttpListenerBasicIdentity(string username, string password) :
            base(username, "Basic")
        {
            m_Password = password;
        }

        public virtual string Password
        {
            get
            {
                return m_Password;
            }
        }
    }
}
