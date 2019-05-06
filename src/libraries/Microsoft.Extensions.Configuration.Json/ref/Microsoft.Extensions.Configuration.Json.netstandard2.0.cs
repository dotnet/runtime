// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration
{
    public static partial class JsonConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddJsonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, Microsoft.Extensions.FileProviders.IFileProvider provider, string path, bool optional, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddJsonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<Microsoft.Extensions.Configuration.Json.JsonConfigurationSource> configureSource) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddJsonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddJsonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddJsonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.Json
{
    public partial class JsonConfigurationProvider : Microsoft.Extensions.Configuration.FileConfigurationProvider
    {
        public JsonConfigurationProvider(Microsoft.Extensions.Configuration.Json.JsonConfigurationSource source) : base (default(Microsoft.Extensions.Configuration.FileConfigurationSource)) { }
        public override void Load(System.IO.Stream stream) { }
    }
    public partial class JsonConfigurationSource : Microsoft.Extensions.Configuration.FileConfigurationSource
    {
        public JsonConfigurationSource() { }
        public override Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
}
