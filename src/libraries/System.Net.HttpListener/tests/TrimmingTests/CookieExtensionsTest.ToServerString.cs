// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Threading.Tasks;

namespace CookieExtensionsTest
{
    /// <summary>
    /// Tests that the System.Net.CookieExtensions.ToServerString()
    /// method works as expected when used in a trimmed application.
    /// </summary>
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var helper = new TestHelper();

            CookieCollection cookies = new CookieCollection
            {
                new Cookie("name1", "value1"),
                new Cookie("name2", "value2") { Port = "\"300\"" }
            };

            int expectedBytes = 196;
            string expectedSetCookie = "Set-Cookie: name1=value1";
            string expectedSetCookie2 = "Set-Cookie2: name2=value2; Port=\"300\"; Version=1";
    
            HttpListenerResponse response = await helper.GetResponse();
            response.Cookies = cookies;

            response.Close();

            if (expectedSetCookie.Replace("Set-Cookie: ", "") != response.Headers["Set-Cookie"] ||
                expectedSetCookie2.Replace("Set-Cookie2: ", "") != response.Headers["Set-Cookie2"])
            {
                return -1;
            }

            string clientResponse = helper.GetClientResponse(expectedBytes);

            if (!clientResponse.Contains($"\r\n{expectedSetCookie}\r\n") ||
                !clientResponse.Contains($"\r\n{expectedSetCookie2}\r\n"))
            {
                return -1;    
            }

            return 100;
        }
    }
}
