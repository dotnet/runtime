// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ILCompiler.Logging;
using ILLink.Shared;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

#nullable enable

namespace ILCompiler.Dataflow
{
    // Currently this is implemented using heuristics
    public class CompilerGeneratedState
    {
        readonly ILProvider _ilProvider;
        // The MetadataType keys must be type definitions (uninstantiated)
        readonly Dictionary<MetadataType, MethodDesc> _compilerGeneratedTypeToUserCodeMethod;
        readonly Dictionary<MetadataType, TypeArgumentInfo> _generatedTypeToTypeArgumentInfo;
        readonly record struct TypeArgumentInfo(
            /// <summary>The method which calls the ctor for the given type</summary>
            MethodDesc CreatingMethod,
            /// <summary>Attributes for the type, pulled from the creators type arguments</summary>
            IReadOnlyList<GenericParameterDesc?>? OriginalAttributes);

        readonly Dictionary<MethodDesc, MethodDesc> _compilerGeneratedMethodToUserCodeMethod;

        // For each type that has had its cache populated, stores a map of methods which have corresponding
        // compiler-generated members (either methods or state machine types) to those compiler-generated members,
        // or null if the type has no methods with compiler-generated members.
        readonly Dictionary<TypeDesc, Dictionary<MethodDesc, List<TypeSystemEntity>>?> _cachedTypeToCompilerGeneratedMembers;

        public CompilerGeneratedState(ILProvider ilProvider)
        {
            _ilProvider = ilProvider;

            _compilerGeneratedTypeToUserCodeMethod = new Dictionary<MetadataType, MethodDesc>();
            _generatedTypeToTypeArgumentInfo = new Dictionary<MetadataType, TypeArgumentInfo>();
            _compilerGeneratedMethodToUserCodeMethod = new Dictionary<MethodDesc, MethodDesc>();
            _cachedTypeToCompilerGeneratedMembers = new Dictionary<TypeDesc, Dictionary<MethodDesc, List<TypeSystemEntity>>?>();
        }

        public Logger? Logger { get; set; }

        static IEnumerable<MetadataType> GetCompilerGeneratedNestedTypes(TypeDesc type)
        {
            if (type is not MetadataType metadataType)
                yield break;

            foreach (var nestedType in metadataType.GetNestedTypes())
            {
                if (!CompilerGeneratedNames.IsGeneratedMemberName(nestedType.Name))
                    continue;

                yield return nestedType;

                foreach (var recursiveNestedType in GetCompilerGeneratedNestedTypes(nestedType))
                    yield return recursiveNestedType;
            }
        }

        // "Nested function" refers to lambdas and local functions.
        public static bool IsNestedFunctionOrStateMachineMember(TypeSystemEntity member)
        {
            if (member is MethodDesc method && CompilerGeneratedNames.IsLambdaOrLocalFunction(method.Name))
                return true;

            if (GetOwningType(member) is not MetadataType declaringType)
                return false;

            return CompilerGeneratedNames.IsStateMachineType(declaringType.Name);
        }

        public static bool TryGetStateMachineType(MethodDesc method, [NotNullWhen(true)] out MetadataType? stateMachineType)
        {
            stateMachineType = null;
            // Discover state machine methods.
            if (method is not EcmaMethod ecmaMethod)
                return false;

            CustomAttributeValue<TypeDesc>? decodedAttribute = null;
            decodedAttribute = ecmaMethod.GetDecodedCustomAttribute("System.Runtime.CompilerServices", "AsyncIteratorStateMachineAttribute");
            if (decodedAttribute == null)
                decodedAttribute = ecmaMethod.GetDecodedCustomAttribute("System.Runtime.CompilerServices", "AsyncStateMachineAttribute");
            if (decodedAttribute == null)
                decodedAttribute = ecmaMethod.GetDecodedCustomAttribute("System.Runtime.CompilerServices", "IteratorStateMachineAttribute");

            if (decodedAttribute == null)
                return false;

            stateMachineType = GetFirstConstructorArgumentAsType(decodedAttribute.Value) as MetadataType;
            return stateMachineType != null;
        }

