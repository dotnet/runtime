// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader;

namespace Microsoft.DotNet.Diagnostics.CdacDumpInspect;

internal sealed record ParsedDescriptor(
    string Name,
    ulong Address,
    ContractDescriptorParser.ContractDescriptor Descriptor,
    ulong[] PointerData);
