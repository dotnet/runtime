// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;

internal static class WasmBrowserHost
{
    public static void Run(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(config =>
            {
                var applicationPath = args.SkipWhile(a => a != "--applicationpath").Skip(1).First();
                var applicationDirectory = Path.GetDirectoryName(applicationPath)!;
                var name = Path.ChangeExtension(applicationPath, ".staticwebassets.runtime.json");
                name = !File.Exists(name) ? Path.ChangeExtension(applicationPath, ".StaticWebAssets.xml") : name;

                var inMemoryConfiguration = new Dictionary<string, string?>
                {
                    [WebHostDefaults.EnvironmentKey] = "Development",
                    ["Logging:LogLevel:Microsoft"] = "Warning",
                    ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Information",
                    [WebHostDefaults.StaticWebAssetsKey] = name,
                    ["ApplyCopHeaders"] = args.Contains("--apply-cop-headers").ToString()
                };

                config.AddInMemoryCollection(inMemoryConfiguration);
                config.AddJsonFile(Path.Combine(applicationDirectory, "blazor-devserversettings.json"), optional: true, reloadOnChange: true);
            })
            .ConfigureWebHostDefaults(app =>
            {
                app.UseStaticWebAssets();
                app.UseStartup<Startup>();
            })
            .Build();


        host.Run();
    }
}
