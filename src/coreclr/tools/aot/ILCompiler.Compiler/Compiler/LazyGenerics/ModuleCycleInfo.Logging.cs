// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILLink.Shared;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    internal static partial class LazyGenericsSupport
    {
        internal partial class GenericCycleDetector
        {
            public void LogWarnings(Logger logger)
            {
                var problems = new List<KeyValuePair<EntityPair, ModuleCycleInfo>>(_actualProblems);

                // Might need to sort these if we care about warning determinism, but we probably don't.

                var reportedProblems = new HashSet<EntityPair>();

                foreach (var actualProblem in _actualProblems)
                {
                    TypeSystemEntity referent = actualProblem.Key.Referent;
                    TypeSystemEntity owner = actualProblem.Key.Owner;

                    TypeSystemEntity referentDefinition = referent is TypeDesc referentType ? referentType.GetTypeDefinition()
                        : ((MethodDesc)referent).GetTypicalMethodDefinition();
                    TypeSystemEntity ownerDefinition = owner is TypeDesc ownerType ? ownerType.GetTypeDefinition()
                        : ((MethodDesc)owner).GetTypicalMethodDefinition();

                    if (!reportedProblems.Add(new EntityPair(ownerDefinition, referentDefinition)))
                        continue;

                    ModuleCycleInfo cycleInfo = actualProblem.Value;
                    bool first = true;
                    string message = "";
                    foreach (TypeSystemEntity cycleEntity in cycleInfo.EntitiesInCycles)
                    {
                        if (!first)
                            message += ", ";

                        first = false;

                        message += $"'{cycleEntity.GetDisplayName()}'";
                    }

                    logger.LogWarning(actualProblem.Key.Owner, DiagnosticId.GenericRecursionCycle, actualProblem.Key.Referent.GetDisplayName(), message);
                }
            }
        }
    }
}
