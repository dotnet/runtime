// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Configuration
{
    public partial class BinderOptions
    {
        public BinderOptions() { }
        public bool BindNonPublicProperties { get { throw null; } set { } }
        public bool ErrorOnUnknownConfiguration { get { throw null; } set { } }
    }
    public static partial class ConfigurationBinder
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Cannot statically analyze the type of instance so its members may be trimmed")]
        public static void Bind(this Microsoft.Extensions.Configuration.IConfiguration configuration, object instance) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Cannot statically analyze the type of instance so its members may be trimmed")]
        public static void Bind(this Microsoft.Extensions.Configuration.IConfiguration configuration, object instance, System.Action<Microsoft.Extensions.Configuration.BinderOptions> configureOptions) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Cannot statically analyze the type of instance so its members may be trimmed")]
        public static void Bind(this Microsoft.Extensions.Configuration.IConfiguration configuration, string key, object instance) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
        public static object Get(this Microsoft.Extensions.Configuration.IConfiguration configuration, System.Type type) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
        public static object Get(this Microsoft.Extensions.Configuration.IConfiguration configuration, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type type, System.Action<Microsoft.Extensions.Configuration.BinderOptions> configureOptions) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
        public static object GetValue(this Microsoft.Extensions.Configuration.IConfiguration configuration, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type type, string key) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
        public static object GetValue(this Microsoft.Extensions.Configuration.IConfiguration configuration, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type type, string key, object defaultValue) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
        public static T GetValue<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] T>(this Microsoft.Extensions.Configuration.IConfiguration configuration, string key) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
        public static T GetValue<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] T>(this Microsoft.Extensions.Configuration.IConfiguration configuration, string key, T defaultValue) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
        public static T Get<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] T>(this Microsoft.Extensions.Configuration.IConfiguration configuration) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
        public static T Get<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] T>(this Microsoft.Extensions.Configuration.IConfiguration configuration, System.Action<Microsoft.Extensions.Configuration.BinderOptions> configureOptions) { throw null; }
    }
}
