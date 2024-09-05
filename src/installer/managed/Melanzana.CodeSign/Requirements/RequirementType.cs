// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.CodeSign.Requirements
{
    public enum RequirementType : uint
    {
        Host = 1u,
        Guest,
        Designated,
        Library,
        Plugin,
        Invalid
    }
}
