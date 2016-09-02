// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Web;

public class BasicAuthModule : IHttpModule
{
    public void Dispose()
    {
    }

    public void Init(HttpApplication context)
    {
        context.EndRequest += Context_EndRequest; ;
    }

    private void Context_EndRequest(object sender, EventArgs e)
    {
        var context = (HttpApplication)sender;

        var response = context.Response;
        if (response.StatusCode != 401)
        {
            return;
        }

        response.AddHeader("WWW-Authenticate", "Basic");
    }
}
