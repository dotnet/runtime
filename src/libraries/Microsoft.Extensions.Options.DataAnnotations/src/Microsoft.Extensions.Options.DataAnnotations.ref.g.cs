// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// This file is auto-generated and any changes to it will be lost.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class OptionsBuilderDataAnnotationsExtensions
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Uses DataAnnotationValidateOptions which is unsafe given that the options type passed in when calling Validate cannot be statically analyzed so its members may be trimmed.")]
        public static Microsoft.Extensions.Options.OptionsBuilder<TOptions> ValidateDataAnnotations<TOptions>(this Microsoft.Extensions.Options.OptionsBuilder<TOptions> optionsBuilder) where TOptions : class { throw null; }
    }
}
namespace Microsoft.Extensions.Options
{
    public partial class DataAnnotationValidateOptions<TOptions> : Microsoft.Extensions.Options.IValidateOptions<TOptions> where TOptions : class
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The implementation of Validate method on this type will walk through all properties of the passed in options object, and its type cannot be statically analyzed so its members may be trimmed.")]
        public DataAnnotationValidateOptions(string? name) { }
        public string? Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification="Suppressing the warnings on this method since the constructor of the type is annotated as RequiresUnreferencedCode.")]
        public Microsoft.Extensions.Options.ValidateOptionsResult Validate(string? name, TOptions options) { throw null; }
    }
}
