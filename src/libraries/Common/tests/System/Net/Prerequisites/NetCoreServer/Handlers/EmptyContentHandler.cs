// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class EmptyContentHandler
    {
        public static void Invoke(HttpContext context)
        {
            // By default, this empty method sends back a 200 status code with 'Content-Length: 0' response header.
            // There are no other entity-body related (i.e. 'Content-Type') headers returned.
        }
    }
}
