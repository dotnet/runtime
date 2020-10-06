// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyModel
{
    public partial class DependencyContextWriter
    {
        public void Write(DependencyContext context, Stream stream)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            using (var bufferWriter = new ArrayBufferWriter())
            {
                // Custom encoder is required to fix https://github.com/dotnet/core-setup/issues/7137
                // Since the JSON is only written to a file that is read by the SDK (and not transmitted over the wire),
                // it is safe to skip escaping certain characters in this scenario
                // (that would otherwise be escaped, by default, as part of defense-in-depth, such as +).
                var options = new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                var jsonWriter = new Utf8JsonWriter(bufferWriter, options);
                WriteCore(context, new UnifiedJsonWriter(jsonWriter));
                bufferWriter.CopyTo(stream);
            }
        }
    }
}
