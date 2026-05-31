// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class OptionsBuilderDataAnnotationsExtensions
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Uses DataAnnotationValidateOptionsAsync which is unsafe given that the options type passed in when calling Validate cannot be statically analyzed so its members may be trimmed.")]
        public static Microsoft.Extensions.Options.OptionsBuilder<TOptions> ValidateDataAnnotationsAsync<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions>(this Microsoft.Extensions.Options.OptionsBuilder<TOptions> optionsBuilder) where TOptions : class { throw null; }
    }
}
namespace Microsoft.Extensions.Options
{
    public partial class DataAnnotationValidateOptionsAsync<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions> : Microsoft.Extensions.Options.IAsyncValidateOptions<TOptions> where TOptions : class
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The implementation of ValidateAsync method on this type will walk through all properties of the passed in options object, and its type cannot be statically analyzed so its members may be trimmed.")]
        public DataAnnotationValidateOptionsAsync(string? name) { }
        public string? Name { get { throw null; } }
        public System.Threading.Tasks.Task<Microsoft.Extensions.Options.ValidateOptionsResult> ValidateAsync(string? name, TOptions options, System.Threading.CancellationToken cancellationToken = default) { throw null; }
    }
}