        /// <summary>
        /// Walks the type and its descendents to find Roslyn-compiler generated
        /// code and gather information to map it back to original user code. If
        /// a compiler-generated type is passed in directly, this method will walk
        /// up and find the nearest containing user type. Returns the nearest user type,
        /// or null if none was found.
        /// </summary>
        MetadataType? PopulateCacheForType(MetadataType? type)
        {
            Debug.Assert(type == type?.GetTypeDefinition());

            // Look in the declaring type if this is a compiler-generated type (state machine or display class).
            // State machines can be emitted into display classes, so we may also need to go one more level up.
            // To avoid depending on implementation details, we go up until we see a non-compiler-generated type.
            // This is the counterpart to GetCompilerGeneratedNestedTypes.
            while (type != null && CompilerGeneratedNames.IsGeneratedMemberName(type.Name))
                type = type.ContainingType as MetadataType;

            if (type is null)
                return null;

            // Avoid repeat scans of the same type
            if (_cachedTypeToCompilerGeneratedMembers.ContainsKey(type))
                return type;

            var callGraph = new CompilerGeneratedCallGraph();
            var userDefinedMethods = new HashSet<MethodDesc>();

            void ProcessMethod(MethodDesc method)
            {
                Debug.Assert(method == method.GetTypicalMethodDefinition());

                bool isStateMachineMember = CompilerGeneratedNames.IsStateMachineType(((MetadataType)method.OwningType).Name);
                if (!CompilerGeneratedNames.IsLambdaOrLocalFunction(method.Name))
                {
                    if (!isStateMachineMember)
                    {
                        // If it's not a nested function, track as an entry point to the call graph.
                        var added = userDefinedMethods.Add(method);
                        Debug.Assert(added);
                    }
                }
                else
                {
                    // We don't expect lambdas or local functions to be emitted directly into
                    // state machine types.
                    Debug.Assert(!isStateMachineMember);
                }

                // Discover calls or references to lambdas or local functions. This includes
                // calls to local functions, and lambda assignments (which use ldftn).
                var methodBody = _ilProvider.GetMethodIL(method);
                if (methodBody != null)
                {
                    ILReader reader = new ILReader(methodBody.GetILBytes());
                    while (reader.HasNext)
                    {
                        ILOpcode opcode = reader.ReadILOpcode();
                        MethodDesc? lambdaOrLocalFunction;
                        switch (opcode) {
                            case ILOpcode.ldftn:
                            case ILOpcode.ldtoken:
                            case ILOpcode.call:
                            case ILOpcode.callvirt:
                            case ILOpcode.newobj:
                                lambdaOrLocalFunction = methodBody.GetObject(reader.ReadILToken()) as MethodDesc;
                                break;

                            default:
                                lambdaOrLocalFunction = null;
                                reader.Skip(opcode);
                                break;
                        }

                        if (lambdaOrLocalFunction == null)
                            continue;

                        lambdaOrLocalFunction = lambdaOrLocalFunction.GetTypicalMethodDefinition();

                        if (lambdaOrLocalFunction.IsConstructor &&
                            lambdaOrLocalFunction.OwningType is MetadataType generatedType &&
                            // Don't consider calls in the same type, like inside a static constructor
                            method.OwningType != generatedType &&
                            CompilerGeneratedNames.IsLambdaDisplayClass(generatedType.Name))
                        {
                            generatedType = (MetadataType)generatedType.GetTypeDefinition();

                            // fill in null for now, attribute providers will be filled in later
                            if (!_generatedTypeToTypeArgumentInfo.TryAdd(generatedType, new TypeArgumentInfo(method, null)))
                            {
                                var alreadyAssociatedMethod = _generatedTypeToTypeArgumentInfo[generatedType].CreatingMethod;
                                Logger?.LogWarning(new MessageOrigin(method), DiagnosticId.MethodsAreAssociatedWithUserMethod, method.GetDisplayName(), alreadyAssociatedMethod.GetDisplayName(), generatedType.GetDisplayName());
                            }
                            continue;
                        }

                        if (!CompilerGeneratedNames.IsLambdaOrLocalFunction(lambdaOrLocalFunction.Name))
                            continue;

                        if (isStateMachineMember)
                        {
                            if (method.OwningType is MetadataType owningType)
                            {
                                callGraph.TrackCall(owningType, lambdaOrLocalFunction);
                            }
                        }
                        else
                        {
                            callGraph.TrackCall(method, lambdaOrLocalFunction);
                        }
                    }
                }

                if (TryGetStateMachineType(method, out MetadataType? stateMachineType))
                {
                    Debug.Assert(stateMachineType.ContainingType == type ||
                        (CompilerGeneratedNames.IsGeneratedMemberName(stateMachineType.ContainingType.Name) &&
                         stateMachineType.ContainingType.ContainingType == type));
                    Debug.Assert(stateMachineType == stateMachineType.GetTypeDefinition());

                    callGraph.TrackCall(method, stateMachineType);

                    if (!_compilerGeneratedTypeToUserCodeMethod.TryAdd(stateMachineType, method))
                    {
                        var alreadyAssociatedMethod = _compilerGeneratedTypeToUserCodeMethod[stateMachineType];
                        Logger?.LogWarning(new MessageOrigin(method), DiagnosticId.MethodsAreAssociatedWithStateMachine, method.GetDisplayName(), alreadyAssociatedMethod.GetDisplayName(), stateMachineType.GetDisplayName());
                    }
                    // Already warned above if multiple methods map to the same type
                    // Fill in null for argument providers now, the real providers will be filled in later
                    _ = _generatedTypeToTypeArgumentInfo.TryAdd(stateMachineType, new TypeArgumentInfo(method, null));
                }
            }

            // Look for state machine methods, and methods which call local functions.
            foreach (MethodDesc method in type.GetMethods())
                ProcessMethod(method);

            // Also scan compiler-generated state machine methods (in case they have calls to nested functions),
            // and nested functions inside compiler-generated closures (in case they call other nested functions).

            // State machines can be emitted into lambda display classes, so we need to go down at least two
            // levels to find calls from iterator nested functions to other nested functions. We just recurse into
            // all compiler-generated nested types to avoid depending on implementation details.

            foreach (var nestedType in GetCompilerGeneratedNestedTypes(type))
            {
                foreach (var method in nestedType.GetMethods())
                    ProcessMethod(method);
            }

            // Now we've discovered the call graphs for calls to nested functions.
            // Use this to map back from nested functions to the declaring user methods.

            // Note: This maps all nested functions back to the user code, not to the immediately
            // declaring local function. The IL doesn't contain enough information in general for
            // us to determine the nesting of local functions and lambdas.

            // Note: this only discovers nested functions which are referenced from the user
            // code or its referenced nested functions. There is no reliable way to determine from
            // IL which user code an unused nested function belongs to.

            Dictionary<MethodDesc, List<TypeSystemEntity>>? compilerGeneratedCallees = null;
            foreach (var userDefinedMethod in userDefinedMethods)
            {
                var callees = callGraph.GetReachableMembers(userDefinedMethod);
                if (!callees.Any())
                    continue;

                compilerGeneratedCallees ??= new Dictionary<MethodDesc, List<TypeSystemEntity>>();
                compilerGeneratedCallees.Add(userDefinedMethod, new List<TypeSystemEntity>(callees));

                foreach (var compilerGeneratedMember in callees)
                {
                    switch (compilerGeneratedMember)
                    {
                        case MethodDesc nestedFunction:
                            Debug.Assert(CompilerGeneratedNames.IsLambdaOrLocalFunction(nestedFunction.Name));
                            // Nested functions get suppressions from the user method only.
                            if (!_compilerGeneratedMethodToUserCodeMethod.TryAdd(nestedFunction, userDefinedMethod))
                            {
                                var alreadyAssociatedMethod = _compilerGeneratedMethodToUserCodeMethod[nestedFunction];
                                Logger?.LogWarning(new MessageOrigin(userDefinedMethod), DiagnosticId.MethodsAreAssociatedWithUserMethod, userDefinedMethod.GetDisplayName(), alreadyAssociatedMethod.GetDisplayName(), nestedFunction.GetDisplayName());
                            }
                            break;
                        case MetadataType stateMachineType:
                            // Types in the call graph are always state machine types
                            // For those all their methods are not tracked explicitly in the call graph; instead, they
                            // are represented by the state machine type itself.
                            // We are already tracking the association of the state machine type to the user code method
                            // above, so no need to track it here.
                            Debug.Assert(CompilerGeneratedNames.IsStateMachineType(stateMachineType.Name));
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }

            // Now that we have instantiating methods fully filled out, walk the generated types and fill in the attribute
            // providers
            foreach (var generatedType in _generatedTypeToTypeArgumentInfo.Keys)
            {
                Debug.Assert(generatedType == generatedType.GetTypeDefinition());

                if (HasGenericParameters(generatedType))
                    MapGeneratedTypeTypeParameters(generatedType);
            }

            _cachedTypeToCompilerGeneratedMembers.Add(type, compilerGeneratedCallees);
            return type;

            /// <summary>
            /// Check if the type itself is generic. The only difference is that
            /// if the type is a nested type, the generic parameters from its
            /// parent type don't count.
            /// </summary>
            static bool HasGenericParameters(MetadataType typeDef)
            {
                if (typeDef.ContainingType == null)
                    return typeDef.HasInstantiation;

                return typeDef.Instantiation.Length > typeDef.ContainingType.Instantiation.Length;
            }

            void MapGeneratedTypeTypeParameters(MetadataType generatedType)
            {
                Debug.Assert(CompilerGeneratedNames.IsGeneratedType(generatedType.Name));
                Debug.Assert(generatedType == generatedType.GetTypeDefinition());

                if (!_generatedTypeToTypeArgumentInfo.TryGetValue(generatedType, out var typeInfo))
                {
                    // This can happen for static (non-capturing) closure environments, where more than
                    // nested function can map to the same closure environment. Since the current functionality
                    // is based on a one-to-one relationship between environments (types) and methods, this is
                    // not supported.
                    return;
                }

                if (typeInfo.OriginalAttributes is not null)
                {
                    return;
                }
                var method = typeInfo.CreatingMethod;
                var body = _ilProvider.GetMethodIL(method);
                if (body is not null)
                {
                    var typeArgs = new GenericParameterDesc?[generatedType.Instantiation.Length];
                    var typeRef = ScanForInit(generatedType, body);
                    if (typeRef is null)
                    {
                        return;
                    }

                    // The typeRef is going to be a generic instantiation with signature variables
                    // We need to figure out the actual generic parameters which were used to create these
                    // so instantiate the typeRef in the context of the method body where it is created
                    TypeDesc instantiatedType = typeRef.InstantiateSignature(method.OwningType.Instantiation, method.Instantiation);
                    for (int i = 0; i < instantiatedType.Instantiation.Length; i++)
                    {
                        var typeArg = instantiatedType.Instantiation[i];
                        // Start with the existing parameters, in case we can't find the mapped one
                        GenericParameterDesc? userAttrs = generatedType.Instantiation[i] as GenericParameterDesc;
                        // The type parameters of the state machine types are alpha renames of the
                        // the method parameters, so the type ref should always be a GenericParameter. However,
                        // in the case of nesting, there may be multiple renames, so if the parameter is a method
                        // we know we're done, but if it's another state machine, we have to keep looking to find
                        // the original owner of that state machine.
                        if (typeArg is GenericParameterDesc { Kind: { } kind } param)
                        {
                            if (kind == GenericParameterKind.Method)
                            {
                                userAttrs = param;
                            }
                            else
                            {
                                // Must be a type ref
                                if (method.OwningType is not MetadataType owningType || !CompilerGeneratedNames.IsGeneratedType(owningType.Name))
                                {
                                    userAttrs = param;
                                }
                                else
                                {
                                    owningType = (MetadataType)owningType.GetTypeDefinition();
                                    MapGeneratedTypeTypeParameters(owningType);
                                    if (_generatedTypeToTypeArgumentInfo.TryGetValue(owningType, out var owningInfo) &&
                                        owningInfo.OriginalAttributes is { } owningAttrs)
                                    {
                                        userAttrs = owningAttrs[param.Index];
                                    }
                                }
                            }
                        }

                        typeArgs[i] = userAttrs;
                    }
                    _generatedTypeToTypeArgumentInfo[generatedType] = typeInfo with { OriginalAttributes = typeArgs };
                }
            }

            MetadataType? ScanForInit(MetadataType stateMachineType, MethodIL body)
            {
                ILReader reader = new ILReader(body.GetILBytes());
                while (reader.HasNext)
                {
                    ILOpcode opcode = reader.ReadILOpcode();
                    switch (opcode)
                    {
                        case ILOpcode.initobj:
                        case ILOpcode.newobj:
                            if (body.GetObject(reader.ReadILToken()) is MethodDesc { OwningType: MetadataType owningType }
                                && stateMachineType == owningType.GetTypeDefinition())
                            {
                                return owningType;
                            }
                            break;

                        default:
                            reader.Skip(opcode);
                            break;
                    }
                }
                return null;
            }
        }

        static TypeDesc? GetFirstConstructorArgumentAsType(CustomAttributeValue<TypeDesc> attribute)
        {
            if (attribute.FixedArguments.Length == 0)
                return null;

            return attribute.FixedArguments[0].Value as TypeDesc;
        }

        public bool TryGetCompilerGeneratedCalleesForUserMethod(MethodDesc method, [NotNullWhen(true)] out List<TypeSystemEntity>? callees)
        {
            method = method.GetTypicalMethodDefinition();

            callees = null;
            if (IsNestedFunctionOrStateMachineMember(method))
                return false;

            if (method.OwningType is not MetadataType owningType)
                return false;

            var typeToCache = PopulateCacheForType(owningType);
            if (typeToCache is null)
                return false;

            return _cachedTypeToCompilerGeneratedMembers[typeToCache]?.TryGetValue(method, out callees) == true;
        }

        /// <summary>
        /// Gets the attributes on the "original" method of a generated type, i.e. the
        /// attributes on the corresponding type parameters from the owning method.
        /// </summary>
        public IReadOnlyList<GenericParameterDesc?>? GetGeneratedTypeAttributes(MetadataType type)
        {
            MetadataType generatedType = (MetadataType)type.GetTypeDefinition();
            Debug.Assert(CompilerGeneratedNames.IsGeneratedType(generatedType.Name));

            var typeToCache = PopulateCacheForType(generatedType);
            if (typeToCache is null)
                return null;

            if (_generatedTypeToTypeArgumentInfo.TryGetValue(generatedType, out var typeInfo))
            {
                return typeInfo.OriginalAttributes;
            }
            return null;
        }

        // For state machine types/members, maps back to the state machine method.
        // For local functions and lambdas, maps back to the owning method in user code (not the declaring
        // lambda or local function, because the IL doesn't contain enough information to figure this out).
        public bool TryGetOwningMethodForCompilerGeneratedMember(TypeSystemEntity sourceMember, [NotNullWhen(true)] out MethodDesc? owningMethod)
        {
            owningMethod = null;
            if (sourceMember == null)
                return false;

            MethodDesc? compilerGeneratedMethod = sourceMember as MethodDesc;
            if (compilerGeneratedMethod != null)
            {
                if (_compilerGeneratedMethodToUserCodeMethod.TryGetValue(compilerGeneratedMethod, out owningMethod))
                    return true;
            }

            MetadataType? sourceType = ((sourceMember as TypeDesc) ?? GetOwningType(sourceMember))?.GetTypeDefinition() as MetadataType;
            if (sourceType is null)
                return false;

            if (_compilerGeneratedTypeToUserCodeMethod.TryGetValue(sourceType, out owningMethod))
                return true;

            if (!IsNestedFunctionOrStateMachineMember(sourceMember))
                return false;

            // sourceType is a state machine type, or the type containing a lambda or local function.
            // Search all methods to find the one which points to the type as its
            // state machine implementation.
            var typeToCache = PopulateCacheForType(sourceType);
            if (typeToCache is null)
                return false;

            if (compilerGeneratedMethod != null)
            {
                if (_compilerGeneratedMethodToUserCodeMethod.TryGetValue(compilerGeneratedMethod, out owningMethod))
                    return true;
            }

            if (_compilerGeneratedTypeToUserCodeMethod.TryGetValue(sourceType, out owningMethod))
                return true;

            return false;
        }

        private static TypeDesc? GetOwningType(TypeSystemEntity entity)
        {
            return entity switch
            {
                MethodDesc method => method.OwningType,
                FieldDesc field => field.OwningType,
                MetadataType type => type.ContainingType,
                PropertyPseudoDesc property => property.OwningType,
                EventPseudoDesc @event => @event.OwningType,
                _ => null
            };
        }
    }
}
