// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class OptionsBuilderDataAnnotationsExtensions
    {
        public static Microsoft.Extensions.Options.OptionsBuilder<TOptions> ValidateDataAnnotations<TOptions>(this Microsoft.Extensions.Options.OptionsBuilder<TOptions> optionsBuilder) where TOptions : class { throw null; }
    }
}
namespace Microsoft.Extensions.Options
{
    public partial class DataAnnotationValidateOptions<TOptions> : Microsoft.Extensions.Options.IValidateOptions<TOptions> where TOptions : class
    {
        public DataAnnotationValidateOptions(string name) { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.Options.ValidateOptionsResult Validate(string name, TOptions options) { throw null; }
    }
}
