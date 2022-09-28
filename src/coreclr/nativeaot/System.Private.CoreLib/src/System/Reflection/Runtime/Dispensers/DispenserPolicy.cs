// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Reflection.Runtime.Dispensers
{
    //
    // Base class for a policy that maps dispense scenarios to the caching algorithm used.
    //
    internal abstract class DispenserPolicy
    {
        public abstract DispenserAlgorithm GetAlgorithm(DispenserScenario scenario);
    }
}
