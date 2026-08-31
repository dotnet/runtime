// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Threading.Tasks;

namespace CookieExtensionsTest
{
    /// <summary>
    /// Tests that the System.Net.CookieExtensions.Clone()
    /// method works as expected when used in a trimmed application.
    /// </summary>
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var helper = new TestHelper();
            HttpListenerResponse response = await helper.GetResponse();
            var cookie = new Cookie("name", "value");
            response.SetCookie(cookie);

            // Cookies are cloned.
            cookie.Value = "value3";
            if (response.Cookies[0].Value != "value")
            {
                return -1;
            }

            return 100;
        }
    }
}
