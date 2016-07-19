// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebServer
{
    /// <summary>
    /// Summary description for EmptyContent
    /// </summary>
    public class EmptyContent : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            // By default, this empty method sends back a 200 status code with 'Content-Length: 0' response header.
            // There are no other entity-body related (i.e. 'Content-Type') headers returned.
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
