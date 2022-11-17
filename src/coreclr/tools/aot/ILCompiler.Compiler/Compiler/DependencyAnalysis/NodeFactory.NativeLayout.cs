// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Internal.Text;
using Internal.TypeSystem;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// Part of Node factory that deals with nodes describing native layout information
    public partial class NodeFactory
    {
        /// <summary>
        /// Helper class that provides a level of grouping for all the native layout lookups
        /// </summary>
        public class NativeLayoutHelper
        {
            private NodeFactory _factory;

            public NativeLayoutHelper(NodeFactory factory)
            {
                _factory = factory;
                CreateNodeCaches();
            }

            private void CreateNodeCaches()
            {
                _typeSignatures = new NodeCache<TypeDesc, NativeLayoutTypeSignatureVertexNode>(type =>
                {
                    return NativeLayoutTypeSignatureVertexNode.NewTypeSignatureVertexNode(_factory, type);
                });

                _methodSignatures = new NodeCache<MethodSignature, NativeLayoutMethodSignatureVertexNode>(signature =>
                {
                    return new NativeLayoutMethodSignatureVertexNode(_factory, signature);
                });

                _callingConventionSlots = new NodeCache<CallingConventionConverterKey, NativeLayoutCallingConventionConverterGenericDictionarySlotNode>(key =>
                {
                    return new NativeLayoutCallingConventionConverterGenericDictionarySlotNode(key.Signature, key.ConverterKind);
                });

                _methodNameAndSignatures = new NodeCache<MethodDesc, NativeLayoutMethodNameAndSignatureVertexNode>(method =>
                {
                    return new NativeLayoutMethodNameAndSignatureVertexNode(_factory, method);
                });

                _placedSignatures = new NodeCache<NativeLayoutVertexNode, NativeLayoutPlacedSignatureVertexNode>(vertexNode =>
                {
                    return new NativeLayoutPlacedSignatureVertexNode(vertexNode);
                });

                _placedVertexSequence = new NodeCache<VertexSequenceKey, NativeLayoutPlacedVertexSequenceVertexNode>(vertices =>
                {
                    return new NativeLayoutPlacedVertexSequenceVertexNode(vertices.Vertices);
                });

                _placedUIntVertexSequence = new NodeCache<List<uint>, NativeLayoutPlacedVertexSequenceOfUIntVertexNode>(uints =>
                {
                    return new NativeLayoutPlacedVertexSequenceOfUIntVertexNode(uints);
                }, new UIntSequenceComparer());

                _methodLdTokenSignatures = new NodeCache<MethodDesc, NativeLayoutMethodLdTokenVertexNode>(method =>
                {
                    return new NativeLayoutMethodLdTokenVertexNode(_factory, method);
                });

                _fieldLdTokenSignatures = new NodeCache<FieldDesc, NativeLayoutFieldLdTokenVertexNode>(field =>
                {
                    return new NativeLayoutFieldLdTokenVertexNode(_factory, field);
                });

                _nativeLayoutSignatureNodes = new NodeCache<NativeLayoutSignatureKey, NativeLayoutSignatureNode>(key =>
                {
                    return new NativeLayoutSignatureNode(key.SignatureVertex, key.Identity, key.IdentityPrefix);
                });

                _templateMethodEntries = new NodeCache<MethodDesc, NativeLayoutTemplateMethodSignatureVertexNode>(method =>
                {
                    return new NativeLayoutTemplateMethodSignatureVertexNode(_factory, method);
                });

                _templateMethodLayouts = new NodeCache<MethodDesc, NativeLayoutTemplateMethodLayoutVertexNode>(method =>
                {
                    return new NativeLayoutTemplateMethodLayoutVertexNode(_factory, method);
                });

                _templateTypeLayouts = new NodeCache<TypeDesc, NativeLayoutTemplateTypeLayoutVertexNode>(type =>
                {
                    return new NativeLayoutTemplateTypeLayoutVertexNode(_factory, type);
                });

                _typeHandle_GenericDictionarySlots = new NodeCache<TypeDesc, NativeLayoutTypeHandleGenericDictionarySlotNode>(type =>
                {
                    return new NativeLayoutTypeHandleGenericDictionarySlotNode(_factory, type);
                });

                _gcStatic_GenericDictionarySlots = new NodeCache<TypeDesc, NativeLayoutGcStaticsGenericDictionarySlotNode>(type =>
                {
                    return new NativeLayoutGcStaticsGenericDictionarySlotNode(_factory, type);
                });

                _nonGcStatic_GenericDictionarySlots = new NodeCache<TypeDesc, NativeLayoutNonGcStaticsGenericDictionarySlotNode>(type =>
                {
                    return new NativeLayoutNonGcStaticsGenericDictionarySlotNode(_factory, type);
                });

                _unwrapNullable_GenericDictionarySlots = new NodeCache<TypeDesc, NativeLayoutUnwrapNullableGenericDictionarySlotNode>(type =>
                {
                    return new NativeLayoutUnwrapNullableGenericDictionarySlotNode(_factory, type);
                });

                _typeSize_GenericDictionarySlots = new NodeCache<TypeDesc, NativeLayoutTypeSizeGenericDictionarySlotNode>(type =>
                {
                    return new NativeLayoutTypeSizeGenericDictionarySlotNode(_factory, type);
                });

                _allocateObject_GenericDictionarySlots = new NodeCache<TypeDesc, NativeLayoutAllocateObjectGenericDictionarySlotNode>(type =>
                {
                    return new NativeLayoutAllocateObjectGenericDictionarySlotNode(_factory, type);
                });

                _castClass_GenericDictionarySlots = new NodeCache<TypeDesc, NativeLayoutCastClassGenericDictionarySlotNode>(type =>
                {
                    return new NativeLayoutCastClassGenericDictionarySlotNode(_factory, type);
                });

                _isInst_GenericDictionarySlots = new NodeCache<TypeDesc, NativeLayoutIsInstGenericDictionarySlotNode>(type =>
                {
                    return new NativeLayoutIsInstGenericDictionarySlotNode(_factory, type);
                });

                _threadStaticIndex_GenericDictionarySlots = new NodeCache<TypeDesc, NativeLayoutThreadStaticBaseIndexDictionarySlotNode>(type =>
                {
                    return new NativeLayoutThreadStaticBaseIndexDictionarySlotNode(_factory, type);
                });

                _defaultConstructor_GenericDictionarySlots = new NodeCache<TypeDesc, NativeLayoutDefaultConstructorGenericDictionarySlotNode>(type =>
                {
                    return new NativeLayoutDefaultConstructorGenericDictionarySlotNode(_factory, type);
                });

                _allocateArray_GenericDictionarySlots = new NodeCache<TypeDesc, NativeLayoutAllocateArrayGenericDictionarySlotNode>(type =>
                {
                    return new NativeLayoutAllocateArrayGenericDictionarySlotNode(_factory, type);
                });

                _interfaceCell_GenericDictionarySlots = new NodeCache<MethodDesc, NativeLayoutInterfaceDispatchGenericDictionarySlotNode>(method =>
               {
                   return new NativeLayoutInterfaceDispatchGenericDictionarySlotNode(_factory, method);
               });

                _methodDictionary_GenericDictionarySlots = new NodeCache<MethodDesc, NativeLayoutMethodDictionaryGenericDictionarySlotNode>(method =>
               {
                   return new NativeLayoutMethodDictionaryGenericDictionarySlotNode(_factory, method);
               });

                _methodEntrypoint_GenericDictionarySlots = new NodeCache<MethodEntrypointSlotKey, NativeLayoutMethodEntrypointGenericDictionarySlotNode>(key =>
               {
                   return new NativeLayoutMethodEntrypointGenericDictionarySlotNode(_factory, key.Method, key.FunctionPointerTarget, key.Unboxing);
               });

                _fieldLdToken_GenericDictionarySlots = new NodeCache<FieldDesc, NativeLayoutFieldLdTokenGenericDictionarySlotNode>(field =>
                {
                    return new NativeLayoutFieldLdTokenGenericDictionarySlotNode(field);
                });

                _methodLdToken_GenericDictionarySlots = new NodeCache<MethodDesc, NativeLayoutMethodLdTokenGenericDictionarySlotNode>(method =>
                {
                    return new NativeLayoutMethodLdTokenGenericDictionarySlotNode(method);
                });

                _fieldOffset_GenericDictionaryslots = new NodeCache<FieldDesc, NativeLayoutFieldOffsetGenericDictionarySlotNode>(field =>
                {
                    return new NativeLayoutFieldOffsetGenericDictionarySlotNode(field);
                });

                _vtableOffset_GenericDictionaryslots = new NodeCache<MethodDesc, NativeLayoutVTableOffsetGenericDictionarySlotNode>(method =>
                {
                    return new NativeLayoutVTableOffsetGenericDictionarySlotNode(method);
                });

                _dictionarySignatures = new NodeCache<TypeSystemEntity, NativeLayoutDictionarySignatureNode>(owningMethodOrType =>
                {
                    return new NativeLayoutDictionarySignatureNode(_factory, owningMethodOrType);
                });

                _integerSlots = new NodeCache<int, NativeLayoutIntegerDictionarySlotNode>(value =>
                {
                    return new NativeLayoutIntegerDictionarySlotNode(value);
                });

                _otherSlotPointerSlots = new NodeCache<int, NativeLayoutPointerToOtherSlotDictionarySlotNode>(otherSlotIndex =>
                {
                    return new NativeLayoutPointerToOtherSlotDictionarySlotNode(otherSlotIndex);
                });

                _constrainedMethodUseSlots = new NodeCache<ConstrainedMethodUseKey, NativeLayoutConstrainedMethodDictionarySlotNode>(constrainedMethodUse =>
                {
                    return new NativeLayoutConstrainedMethodDictionarySlotNode(constrainedMethodUse.ConstrainedMethod, constrainedMethodUse.ConstraintType, constrainedMethodUse.DirectCall);
                });
            }

            // Produce a set of dependencies that is necessary such that if this type
            // needs to be used referenced from a NativeLayout template, that the template
            // will be properly constructable.  (This is done by ensuring that all
            // canonical types in the deconstruction of the type are ConstructedEEType instead
            // of just necessary. (Which is what the actual templates signatures will ensure)
            public IEnumerable<IDependencyNode> TemplateConstructableTypes(TypeDesc type)
            {
                // Array types are the only parameterized types that have templates
                if (type.IsSzArray && !type.IsArrayTypeWithoutGenericInterfaces())
                {
                    TypeDesc arrayCanonicalType = type.ConvertToCanonForm(CanonicalFormKind.Specific);

                    // Add a dependency on the template for this type, if the canonical type should be generated into this binary.
                    if (arrayCanonicalType.IsCanonicalSubtype(CanonicalFormKind.Any) && !_factory.NecessaryTypeSymbol(arrayCanonicalType).RepresentsIndirectionCell)
                    {
                        yield return _factory.NativeLayout.TemplateTypeLayout(arrayCanonicalType);
                    }
                }

                while (type.IsParameterizedType)
                {
                    type = ((ParameterizedType)type).ParameterType;
                }

                TypeDesc canonicalType = type.ConvertToCanonForm(CanonicalFormKind.Specific);
                yield return _factory.MaximallyConstructableType(canonicalType);

                // Add a dependency on the template for this type, if the canonical type should be generated into this binary.
                if (canonicalType.IsCanonicalSubtype(CanonicalFormKind.Any) && !_factory.NecessaryTypeSymbol(canonicalType).RepresentsIndirectionCell)
                {
                    if (!_factory.TypeSystemContext.IsCanonicalDefinitionType(canonicalType, CanonicalFormKind.Any))
                        yield return _factory.NativeLayout.TemplateTypeLayout(canonicalType);
                }

                foreach (TypeDesc instantiationType in type.Instantiation)
                {
                    foreach (var dependency in TemplateConstructableTypes(instantiationType))
                        yield return dependency;
                }
            }

            // Produce a set of dependencies that is necessary such that if this type
            // needs to be used referenced from a NativeLayout template and any Universal Shared
            // instantiation is all that is needed, that the template
            // will be properly constructable.  (This is done by ensuring that all
            // canonical types in the deconstruction of the type are ConstructedEEType instead
            // of just necessary, and that the USG variant of the template is created
            // (Which is what the actual templates signatures will ensure)
            public IEnumerable<IDependencyNode> UniversalTemplateConstructableTypes(TypeDesc type)
            {
                while (type.IsParameterizedType)
                {
                    type = ((ParameterizedType)type).ParameterType;
                }

                if (type.IsSignatureVariable)
                    yield break;

                TypeDesc canonicalType = type.ConvertToCanonForm(CanonicalFormKind.Universal);
                yield return _factory.MaximallyConstructableType(canonicalType);

                foreach (TypeDesc instantiationType in type.Instantiation)
                {
                    foreach (var dependency in UniversalTemplateConstructableTypes(instantiationType))
                        yield return dependency;
                }
            }

            private NodeCache<TypeDesc, NativeLayoutTypeSignatureVertexNode> _typeSignatures;
            internal NativeLayoutTypeSignatureVertexNode TypeSignatureVertex(TypeDesc type)
            {
                if (type.IsRuntimeDeterminedType)
                {
                    GenericParameterDesc genericParameter = ((RuntimeDeterminedType)type).RuntimeDeterminedDetailsType;
                    type = _factory.TypeSystemContext.GetSignatureVariable(genericParameter.Index, method: (genericParameter.Kind == GenericParameterKind.Method));
                }

                if (type.Category == TypeFlags.FunctionPointer)
                {
                    // Pretend for now it's an IntPtr, may need to be revisited depending on https://github.com/dotnet/runtime/issues/11354
                    type = _factory.TypeSystemContext.GetWellKnownType(WellKnownType.IntPtr);
                }

                return _typeSignatures.GetOrAdd(type);
            }

            private NodeCache<MethodSignature, NativeLayoutMethodSignatureVertexNode> _methodSignatures;
            internal NativeLayoutMethodSignatureVertexNode MethodSignatureVertex(MethodSignature signature)
            {
                return _methodSignatures.GetOrAdd(signature);
            }

            private NodeCache<CallingConventionConverterKey, NativeLayoutCallingConventionConverterGenericDictionarySlotNode> _callingConventionSlots;
            internal NativeLayoutCallingConventionConverterGenericDictionarySlotNode CallingConventionConverter(CallingConventionConverterKey key)
            {
                return _callingConventionSlots.GetOrAdd(key);
            }

            private NodeCache<MethodDesc, NativeLayoutMethodNameAndSignatureVertexNode> _methodNameAndSignatures;
            internal NativeLayoutMethodNameAndSignatureVertexNode MethodNameAndSignatureVertex(MethodDesc method)
            {
                return _methodNameAndSignatures.GetOrAdd(method);
            }

            private NodeCache<NativeLayoutVertexNode, NativeLayoutPlacedSignatureVertexNode> _placedSignatures;
            internal NativeLayoutPlacedSignatureVertexNode PlacedSignatureVertex(NativeLayoutVertexNode vertexNode)
            {
                return _placedSignatures.GetOrAdd(vertexNode);
            }

            private struct VertexSequenceKey : IEquatable<VertexSequenceKey>
            {
                public readonly List<NativeLayoutVertexNode> Vertices;

                public VertexSequenceKey(List<NativeLayoutVertexNode> vertices)
                {
                    Vertices = vertices;
                }

                public override bool Equals(object obj)
                {
                    VertexSequenceKey? other = obj as VertexSequenceKey?;
                    if (other.HasValue)
                        return Equals(other.Value);
                    else
                        return false;
                }

                public bool Equals(VertexSequenceKey other)
                {
                    if (other.Vertices.Count != Vertices.Count)
                        return false;

                    for (int i = 0; i < Vertices.Count; i++)
                    {
                        if (other.Vertices[i] != Vertices[i])
                            return false;
                    }

                    return true;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static int _rotl(int value, int shift)
                {
                    // This is expected to be optimized into a single rotl instruction
                    return (int)(((uint)value << shift) | ((uint)value >> (32 - shift)));
                }

                public override int GetHashCode()
                {
                    int hashcode = 0;
                    foreach (NativeLayoutVertexNode node in Vertices)
                    {
                        hashcode ^= node.GetHashCode();
                        hashcode = _rotl(hashcode, 5);
                    }
                    return hashcode;
                }
            }

            private NodeCache<VertexSequenceKey, NativeLayoutPlacedVertexSequenceVertexNode> _placedVertexSequence;
            internal NativeLayoutPlacedVertexSequenceVertexNode PlacedVertexSequence(List<NativeLayoutVertexNode> vertices)
            {
                return _placedVertexSequence.GetOrAdd(new VertexSequenceKey(vertices));
            }

            private sealed class UIntSequenceComparer : IEqualityComparer<List<uint>>
            {
                bool IEqualityComparer<List<uint>>.Equals(List<uint> x, List<uint> y)
                {
                    if (x.Count != y.Count)
                        return false;

                    for (int i = 0; i < x.Count; i++)
                    {
                        if (x[i] != y[i])
                            return false;
                    }
                    return true;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static int _rotl(int value, int shift)
                {
                    // This is expected to be optimized into a single rotl instruction
                    return (int)(((uint)value << shift) | ((uint)value >> (32 - shift)));
                }

                int IEqualityComparer<List<uint>>.GetHashCode(List<uint> obj)
                {
                    int hashcode = 0x42284781;
                    foreach (uint u in obj)
                    {
                        hashcode ^= (int)u;
                        hashcode = _rotl(hashcode, 5);
                    }

                    return hashcode;
                }
            }
            private NodeCache<List<uint>, NativeLayoutPlacedVertexSequenceOfUIntVertexNode> _placedUIntVertexSequence;
            internal NativeLayoutPlacedVertexSequenceOfUIntVertexNode PlacedUIntVertexSequence(List<uint> uints)
            {
                return _placedUIntVertexSequence.GetOrAdd(uints);
            }

            private NodeCache<MethodDesc, NativeLayoutMethodLdTokenVertexNode> _methodLdTokenSignatures;
            internal NativeLayoutMethodLdTokenVertexNode MethodLdTokenVertex(MethodDesc method)
            {
                return _methodLdTokenSignatures.GetOrAdd(method);
            }

            private NodeCache<FieldDesc, NativeLayoutFieldLdTokenVertexNode> _fieldLdTokenSignatures;
            internal NativeLayoutFieldLdTokenVertexNode FieldLdTokenVertex(FieldDesc field)
            {
                return _fieldLdTokenSignatures.GetOrAdd(field);
            }

            private struct NativeLayoutSignatureKey : IEquatable<NativeLayoutSignatureKey>
            {
                public NativeLayoutSignatureKey(NativeLayoutSavedVertexNode signatureVertex, Utf8String identityPrefix, TypeSystemEntity identity)
                {
                    SignatureVertex = signatureVertex;
                    IdentityPrefix = identityPrefix;
                    Identity = identity;
                }

                public NativeLayoutSavedVertexNode SignatureVertex { get; }
                public Utf8String IdentityPrefix { get; }
                public TypeSystemEntity Identity { get; }

                public override bool Equals(object obj)
                {
                    if (!(obj is NativeLayoutSignatureKey))
                        return false;

                    return Equals((NativeLayoutSignatureKey)obj);
                }

                public override int GetHashCode()
                {
                    return SignatureVertex.GetHashCode();
                }

                public bool Equals(NativeLayoutSignatureKey other)
                {
                    if (SignatureVertex != other.SignatureVertex)
                        return false;

                    if (!IdentityPrefix.Equals(other.IdentityPrefix))
                        return false;

                    if (Identity != other.Identity)
                        return false;

                    return true;
                }
            }

            private NodeCache<NativeLayoutSignatureKey, NativeLayoutSignatureNode> _nativeLayoutSignatureNodes;
            public NativeLayoutSignatureNode NativeLayoutSignature(NativeLayoutSavedVertexNode signature, Utf8String identityPrefix, TypeSystemEntity identity)
            {
                return _nativeLayoutSignatureNodes.GetOrAdd(new NativeLayoutSignatureKey(signature, identityPrefix, identity));
            }

            private NodeCache<MethodDesc, NativeLayoutTemplateMethodSignatureVertexNode> _templateMethodEntries;
            internal NativeLayoutTemplateMethodSignatureVertexNode TemplateMethodEntry(MethodDesc method)
            {
                return _templateMethodEntries.GetOrAdd(method);
            }

            private NodeCache<MethodDesc, NativeLayoutTemplateMethodLayoutVertexNode> _templateMethodLayouts;
            public NativeLayoutTemplateMethodLayoutVertexNode TemplateMethodLayout(MethodDesc method)
            {
                return _templateMethodLayouts.GetOrAdd(method);
            }

            private NodeCache<TypeDesc, NativeLayoutTemplateTypeLayoutVertexNode> _templateTypeLayouts;
            public NativeLayoutTemplateTypeLayoutVertexNode TemplateTypeLayout(TypeDesc type)
            {
                return _templateTypeLayouts.GetOrAdd(GenericTypesTemplateMap.ConvertArrayOfTToRegularArray(_factory, type));
            }

            private NodeCache<TypeDesc, NativeLayoutTypeHandleGenericDictionarySlotNode> _typeHandle_GenericDictionarySlots;
            public NativeLayoutTypeHandleGenericDictionarySlotNode TypeHandleDictionarySlot(TypeDesc type)
            {
                return _typeHandle_GenericDictionarySlots.GetOrAdd(type);
            }

            private NodeCache<TypeDesc, NativeLayoutGcStaticsGenericDictionarySlotNode> _gcStatic_GenericDictionarySlots;
            public NativeLayoutGcStaticsGenericDictionarySlotNode GcStaticDictionarySlot(TypeDesc type)
            {
                return _gcStatic_GenericDictionarySlots.GetOrAdd(type);
            }

            private NodeCache<TypeDesc, NativeLayoutNonGcStaticsGenericDictionarySlotNode> _nonGcStatic_GenericDictionarySlots;
            public NativeLayoutNonGcStaticsGenericDictionarySlotNode NonGcStaticDictionarySlot(TypeDesc type)
            {
                return _nonGcStatic_GenericDictionarySlots.GetOrAdd(type);
            }

            private NodeCache<TypeDesc, NativeLayoutUnwrapNullableGenericDictionarySlotNode> _unwrapNullable_GenericDictionarySlots;
            public NativeLayoutUnwrapNullableGenericDictionarySlotNode UnwrapNullableTypeDictionarySlot(TypeDesc type)
            {
                return _unwrapNullable_GenericDictionarySlots.GetOrAdd(type);
            }

            private NodeCache<TypeDesc, NativeLayoutTypeSizeGenericDictionarySlotNode> _typeSize_GenericDictionarySlots;
            public NativeLayoutTypeSizeGenericDictionarySlotNode TypeSizeDictionarySlot(TypeDesc type)
            {
                return _typeSize_GenericDictionarySlots.GetOrAdd(type);
            }

            private NodeCache<TypeDesc, NativeLayoutAllocateObjectGenericDictionarySlotNode> _allocateObject_GenericDictionarySlots;
            public NativeLayoutAllocateObjectGenericDictionarySlotNode AllocateObjectDictionarySlot(TypeDesc type)
            {
                return _allocateObject_GenericDictionarySlots.GetOrAdd(type);
            }

            private NodeCache<TypeDesc, NativeLayoutCastClassGenericDictionarySlotNode> _castClass_GenericDictionarySlots;
            public NativeLayoutCastClassGenericDictionarySlotNode CastClassDictionarySlot(TypeDesc type)
            {
                return _castClass_GenericDictionarySlots.GetOrAdd(type);
            }

            private NodeCache<TypeDesc, NativeLayoutIsInstGenericDictionarySlotNode> _isInst_GenericDictionarySlots;
            public NativeLayoutIsInstGenericDictionarySlotNode IsInstDictionarySlot(TypeDesc type)
            {
                return _isInst_GenericDictionarySlots.GetOrAdd(type);
            }

            private NodeCache<TypeDesc, NativeLayoutThreadStaticBaseIndexDictionarySlotNode> _threadStaticIndex_GenericDictionarySlots;
            public NativeLayoutThreadStaticBaseIndexDictionarySlotNode ThreadStaticBaseIndexDictionarySlotNode(TypeDesc type)
            {
                return _threadStaticIndex_GenericDictionarySlots.GetOrAdd(type);
            }

            private NodeCache<TypeDesc, NativeLayoutDefaultConstructorGenericDictionarySlotNode> _defaultConstructor_GenericDictionarySlots;
            public NativeLayoutDefaultConstructorGenericDictionarySlotNode DefaultConstructorDictionarySlot(TypeDesc type)
            {
                return _defaultConstructor_GenericDictionarySlots.GetOrAdd(type);
            }

            private NodeCache<TypeDesc, NativeLayoutAllocateArrayGenericDictionarySlotNode> _allocateArray_GenericDictionarySlots;
            public NativeLayoutAllocateArrayGenericDictionarySlotNode AllocateArrayDictionarySlot(TypeDesc type)
            {
                return _allocateArray_GenericDictionarySlots.GetOrAdd(type);
            }

            private NodeCache<MethodDesc, NativeLayoutInterfaceDispatchGenericDictionarySlotNode> _interfaceCell_GenericDictionarySlots;
            public NativeLayoutInterfaceDispatchGenericDictionarySlotNode InterfaceCellDictionarySlot(MethodDesc method)
            {
                return _interfaceCell_GenericDictionarySlots.GetOrAdd(method);
            }

            private NodeCache<MethodDesc, NativeLayoutMethodDictionaryGenericDictionarySlotNode> _methodDictionary_GenericDictionarySlots;
            public NativeLayoutMethodDictionaryGenericDictionarySlotNode MethodDictionaryDictionarySlot(MethodDesc method)
            {
                return _methodDictionary_GenericDictionarySlots.GetOrAdd(method);
            }

            public readonly NativeLayoutNotSupportedDictionarySlotNode NotSupportedDictionarySlot = new NativeLayoutNotSupportedDictionarySlotNode();

            private struct MethodEntrypointSlotKey : IEquatable<MethodEntrypointSlotKey>
            {
                public readonly bool Unboxing;
                public readonly MethodDesc Method;
                public readonly IMethodNode FunctionPointerTarget;

                public MethodEntrypointSlotKey(MethodDesc method, bool unboxing, IMethodNode functionPointerTarget)
                {
                    Unboxing = unboxing;
                    Method = method;
                    FunctionPointerTarget = functionPointerTarget;
                }

                public override bool Equals(object obj)
                {
                    MethodEntrypointSlotKey? other = obj as MethodEntrypointSlotKey?;
                    if (other.HasValue)
                        return Equals(other.Value);
                    else
                        return false;
                }

                public bool Equals(MethodEntrypointSlotKey other)
                {
                    if (other.Unboxing != Unboxing)
                        return false;

                    if (other.Method != Method)
                        return false;

                    if (other.FunctionPointerTarget != FunctionPointerTarget)
                        return false;

                    return true;
                }

                public override int GetHashCode()
                {
                    int hashCode = Method.GetHashCode() ^ (Unboxing ? 1 : 0);
                    if (FunctionPointerTarget != null)
                        hashCode ^= FunctionPointerTarget.GetHashCode();
                    return hashCode;
                }
            }

            private NodeCache<MethodEntrypointSlotKey, NativeLayoutMethodEntrypointGenericDictionarySlotNode> _methodEntrypoint_GenericDictionarySlots;
            public NativeLayoutMethodEntrypointGenericDictionarySlotNode MethodEntrypointDictionarySlot(MethodDesc method, bool unboxing, IMethodNode functionPointerTarget)
            {
                return _methodEntrypoint_GenericDictionarySlots.GetOrAdd(new MethodEntrypointSlotKey(method, unboxing, functionPointerTarget));
            }

            private NodeCache<FieldDesc, NativeLayoutFieldLdTokenGenericDictionarySlotNode> _fieldLdToken_GenericDictionarySlots;
            public NativeLayoutFieldLdTokenGenericDictionarySlotNode FieldLdTokenDictionarySlot(FieldDesc field)
            {
                return _fieldLdToken_GenericDictionarySlots.GetOrAdd(field);
            }

            private NodeCache<MethodDesc, NativeLayoutMethodLdTokenGenericDictionarySlotNode> _methodLdToken_GenericDictionarySlots;
            public NativeLayoutMethodLdTokenGenericDictionarySlotNode MethodLdTokenDictionarySlot(MethodDesc method)
            {
                return _methodLdToken_GenericDictionarySlots.GetOrAdd(method);
            }

            private NodeCache<FieldDesc, NativeLayoutFieldOffsetGenericDictionarySlotNode> _fieldOffset_GenericDictionaryslots;
            public NativeLayoutFieldOffsetGenericDictionarySlotNode FieldOffsetDictionarySlot(FieldDesc field)
            {
                return _fieldOffset_GenericDictionaryslots.GetOrAdd(field);
            }

            private NodeCache<MethodDesc, NativeLayoutVTableOffsetGenericDictionarySlotNode> _vtableOffset_GenericDictionaryslots;
            public NativeLayoutVTableOffsetGenericDictionarySlotNode VTableOffsetDictionarySlot(MethodDesc method)
            {
                return _vtableOffset_GenericDictionaryslots.GetOrAdd(method);
            }

            private NodeCache<TypeSystemEntity, NativeLayoutDictionarySignatureNode> _dictionarySignatures;
            public NativeLayoutDictionarySignatureNode DictionarySignature(TypeSystemEntity owningMethodOrType)
            {
                return _dictionarySignatures.GetOrAdd(owningMethodOrType);
            }

            private NodeCache<int, NativeLayoutIntegerDictionarySlotNode> _integerSlots;
            public NativeLayoutIntegerDictionarySlotNode IntegerSlot(int value)
            {
                return _integerSlots.GetOrAdd(value);
            }

            private NodeCache<int, NativeLayoutPointerToOtherSlotDictionarySlotNode> _otherSlotPointerSlots;
            public NativeLayoutPointerToOtherSlotDictionarySlotNode PointerToOtherSlot(int otherSlotIndex)
            {
                return _otherSlotPointerSlots.GetOrAdd(otherSlotIndex);
            }

            private NodeCache<ConstrainedMethodUseKey, NativeLayoutConstrainedMethodDictionarySlotNode> _constrainedMethodUseSlots;
            public NativeLayoutConstrainedMethodDictionarySlotNode ConstrainedMethodUse(MethodDesc constrainedMethod, TypeDesc constraintType, bool directCall)
            {
                return _constrainedMethodUseSlots.GetOrAdd(new ConstrainedMethodUseKey(constrainedMethod, constraintType, directCall));
            }
        }

        public NativeLayoutHelper NativeLayout;
    }
}
