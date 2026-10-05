// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern "C"
{
    struct ContractDescriptor;
    extern ContractDescriptor DNMDContractDescriptor;
    ContractDescriptor* g_dnmdContractDescriptor = &DNMDContractDescriptor;
}
