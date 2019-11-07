// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Newtonsoft.Json;

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
            using (var writer = new StreamWriter(stream))
            {
                using (var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
                {
                    WriteCore(context, new UnifiedJsonWriter(jsonWriter));
                }
            }
        }
    }
}
