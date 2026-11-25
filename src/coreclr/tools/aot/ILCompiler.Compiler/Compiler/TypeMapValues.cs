// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

using ILCompiler.DependencyAnalysis;
using Internal.IL;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// What the entry of a type map group stores for a typed value flavor (see ITypeMapValue).
    /// The numeric values are encoded in the entries of the image.
    /// </summary>
    internal enum TypeMapValueKind : uint
    {
        /// <summary>The data was computed at compile time and is part of the image.</summary>
        Data = 0,

        /// <summary>The getter is not evaluated at compile time (it can not be), the entry points to a stub that calls it.</summary>
        Getter = 1,

        /// <summary>The mapped type does not implement ITypeMapValue for the value type.</summary>
        NotImplemented = 2,
    }

    /// <summary>
    /// Describes the value of a type map entry for a typed value flavor: the data of the getter of the mapped type evaluated at compile time,
    /// a stub that calls the getter, or nothing (the mapped type does not provide the value).
    /// </summary>
    internal sealed class TypeMapValueEvaluation
    {
        private TypeMapValueEvaluation(TypeDesc target, TypeDesc valueType, TypeMapValueKind kind)
        {
            Target = target;
            ValueType = valueType;
            Kind = kind;
        }

        public TypeDesc Target { get; }
        public TypeDesc ValueType { get; }
        public TypeMapValueKind Kind { get; }

        /// <summary>The data of the value (<see cref="TypeMapValueKind.Data"/>).</summary>
        public TypePreinit.ISerializableValue Data { get; private init; }

        /// <summary>The stub that returns the value (<see cref="TypeMapValueKind.Getter"/>).</summary>
        public MethodDesc Stub { get; private init; }

        /// <summary>Why the compiler could not evaluate the getter (<see cref="TypeMapValueKind.Getter"/>).</summary>
        public string FallbackReason { get; private init; }

        public static TypeMapValueEvaluation ForData(TypeDesc target, TypeDesc valueType, TypePreinit.ISerializableValue data)
            => new TypeMapValueEvaluation(target, valueType, TypeMapValueKind.Data) { Data = data };

        public static TypeMapValueEvaluation ForGetter(TypeDesc target, TypeDesc valueType, MethodDesc stub, string reason)
            => new TypeMapValueEvaluation(target, valueType, TypeMapValueKind.Getter) { Stub = stub, FallbackReason = reason };

        public static TypeMapValueEvaluation ForNotImplemented(TypeDesc target, TypeDesc valueType)
            => new TypeMapValueEvaluation(target, valueType, TypeMapValueKind.NotImplemented);

        /// <summary>
        /// The node the entry needs to be in the image (null if there is none). The entries of a typed value flavor are only part of the image if it is.
        /// </summary>
        public object GetDependencyNode(NodeFactory factory, TypeDesc typeMapGroup, bool isProxy)
            => Kind switch
            {
                TypeMapValueKind.Data => factory.TypeMapValueData(typeMapGroup, isProxy, this),
                TypeMapValueKind.Getter => factory.MethodEntrypoint(Stub),
                _ => null,
            };
    }

    /// <summary>
    /// A typed value flavor of a type map group: the value type and a way to get the value of the entries.
    /// </summary>
    internal sealed class TypeMapValueFlavor
    {
        private readonly Func<TypeDesc, TypeMapValueEvaluation> _evaluate;

        public TypeMapValueFlavor(TypeDesc valueType, Func<TypeDesc, TypeMapValueEvaluation> evaluate)
        {
            ValueType = valueType;
            _evaluate = evaluate;
        }

        public TypeDesc ValueType { get; }

        public TypeMapValueEvaluation Evaluate(TypeDesc target) => _evaluate(target);
    }

    internal static class TypeMapValueSupport
    {
        /// <summary>
        /// The number of low bits of the data of an entry for a typed value flavor that hold its <see cref="TypeMapValueKind"/>,
        /// the rest is the index. The runtime reader (TypeMapLazyDictionary) has the same encoding.
        /// </summary>
        private const int ValueKindBits = 2;

        /// <summary>
        /// Computes the value of the getter of <paramref name="target"/> for the value type, at compile time if possible.
        /// </summary>
        public static TypeMapValueEvaluation Evaluate(NodeFactory factory, TypeDesc target, TypeDesc valueType)
        {
            CompilerTypeSystemContext context = factory.TypeSystemContext;
            DefType valueInterface = ((MetadataType)context.SystemModule.GetKnownType("System.Runtime.InteropServices"u8, "ITypeMapValue`1"u8))
                .MakeInstantiatedType(valueType);

            string reason;
            try
            {
                if (!ImplementsValueInterface(target, valueInterface))
                {
                    // The runtime reports that a type that doesn't provide the value does not provide it.
                    return TypeMapValueEvaluation.ForNotImplemented(target, valueType);
                }

                MethodDesc interfaceGetter = valueInterface.GetKnownMethod("get_Value"u8, null);
                DefaultInterfaceMethodResolution staticResolution = default;
                MethodDesc getter = target.GetClosestDefType().TryResolveConstraintMethodApprox(valueInterface, interfaceGetter, out bool forceUseRuntimeLookup, ref staticResolution);

                if (getter is null || forceUseRuntimeLookup)
                {
                    reason = "The implementation of the getter could not be resolved";
                }
                else
                {
                    TypePreinit.GetterEvaluationResult result = factory.PreinitializationManager.ScanGetter(getter);
                    if (!result.IsSuccessful)
                    {
                        reason = result.FailureReason;
                    }
                    else if (result.StaticField is { OwningType: MetadataType staticOwner } && !factory.PreinitializationManager.IsPreinitialized(staticOwner))
                    {
                        // The object is made by code that runs when the program does, and a table can't refer to it.
                        reason = $"The result is the object of a static of '{staticOwner}', whose static constructor is not preinitialized";
                    }
                    else
                    {
                        return TypeMapValueEvaluation.ForData(target, valueType, result.Value);
                    }
                }
            }
            catch (TypeSystemException)
            {
                // The type can't be loaded: the entry is not usable, the same as a type that doesn't provide the value.
                return TypeMapValueEvaluation.ForNotImplemented(target, valueType);
            }

            return TypeMapValueEvaluation.ForGetter(target, valueType, context.GetTypeMapGetValueStub(target, valueType), reason);
        }

        private static bool ImplementsValueInterface(TypeDesc target, DefType valueInterface)
        {
            return target.IsDefType
                && !target.IsInterface
                && !target.IsGenericDefinition
                && !target.ContainsSignatureVariables()
                && !target.IsCanonicalSubtype(CanonicalFormKind.Any)
                && target.CanCastTo(valueInterface);
        }

        /// <summary>
        /// Encodes the key of an external type map entry, followed by the mapped type if the flavors have it. The typed values are appended by <see cref="AppendEntryData"/>.
        /// </summary>
        public static Vertex CreateExternalEntry(NativeWriter writer, INativeFormatTypeReferenceProvider externalReferences, TypeMapFlavors flavors, string key, TypeDesc target)
        {
            Vertex keyVertex = writer.GetStringConstant(key);

            return (flavors & TypeMapFlavors.Types) != 0
                ? writer.GetTuple(keyVertex, externalReferences.EncodeReferenceToType(writer, target, null))
                : keyVertex;
        }

        /// <summary>
        /// Encodes the key of a proxy type map entry, followed by the mapped type if the flavors have it. The typed values are appended by <see cref="AppendEntryData"/>.
        /// </summary>
        public static Vertex CreateProxyEntry(NativeWriter writer, INativeFormatTypeReferenceProvider externalReferences, TypeMapFlavors flavors, TypeDesc key, TypeDesc value)
        {
            Vertex keyVertex = externalReferences.EncodeReferenceToType(writer, key, null);

            return (flavors & TypeMapFlavors.Types) != 0
                ? writer.GetTuple(keyVertex, externalReferences.EncodeReferenceToType(writer, value, null))
                : keyVertex;
        }

        /// <summary>
        /// Encodes the data of the entry for each of the value flavors, after the key and the other flavors.
        /// </summary>
        public static Vertex AppendEntryData(
            NodeFactory factory,
            NativeWriter writer,
            Vertex entry,
            IReadOnlyList<TypeMapValueFlavor> valueFlavors,
            IReadOnlyList<Dictionary<TypeDesc, int>> ranks,
            TypeDesc target)
        {
            for (int i = 0; i < valueFlavors.Count; i++)
            {
                TypeMapValueEvaluation evaluation = valueFlavors[i].Evaluate(target);
                uint data;
                switch (evaluation.Kind)
                {
                    case TypeMapValueKind.Data:
                        data = ((uint)ranks[i][target] << ValueKindBits) | (uint)TypeMapValueKind.Data;
                        break;
                    case TypeMapValueKind.Getter:
                        data = (factory.GetCommonFixupIndex(factory.MethodEntrypoint(evaluation.Stub)) << ValueKindBits) | (uint)TypeMapValueKind.Getter;
                        break;
                    default:
                        data = (uint)TypeMapValueKind.NotImplemented;
                        break;
                }

                entry = writer.GetTuple(entry, writer.GetUnsignedConstant(data));
            }

            return entry;
        }

        /// <summary>
        /// The position of the data of each target in the data table of each value flavor.
        /// </summary>
        public static Dictionary<TypeDesc, int>[] GetRanks(NodeFactory factory, TypeDesc typeMapGroup, bool isProxy, IReadOnlyList<TypeMapValueFlavor> valueFlavors)
        {
            var ranks = new Dictionary<TypeDesc, int>[valueFlavors.Count];
            for (int i = 0; i < ranks.Length; i++)
            {
                ranks[i] = new Dictionary<TypeDesc, int>();
                TypeMapValueArrayNode array = factory.TypeMapValueArray(typeMapGroup, isProxy, valueFlavors[i].ValueType);
                int rank = 0;
                foreach (TypeMapValueDataNode node in array.GetSortedNodes())
                {
                    ranks[i].Add(node.Target, rank++);
                }
            }

            return ranks;
        }

        /// <summary>
        /// Creates the header of a group: the group type, the state, then if there are value flavors the description of the flavors,
        /// then the hash table.
        /// </summary>
        public static Vertex CreateGroupHeader(
            NodeFactory factory,
            NativeWriter writer,
            INativeFormatTypeReferenceProvider externalReferences,
            TypeDesc typeMapGroup,
            bool isProxy,
            TypeMapFlavors flavors,
            IReadOnlyList<TypeMapValueFlavor> valueFlavors,
            Vertex hashTable)
        {
            Vertex typeMapStateVertex = writer.GetUnsignedConstant((uint)flavors);
            Vertex typeMapGroupVertex = externalReferences.EncodeReferenceToType(writer, typeMapGroup, null);

            if (valueFlavors is null || valueFlavors.Count == 0)
            {
                Debug.Assert((flavors & TypeMapFlavors.Values) == 0);
                return writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, hashTable);
            }

            Debug.Assert((flavors & TypeMapFlavors.Values) != 0);

            Vertex header = writer.GetTuple(typeMapGroupVertex, typeMapStateVertex);
            header = writer.GetTuple(header, writer.GetUnsignedConstant((uint)valueFlavors.Count));
            foreach (TypeMapValueFlavor flavor in valueFlavors)
            {
                header = writer.GetTuple(header, externalReferences.EncodeReferenceToType(writer, flavor.ValueType, null));

                // The index of the data table + 1, 0 if the flavor has no data.
                TypeMapValueArrayNode array = factory.TypeMapValueArray(typeMapGroup, isProxy, flavor.ValueType);
                uint tableIndex = array.ShouldSkipEmittingObjectNode(factory) ? 0 : factory.GetCommonFixupIndex(array) + 1;
                header = writer.GetTuple(header, writer.GetUnsignedConstant(tableIndex));
            }

            return writer.GetTuple(header, hashTable);
        }
    }
}
