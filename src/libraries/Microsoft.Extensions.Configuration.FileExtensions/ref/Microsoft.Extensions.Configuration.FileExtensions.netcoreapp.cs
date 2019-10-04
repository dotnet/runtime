// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration
{
    public static partial class FileConfigurationExtensions
    {
        public static System.Action<Microsoft.Extensions.Configuration.FileLoadExceptionContext> GetFileLoadExceptionHandler(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
        public static Microsoft.Extensions.FileProviders.IFileProvider GetFileProvider(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder SetBasePath(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string basePath) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder SetFileLoadExceptionHandler(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<Microsoft.Extensions.Configuration.FileLoadExceptionContext> handler) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder SetFileProvider(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, Microsoft.Extensions.FileProviders.IFileProvider fileProvider) { throw null; }
    }
    public abstract partial class FileConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider, System.IDisposable
    {
        public FileConfigurationProvider(Microsoft.Extensions.Configuration.FileConfigurationSource source) { }
        public Microsoft.Extensions.Configuration.FileConfigurationSource Source { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public override void Load() { }
        public abstract void Load(System.IO.Stream stream);
        public override string ToString() { throw null; }
    }
    public abstract partial class FileConfigurationSource : Microsoft.Extensions.Configuration.IConfigurationSource
    {
        protected FileConfigurationSource() { }
        public Microsoft.Extensions.FileProviders.IFileProvider FileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Action<Microsoft.Extensions.Configuration.FileLoadExceptionContext> OnLoadException { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Optional { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int ReloadDelay { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ReloadOnChange { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public abstract Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder);
        public void EnsureDefaults(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { }
        public void ResolveFileProvider() { }
    }
    public partial class FileLoadExceptionContext
    {
        public FileLoadExceptionContext() { }
        public System.Exception Exception { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Ignore { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.Configuration.FileConfigurationProvider Provider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
