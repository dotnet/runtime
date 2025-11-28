// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class GetNugetConfigTask : Task
{
    [Required]
    public string InputFile { get; set; }

    [Required]
    public string ArtifactsDir { get; set; }

    [Required]
    public string Configuration { get; set; }

    [Output]
    public string NugetConfigContent { get; set; }

    public override bool Execute()
    {
        try
        {
            XDocument doc = XDocument.Load(InputFile);
            XElement packageSources = doc.Root.Element("packageSources");

            if (packageSources == null)
            {
                packageSources = new XElement("packageSources");
                doc.Root.Add(packageSources);
            }

            XElement newSource = new XElement("add",
                new XAttribute("key", "nuget-local"),
                new XAttribute("value", Path.Combine(ArtifactsDir, "packages", Configuration, "Shipping"))
            );

            packageSources.Add(newSource);

            NugetConfigContent = doc.ToString();
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }
    }
}
