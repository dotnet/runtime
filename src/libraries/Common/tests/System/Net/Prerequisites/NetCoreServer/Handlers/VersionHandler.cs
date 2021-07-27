// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class VersionHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            string versionInfo = GetVersionInfo();
            byte[] bytes = Encoding.UTF8.GetBytes(versionInfo);

            context.Response.ContentType = "text/plain";
            context.Response.ContentLength = bytes.Length;
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }

        private static string GetVersionInfo()
        {
            Type t = typeof(VersionHandler);
            string path = t.Assembly.Location;
            FileVersionInfo fi = FileVersionInfo.GetVersionInfo(path);

            var buffer = new StringBuilder();
            buffer.AppendLine("Information for: " + Path.GetFileName(path));
            buffer.AppendLine("Location: " + Path.GetDirectoryName(path));
            buffer.AppendLine("Framework: " + RuntimeInformation.FrameworkDescription);
            buffer.AppendLine("File Version: " + fi.FileVersion);
            buffer.AppendLine("Product Version: " + fi.ProductVersion);
            buffer.AppendLine("Creation Date: " + File.GetCreationTime(path));
            buffer.AppendLine("Last Modified: " + File.GetLastWriteTime(path));

            return buffer.ToString();
        }        
    }
}
