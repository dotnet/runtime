// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Text.Json;

namespace Microsoft.Extensions.Logging.Console
{
    public class JsonLogFormatterOptions
    {
        public JsonLogFormatterOptions() { }
        public JsonWriterOptions JsonWriterOptions { get; set; }
        // or public JsonSerializerOptions JsonSerializerOptions { get; set; }
    }
}
