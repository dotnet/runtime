// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

namespace Microsoft.Workload.Build.Tasks;

/*
 * Used for patching a nuget.config to:
 *
 * 1. Add a new package source to the nuget.config
 * 2. Add a new package source mapping to the nuget.config
 *
 * This is useful specifically the case of workload testing
 */
public class PatchNuGetConfig : Task
{
    [Required, NotNull]
    public string? TemplateNuGetConfigPath { get; set; }

    [Required, NotNull]
    public string?        LocalNuGetsPath    { get; set; }

    public string? OutputPath { get; set; }

    /*
     * Value: ["*Aspire*", "Foo*"]
     * This will be translated to:
     * <packageSourceMapping>
     *  <packageSource key="nuget-local">
     *    <package pattern="*Aspire*" />
     *    <package pattern="Foo*" />
     *  </packageSource>
     *
     * This is useful when using Central Package Management (https://learn.microsoft.com/nuget/consume-packages/central-package-management)
    */
    public string[] NuGetConfigPackageSourceMappings { get; set; } = Array.Empty<string>();

    public string   PackageSourceNameForBuiltPackages { get; set; } = "nuget-local";

    public override bool Execute()
    {
        try
        {
            Validate(TemplateNuGetConfigPath, PackageSourceNameForBuiltPackages, OutputPath);
            GetNuGetConfig(TemplateNuGetConfigPath, LocalNuGetsPath, PackageSourceNameForBuiltPackages, NuGetConfigPackageSourceMappings, OutputPath!);
            Log.LogMessage(MessageImportance.Low, $"Generated patched nuget.config at {OutputPath}");
            return true;
        }
        catch (LogAsErrorException laee)
        {
            Log.LogError(laee.Message);
            return false;
        }
    }

    private static void Validate(string? templateNuGetConfigPath, string? packageSourceNameForBuiltPackages, string? outputPath)
    {
        if (string.IsNullOrEmpty(templateNuGetConfigPath))
            throw new LogAsErrorException($"{nameof(templateNuGetConfigPath)} is required");

        if (!File.Exists(templateNuGetConfigPath))
            throw new LogAsErrorException($"Cannot find {nameof(templateNuGetConfigPath)}={templateNuGetConfigPath}");

        if (string.IsNullOrEmpty(packageSourceNameForBuiltPackages))
            throw new LogAsErrorException($"{nameof(packageSourceNameForBuiltPackages)} is required");

        if (string.IsNullOrEmpty(outputPath))
            throw new LogAsErrorException($"{nameof(outputPath)} is required");

        if (Directory.Exists(outputPath))
            throw new LogAsErrorException($"{nameof(outputPath)}={outputPath} is a directory, it should be a file");
    }

    public static void GetNuGetConfig(string templateNuGetConfigPath, string localNuGetsPath, string packageSourceNameForBuiltPackages, string[] nuGetConfigPackageSourceMappings, string outputPath)
    {
        Validate(templateNuGetConfigPath, packageSourceNameForBuiltPackages, outputPath);

        XDocument doc = XDocument.Load(templateNuGetConfigPath);
        string xpath = "/configuration/packageSources";
        XElement? packageSources = doc.XPathSelectElement(xpath);
        if (packageSources is null)
            throw new LogAsErrorException($"Could not find {xpath} in {templateNuGetConfigPath}");

        var newPackageSourceElement = new XElement("add",
                                        new XAttribute("key", packageSourceNameForBuiltPackages),
                                        new XAttribute("value", $"file://{localNuGetsPath}"));
        if (packageSources.LastNode is not null)
        {
            packageSources.LastNode.AddAfterSelf(newPackageSourceElement);
        }
        else
        {
            packageSources.Add(newPackageSourceElement);
        }

        if (nuGetConfigPackageSourceMappings.Length > 0)
        {
            string mappingXpath = "/configuration/packageSourceMapping";
            XElement? packageSourceMapping = doc.XPathSelectElement(mappingXpath);
            if (packageSourceMapping is null)
            {
                if (doc.Root is null)
                    throw new LogAsErrorException($"Could not find root element in {templateNuGetConfigPath}");

                packageSourceMapping = new XElement("packageSourceMapping");
                doc.Root.Add(packageSourceMapping);
            }

            var newPackageSourceMappingElement = new XElement("packageSource",
                                                    new XAttribute("key", packageSourceNameForBuiltPackages),
                                                    nuGetConfigPackageSourceMappings.Select
                                                        (pattern => new XElement("package", new XAttribute("pattern", pattern))));
            if (packageSourceMapping.FirstNode is not null)
            {
                packageSourceMapping.FirstNode?.AddBeforeSelf(newPackageSourceMappingElement);
            }
            else
            {
                packageSourceMapping.Add(newPackageSourceMappingElement);
            }
        }

        using var xw = XmlWriter.Create(outputPath, new XmlWriterSettings { Indent = true, NewLineHandling = NewLineHandling.None, Encoding = Encoding.UTF8 });
        doc.WriteTo(xw);
        xw.Close();
    }
}
