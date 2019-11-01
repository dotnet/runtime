// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Extensions.DependencyModel
{
    public partial class DependencyContextJsonReader : IDependencyContextReader
    {
        public DependencyContext Read(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using (var streamReader = new StreamReader(stream))
            {
                using (var reader = new JsonTextReader(streamReader))
                {
                    return Read(reader);
                }
            }
        }

        private DependencyContext Read(JsonTextReader jsonReader)
        {
            var reader = new UnifiedJsonReader(jsonReader);
            return ReadCore(reader);
        }
    }
}
