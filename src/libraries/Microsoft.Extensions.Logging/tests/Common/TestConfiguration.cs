// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Microsoft.Extensions.Logging.Test
{
    internal class TestConfiguration : JsonConfigurationProvider
    {
        private Func<string> _json;
        public TestConfiguration(JsonConfigurationSource source, Func<string> json)
            : base(source)
        {
            _json = json;
        }

        public override void Load()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(_json());
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            Load(stream);
        }

        public static ConfigurationRoot Create(Func<string> getJson)
        {
            var provider = new TestConfiguration(new JsonConfigurationSource { Optional = true }, getJson);
            return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
        }
    }
}