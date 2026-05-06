// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Threading.Tasks;

namespace CookieExtensionsTest
{
    /// <summary>
    /// Tests that the System.Net.CookieExtensions.InternalAdd()
    /// method works as expected when used in a trimmed application.
    /// </summary>
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var helper = new TestHelper();

            string cookieString = "cookie: name=value";
            Cookie expected = new Cookie("name", "value");

            HttpListenerRequest request = await helper.GetRequest("POST", new[] { cookieString });
            Cookie actual = request.Cookies[0];

            if (request.Cookies.Count != 1 ||
                actual.Name != expected.Name ||
                actual.Value != expected.Value ||
                actual.Port != expected.Port ||
                actual.Path != expected.Path ||
                actual.Domain != expected.Domain)
            {
                return -1;
            }

            return 100;
        }
    }
}
