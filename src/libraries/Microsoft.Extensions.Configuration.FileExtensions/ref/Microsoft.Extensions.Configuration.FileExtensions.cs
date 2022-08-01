// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Configuration
{
    public static partial class FileConfigurationExtensions
    {
        public static System.Action<Microsoft.Extensions.Configuration.FileLoadExceptionContext>? GetFileLoadExceptionHandler(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
        public static Microsoft.Extensions.FileProviders.IFileProvider GetFileProvider(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder SetBasePath(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string basePath) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder SetFileLoadExceptionHandler(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<Microsoft.Extensions.Configuration.FileLoadExceptionContext> handler) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder SetFileProvider(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, Microsoft.Extensions.FileProviders.IFileProvider fileProvider) { throw null; }
    }
    public abstract partial class FileConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider, System.IDisposable
    {
        public FileConfigurationProvider(Microsoft.Extensions.Configuration.FileConfigurationSource source) { }
        public Microsoft.Extensions.Configuration.FileConfigurationSource Source { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public override void Load() { }
        public abstract void Load(System.IO.Stream stream);
        public override string ToString() { throw null; }
    }
    public abstract partial class FileConfigurationSource : Microsoft.Extensions.Configuration.IConfigurationSource
    {
        protected FileConfigurationSource() { }
        public Microsoft.Extensions.FileProviders.IFileProvider? FileProvider { get { throw null; } set { } }
        public System.Action<Microsoft.Extensions.Configuration.FileLoadExceptionContext>? OnLoadException { get { throw null; } set { } }
        public bool Optional { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.DisallowNull]
        public string? Path { get { throw null; } set { } }
        public int ReloadDelay { get { throw null; } set { } }
        public bool ReloadOnChange { get { throw null; } set { } }
        public abstract Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder);
        public void EnsureDefaults(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { }
        public void ResolveFileProvider() { }
    }
    public partial class FileLoadExceptionContext
    {
        public FileLoadExceptionContext() { }
        public System.Exception Exception { get { throw null; } set { } }
        public bool Ignore { get { throw null; } set { } }
        public Microsoft.Extensions.Configuration.FileConfigurationProvider Provider { get { throw null; } set { } }
    }
}
