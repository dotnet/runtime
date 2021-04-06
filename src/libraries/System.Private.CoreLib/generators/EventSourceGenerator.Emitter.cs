// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Generators
{
    public partial class EventSourceGenerator
    {
        private class Emitter
        {
            private readonly StringBuilder _builder = new StringBuilder(1024);
            private readonly GeneratorExecutionContext _context;

            public Emitter(GeneratorExecutionContext context) => _context = context;

            public void Emit(EventSourceClass[] eventSources, CancellationToken cancellationToken, ITypeSymbol stringTypeSymbol)
            {
                foreach (EventSourceClass? ec in eventSources)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // stop any additional work
                        break;
                    }

                    _builder.AppendLine("using System;");
                    _builder.AppendLine("using System.Diagnostics.Tracing;");
                    GenType(ec, stringTypeSymbol);
                    _context.AddSource($"{ec.ClassName}.Generated", SourceText.From(_builder.ToString(), Encoding.UTF8));//, Encoding.UTF8));

                    _builder.Clear();
                }
            }


            private void GenType(EventSourceClass ec, ITypeSymbol stringTypeSymbol)
            {
                if (!string.IsNullOrWhiteSpace(ec.Namespace))
                {
                    _builder.AppendLine($@"
namespace {ec.Namespace}
{{");
                }

                _builder.AppendLine($@"
    partial class {ec.ClassName}
    {{");
                GenerateConstructor(ec, stringTypeSymbol);
                
                _builder.AppendLine($@"
    }}");

                if (!string.IsNullOrWhiteSpace(ec.Namespace))
                {
                    _builder.AppendLine($@"
}}");
                }
                
            }

            private void GenerateConstructor(EventSourceClass ec, ITypeSymbol stringTypeSymbol)
            {
                _builder.AppendLine($@"
        private {ec.ClassName}() : base(new Guid({ec.Guid.ToString("x").Replace("{", "").Replace("}", "")}), ""{ec.SourceName}"") {{");
                EventDataBuilder.BuildEventDescriptor(_builder, ec.Events);
                _builder.AppendLine("        m_EventMetadataInitializer = () => new byte[] {");
                byte[] metadataBytes = Encoding.UTF8.GetBytes(MetadataForProvider(ec, stringTypeSymbol));

                int byteCnt = 1;
                foreach (byte b in metadataBytes)
                {
                    _builder.Append($"0x{b:x}, ");
                    if (byteCnt++ % 100 == 0)
                    {
                        _builder.AppendLine("");
                        _builder.Append("            ");
                    }
                }
                _builder.AppendLine("");
                _builder.AppendLine(@"        };");
 

                _builder.AppendLine("        }");
            }

            private void GenerateProviderMetadata(string sourceName)
            {
                _builder.Append(@"
        private protected override ReadOnlySpan<byte> ProviderMetadata => new byte[] { ");

                byte[] metadataBytes = MetadataForString(sourceName);
                foreach (byte b in metadataBytes)
                {
                    _builder.Append($"0x{b:x}, ");
                }

                _builder.AppendLine(@"};");
            }

            // From System.Private.CoreLib
            private static byte[] MetadataForString(string name)
            {
                CheckName(name);
                int metadataSize = Encoding.UTF8.GetByteCount(name) + 3;
                byte[]? metadata = new byte[metadataSize];
                ushort totalSize = checked((ushort)(metadataSize));
                metadata[0] = unchecked((byte)totalSize);
                metadata[1] = unchecked((byte)(totalSize >> 8));
                Encoding.UTF8.GetBytes(name, 0, name.Length, metadata, 2);
                return metadata;
            }

            private string MetadataForProvider(EventSourceClass ec, ITypeSymbol stringTypeSymbol)
            {
                ManifestBuilder manifest = new ManifestBuilder(_builder, ec.Namespace + "." + ec.ClassName, ec.Guid, ec.KeywordMap, ec.TaskMap);
                // Add an entry unconditionally for event ID 0 which will be for a string message.
                manifest.StartEvent("EventSourceMessage", new EventAttribute(0) { Level = EventLevel.LogAlways, Task = (EventTask)0xFFFE });
                manifest.AddEventParameter(stringTypeSymbol, "message");
                manifest.EndEvent("EventSourceMessage");

                // ensure we have keywords for the session-filtering reserved bits
                {
                    manifest.AddKeyword("Session3", (long)0x1000 << 32);
                    manifest.AddKeyword("Session2", (long)0x2000 << 32);
                    manifest.AddKeyword("Session1", (long)0x4000 << 32);
                    manifest.AddKeyword("Session0", (long)0x8000 << 32);
                }

                // add map
                manifest.AddMap(ec.Maps);

                foreach (EventSourceEvent evt in ec.Events)
                {
                    EventAttribute eventAttribute = new EventAttribute(Int32.Parse(evt.Id));
                    eventAttribute.Level = (EventLevel)(Int32.Parse(evt.Level));
                    if (String.IsNullOrEmpty(evt.Keywords))
                    {
                        eventAttribute.Keywords = EventKeywords.None;
                    }
                    else
                    {
                        eventAttribute.Keywords = (EventKeywords)(Int64.Parse(evt.Keywords));
                    }
                    eventAttribute.Version = (byte)(Int32.Parse(evt.Version));

                    manifest.StartEvent(evt.Name, eventAttribute);
                    
                    if (evt.Parameters is not null)
                    {
                        foreach (EventParameter param in evt.Parameters)
                        {
                            if (param.Type is not null)
                            {
                                manifest.AddEventParameter(param.Type, param.Name);
                            }
                        }
                    }

                    manifest.EndEvent(evt.Name);
                }
                return manifest.CreateManifestString();
            }

            private static void CheckName(string? name)
            {
                if (name != null && 0 <= name.IndexOf('\0'))
                {
                    throw new ArgumentOutOfRangeException(nameof(name));
                }
            }
        }
    }
}
