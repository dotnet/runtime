// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public partial class ContractDescriptorSourceFileEmitter
{
    private const string JsonDescriptorKey = "jsonDescriptor";
    private const string JsonDescriptorSizeKey = "jsonDescriptorSize";
    private const string PointerDataCount = "pointerDataCount";
    private const string PlatformFlags = "platformFlags";

    private readonly string _templateFilePath;

    public ContractDescriptorSourceFileEmitter(string templateFilePath)
    {
        _templateFilePath = templateFilePath;
    }

    [GeneratedRegex("%%([a-zA-Z0-9_]+)%%", RegexOptions.CultureInvariant)]
    private static partial Regex FindTemplatePlaceholderRegex { get; }

    private string GetTemplateString()
    {
        return File.ReadAllText(_templateFilePath);
    }

    public void SetPointerDataCount(int count)
    {
        Elements[PointerDataCount] = count.ToString();
    }

    public void SetPlatformFlags(uint platformFlags)
    {
        Elements[PlatformFlags] = $"0x{platformFlags:x8}";
    }

    /// <remarks>The jsonDescriptor should not be C escaped</remarks>
    public void SetJsonDescriptor(string jsonDescriptor)
    {
        int count = jsonDescriptor.Length; // return the length before escaping
        string escaped = CStringEscape.Replace(jsonDescriptor, "\\$1");

        // MSVC limits individual string literals to about 2048 bytes (error C2026).
        // The C standard only guarantees 4095 characters per literal. To stay portable,
        // we split long strings into adjacent literals ("chunk1" "chunk2") which are
        // concatenated into a single contiguous string at compile time per the C standard.
        const int MaxChunkSize = 2000;
        if (escaped.Length > MaxChunkSize)
        {
            StringBuilder sb = new StringBuilder(escaped.Length + escaped.Length / MaxChunkSize * 4);
            int offset = 0;
            while (offset < escaped.Length)
            {
                if (offset > 0)
                    sb.Append("\" \"");
                int chunkEnd = Math.Min(offset + MaxChunkSize, escaped.Length);
                // Don't split in the middle of a \" escape sequence
                if (chunkEnd < escaped.Length && escaped[chunkEnd - 1] == '\\')
                    chunkEnd--;
                sb.Append(escaped, offset, chunkEnd - offset);
                offset = chunkEnd;
            }
            escaped = sb.ToString();
        }

        Elements[JsonDescriptorKey] = escaped;
        Elements[JsonDescriptorSizeKey] = count.ToString();
    }

    [GeneratedRegex("(\")", RegexOptions.CultureInvariant)]
    private static partial Regex CStringEscape { get; }

    public Dictionary<string, string> Elements { get; } = new();

    public void Emit(TextWriter dest)
    {
        var template = GetTemplateString();
        var matches = FindTemplatePlaceholderRegex.Matches(template);
        var prevPos = 0;
        foreach (Match match in matches)
        {
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
