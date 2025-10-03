// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    internal static partial class LazyGenericsSupport
    {
        private sealed partial class GraphBuilder
        {
            // Generate the illegal recursion graph as specified in ECMA 335 II.9.2 Generics and recursive inheritance graphs
            public GraphBuilder(EcmaType type)
            {
                _graph = new Graph<EcmaGenericParameter>();

                if (type.HasInstantiation)
                {
                    HashSet<TypeDesc> types = new HashSet<TypeDesc>();
                    List<TypeDesc> typesToProcess = new List<TypeDesc>();
                    typesToProcess.Add(type);
                    while (typesToProcess.Count > 0)
                    {
                        var processType = typesToProcess[typesToProcess.Count - 1];
                        typesToProcess.RemoveAt(typesToProcess.Count - 1);

                        if (processType.IsParameterizedType)
                        {
                            if (processType.GetParameterType() is InstantiatedType instantiatedType)
                            {
                                AddProcessType(instantiatedType.GetTypeDefinition());
                                WalkFormals(instantiatedType, expanded: true);
                            }
                        }
                        else if (processType.GetTypeDefinition() == processType)
                        {
                            if (processType.HasInstantiation && processType is EcmaType ecmaProcessType)
                            {
                                AddSpecializedType(ecmaProcessType.BaseType);
                                foreach (var t in ecmaProcessType.ExplicitlyImplementedInterfaces)
                                {
                                    AddSpecializedType(t);
                                }

                                void AddSpecializedType(TypeDesc typeToSpecialize)
                                {
                                    if (typeToSpecialize != null)
                                    {
                                        var specializedType = typeToSpecialize.InstantiateSignature(processType.Instantiation, default(Instantiation));
                                        AddProcessType(specializedType);
                                    }
                                }
                            }
                        }
                        else if (processType is InstantiatedType instantiatedType)
                        {
                            AddProcessType(instantiatedType.GetTypeDefinition());
                            WalkFormals(instantiatedType, expanded: false);
                        }

                        void WalkFormals(InstantiatedType instantiatedType, bool expanded)
                        {
                            for (int i = 0; i < instantiatedType.Instantiation.Length; i++)
                            {
                                var formal = instantiatedType.GetTypeDefinition().Instantiation[i] as GenericParameterDesc;
                                if (instantiatedType.Instantiation[i] is GenericParameterDesc genParam)
                                {
                                    AddEdge(genParam, formal, expanded);
                                }
                                else
                                {
                                    foreach (var genParameter in GetGenericParameters(instantiatedType.Instantiation[i]))
                                    {
                                        AddEdge(genParameter, formal, true);
                                    }
                                }
                            }
                        }

                        void AddProcessType(TypeDesc type)
                        {
                            if (type == null)
                                return;

                            if (types.Add(type))
                            {
                                typesToProcess.Add(type);
                            }

                            if (type is ParameterizedType paramType)
                            {
                                AddProcessType(paramType.GetParameterType());
                            }

                            foreach (var instType in type.Instantiation)
                            {
                                AddProcessType(instType);
                            }
                        }

                        void AddEdge(GenericParameterDesc from, GenericParameterDesc to, bool flagged)
                        {
                            EcmaGenericParameter fromEcma = from as EcmaGenericParameter;
                            EcmaGenericParameter toEcma = to as EcmaGenericParameter;
                            if (fromEcma == null || toEcma == null)
                                return;
                            _graph.AddEdge(fromEcma, toEcma, flagged);
                        }

                        IEnumerable<GenericParameterDesc> GetGenericParameters(TypeDesc t)
                        {
                            if (t is GenericParameterDesc genParamDesc)
                            {
                                yield return genParamDesc;
                            }
                            else if (t is ParameterizedType paramType)
                            {
                                foreach (var genParamType in GetGenericParameters(paramType.GetParameterType()))
                                {
                                    yield return genParamType;
                                }
                            }
                            else if (t.HasInstantiation)
                            {
                                foreach (var instType in t.Instantiation)
                                {
                                    foreach (var genParamType in GetGenericParameters(instType))
                                    {
                                        yield return genParamType;
                                    }
                                }
                            }
                            else if (t is FunctionPointerType fptrType)
                            {
                                foreach (var genParamType in GetGenericParameters(fptrType.Signature.ReturnType))
                                {
                                    yield return genParamType;
                                }
                                foreach (var parameterType in fptrType.Signature)
                                {
                                    foreach (var genParamType in GetGenericParameters(parameterType))
                                    {
                                        yield return genParamType;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            public GraphBuilder(EcmaModule assembly)
            {
                _graph = new Graph<EcmaGenericParameter>();
                _metadataReader = assembly.MetadataReader;

                foreach (TypeDefinitionHandle typeHandle in _metadataReader.TypeDefinitions)
                {
                    TypeDefinition typeDefinition = _metadataReader.GetTypeDefinition(typeHandle);

                    // Only things that deal with some sort of genericness could form cycles.
                    // Do not look at any types/member where genericness is not involved.
                    // Do not even bother getting type system entities for those that are not generic.
                    bool isGenericType = typeDefinition.GetGenericParameters().Count > 0;
                    if (isGenericType)
                    {
                        try
                        {
                            var ecmaType = (EcmaType)assembly.GetObject(typeHandle);
                            WalkAncestorTypes(ecmaType);
                        }
                        catch (TypeSystemException)
                        {
                        }
                    }

                    foreach (MethodDefinitionHandle methodHandle in typeDefinition.GetMethods())
                    {
                        // We need to look at methods on generic types, or generic methods.
                        bool needsScanning = isGenericType;

                        if (!needsScanning)
                        {
                            MethodDefinition methodDefinition = _metadataReader.GetMethodDefinition(methodHandle);
                            BlobReader sigBlob = _metadataReader.GetBlobReader(methodDefinition.Signature);
                            needsScanning = sigBlob.ReadSignatureHeader().IsGeneric;
                        }

                        if (needsScanning)
                        {
                            try
                            {
                                var ecmaMethod = (EcmaMethod)assembly.GetObject(methodHandle);
                                WalkMethod(ecmaMethod);

                                if (ecmaMethod.IsVirtual)
                                    LookForVirtualOverrides(ecmaMethod);
                            }
                            catch (TypeSystemException)
                            {
                            }
                        }
                    }

                    if (isGenericType)
                    {
                        Instantiation typeContext = default;

                        foreach (FieldDefinitionHandle fieldHandle in typeDefinition.GetFields())
                        {
                            try
                            {
                                var ecmaField = (EcmaField)assembly.GetObject(fieldHandle);

                                if (typeContext.IsNull)
                                {
                                    typeContext = ecmaField.OwningType.Instantiation;
                                    Debug.Assert(!typeContext.IsNull);
                                }

                                ProcessTypeReference(ecmaField.FieldType, typeContext, default);
                            }
                            catch (TypeSystemException)
                            {
                            }
                        }
                    }
                }
                return;
            }

            public Graph<EcmaGenericParameter> Graph { get { return _graph; } }

            // Base types and interfaces.
            private void WalkAncestorTypes(EcmaType declaringType)
            {
                TypeDesc baseType = declaringType.BaseType;
                Instantiation typeContext = declaringType.Instantiation;
                if (baseType != null)
                {
                    ProcessAncestorType(baseType, typeContext);
                }
                foreach (DefType ifcType in declaringType.RuntimeInterfaces)
                {
                    ProcessAncestorType(ifcType, typeContext);
                }
            }

            private void ProcessAncestorType(TypeDesc ancestorType, Instantiation typeContext)
            {
                var embeddingState = new EmbeddingStateList(this);
                ForEachEmbeddedGenericFormal(ancestorType, typeContext, Instantiation.Empty, ref embeddingState);
            }

            private void LookForVirtualOverrides(EcmaMethod method)
            {
                // We don't currently attempt to handle this for non-generics.
                if (!method.HasInstantiation)
                    return;

                // If this is a generic virtual method, add an edge from each of the generic parameters
                // of the implementation to the generic parameters of the declaration - any call to the
                // declaration will be modeled as if the declaration was calling into the implementation.

                var decl = (EcmaMethod)MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(method).GetTypicalMethodDefinition();
                if (decl != method)
                {
                    RecordBinding(this, decl.Instantiation, method.Instantiation);
                }
                else
                {
                    TypeDesc methodOwningType = method.OwningType;

                    // This is the slot definition. Does it implement an interface?
                    // (This has obvious holes. They haven't show up as issues so far.)
                    foreach (DefType interfaceType in methodOwningType.RuntimeInterfaces)
                    {
                        foreach (MethodDesc interfaceMethod in interfaceType.GetVirtualMethods())
                        {
                            // Trivially reject looking at interface methods that for sure can't be implemented by
                            // the method we're looking at.
                            if (!interfaceMethod.IsVirtual
                                || interfaceMethod.Instantiation.Length != method.Instantiation.Length
                                || interfaceMethod.Signature.Length != method.Signature.Length
                                || interfaceMethod.Signature.IsStatic != method.Signature.IsStatic)
                                continue;

                            MethodDesc impl = interfaceMethod.Signature.IsStatic ?
                                methodOwningType.ResolveInterfaceMethodToStaticVirtualMethodOnType(interfaceMethod) :
                                methodOwningType.ResolveInterfaceMethodToVirtualMethodOnType(interfaceMethod);
                            if (impl?.GetMethodDefinition() == method)
                            {
                                RecordBinding(this, interfaceMethod.Instantiation, method.Instantiation);
                                // Continue the loop in case this method implements multiple interfaces
                            }
                        }
                    }
                }

                static void RecordBinding(GraphBuilder builder, Instantiation declInstantiation, Instantiation implInstantiation)
                {
                    for (int i = 0; i < declInstantiation.Length; i++)
                    {
                        builder.RecordBinding(
                            (EcmaGenericParameter)implInstantiation[i],
                            (EcmaGenericParameter)declInstantiation[i],
                            isProperEmbedding: false);
                    }
                }
            }

            private void WalkMethod(EcmaMethod method)
            {
                Instantiation typeContext = method.OwningType.Instantiation;
                Instantiation methodContext = method.Instantiation;

                MethodSignature methodSig = method.Signature;
                ProcessTypeReference(methodSig.ReturnType, typeContext, methodContext);
                foreach (TypeDesc parameterType in methodSig)
                {
                    ProcessTypeReference(parameterType, typeContext, methodContext);
                }

                if (method.IsAbstract)
                {
                    return;
                }

                var methodIL = EcmaMethodIL.Create(method);
                if (methodIL == null)
                {
                    return;
                }

                // Walk the method body looking at referenced things that have some genericness.
                // Nongeneric things cannot be forming cycles.
                // In particular, we don't care about MemberRefs to non-generic things, TypeDefs/MethodDefs/FieldDefs.
                // Avoid the work to even materialize type system entities for those.

                ILReader reader = new ILReader(methodIL.GetILBytes());

                while (reader.HasNext)
                {
                    ILOpcode opcode = reader.ReadILOpcode();
                    switch (opcode)
                    {
                        case ILOpcode.sizeof_:
                        case ILOpcode.newarr:
                        case ILOpcode.initobj:
                        case ILOpcode.stelem:
                        case ILOpcode.ldelem:
                        case ILOpcode.ldelema:
                        case ILOpcode.box:
                        case ILOpcode.unbox:
                        case ILOpcode.unbox_any:
                        case ILOpcode.cpobj:
                        case ILOpcode.ldobj:
                        case ILOpcode.castclass:
                        case ILOpcode.isinst:
                        case ILOpcode.stobj:
                        case ILOpcode.refanyval:
                        case ILOpcode.mkrefany:
                        case ILOpcode.constrained:
                            EntityHandle accessedType = MetadataTokens.EntityHandle(reader.ReadILToken());
                        typeCase:
                            if (accessedType.Kind == HandleKind.TypeSpecification)
                            {
                                var t = methodIL.GetObject(MetadataTokens.GetToken(accessedType), NotFoundBehavior.ReturnNull) as TypeDesc;
                                if (t != null)
                                {
                                    ProcessTypeReference(t, typeContext, methodContext);
                                }
                            }
                            break;

                        case ILOpcode.stsfld:
                        case ILOpcode.ldsfld:
                        case ILOpcode.ldsflda:
                        case ILOpcode.stfld:
                        case ILOpcode.ldfld:
                        case ILOpcode.ldflda:
                            EntityHandle accessedField = MetadataTokens.EntityHandle(reader.ReadILToken());
                        fieldCase:
                            if (accessedField.Kind == HandleKind.MemberReference)
                            {
                                accessedType = _metadataReader.GetMemberReference((MemberReferenceHandle)accessedField).Parent;
                                goto typeCase;
                            }
                            break;

                        case ILOpcode.call:
                        case ILOpcode.callvirt:
                        case ILOpcode.newobj:
                        case ILOpcode.ldftn:
                        case ILOpcode.ldvirtftn:
                        case ILOpcode.jmp:
                            EntityHandle accessedMethod = MetadataTokens.EntityHandle(reader.ReadILToken());
                        methodCase:
                            if (accessedMethod.Kind == HandleKind.MethodSpecification
                                || (accessedMethod.Kind == HandleKind.MemberReference
                                     && _metadataReader.GetMemberReference((MemberReferenceHandle)accessedMethod).Parent.Kind == HandleKind.TypeSpecification))
                            {
                                var m = methodIL.GetObject(MetadataTokens.GetToken(accessedMethod), NotFoundBehavior.ReturnNull) as MethodDesc;
                                if (m != null)
                                {
                                    ProcessTypeReference(m.OwningType, typeContext, methodContext);
                                    ProcessMethodCall(m, typeContext, methodContext);
                                }
                            }
                            break;

                        case ILOpcode.ldtoken:
                            EntityHandle accessedEntity = MetadataTokens.EntityHandle(reader.ReadILToken());
                            if (accessedEntity.Kind == HandleKind.MethodSpecification
                                || (accessedEntity.Kind == HandleKind.MemberReference && _metadataReader.GetMemberReference((MemberReferenceHandle)accessedEntity).GetKind() == MemberReferenceKind.Method))
                            {
                                accessedMethod = accessedEntity;
                                goto methodCase;
                            }
                            else if (accessedEntity.Kind == HandleKind.MemberReference)
                            {
                                accessedField = accessedEntity;
                                goto fieldCase;
                            }
                            else if (accessedEntity.Kind == HandleKind.TypeSpecification)
                            {
                                accessedType = accessedEntity;
                                goto typeCase;
                            }
                            break;

                        default:
                            reader.Skip(opcode);
                            break;
                    }
                }
            }

            /// <summary>
            /// Inside a method body, we found a reference to another type (e.g. ldtoken, or a member access.)
            /// If the type is a generic instance, record any bindings between its formals and the referencer's
            /// formals.
            /// </summary>
            private void ProcessTypeReference(TypeDesc typeReference, Instantiation typeContext, Instantiation methodContext)
            {
                var embeddingState = new EmbeddingStateList(this);
                ForEachEmbeddedGenericFormal(typeReference, typeContext, methodContext, ref embeddingState);
            }

            /// <summary>
            /// Records the fact that the type formal "receiver" is being bound to a type expression that references
            /// "embedded."
            /// </summary>
            private void RecordBinding(EcmaGenericParameter receiver, EcmaGenericParameter embedded, bool isProperEmbedding)
            {
                bool flagged;
                if (isProperEmbedding)
                {
                    // If we got here, we have a potential codepath that binds "receiver" to a type expression involving "embedded"
                    // (and is not simply "embedded" itself.)
                    flagged = true;
                }
                else
                {
                    // If we got here, we have a potential codepath that binds "receiver" to a type expression that is simply "embedded"
                    flagged = false;
                }

                _graph.AddEdge(embedded, receiver, flagged);

                return;
            }

            private Graph<EcmaGenericParameter> _graph;
            private MetadataReader _metadataReader;
        }
    }
}
