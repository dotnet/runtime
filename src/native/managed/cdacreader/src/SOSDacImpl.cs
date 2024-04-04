// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader;

[GeneratedComInterface]
[Guid("4eca42d8-7e7b-4c8a-a116-7bfbf6929267")]
internal partial interface ISOSDacInterface9
{
    int GetBreakingChangeVersion();
}

/// <summary>
/// Implementation of ISOSDacInterface* interfaces intended to be passed out to consumers
/// interacting with the DAC via those COM interfaces.
/// </summary>
[GeneratedComClass]
internal sealed partial class SOSDacImpl : ISOSDacInterface9
{
    private readonly Target _target;

    public SOSDacImpl(Target target)
    {
        _target = target;
    }

    public int GetBreakingChangeVersion()
    {
        // TODO: Return non-hard-coded version
        return 4;
    }
}
