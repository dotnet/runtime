// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
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
                var state = new JsonWriterState(options: new JsonWriterOptions { Indented = true });
                var jsonWriter = new Utf8JsonWriter(bufferWriter, state);
                WriteCore(context, new UnifiedJsonWriter(jsonWriter));
                bufferWriter.CopyTo(stream);
            }
        }
    }
}
