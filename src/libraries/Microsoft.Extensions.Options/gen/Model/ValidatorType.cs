// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Options.Generators
{
    internal sealed record class ValidatorType(
        string Namespace,
        string Name,
        string NameWithoutGenerics,
        string DeclarationKeyword,
        List<string> ParentTypes,
        bool IsSynthetic,
        IList<ValidatedModel> ModelsToValidate);
}
