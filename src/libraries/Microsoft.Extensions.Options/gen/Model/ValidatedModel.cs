// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Options.Generators
{
    internal sealed record class ValidatedModel(
        string Name,
        string SimpleName,
        bool SelfValidates,
        List<ValidatedMember> MembersToValidate);
}
