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

            private readonly int _cutoffPoint;

            public GenericCycleDetector(int cutoffPoint)
            {
                _cutoffPoint = cutoffPoint;
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

            private bool IsDeepPossiblyCyclicInstantiation(TypeDesc type, List<TypeDesc> seenTypes = null)
            {
                switch (type.Category)
                {
                    case TypeFlags.Array:
                    case TypeFlags.SzArray:
                        return IsDeepPossiblyCyclicInstantiation(((ParameterizedType)type).ParameterType, seenTypes);
                    default:
                        TypeDesc typeDef = type.GetTypeDefinition();
                        if (type != typeDef)
                        {
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

                                    if (count > _cutoffPoint)
                                    {
                                        return true;
                                    }
                                }
                            }

                            bool result = IsDeepPossiblyCyclicInstantiation(type.Instantiation, seenTypes);
                            seenTypes.RemoveAt(seenTypes.Count - 1);
                            return result;
                        }
                        return false;
                }
            }

            private bool IsDeepPossiblyCyclicInstantiation(Instantiation instantiation, List<TypeDesc> seenTypes = null)
            {
                foreach (TypeDesc arg in instantiation)
                {
                    if (IsDeepPossiblyCyclicInstantiation(arg, seenTypes))
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool IsDeepPossiblyCyclicInstantiation(MethodDesc method)
            {
                return IsDeepPossiblyCyclicInstantiation(method.Instantiation) || IsDeepPossiblyCyclicInstantiation(method.OwningType);
            }

            public void DetectCycle(TypeSystemEntity owner, TypeSystemEntity referent)
            {
                // Not clear if generic recursion through fields is a thing
                if (referent is FieldDesc)
                {
                    return;
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

                EcmaModule ownerModule = (ownerDefinition as EcmaType)?.EcmaModule ?? ((EcmaMethod)ownerDefinition).Module;

                ModuleCycleInfo cycleInfo = _hashtable.GetOrCreateValue(ownerModule);
                if (cycleInfo.FormsCycle(ownerDefinition))
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
        }
    }
}
