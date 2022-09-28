// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Configuration
{
    public static partial class ConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder Add<TSource>(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<TSource>? configureSource) where TSource : Microsoft.Extensions.Configuration.IConfigurationSource, new() { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>> AsEnumerable(this Microsoft.Extensions.Configuration.IConfiguration configuration) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>> AsEnumerable(this Microsoft.Extensions.Configuration.IConfiguration configuration, bool makePathsRelative) { throw null; }
        public static bool Exists([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] this Microsoft.Extensions.Configuration.IConfigurationSection? section) { throw null; }
        public static string? GetConnectionString(this Microsoft.Extensions.Configuration.IConfiguration configuration, string name) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationSection GetRequiredSection(this Microsoft.Extensions.Configuration.IConfiguration configuration, string key) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property)]
    public sealed partial class ConfigurationKeyNameAttribute : System.Attribute
    {
        public ConfigurationKeyNameAttribute(string name) { }
        public string Name { get { throw null; } }
    }
    public static partial class ConfigurationPath
    {
        public static readonly string KeyDelimiter;
        public static string Combine(System.Collections.Generic.IEnumerable<string> pathSegments) { throw null; }
        public static string Combine(params string[] pathSegments) { throw null; }
        public static string? GetParentPath(string? path) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("path")]
        public static string? GetSectionKey(string? path) { throw null; }
    }
    public static partial class ConfigurationRootExtensions
    {
        public static string GetDebugView(this Microsoft.Extensions.Configuration.IConfigurationRoot root) { throw null; }
        public static string GetDebugView(this IConfigurationRoot root, System.Func<ConfigurationDebugViewContext, string>? processValue) { throw null; }
    }
    public readonly partial struct ConfigurationDebugViewContext
    {
        public ConfigurationDebugViewContext(string path, string key, string? value, IConfigurationProvider configurationProvider) { throw null; }
        public string Path { get; }
        public string Key { get; }
        public string? Value { get; }
        public IConfigurationProvider ConfigurationProvider { get; }
    }
    public partial interface IConfiguration
    {
        string? this[string key] { get; set; }
        System.Collections.Generic.IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren();
        Microsoft.Extensions.Primitives.IChangeToken GetReloadToken();
        Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key);
    }
    public partial interface IConfigurationBuilder
    {
        System.Collections.Generic.IDictionary<string, object> Properties { get; }
        System.Collections.Generic.IList<Microsoft.Extensions.Configuration.IConfigurationSource> Sources { get; }
        Microsoft.Extensions.Configuration.IConfigurationBuilder Add(Microsoft.Extensions.Configuration.IConfigurationSource source);
        Microsoft.Extensions.Configuration.IConfigurationRoot Build();
    }
    public partial interface IConfigurationProvider
    {
        System.Collections.Generic.IEnumerable<string> GetChildKeys(System.Collections.Generic.IEnumerable<string> earlierKeys, string? parentPath);
        Microsoft.Extensions.Primitives.IChangeToken GetReloadToken();
        void Load();
        void Set(string key, string? value);
        bool TryGet(string key, out string? value);
    }
    public partial interface IConfigurationRoot : Microsoft.Extensions.Configuration.IConfiguration
    {
        System.Collections.Generic.IEnumerable<Microsoft.Extensions.Configuration.IConfigurationProvider> Providers { get; }
        void Reload();
    }
    public partial interface IConfigurationSection : Microsoft.Extensions.Configuration.IConfiguration
    {
        string Key { get; }
        string Path { get; }
        string? Value { get; set; }
    }
    public partial interface IConfigurationSource
    {
        Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder);
    }
}
