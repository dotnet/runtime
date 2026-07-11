// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace KeywordNames
{
#pragma warning disable SA1649
#pragma warning disable SA1402

    public class @class
    {
        [Required]
        [MinLength(5)]
        public string? @string { get; set; }
    }

    public class FirstModel
    {
        [Required]
        [MinLength(5)]
        public string? @namespace { get; set; }

        [Compare(nameof(@namespace))]
        public string? @if { get; set; }

        [ValidateObjectMembers]
        public @class? @event { get; set; }

        [ValidateEnumeratedItems]
        public IList<@class>? @const { get; set; }
    }

    [OptionsValidator]
    public partial class FirstValidator : IValidateOptions<FirstModel>
    {
    }
}

// A separate letters-only namespace so the keyword-named validator's sort key inside the emitter
// is never decided by comparing '@' against other characters, which ICU and NLS order differently.
namespace KeywordNamesNested
{
    public partial class @base
    {
        [OptionsValidator]
        public partial class @void : IValidateOptions<KeywordNames.@class>
        {
        }
    }
}

namespace @struct.@interface
{
    public class @sealed
    {
        [Required]
        [MinLength(5)]
        public string? @string { get; set; }
    }

    public class SecondModel
    {
        [Required]
        [MinLength(5)]
        public string? @public { get; set; }

        [ValidateObjectMembers]
        public @sealed? @return { get; set; }
    }

    [OptionsValidator]
    public partial class SecondValidator : IValidateOptions<SecondModel>
    {
    }
}
