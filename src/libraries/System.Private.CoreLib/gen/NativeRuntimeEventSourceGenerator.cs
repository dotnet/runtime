// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Generators
{
    [Generator]
    public sealed class NativeRuntimeEventSourceGenerator : IIncrementalGenerator
    {
        private static readonly XNamespace EventNs = "http://schemas.microsoft.com/win/2004/08/events";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AdditionalText> manifestFiles = context.AdditionalTextsProvider.Where(f => f.Path.EndsWith(".man", StringComparison.OrdinalIgnoreCase));
            IncrementalValuesProvider<AdditionalText> inclusionFiles = context.AdditionalTextsProvider.Where(f => f.Path.EndsWith(".lst", StringComparison.OrdinalIgnoreCase));

            IncrementalValuesProvider<(AdditionalText Left, System.Collections.Immutable.ImmutableArray<AdditionalText> Right)> combined = manifestFiles.Combine(inclusionFiles.Collect());

            context.RegisterSourceOutput(combined, (spc, tuple) =>
            {
                AdditionalText manifestFile = tuple.Left;
                System.Collections.Immutable.ImmutableArray<AdditionalText> inclusionFiles = tuple.Right;
                string manifestText = manifestFile.GetText(spc.CancellationToken)?.ToString();
                if (string.IsNullOrEmpty(manifestText))
                {
                    return;
                }

                var manifest = XDocument.Parse(manifestText);

                string inclusionText = inclusionFiles.FirstOrDefault()?.GetText(spc.CancellationToken)?.ToString();

                Dictionary<string, HashSet<string>> inclusionList = ParseInclusionListFromString(inclusionText);

                foreach (KeyValuePair<string, string> kvp in manifestsToGenerate)
                {
                    string providerName = kvp.Key;
                    string className = providerNameToClassNameMap[providerName];
                    XElement? providerNode = manifest
                        .Descendants(EventNs + "provider")
                        .FirstOrDefault(e => (string)e.Attribute("name") == providerName);

                    if (providerNode is null)
                    {
                        continue;
                    }

                    string source = GenerateEventSourceClass(providerNode, className, inclusionList);
                    spc.AddSource($"{className}.g.cs", SourceText.From(source, System.Text.Encoding.UTF8));
                }
            });
        }

        private static Dictionary<string, HashSet<string>> ParseInclusionListFromString(string? inclusionText)
        {
            Dictionary<string, HashSet<string>> inclusionList = [];
            if (string.IsNullOrEmpty(inclusionText))
            {
                return inclusionList;
            }

            using var reader = new StringReader(inclusionText);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                {
                    continue;
                }

                string[] tokens = trimmed.Split(':');
                if (tokens.Length == 0)
                {
                    continue;
                }

                if (tokens.Length > 2)
                {
                    continue;
                }

                string providerName, eventName;
                if (tokens.Length == 2)
                {
                    providerName = tokens[0];
                    eventName = tokens[1];
                }
                else
                {
                    providerName = "*";
                    eventName = tokens[0];
                }
                if (!inclusionList.TryGetValue(providerName, out HashSet<string>? value))
                {
                    value = [];
                    inclusionList[providerName] = value;
                }

                value.Add(eventName);
            }
            return inclusionList;
        }

        private static bool IncludeEvent(Dictionary<string, HashSet<string>> inclusionList, string providerName, string eventName)
        {
            if (inclusionList == null || inclusionList.Count == 0)
            {
                return true;
            }

            if (inclusionList.TryGetValue(providerName, out HashSet<string>? events) && events.Contains(eventName))
            {
                return true;
            }

            if (inclusionList.TryGetValue("*", out HashSet<string>? wildcardEvents) && wildcardEvents.Contains(eventName))
            {
                return true;
            }

            return false;
        }

        private static string GenerateEventSourceClass(XElement providerNode, string className, Dictionary<string, HashSet<string>> inclusionList)
        {
            var sw = new StringWriter();

            sw.WriteLine($$"""
                // Licensed to the .NET Foundation under one or more agreements.
                // The .NET Foundation licenses this file to you under the MIT license.
                // <auto-generated/>

                using System;

                namespace System.Diagnostics.Tracing
                {
                    internal sealed partial class {{className}} : EventSource
                    {
                """);

            GenerateKeywordsClass(providerNode, sw, inclusionList);
            GenerateEventMethods(providerNode, sw, inclusionList);

            sw.WriteLine("""
                    }
                }
                """);
            return sw.ToString();
        }

        private static void GenerateKeywordsClass(XElement providerNode, StringWriter writer, Dictionary<string, HashSet<string>> inclusionList)
        {
            string? providerName = providerNode.Attribute("name")?.Value;

            if (providerName is null)
            {
                return;
            }

            XElement eventsNode = providerNode.Element(EventNs + "events");
            if (eventsNode is null)
            {
                return;
            }

            IEnumerable<XElement> eventNodes = eventsNode.Elements(EventNs + "event");
            var usedKeywords = new HashSet<string>();
            foreach (XElement? eventNode in eventNodes)
            {
                string? eventName = eventNode.Attribute("symbol")?.Value;

                if (eventName is null
                    || !IncludeEvent(inclusionList, providerName, eventName))
                {
                    continue;
                }

                string? keywords = eventNode.Attribute("keywords")?.Value;
                if (!string.IsNullOrEmpty(keywords))
                {
                    foreach (string? kw in keywords.Split([' '], StringSplitOptions.RemoveEmptyEntries))
                    {
                        usedKeywords.Add(kw);
                    }
                }
            }
            XElement? keywordsNode = providerNode.Element(EventNs + "keywords");
            if (keywordsNode is null)
            {
                return;
            }

            writer.WriteLine("""
                        public static class Keywords
                        {
                """);

            foreach (XElement keywordNode in keywordsNode.Elements(EventNs + "keyword"))
            {
                string? name = keywordNode.Attribute("name")?.Value;
                string? mask = keywordNode.Attribute("mask")?.Value;
                if (name is not null && mask is not null && usedKeywords.Contains(name))
                {
                    writer.WriteLine($"            public const EventKeywords {name} = (EventKeywords){mask};");
                }
            }

            writer.WriteLine("""
                        }

                """);
        }

        private static void GenerateEventMethods(XElement providerNode, StringWriter writer, Dictionary<string, HashSet<string>> inclusionList)
        {
            string? providerName = providerNode.Attribute("name")?.Value;

            if (providerName is null)
            {
                return;
            }

            XElement eventsNode = providerNode.Element(EventNs + "events");
            if (eventsNode == null)
            {
                return;
            }

            var eventNodes = eventsNode.Elements(EventNs + "event").ToList();
            XElement templatesNode = providerNode.Element(EventNs + "templates");
            var templateDict = new Dictionary<string, XElement>();
            if (templatesNode != null)
            {
                foreach (XElement? template in templatesNode.Elements(EventNs + "template"))
                {
                    string? name = template.Attribute("tid")?.Value;
                    if (!string.IsNullOrEmpty(name))
                    {
                        templateDict[name] = template;
                    }
                }
            }

            // Build a dictionary of eventID -> latest version
            Dictionary<string, string> latestEventVersions = [];
            foreach (XElement? eventNode in eventNodes)
            {
                string? eventName = eventNode.Attribute("symbol")?.Value;
                if (eventName is null
                    || !IncludeEvent(inclusionList, providerName, eventName))
                {
                    continue;
                }

                string? eventId = eventNode.Attribute("value")?.Value;
                string? version = eventNode.Attribute("version")?.Value;
                if (eventId is not null && version is not null)
                {
                    if (!latestEventVersions.TryGetValue(eventId, out string? existingVersion) || string.CompareOrdinal(version, existingVersion) > 0)
                    {
                        latestEventVersions[eventId] = version;
                    }
                }
            }

            foreach (XElement? eventNode in eventNodes)
            {
                string? eventName = eventNode.Attribute("symbol")?.Value;
                if (eventName is null
                    || !IncludeEvent(inclusionList, providerName, eventName))
                {
                    continue;
                }

                if (IsEventManuallyHandled(eventName))
                {
                    continue;
                }

                string? eventId = eventNode.Attribute("value")?.Value;
                string? version = eventNode.Attribute("version")?.Value;
                // Only emit the event if it is the latest version for this eventId
                if (eventId is null || version is null || latestEventVersions[eventId] != version)
                {
                    continue;
                }

                string? level = eventNode.Attribute("level")?.Value;
                IEnumerable<string>? keywords = eventNode
                    .Attribute("keywords")
                    ?.Value
                    .ToString()
                    .Split([' '], StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => $"Keywords.{k}");

                writer.Write($"        [Event({eventId}, Version = {version}, Level = EventLevel.{level?.Replace("win:", "")}");

                if (keywords?.Any() == true)
                {
                    writer.Write($", Keywords = {string.Join(" | ", keywords)}");
                }

                writer.WriteLine(")]");

                // Write the method signature
                writer.Write($"        private void {eventName}(");

                string? templateValue = eventNode.Attribute("template")?.Value;

                if (!string.IsNullOrEmpty(templateValue)
                    && templateDict.TryGetValue(templateValue, out XElement? template))
                {
                    IEnumerable<XElement> dataNodes = template.Elements(EventNs + "data").ToArray();
                    var paramList = new List<string>();

                    // Calculate the number of arguments to emit.
                    // COMPAT: Cut the parameter list at any binary or ansi string arguments,
                    // or if the count attribute is set on any of the parameters.
                    int numArgumentsToEmit = 0;
                    foreach (XElement data in dataNodes)
                    {
                        string? paramType = data.Attribute("inType")?.Value.ToString();

                        if (paramType is "win:Binary" or "win:AnsiString")
                        {
                            break;
                        }

                        if (!string.IsNullOrEmpty(data.Attribute("count")?.Value))
                        {
                            break;
                        }

                        numArgumentsToEmit++;
                    }

                    foreach (XElement data in dataNodes)
                    {
                        if (numArgumentsToEmit-- <= 0)
                        {
                            break;
                        }

                        string? paramType = data.Attribute("inType")?.Value;
                        string? paramName = data.Attribute("name")?.Value;
                        if (paramType is not null && paramName is not null
                            && manifestTypeToCSharpTypeMap.TryGetValue(paramType, out string? csType))
                        {
                            paramList.Add($"{csType} {paramName}");
                        }
                        else if (paramType is not null && paramName is not null)
                        {
                            paramList.Add($"object {paramName}");
                        }
                    }
                    writer.Write(string.Join(", ", paramList));
                }

                writer.WriteLine("""
                    )
                    {
                        // To have this event be emitted from managed side, refer to NativeRuntimeEventSource.cs
                        throw new NotImplementedException();
                    }

                    """);
            }
        }

        private static bool IsEventManuallyHandled(string eventName)
        {
            foreach (string handledEvent in manuallyHandledEventSymbols)
            {
                if (eventName.StartsWith(handledEvent, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static readonly Dictionary<string, string> manifestsToGenerate = new()
        {
            { "Microsoft-Windows-DotNETRuntime", "NativeRuntimeEventSource.Generated.cs" }
        };

        private static readonly Dictionary<string, string> providerNameToClassNameMap = new()
        {
            { "Microsoft-Windows-DotNETRuntime", "NativeRuntimeEventSource" }
        };

        private static readonly Dictionary<string, string> manifestTypeToCSharpTypeMap = new()
        {
            { "win:UInt8", "byte" },
            { "win:UInt16", "ushort" },
            { "win:UInt32", "uint" },
            { "win:UInt64", "ulong" },
            { "win:Int32", "int" },
            { "win:Int64", "long" },
            { "win:Pointer", "IntPtr" },
            { "win:UnicodeString", "string" },
            { "win:Binary", "byte[]" },
            { "win:Double", "double" },
            { "win:Boolean", "bool" },
            { "win:GUID", "Guid" },
        };

        private static readonly List<string> manuallyHandledEventSymbols =
        [
            // Some threading events are defined manually in NativeRuntimeEventSource.Threading.cs
            "ThreadPool",
            "Contention",
            "WaitHandle"
        ];
    }
}
