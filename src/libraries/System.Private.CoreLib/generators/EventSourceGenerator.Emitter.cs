// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Generators
{
    public partial class EventSourceGenerator
    {
        private sealed class Emitter
        {
            private readonly StringBuilder _builder = new StringBuilder(1024);
            private readonly GeneratorExecutionContext _context;

            public Emitter(GeneratorExecutionContext context) => _context = context;

            public void Emit(EventSourceClass[] eventSources, CancellationToken cancellationToken)
            {
                foreach (EventSourceClass? ec in eventSources)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // stop any additional work
                        break;
                    }

                    _builder.AppendLine("using System;");
                    GenType(ec);

                    _context.AddSource($"{ec.ClassName}.Generated", SourceText.From(_builder.ToString(), Encoding.UTF8));

                    _builder.Clear();
                }
            }

            private void GenType(EventSourceClass ec)
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
                GenerateConstructor(ec);

                GenerateProviderMetadata(ec.SourceName);

                _builder.AppendLine($@"
    }}");

                if (!string.IsNullOrWhiteSpace(ec.Namespace))
                {
                    _builder.AppendLine($@"
}}");
                }
            }

            private void GenerateConstructor(EventSourceClass ec)
            {
                _builder.AppendLine($@"
        private {ec.ClassName}() : base(new Guid({ec.Guid.ToString("x").Replace("{", "").Replace("}", "")}), ""{ec.SourceName}"") {{ }}");
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
