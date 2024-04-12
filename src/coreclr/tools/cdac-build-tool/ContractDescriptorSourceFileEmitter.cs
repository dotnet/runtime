// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public partial class ContractDescriptorSourceFileEmitter
{
    public const string TemplateResourceName = "Microsoft.DotNet.Diagnostics.DataContract.Resources.contract-descriptor.c.in";
    internal const string JsonDescriptorKey = "jsonDescriptor";
    internal const string JsonDescriptorSizeKey = "jsonDescriptorSize";
    internal const string PointerDataCount = "pointerDataCount";
    internal const string PlatformFlags = "platformFlags";

    public ContractDescriptorSourceFileEmitter()
    {

    }

    [GeneratedRegex("%%([a-zA-Z0-9_]+)%%", RegexOptions.CultureInvariant)]
    private static partial Regex FindTemplateSigilRegex();

    internal Stream GetTemplateStream()
    {
        return typeof(ContractDescriptorSourceFileEmitter).Assembly.GetManifestResourceStream(TemplateResourceName)!;
    }

    internal string GetTemplateString()
    {
        using var reader = new StreamReader(GetTemplateStream(), System.Text.Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public void SetPointerDataCount(int count)
    {
        Elements[PointerDataCount] = count.ToString();
    }

    public void SetPlatformFlags(uint platformFlags)
    {
        Elements[PlatformFlags] = $"0x{platformFlags:x8}";
    }

    // The string should be C escaped
    // The length should be the length of the unescaped string
    // FIXME: move the escaping here.
    public void SetJsonDescriptor(string jsonDescriptor)
    {
        var count = jsonDescriptor.Length; // return the length before escaping
        var escaped = CStringEscape().Replace(jsonDescriptor, "\\$1");
        Elements[JsonDescriptorKey] = escaped;
        Elements[JsonDescriptorSizeKey] = count.ToString();
    }

    [GeneratedRegex("(\")", RegexOptions.CultureInvariant)]
    private static partial Regex CStringEscape();

    public Dictionary<string,string> Elements {get; } = new();

    public void Emit(TextWriter dest)
    {
        var template = GetTemplateString();
        var matches = FindTemplateSigilRegex().Matches(template);
        var prevPos = 0;
        foreach (Match match in matches) {
            // copy everything from the end of the last match (prevPos) to just before the current match to the output
            dest.Write(template.AsSpan(prevPos, match.Index - prevPos));

            // lookup the capture key and write it out

            var key = match.Groups[1].Captures[0].Value;
            if (!Elements.TryGetValue(key, out string? result))
            {
                throw new InvalidOperationException ($"no replacement for {key}");
            }
            dest.Write(result);
            prevPos = match.Index + match.Length;
        }
        // write everything from the prevPos to the end of the template
        dest.Write(template.AsSpan(prevPos));
    }
}
