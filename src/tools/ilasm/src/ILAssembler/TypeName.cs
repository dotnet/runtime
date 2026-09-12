// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILAssembler
{
    internal sealed record TypeName(TypeName? ContainingTypeName, string DottedName);
}
