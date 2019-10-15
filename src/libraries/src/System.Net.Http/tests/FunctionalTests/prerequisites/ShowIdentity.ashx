<%@ WebHandler Language="C#" Class="ShowIdentity" %>

using System;
using System.Web;

public class ShowIdentity : IHttpHandler 
{
    public void ProcessRequest (HttpContext context)
    {
        bool isAuthenticated = context.Request.IsAuthenticated;
        string user = isAuthenticated ? context.Request.LogonUserIdentity.Name : string.Empty;

        context.Response.ContentType = "application/json";
        string json =
            "{\r\n" +
            "  \"Authenticated\": \"" + isAuthenticated + "\",\r\n" +
            "  \"User\": \""+ user + "\"\r\n" +
            "}\r\n";

        context.Response.Write(json);
    }

    public bool IsReusable
    {
        get
        {
            return true;
        }
    }
}
