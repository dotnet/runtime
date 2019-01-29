// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
