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
