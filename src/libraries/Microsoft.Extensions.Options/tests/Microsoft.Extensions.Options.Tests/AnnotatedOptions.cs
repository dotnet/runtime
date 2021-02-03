// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System;

namespace Microsoft.Extensions.Options.Tests
{
    public class AnnotatedOptions
    {
        [Required]
        public string Required { get; set; }

        [StringLength(5, ErrorMessage = "Too long.")]
        public string StringLength { get; set; }

        [Range(-5, 5, ErrorMessage = "Out of range.")]
        public int IntRange { get; set; }

        [From(Accepted = "USA")]
        public string Custom { get; set; }

        [DepValidator(Target = "Dep2")]
        public string Dep1 { get; set; }
        public string Dep2 { get; set; }
    }
}
