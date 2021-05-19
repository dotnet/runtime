// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class OptionsBuilderDataAnnotationsExtensions
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Uses DataAnnotationValidateOptions which is unsafe given that the options type passed in when calling Validate cannot be statically analyzed so its members may be trimmed.")]
        public static Microsoft.Extensions.Options.OptionsBuilder<TOptions> ValidateDataAnnotations<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions>(this Microsoft.Extensions.Options.OptionsBuilder<TOptions> optionsBuilder) where TOptions : class { throw null; }
    }
}
namespace Microsoft.Extensions.Options
{
    public partial class DataAnnotationValidateOptions<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions> : Microsoft.Extensions.Options.IValidateOptions<TOptions> where TOptions : class
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The implementation of Validate method on this type will walk through all properties of the passed in options object, and its type cannot be statically analyzed so its members may be trimmed.")]
        public DataAnnotationValidateOptions(string name) { }
        public string Name { get { throw null; } }
        public Microsoft.Extensions.Options.ValidateOptionsResult Validate(string name, TOptions options) { throw null; }
    }
}
