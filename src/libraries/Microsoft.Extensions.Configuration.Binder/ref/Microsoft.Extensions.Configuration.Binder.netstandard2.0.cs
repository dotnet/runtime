// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration
{
    public partial class BinderOptions
    {
        public BinderOptions() { }
        public bool BindNonPublicProperties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class ConfigurationBinder
    {
        public static void Bind(this Microsoft.Extensions.Configuration.IConfiguration configuration, object instance) { }
        public static void Bind(this Microsoft.Extensions.Configuration.IConfiguration configuration, object instance, System.Action<Microsoft.Extensions.Configuration.BinderOptions> configureOptions) { }
        public static void Bind(this Microsoft.Extensions.Configuration.IConfiguration configuration, string key, object instance) { }
        public static object Get(this Microsoft.Extensions.Configuration.IConfiguration configuration, System.Type type) { throw null; }
        public static object Get(this Microsoft.Extensions.Configuration.IConfiguration configuration, System.Type type, System.Action<Microsoft.Extensions.Configuration.BinderOptions> configureOptions) { throw null; }
        public static object GetValue(this Microsoft.Extensions.Configuration.IConfiguration configuration, System.Type type, string key) { throw null; }
        public static object GetValue(this Microsoft.Extensions.Configuration.IConfiguration configuration, System.Type type, string key, object defaultValue) { throw null; }
        public static T GetValue<T>(this Microsoft.Extensions.Configuration.IConfiguration configuration, string key) { throw null; }
        public static T GetValue<T>(this Microsoft.Extensions.Configuration.IConfiguration configuration, string key, T defaultValue) { throw null; }
        public static T Get<T>(this Microsoft.Extensions.Configuration.IConfiguration configuration) { throw null; }
        public static T Get<T>(this Microsoft.Extensions.Configuration.IConfiguration configuration, System.Action<Microsoft.Extensions.Configuration.BinderOptions> configureOptions) { throw null; }
    }
}
