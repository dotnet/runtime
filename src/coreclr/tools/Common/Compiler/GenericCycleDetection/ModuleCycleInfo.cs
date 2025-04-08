// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

#if !READYTORUN
using ILLink.Shared;
#endif

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    internal static partial class LazyGenericsSupport
    {
        private sealed class ModuleCycleInfo
        {
            private readonly HashSet<TypeSystemEntity> _entitiesInCycles;

            public IEnumerable<TypeSystemEntity> EntitiesInCycles => _entitiesInCycles;

            public EcmaModule Module { get; }

            public ModuleCycleInfo(EcmaModule module, HashSet<TypeSystemEntity> entitiesInCycles)
            {
                Module = module;
                _entitiesInCycles = entitiesInCycles;
            }

            public bool FormsCycle(TypeSystemEntity owner)
            {
                Debug.Assert(owner is EcmaMethod || owner is EcmaType);
                TypeDesc ownerType = (owner as EcmaMethod)?.OwningType;
                return _entitiesInCycles.Contains(owner) || (ownerType != null && _entitiesInCycles.Contains(ownerType));
            }
        }

        private sealed class CycleInfoHashtable : LockFreeReaderHashtable<EcmaModule, ModuleCycleInfo>
        {
            protected override bool CompareKeyToValue(EcmaModule key, ModuleCycleInfo value) => key == value.Module;
            protected override bool CompareValueToValue(ModuleCycleInfo value1, ModuleCycleInfo value2) => value1.Module == value2.Module;
            protected override int GetKeyHashCode(EcmaModule key) => key.GetHashCode();
            protected override int GetValueHashCode(ModuleCycleInfo value) => value.Module.GetHashCode();

            protected override ModuleCycleInfo CreateValueFromKey(EcmaModule key)
            {
                GraphBuilder gb = new GraphBuilder(key);
                Graph<EcmaGenericParameter> graph = gb.Graph;

                var formalsNeedingLazyGenerics = graph.ComputeVerticesInvolvedInAFlaggedCycle();
                var entitiesNeedingLazyGenerics = new HashSet<TypeSystemEntity>();

                foreach (EcmaGenericParameter formal in formalsNeedingLazyGenerics)
                {
                    var formalDefinition = key.MetadataReader.GetGenericParameter(formal.Handle);
                    if (formal.Kind == GenericParameterKind.Type)
                    {
                        entitiesNeedingLazyGenerics.Add(key.GetType(formalDefinition.Parent));
                    }
                    else
                    {
                        entitiesNeedingLazyGenerics.Add(key.GetMethod(formalDefinition.Parent));
                    }
                }

                return new ModuleCycleInfo(key, entitiesNeedingLazyGenerics);
            }
        }

        internal static bool CheckForECMAIllegalGenericRecursion(EcmaType type)
        {
            GraphBuilder gb = new GraphBuilder(type);
            Graph<EcmaGenericParameter> graph = gb.Graph;

            var flaggedCycleData = graph.ComputeVerticesInvolvedInAFlaggedCycle();

            foreach (var _ in flaggedCycleData)
            {
                // If the list isn't empty, there is an illegal generic recursion
                return true;
            }

            return false;
        }

        internal sealed class GenericCycleDetector
        {
            private readonly CycleInfoHashtable _hashtable = new CycleInfoHashtable();

            private readonly struct EntityPair : IEquatable<EntityPair>
            {
                public readonly TypeSystemEntity Owner;
                public readonly TypeSystemEntity Referent;
                public EntityPair(TypeSystemEntity owner, TypeSystemEntity referent)
                    => (Owner, Referent) = (owner, referent);
                public bool Equals(EntityPair other) => Owner == other.Owner && Referent == other.Referent;
                public override bool Equals(object obj) => obj is EntityPair p && Equals(p);
                public override int GetHashCode() => HashCode.Combine(Owner.GetHashCode(), Referent.GetHashCode());
            }

            // This is a set of entities that had actual problems that caused us to abort compilation
            // somewhere.
            // Would prefer this to be a ConcurrentHashSet but there isn't any. ModuleCycleInfo can be looked up
            // from the key, but since this is a key/value pair, might as well use the value too...
            private readonly ConcurrentDictionary<EntityPair, ModuleCycleInfo> _actualProblems = new ConcurrentDictionary<EntityPair, ModuleCycleInfo>();

            private readonly int _depthCutoff;
            private readonly int _breadthCutoff;

            public GenericCycleDetector(int depthCutoff, int breadthCutoff)
            {
                _depthCutoff = depthCutoff;
                _breadthCutoff = breadthCutoff;
            }

            private bool IsDeepPossiblyCyclicInstantiation(TypeSystemEntity entity)
            {
                if (entity is TypeDesc type)
                {
                    return IsDeepPossiblyCyclicInstantiation(type);
                }
                else
                {
                    return IsDeepPossiblyCyclicInstantiation((MethodDesc)entity);
                }
            }

            private bool IsDeepPossiblyCyclicInstantiation(TypeDesc type)
            {
                int breadthCounter = 0;
                return IsDeepPossiblyCyclicInstantiation(type, ref breadthCounter, seenTypes: null);
            }

            private bool IsDeepPossiblyCyclicInstantiation(TypeDesc type, ref int breadthCounter, List<TypeDesc> seenTypes = null)
            {
                switch (type.Category)
                {
                    case TypeFlags.Array:
                    case TypeFlags.SzArray:
                        return IsDeepPossiblyCyclicInstantiation(((ParameterizedType)type).ParameterType, ref breadthCounter, seenTypes);
                    default:
                        TypeDesc typeDef = type.GetTypeDefinition();
                        if (type != typeDef)
                        {
                            if (FormsCycle(typeDef, out ModuleCycleInfo _))
                            {
                                if (_breadthCutoff >= 0 && ++breadthCounter >= _breadthCutoff)
                                {
                                    return true;
                                }
                            }

                            (seenTypes ??= new List<TypeDesc>()).Add(typeDef);
                            for (int i = 0; i < seenTypes.Count; i++)
                            {
                                TypeDesc typeToFind = seenTypes[i];
                                int count = 1;
                                for (int j = i + 1; j < seenTypes.Count; j++)
                                {
                                    if (seenTypes[j] == typeToFind)
                                    {
                                        count++;
                                    }

                                    if (count > _depthCutoff)
                                    {
                                        return true;
                                    }
                                }
                            }

                            bool result = IsDeepPossiblyCyclicInstantiation(type.Instantiation, ref breadthCounter, seenTypes);
                            seenTypes.RemoveAt(seenTypes.Count - 1);
                            return result;
                        }
                        return false;
                }
            }

            private bool IsDeepPossiblyCyclicInstantiation(Instantiation instantiation, ref int breadthCounter, List<TypeDesc> seenTypes)
            {
                foreach (TypeDesc arg in instantiation)
                {
                    if (IsDeepPossiblyCyclicInstantiation(arg, ref breadthCounter, seenTypes))
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool IsDeepPossiblyCyclicInstantiation(MethodDesc method)
            {
                int breadthCounter = 0;
                return IsDeepPossiblyCyclicInstantiation(method.Instantiation, ref breadthCounter, seenTypes: null)
                    || IsDeepPossiblyCyclicInstantiation(method.OwningType, ref breadthCounter, seenTypes: null);
            }

            private bool FormsCycle(TypeSystemEntity entity, out ModuleCycleInfo cycleInfo)
            {
                EcmaModule ownerModule = (entity as EcmaType)?.EcmaModule ?? (entity as EcmaMethod)?.Module;
                if (ownerModule != null)
                {
                    cycleInfo = _hashtable.GetOrCreateValue(ownerModule);
                    return cycleInfo.FormsCycle(entity);
                }
                else
                {
                    cycleInfo = null;
                    return false;
                }
            }

            public void DetectCycle(TypeSystemEntity owner, TypeSystemEntity referent)
            {
                // This allows to disable cycle detection completely (typically for perf reasons as the algorithm is pretty slow)
                if (_depthCutoff < 0)
                    return;

                // Fields don't introduce more genericness than their owning type, so treat as their owning type
                if (referent is FieldDesc referentField)
                {
                    referent = referentField.OwningType;
                }

                var ownerType = owner as TypeDesc;
                var ownerMethod = owner as MethodDesc;
                var ownerDefinition = ownerType?.GetTypeDefinition() ?? (TypeSystemEntity)ownerMethod.GetTypicalMethodDefinition();
                var referentType = referent as TypeDesc;
                var referentMethod = referent as MethodDesc;
                var referentDefinition = referentType?.GetTypeDefinition() ?? (TypeSystemEntity)referentMethod.GetTypicalMethodDefinition();

                // We don't track cycles in non-ecma entities.
                if ((referentDefinition is not EcmaMethod && referentDefinition is not EcmaType)
                    || (ownerDefinition is not EcmaMethod && ownerDefinition is not EcmaType))
                {
                    return;
                }

                if (FormsCycle(ownerDefinition, out ModuleCycleInfo cycleInfo))
                {
                    // Just the presence of a cycle is not a problem, but once we start getting too deep,
                    // we need to cut our losses.
                    if (IsDeepPossiblyCyclicInstantiation(referent))
                    {
                        _actualProblems.TryAdd(new EntityPair(owner, referent), cycleInfo);

                        if (referentType != null)
                        {
                            // TODO: better exception string ID?
                            ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, referentType);
                        }
                        else
                        {
                            // TODO: better exception string ID?
                            ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, referentMethod);
                        }
                    }
                }
            }

#if !READYTORUN
            public void LogWarnings(Logger logger)
            {
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
#endif
        }
    }
}
