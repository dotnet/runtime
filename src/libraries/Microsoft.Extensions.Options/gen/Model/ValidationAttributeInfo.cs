// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Options.Generators
{
    internal sealed record class ValidationAttributeInfo(string AttributeName)
    {
        public List<string> ConstructorArguments { get; } = new();
        public Dictionary<string, string> Properties { get; } = new();
    }
}
