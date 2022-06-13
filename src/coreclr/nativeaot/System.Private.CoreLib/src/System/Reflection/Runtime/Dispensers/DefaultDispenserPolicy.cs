// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Reflection.Runtime.Dispensers
{
    //
    // For now, this is the dispenser policy used inside S.R.R.
    //
    internal sealed class DefaultDispenserPolicy : DispenserPolicy
    {
        public sealed override DispenserAlgorithm GetAlgorithm(DispenserScenario scenario)
        {
            switch (scenario)
            {
                // Assembly + NamespaceTypeName to Type
                case DispenserScenario.AssemblyAndNamespaceTypeName_Type:
                    return DispenserAlgorithm.ReuseAsLongAsValueIsAlive;

                // Assembly refName to Assembly
                case DispenserScenario.AssemblyRefName_Assembly:
                    return DispenserAlgorithm.ReuseAsLongAsValueIsAlive;

                // RuntimeAssembly to CaseInsensitiveTypeDictionary
                case DispenserScenario.RuntimeAssembly_CaseInsensitiveTypeDictionary:
                    return DispenserAlgorithm.ReuseAlways;

                // Scope definition handle to RuntimeAssembly
                case DispenserScenario.Scope_Assembly:
                    return DispenserAlgorithm.ReuseAlways; // Match policy used for runtime Assembly instances in other runtime flavors.

                default:
                    return DispenserAlgorithm.CreateAlways;
            }
        }
    }
}
