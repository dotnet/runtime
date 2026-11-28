// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public static class TypeExtensions
    {
        public static bool IsSealed(this TypeDesc type)
        {
            var metadataType = type as MetadataType;
            if (metadataType != null)
            {
                return metadataType.IsSealed || metadataType.IsModuleType;
            }

            Debug.Assert(type.IsArray, "IsSealed on a type with no virtual methods?");
            return true;
        }

        /// <summary>
        /// Gets the type that defines virtual method slots for the specified type.
        /// </summary>
        public static DefType GetClosestDefType(this TypeDesc type)
        {
#if ILTRIM
            return type.IsArray ? type.Context.GetWellKnownType(WellKnownType.Array) : (DefType)type;
#else
            return ((CompilerTypeSystemContext)type.Context).GetClosestDefType(type);
#endif
        }

        /// <summary>
        /// Gets a value indicating whether the method requires a hidden instantiation argument in addition
        /// to the formal arguments defined in the method signature.
        /// </summary>
        public static bool RequiresInstArg(this MethodDesc method)
        {
            return method.IsSharedByGenericInstantiations &&
                (method.HasInstantiation || method.Signature.IsStatic || method.ImplementationType.IsValueType || (method.ImplementationType.IsInterface && !method.IsAbstract));
        }

        /// <summary>
        /// Gets a value indicating whether the method acquires the generic context from a hidden
        /// instantiation argument that points to the method's generic dictionary.
        /// </summary>
        public static bool RequiresInstMethodDescArg(this MethodDesc method)
        {
            return method.HasInstantiation && method.IsSharedByGenericInstantiations;
        }

        /// <summary>
        /// Gets a value indicating whether the method acquires the generic context from a hidden
        /// instantiation argument that points to the generic dictionary of the method's owning type.
        /// </summary>
        public static bool RequiresInstMethodTableArg(this MethodDesc method)
        {
            return (method.Signature.IsStatic || method.ImplementationType.IsValueType || (method.ImplementationType.IsInterface && !method.IsAbstract)) &&
                method.IsSharedByGenericInstantiations &&
                !method.HasInstantiation;
        }

        /// <summary>
        /// Gets a value indicating whether the method acquires the generic context from the this pointer.
        /// </summary>
        public static bool AcquiresInstMethodTableFromThis(this MethodDesc method)
        {
            return method.IsSharedByGenericInstantiations &&
                !method.HasInstantiation &&
                !method.Signature.IsStatic &&
                !method.ImplementationType.IsValueType &&
                !(method.ImplementationType.IsInterface && !method.IsAbstract);
        }

        /// <summary>
        /// Returns true if '<paramref name="method"/>' is the "Address" method on multidimensional array types.
        /// </summary>
        public static bool IsArrayAddressMethod(this MethodDesc method)
        {
            var arrayMethod = method as ArrayMethod;
            return arrayMethod != null && arrayMethod.Kind == ArrayMethodKind.Address;
        }


        /// <summary>
        /// Returns true if '<paramref name="method"/>' is one of the special methods on multidimensional array types (set, get, address).
        /// </summary>
        public static bool IsArrayMethod(this MethodDesc method)
        {
            var arrayMethod = method as ArrayMethod;
            return arrayMethod != null && (arrayMethod.Kind == ArrayMethodKind.Address ||
                                           arrayMethod.Kind == ArrayMethodKind.Get ||
                                           arrayMethod.Kind == ArrayMethodKind.Set ||
                                           arrayMethod.Kind == ArrayMethodKind.Ctor);
        }

        /// <summary>
        /// Wrapper helper function around the IsCanonicalDefinitionType API on the TypeSystemContext
        /// </summary>
        public static bool IsCanonicalDefinitionType(this TypeDesc type, CanonicalFormKind kind)
        {
            return type.Context.IsCanonicalDefinitionType(type, kind);
        }

        /// <summary>
        /// Determine if a type has a generic depth greater than a given value
        /// </summary>
        public static bool IsGenericDepthGreaterThan(this TypeDesc type, int depth)
        {
            if (depth < 0)
                return true;

            foreach (TypeDesc instantiationType in type.Instantiation)
            {
                if (instantiationType.IsGenericDepthGreaterThan(depth - 1))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determine if a type has a generic depth greater than a given value
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static bool IsGenericDepthGreaterThan(this MethodDesc method, int depth)
        {
            if (method.OwningType.IsGenericDepthGreaterThan(depth))
                return true;

            foreach (TypeDesc type in method.Instantiation)
            {
                if (type.IsGenericDepthGreaterThan(depth))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether an array type does implements the generic collection interfaces. This is the case
        /// for multi-dimensional arrays, and arrays of pointers.
        /// </summary>
        public static bool IsArrayTypeWithoutGenericInterfaces(this TypeDesc type)
        {
            if (!type.IsArray)
                return false;

            var arrayType = (ArrayType)type;
            TypeDesc elementType = arrayType.ElementType;
            return type.IsMdArray || elementType.IsPointer || elementType.IsFunctionPointer;
        }

        public static bool? CompareTypesForEquality(TypeDesc type1, TypeDesc type2)
        {
            // If neither type is a canonical subtype, type handle comparison suffices
            if (!type1.IsCanonicalSubtype(CanonicalFormKind.Any) && !type2.IsCanonicalSubtype(CanonicalFormKind.Any))
            {
                return type1 == type2;
            }

            // If either or both types are canonical subtypes, we can sometimes prove inequality.
            if (AreGuaranteedToRepresentDifferentTypes(type1, type2))
            {
                return false;
            }

            return null;

            static bool AreGuaranteedToRepresentDifferentTypes(TypeDesc type1, TypeDesc type2)
            {
                if (type1.IsCanonicalDefinitionType(CanonicalFormKind.Any) || type2.IsCanonicalDefinitionType(CanonicalFormKind.Any))
                {
                    // Universal canonical definition can match any type. We can't prove inequality.
                    if (type1.IsCanonicalDefinitionType(CanonicalFormKind.Universal) || type2.IsCanonicalDefinitionType(CanonicalFormKind.Universal))
                        return false;

                    return type1.IsGCPointer != type2.IsGCPointer;
                }

                TypeFlags category = type1.Category;
                if (category != type2.Category)
                    return true;

                switch (category)
                {
                    case TypeFlags.Array:
                        if (((ArrayType)type1).Rank != ((ArrayType)type2).Rank)
                            return true;
                        return AreGuaranteedToRepresentDifferentTypes(((ArrayType)type1).ElementType, ((ArrayType)type2).ElementType);
                    case TypeFlags.SzArray:
                    case TypeFlags.ByRef:
                    case TypeFlags.Pointer:
                        return AreGuaranteedToRepresentDifferentTypes(((ParameterizedType)type1).ParameterType, ((ParameterizedType)type2).ParameterType);

                    default:
                        if (type1.IsDefType || type2.IsDefType)
                        {
                            if (!type1.HasSameTypeDefinition(type2))
                                return true;

                            Instantiation inst1 = type1.Instantiation;
                            if (inst1.Length != 0)
                            {
                                var inst2 = type2.Instantiation;
                                Debug.Assert(inst1.Length == inst2.Length);
                                for (int i = 0; i < inst1.Length; i++)
                                {
                                    if (AreGuaranteedToRepresentDifferentTypes(inst1[i], inst2[i]))
                                        return true;
                                }
                            }
                        }
                        break;
                }
                return false;
            }
        }

        public static TypeDesc MergeTypesToCommonParent(TypeDesc ta, TypeDesc tb)
        {
            if (ta == tb)
            {
                return ta;
            }

            // Handle the array case
            if (ta.IsArray)
            {
                if (tb.IsArray)
                {
                    return MergeArrayTypesToCommonParent((ArrayType)ta, (ArrayType)tb);
                }
                else if (tb.IsInterface)
                {
                    // Check to see if we can merge the array to a common interface (such as Derived[] and IList<Base>)
                    if (ta.CanCastTo(tb))
                    {
                        return tb;
                    }
                }
                // keep merging from here
                ta = ta.Context.GetWellKnownType(WellKnownType.Array);
            }
            else if (tb.IsArray)
            {
                if (ta.IsInterface && tb.CanCastTo(ta))
                {
                    return ta;
                }

                tb = tb.Context.GetWellKnownType(WellKnownType.Array);
            }

            Debug.Assert(ta.IsDefType);
            Debug.Assert(tb.IsDefType);

            if (tb.IsInterface)
            {
                if (ta.IsInterface)
                {
                    //
                    // Both classes are interfaces.  Check that if one
                    // interface extends the other.
                    //
                    // Does tb extend ta ?
                    //
                    if (tb.ImplementsEquivalentInterface(ta))
                    {
                        return ta;
                    }

                    //
                    // Does tb extend ta ?
                    //
                    if (ta.ImplementsEquivalentInterface(tb))
                    {
                        return tb;
                    }

                    // No compatible merge found - using Object
                    return ta.Context.GetWellKnownType(WellKnownType.Object);
                }
                else
                {
                    return MergeClassWithInterface(ta, tb);
                }
            }
            else if (ta.IsInterface)
            {
                return MergeClassWithInterface(tb, ta);
            }

            int aDepth = 0;
            int bDepth = 0;

            // find the depth in the class hierarchy for each class
            for (TypeDesc searchType = ta; searchType != null; searchType = searchType.BaseType)
            {
                aDepth++;
            }

            for (TypeDesc searchType = tb; searchType != null; searchType = searchType.BaseType)
            {
                bDepth++;
            }

            // for whichever class is lower down in the hierarchy, walk up the superclass chain
            // to the same level as the other class
            while (aDepth > bDepth)
            {
                ta = ta.BaseType;
                aDepth--;
            }

            while (bDepth > aDepth)
            {
                tb = tb.BaseType;
                bDepth--;
            }

            while (!ta.IsEquivalentTo(tb))
            {
                ta = ta.BaseType;
                tb = tb.BaseType;
            }

            // If no compatible merge is found, we end up using Object

            Debug.Assert(ta != null);

            return ta;
        }

        private static TypeDesc MergeArrayTypesToCommonParent(ArrayType ta, ArrayType tb)
        {
            Debug.Assert(ta.IsArray && tb.IsArray && ta != tb);

            // if no match on the rank the common ancestor is System.Array
            if (ta.IsSzArray != tb.IsSzArray || ta.Rank != tb.Rank)
            {
                return ta.Context.GetWellKnownType(WellKnownType.Array);
            }

            TypeDesc taElem = ta.ElementType;
            TypeDesc tbElem = tb.ElementType;
            Debug.Assert(taElem != tbElem);

            TypeDesc mergeElem;
            if (taElem.IsEquivalentTo(tbElem))
            {
                mergeElem = taElem;
            }
            else if (taElem.IsArray && tbElem.IsArray)
            {
                mergeElem = MergeArrayTypesToCommonParent((ArrayType)taElem, (ArrayType)tbElem);
            }
            else if (taElem.IsGCPointer && tbElem.IsGCPointer)
            {
                // Find the common ancestor of the element types.
                mergeElem = MergeTypesToCommonParent(taElem, tbElem);
            }
            else
            {
                // The element types have nothing in common.
                return ta.Context.GetWellKnownType(WellKnownType.Array);
            }

            if (mergeElem == taElem)
            {
                return ta;
            }

            if (mergeElem == tbElem)
            {
                return tb;
            }

            if (taElem.IsMdArray)
            {
                return mergeElem.MakeArrayType(ta.Rank);
            }

            return mergeElem.MakeArrayType();
        }

        private static bool ImplementsEquivalentInterface(this TypeDesc type, TypeDesc interfaceType)
        {
            foreach (DefType implementedInterface in type.RuntimeInterfaces)
            {
                if (implementedInterface == interfaceType)
                {
                    return true;
                }
            }

            return false;
        }

        // The shared body can also be reached with other type arguments. Require a caller
        // constraint that guarantees this exact interface exists for every legal instantiation.
        public static bool HasStaticInterfaceConstraint(this MethodDesc caller, TypeDesc implementor, MethodDesc slot)
        {
            Instantiation args = caller.Instantiation;
            Instantiation parameters = caller.GetTypicalMethodDefinition().Instantiation;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != implementor)
                    continue;

                foreach (TypeDesc constraint in ((GenericParameterDesc)parameters[i]).TypeConstraints)
                {
                    if (constraint.InstantiateSignature(caller.OwningType.Instantiation, args) == slot.OwningType)
                        return true;
                }
            }

            return false;
        }

        public static MethodDesc TryResolveConstraintMethodApprox(this TypeDesc constrainedType, TypeDesc interfaceType, MethodDesc interfaceMethod, out bool forceRuntimeLookup)
        {
            DefaultInterfaceMethodResolution staticResolution = default;
            return TryResolveConstraintMethodApprox(constrainedType, interfaceType, interfaceMethod, out forceRuntimeLookup, ref staticResolution);
        }

        /// <summary>
        /// Attempts to resolve constrained call to <paramref name="interfaceMethod"/> into a concrete non-unboxing
        /// method on <paramref name="constrainedType"/>.
        /// The ability to resolve constraint methods is affected by the degree of code sharing we are performing
        /// for generic code.
        /// </summary>
        /// <returns>The resolved method or null if the constraint couldn't be resolved.</returns>
        public static MethodDesc TryResolveConstraintMethodApprox(this TypeDesc constrainedType, TypeDesc interfaceType, MethodDesc interfaceMethod, out bool forceRuntimeLookup, ref DefaultInterfaceMethodResolution staticResolution)
            => TryResolveConstraintMethodApprox(constrainedType, interfaceType, interfaceMethod, out forceRuntimeLookup, ref staticResolution, out _);

        // A null exact target can still have a known body. If every matching slot uses the
        // declared default, NativeAOT can supply its exact owner from the call token.
        public static MethodDesc TryResolveConstraintMethodApprox(this TypeDesc constrainedType, TypeDesc interfaceType, MethodDesc interfaceMethod, out bool forceRuntimeLookup, ref DefaultInterfaceMethodResolution staticResolution, out bool usesDeclaredStaticDefault)
        {
            usesDeclaredStaticDefault = false;
            forceRuntimeLookup = false;

            // Interface method may or may not be fully canonicalized here.
            // It would be canonical on the CoreCLR side so canonicalize here to keep the algorithms similar.
            Instantiation methodInstantiation = interfaceMethod.Instantiation;
            interfaceMethod = interfaceMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

            if (interfaceMethod.Signature.IsStatic)
            {
                return TryResolveStaticConstraintMethodApprox(constrainedType, interfaceType, interfaceMethod, methodInstantiation, ref staticResolution, out usesDeclaredStaticDefault);
            }

            // We can't resolve instance constraint calls effectively for reference types, and there's
            // not a lot of perf. benefit in doing it anyway.
            if (!constrainedType.IsValueType)
            {
                return null;
            }

            return TryResolveInstanceConstraintMethodApprox(constrainedType, interfaceType, interfaceMethod, methodInstantiation, out forceRuntimeLookup);
        }

        private static MethodDesc TryResolveStaticConstraintMethodApprox(TypeDesc constrainedType, TypeDesc interfaceType, MethodDesc interfaceMethod, Instantiation methodInstantiation, ref DefaultInterfaceMethodResolution staticResolution, out bool usesDeclaredStaticDefault)
        {
            usesDeclaredStaticDefault = false;
            // Resolve shared candidates on the definition form, before canonicalization merges
            // distinct type parameters. The fallback below instead needs the exact interface
            // instantiation, when available, so the interface scan runs on the type itself.
            TypeDesc resolutionType = constrainedType.IsCanonicalSubtype(CanonicalFormKind.Any)
                ? constrainedType.GetTypeDefinition() : constrainedType;
            TypeSystemContext context = constrainedType.Context;

            MethodDesc genInterfaceMethod = interfaceMethod.GetMethodDefinition();
            MethodDesc method = null;
            TypeDesc staticInterfaceType = interfaceType;
            if (genInterfaceMethod.OwningType.IsInterface)
            {
                // Sometimes (when compiling shared generic code)
                // we don't have enough exact type information at JIT time
                // even to decide whether we will be able to resolve to an unboxed entry point...
                // To cope with this case we always go via the helper function if there's any
                // chance of this happening by checking for all interfaces which might possibly
                // be compatible with the call (verification will have ensured that
                // at least one of them will be)

                // Gather a candidate from every canonically matching interface. A type and its
                // definition have interface maps of identical shape, so the two RuntimeInterfaces
                // arrays correspond index for index. For a variant interface every entry of the
                // same definition participates. The ECMA-335 augments specify the variance pass
                // of interface resolution as returning the first variant match in interface
                // order and the runtime resolves variant static matches the same way, so a
                // shared body must not pick a later family that an earlier entry wins at exact
                // instantiations.
                int potentialMatchingInterfaces = 0;
                int canonicalMatchingInterfaces = 0;
                TypeDesc matchingInterface = null;
                bool targetsAgree = true;
                bool allUseDeclaredSlot = true;
                bool interfaceTypeHasVariance = interfaceType.HasVariance;
                TypeDesc canonInterfaceType = interfaceType.ConvertToCanonForm(CanonicalFormKind.Specific);
                DefType[] runtimeInterfaces = constrainedType.RuntimeInterfaces;
                for (int interfaceIndex = 0; interfaceIndex < runtimeInterfaces.Length; interfaceIndex++)
                {
                    DefType potentialInterfaceType = runtimeInterfaces[interfaceIndex];
                    bool canonicalMatch = potentialInterfaceType.ConvertToCanonForm(CanonicalFormKind.Specific) == canonInterfaceType;
                    if (!canonicalMatch && !(interfaceTypeHasVariance && potentialInterfaceType.HasSameTypeDefinition(interfaceType)))
                        continue;

                    potentialMatchingInterfaces++;
                    if (canonicalMatch)
                    {
                        canonicalMatchingInterfaces++;
                        matchingInterface = potentialInterfaceType;
                    }

                    TypeDesc resolutionInterface = resolutionType.RuntimeInterfaces[interfaceIndex];
                    MethodDesc potentialInterfaceMethod = genInterfaceMethod;
                    if (potentialInterfaceMethod.OwningType != resolutionInterface)
                    {
                        potentialInterfaceMethod = context.GetMethodForInstantiatedType(
                            potentialInterfaceMethod.GetTypicalMethodDefinition(), (InstantiatedType)resolutionInterface);
                    }

                    MethodDesc candidate = ResolveStaticVirtualCandidate(resolutionType, potentialInterfaceMethod)
                        ?? ResolveDefaultCandidate(resolutionType, potentialInterfaceMethod, out _);

                    allUseDeclaredSlot &= candidate == potentialInterfaceMethod;

                    // Compare the full method, including its owning instantiation.
                    // Check the candidate before preserving a previous non-null result.
                    if (candidate is null || (potentialMatchingInterfaces > 1 && candidate != method))
                        targetsAgree = false;

                    // Do not lose track of `method` if we were able to resolve it previously.
                    method = candidate ?? method;
                }

                // Decide. Agreeing candidates are a resolution on their own, anything else must
                // resolve on the exact instantiation or defer to the fallback and runtime lookup.
                if (potentialMatchingInterfaces > 1)
                {
                    // We have more potentially matching interfaces
                    Debug.Assert(interfaceType.HasInstantiation);

                    bool isExactMethodResolved = targetsAgree && method != null;

                    if (!isExactMethodResolved &&
                        !interfaceType.IsCanonicalSubtype(CanonicalFormKind.Any) &&
                        !interfaceType.IsGenericDefinition &&
                        !constrainedType.IsCanonicalSubtype(CanonicalFormKind.Any) &&
                        !constrainedType.IsGenericDefinition)
                    {
                        // We have exact interface and type instantiations (no generic variables and __Canon used
                        // anywhere)
                        if (constrainedType.CanCastTo(interfaceType))
                        {
                            // We can resolve to exact method
                            MethodDesc exactInterfaceMethod = context.GetMethodForInstantiatedType(
                                genInterfaceMethod.GetTypicalMethodDefinition(), (InstantiatedType)interfaceType);

                            method = ResolveStaticVirtualCandidate(constrainedType, exactInterfaceMethod);
                            isExactMethodResolved = method != null;
                        }
                    }

                    if (!isExactMethodResolved)
                    {
                        // With an invariant interface, each matching slot has the same canonical
                        // owner. If every candidate is its slot, only the exact owner varies.
                        usesDeclaredStaticDefault = allUseDeclaredSlot && !interfaceTypeHasVariance &&
                            constrainedType.IsValueType && !constrainedType.HasInstantiation && resolutionType == constrainedType &&
                            interfaceType == genInterfaceMethod.OwningType && interfaceType.IsCanonicalSubtype(CanonicalFormKind.Specific) &&
                            methodInstantiation.Length == 0 && genInterfaceMethod.IsVirtual && !genInterfaceMethod.IsAbstract &&
                            genInterfaceMethod.RequiresInstMethodTableArg();

                        // Let the fallback handle defaults and exact variant matches.
                        method = null;
                    }
                }
                else
                {
                    if (canonicalMatchingInterfaces == 1)
                        staticInterfaceType = matchingInterface;

                    // If we can resolve the interface exactly then do so (e.g. when doing the exact
                    // lookup at runtime, or when not sharing generic code).
                    if (method is null && constrainedType.CanCastTo(interfaceType))
                    {
                        MethodDesc exactInterfaceMethod = genInterfaceMethod;
                        if (genInterfaceMethod.OwningType != interfaceType)
                            exactInterfaceMethod = context.GetMethodForInstantiatedType(
                                genInterfaceMethod.GetTypicalMethodDefinition(), (InstantiatedType)interfaceType);

                        method = ResolveStaticVirtualCandidate(constrainedType, exactInterfaceMethod);
                    }
                }
            }

            // Fallback. Candidates here resolve on the exact instantiation directly, so they
            // return without the definition-form mapping below.
            if (method is null)
            {
                interfaceType = staticInterfaceType;

                // ResolveVirtualStaticMethod in the C++ only resolves when the interface and method
                // are exact (not shared by generic instantiations). For canonical forms, return null.
                // Note: interfaceMethod was canonicalized above so its instantiation may contain
                // __Canon even when the original was exact. Check methodInstantiation (saved before
                // canonicalization) to match the C++ IsSharedByGenericMethodInstantiations check.
                bool methodHasCanonInstantiation = false;
                foreach (TypeDesc arg in methodInstantiation)
                {
                    if (arg.IsCanonicalSubtype(CanonicalFormKind.Any))
                    {
                        methodHasCanonInstantiation = true;
                        break;
                    }
                }

                if (methodHasCanonInstantiation ||
                    interfaceType.IsCanonicalSubtype(CanonicalFormKind.Any))
                {
                    return null;
                }

                // Check that there is no implementation of the interface on this type which is the canonical interface for a shared generic. If so, that indicates that
                // we cannot exactly compute a target method result, as even if there is an exact match in the type hierarchy
                // it isn't guaranteed that we will always find the right result, as we may find a match on a base type when we should find the match
                // on a more derived type.
                TypeDesc interfaceTypeCanonical = interfaceType.ConvertToCanonForm(CanonicalFormKind.Specific);
                if (interfaceType != interfaceTypeCanonical)
                {
                    foreach (DefType runtimeIface in constrainedType.RuntimeInterfaces)
                    {
                        if (runtimeIface == interfaceTypeCanonical)
                        {
                            return null;
                        }
                    }
                }

                MethodDesc staticMethodDef = interfaceMethod.GetMethodDefinition();
                MethodDesc exactInterfaceMethod = staticMethodDef;
                if (staticMethodDef.OwningType != interfaceType)
                    exactInterfaceMethod = context.GetMethodForInstantiatedType(
                        staticMethodDef.GetTypicalMethodDefinition(), (InstantiatedType)interfaceType);

                MethodDesc result = ResolveStaticVirtualCandidate(constrainedType, exactInterfaceMethod);
                if (result is null)
                {
                    result = ResolveDefaultCandidate(constrainedType, exactInterfaceMethod, out DefaultInterfaceMethodResolution dimResolution);
                    staticResolution = dimResolution;
                }

                if (result is null)
                {
                    return null;
                }

                if (methodInstantiation.Length != 0)
                    result = result.MakeInstantiatedMethod(methodInstantiation);

                return result;
            }

            // Map definition-form results back to the constrained type before instantiating.
            if (method.OwningType.IsInterface)
            {
                if (resolutionType != constrainedType)
                    method = method.InstantiateSignature(constrainedType.Instantiation, default);

                staticResolution = DefaultInterfaceMethodResolution.DefaultImplementation;
            }
            else if (resolutionType != constrainedType)
                method = constrainedType.FindMethodOnTypeWithMatchingTypicalMethod(method);

            // We've resolved the method, ignoring its generic method arguments
            // If the method is a generic method then go and get the instantiated descriptor
            if (methodInstantiation.Length != 0)
            {
                method = method.GetMethodDefinition().MakeInstantiatedMethod(methodInstantiation);
            }

            Debug.Assert(method != null);

            return method;
        }

        private static MethodDesc TryResolveInstanceConstraintMethodApprox(TypeDesc constrainedType, TypeDesc interfaceType, MethodDesc interfaceMethod, Instantiation methodInstantiation, out bool forceRuntimeLookup)
        {
            forceRuntimeLookup = false;
            Debug.Assert(constrainedType.IsValueType);

            TypeDesc canonType = constrainedType.ConvertToCanonForm(CanonicalFormKind.Specific);
            TypeSystemContext context = constrainedType.Context;

            MethodDesc genInterfaceMethod = interfaceMethod.GetMethodDefinition();
            MethodDesc method = null;
            if (genInterfaceMethod.OwningType.IsInterface)
            {
                // Sometimes (when compiling shared generic code)
                // we don't have enough exact type information at JIT time
                // even to decide whether we will be able to resolve to an unboxed entry point...
                // To cope with this case we always go via the helper function if there's any
                // chance of this happening by checking for all interfaces which might possibly
                // be compatible with the call (verification will have ensured that
                // at least one of them will be)

                // Gather the implementation from every canonically matching interface.
                int potentialMatchingInterfaces = 0;
                TypeDesc canonInterfaceType = interfaceType.ConvertToCanonForm(CanonicalFormKind.Specific);
                foreach (DefType potentialInterfaceType in canonType.RuntimeInterfaces)
                {
                    if (potentialInterfaceType.ConvertToCanonForm(CanonicalFormKind.Specific) != canonInterfaceType)
                        continue;

                    potentialMatchingInterfaces++;

                    MethodDesc potentialInterfaceMethod = genInterfaceMethod;
                    if (potentialInterfaceMethod.OwningType != potentialInterfaceType)
                    {
                        potentialInterfaceMethod = context.GetMethodForInstantiatedType(
                            potentialInterfaceMethod.GetTypicalMethodDefinition(), (InstantiatedType)potentialInterfaceType);
                    }

                    // Do not lose track of `method` if we were able to resolve it previously.
                    method = canonType.ResolveInterfaceMethodToVirtualMethodOnType(potentialInterfaceMethod) ?? method;

                    // See code:#TryResolveConstraintMethodApprox_DoNotReturnParentMethod
                    if (method != null && !method.OwningType.IsValueType)
                    {
                        // We explicitly wouldn't want to abort if we found a default implementation.
                        // The above resolution doesn't consider the default methods.
                        Debug.Assert(!method.OwningType.IsInterface);
                        return null;
                    }
                }

                Debug.Assert(potentialMatchingInterfaces != 0);

                // Decide. Multiple matches must resolve on the exact instantiation or defer to
                // runtime lookup.
                if (potentialMatchingInterfaces > 1)
                {
                    // We have more potentially matching interfaces
                    Debug.Assert(interfaceType.HasInstantiation);

                    bool isExactMethodResolved = false;

                    if (!interfaceType.IsCanonicalSubtype(CanonicalFormKind.Any) &&
                        !interfaceType.IsGenericDefinition &&
                        !constrainedType.IsCanonicalSubtype(CanonicalFormKind.Any) &&
                        !constrainedType.IsGenericDefinition)
                    {
                        // We have exact interface and type instantiations (no generic variables and __Canon used
                        // anywhere)
                        if (constrainedType.CanCastTo(interfaceType))
                        {
                            // We can resolve to exact method
                            MethodDesc exactInterfaceMethod = context.GetMethodForInstantiatedType(
                                genInterfaceMethod.GetTypicalMethodDefinition(), (InstantiatedType)interfaceType);

                            method = constrainedType.ResolveVariantInterfaceMethodToVirtualMethodOnType(exactInterfaceMethod);
                            isExactMethodResolved = method != null;
                        }
                    }

                    if (!isExactMethodResolved)
                    {
                        // The candidate can be left incorrect because the caller uses runtime lookup.
                        forceRuntimeLookup = true;
                    }
                }
                else
                {
                    // If we can resolve the interface exactly then do so (e.g. when doing the exact
                    // lookup at runtime, or when not sharing generic code).
                    if (constrainedType.CanCastTo(interfaceType))
                    {
                        MethodDesc exactInterfaceMethod = genInterfaceMethod;
                        if (genInterfaceMethod.OwningType != interfaceType)
                            exactInterfaceMethod = context.GetMethodForInstantiatedType(
                                genInterfaceMethod.GetTypicalMethodDefinition(), (InstantiatedType)interfaceType);

                        method = constrainedType.ResolveVariantInterfaceMethodToVirtualMethodOnType(exactInterfaceMethod)
                            ?? ResolveDefaultCandidate(constrainedType, exactInterfaceMethod, out _);
                    }
                }
            }
            else if (genInterfaceMethod.IsVirtual)
            {
                MethodDesc targetMethod = interfaceType.FindMethodOnTypeWithMatchingTypicalMethod(genInterfaceMethod);
                method = constrainedType.FindVirtualFunctionTargetMethodOnObjectType(targetMethod);
            }
            else
            {
                // The method will be null if calling a non-virtual instance
                // methods on System.Object, i.e. when these are used as a constraint.
                return null;
            }

            if (method == null)
            {
                // Fall back to VSD
                return null;
            }

            //#TryResolveConstraintMethodApprox_DoNotReturnParentMethod
            // Callers pass an unboxed receiver to a resolved target. Inherited methods and
            // instance default interface methods still require boxing.
            if (!method.OwningType.IsValueType)
            {
                // Fall back to VSD
                return null;
            }

            // We've resolved the method, ignoring its generic method arguments
            // If the method is a generic method then go and get the instantiated descriptor
            if (methodInstantiation.Length != 0)
            {
                method = method.GetMethodDefinition().MakeInstantiatedMethod(methodInstantiation);
            }

            Debug.Assert(method != null);

            return method;
        }

        // Resolve one static interface method candidate on the given form of the constrained
        // type, honoring variance where the interface declares it.
        private static MethodDesc ResolveStaticVirtualCandidate(TypeDesc type, MethodDesc interfaceMethod)
        {
            return interfaceMethod.OwningType.HasVariance
                ? type.ResolveVariantInterfaceMethodToStaticVirtualMethodOnType(interfaceMethod)
                : type.ResolveInterfaceMethodToStaticVirtualMethodOnType(interfaceMethod);
        }

        // The default-interface-method complement to the virtual-candidate resolvers, shared by
        // the static and instance halves. Partial resolutions (diamond, reabstraction) are
        // reported but produce no candidate.
        private static MethodDesc ResolveDefaultCandidate(TypeDesc type, MethodDesc interfaceMethod, out DefaultInterfaceMethodResolution resolution)
        {
            MethodDesc candidate;
            resolution = interfaceMethod.OwningType.HasVariance
                ? type.ResolveVariantInterfaceMethodToDefaultImplementationOnType(interfaceMethod, out candidate)
                : type.ResolveInterfaceMethodToDefaultImplementationOnType(interfaceMethod, out candidate);
            return resolution == DefaultInterfaceMethodResolution.DefaultImplementation ? candidate : null;
        }

        // Rebind a resolved constrained target to its runtime-determined declaring context and
        // method arguments. An ambiguous declaring context still requires constrained dispatch.
        public static MethodDesc TryGetRuntimeDeterminedConstrainedMethod(this TypeDesc constrainedType,
            MethodDesc method, Instantiation methodInstantiation)
        {
            TypeDesc owner = TryGetRuntimeDeterminedDeclaringType(constrainedType, method.OwningType);
            if (owner == null)
                return null;

            MethodDesc result;
            if (owner is RuntimeDeterminedType runtimeOwner)
                result = method.Context.GetMethodForRuntimeDeterminedType(method.GetTypicalMethodDefinition(), runtimeOwner);
            else if (owner is InstantiatedType instantiatedOwner)
                result = method.Context.GetMethodForInstantiatedType(method.GetTypicalMethodDefinition(), instantiatedOwner);
            else
                result = method.GetMethodDefinition();

            if (result.HasInstantiation)
                result = result.MakeInstantiatedMethod(methodInstantiation);

            Debug.Assert(result.GetCanonMethodTarget(CanonicalFormKind.Specific) == method.GetCanonMethodTarget(CanonicalFormKind.Specific));
            return result;
        }

        private static TypeDesc TryGetRuntimeDeterminedDeclaringType(TypeDesc constrainedType, TypeDesc declaringType)
        {
            TypeDesc constraintDefinition = constrainedType is RuntimeDeterminedType runtimeType
                ? runtimeType.CanonicalType : constrainedType;
            if (constraintDefinition.HasSameTypeDefinition(declaringType))
                return constrainedType;

            if (constrainedType.IsRuntimeDeterminedType)
                return null;

            TypeDesc canonicalOwner = declaringType.ConvertToCanonForm(CanonicalFormKind.Specific);
            if (!declaringType.IsInterface)
            {
                for (TypeDesc current = constrainedType.BaseType; current != null; current = current.BaseType)
                {
                    if (current.ConvertToCanonForm(CanonicalFormKind.Specific) == canonicalOwner)
                        return current;
                }
                Debug.Fail("Not a base?");
                return null;
            }

            TypeDesc result = null;
            foreach (DefType iface in constrainedType.RuntimeInterfaces)
            {
                if (iface.ConvertToCanonForm(CanonicalFormKind.Specific) != canonicalOwner)
                    continue;

                if (result != null && result != iface)
                    return null;

                result = iface;
            }
            Debug.Assert(result != null, "Not an implemented interface?");
            return result;
        }

        private static TypeDesc MergeClassWithInterface(TypeDesc type, TypeDesc interfaceType)
        {
            // Check if the class implements the interface
            if (type.ImplementsEquivalentInterface(interfaceType))
            {
                return interfaceType;
            }

            // Check if the class and the interface implement a common interface
            foreach (var potentialCommonInterface in interfaceType.RuntimeInterfaces)
            {
                if (type.ImplementsEquivalentInterface(potentialCommonInterface))
                {
                    // Found a common interface.  If there are multiple common interfaces, then
                    // the problem is ambiguous so we'll just take the first one--it's the best
                    // we can do.
                    return potentialCommonInterface;
                }
            }

            // No compatible merge found - using Object
            return type.Context.GetWellKnownType(WellKnownType.Object);
        }

        /// <summary>
        /// Normalizes canonical instantiations (converts Foo&lt;object, __Canon&gt; to
        /// Foo&lt;__Canon, __Canon>). Returns identity for non-canonical types.
        /// </summary>
        public static TypeDesc NormalizeInstantiation(this TypeDesc thisType)
        {
            if (thisType.IsCanonicalSubtype(CanonicalFormKind.Any))
                return thisType.ConvertToCanonForm(CanonicalFormKind.Specific);

            return thisType;
        }

        public static Instantiation GetInstantiationThatMeetsConstraints(Instantiation inst, bool allowCanon)
        {
            TypeDesc[] resultArray = new TypeDesc[inst.Length];
            for (int i = 0; i < inst.Length; i++)
            {
                TypeDesc instArg = GetTypeThatMeetsConstraints((GenericParameterDesc)inst[i], allowCanon);
                if (instArg == null)
                    return default(Instantiation);
                resultArray[i] = instArg;
            }

            return new Instantiation(resultArray);
        }

        private static TypeDesc GetTypeThatMeetsConstraints(GenericParameterDesc genericParam, bool allowCanon)
        {
            TypeSystemContext context = genericParam.Context;

            // Universal canon is the best option if it's supported
            if (allowCanon && context.SupportsUniversalCanon)
                return context.UniversalCanonType;

            // Not nullable type is the only thing where we can't substitute reference types
            GenericConstraints constraints = genericParam.Constraints;
            if ((constraints & GenericConstraints.NotNullableValueTypeConstraint) != 0)
                return null;

            // If canon is allowed, we can use that
            if (allowCanon && context.SupportsCanon)
            {
                foreach (var c in genericParam.TypeConstraints)
                {
                    // Could be e.g. "where T : U"
                    // We could try to dig into the U and solve it, but that just opens us up to
                    // recursion and it's just not worth it.
                    if (c.IsSignatureVariable)
                        return null;

                    if (!c.IsGCPointer)
                        return null;
                }

                return genericParam.Context.CanonType;
            }

            // If canon is not allowed, we're limited in our choices.
            TypeDesc constrainedType = null;
            foreach (var c in genericParam.TypeConstraints)
            {
                // Can't do multiple constraints
                if (constrainedType != null)
                    return null;

                // Could be e.g. "where T : IFoo<U>" or "where T : U"
                if (c.ContainsSignatureVariables())
                    return null;

                // If there's unimplemented static abstract methods, this is not a suitable instantiation.
                // We shortcut to look for any static virtuals. It matches what Roslyn does for error CS8920.
                // Once TypeSystemConstraintsHelpers is updated to check constraints around static virtuals,
                // we could dispatch there instead.
                if (c.IsInterface)
                {
                    if (HasStaticVirtualMethods(c))
                        return null;

                    foreach (DefType intface in c.RuntimeInterfaces)
                        if (HasStaticVirtualMethods(intface))
                            return null;

                    static bool HasStaticVirtualMethods(TypeDesc type)
                    {
                        foreach (MethodDesc method in type.GetVirtualMethods())
                            if (method.Signature.IsStatic)
                                return true;
                        return false;
                    }
                }

                constrainedType = c;
            }

            return constrainedType ?? genericParam.Context.GetWellKnownType(WellKnownType.Object);
        }

        public static bool ContainsSignatureVariables(this Instantiation instantiation, bool treatGenericParameterLikeSignatureVariable = false)
        {
            foreach (var arg in instantiation)
            {
                if (arg.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if <paramref name="method"/> is an actual native entrypoint.
        /// There's a distinction between when a method reports it's a PInvoke in the metadata
        /// versus how it's treated in the compiler. For many PInvoke methods the compiler will generate
        /// an IL body. The methods with an IL method body shouldn't be treated as PInvoke within the compiler.
        /// </summary>
        public static bool IsRawPInvoke(this MethodDesc method)
        {
            return method.IsPInvoke && (method is Internal.IL.Stubs.PInvokeTargetNativeMethod);
        }

        public static bool IsDynamicInterfaceCastableImplementation(this MetadataType interfaceType)
        {
            Debug.Assert(interfaceType.IsInterface);
            return interfaceType.HasCustomAttribute("System.Runtime.InteropServices", "DynamicInterfaceCastableImplementationAttribute");
        }

        public static bool HasImpliedRepeatedFields(this MetadataType mdType)
        {
            if (mdType.IsInlineArray)
            {
                return true;
            }

            // If the type is not an [InlineArray] type, do a best-effort detection of whether the type is a fixed buffer type
            // as emitted by the C# compiler.

            if (!mdType.IsSequentialLayout)
            {
                return false;
            }

            if (mdType.GetClassLayout().Size == 0)
            {
                // Unsafe fixed buffers have a specified size in the class layout information.
                return false;
            }

            FieldDesc firstField = null;
            foreach (FieldDesc field in mdType.GetFields())
            {
                if (!field.IsStatic)
                {
                    // A type is only an unsafe fixed buffer type if it has exactly one field.
                    if (firstField is not null)
                    {
                        return false;
                    }
                    firstField = field;
                }
            }

            if (firstField is null)
            {
                return false;
            }
            TypeDesc firstFieldElementType = firstField.FieldType;

            // A fixed buffer type is always a value type that has exactly one value type field at offset 0
            // and whose size is an exact multiple of the size of the field.
            // It is possible that we catch a false positive with this check, but that chance is extremely slim
            // and the user can always change their structure to something more descriptive of what they want
            // instead of adding additional padding at the end of a one-field structure.
            // We do this check here to save looking up the FixedBufferAttribute when loading the field
            // from metadata.
            return firstFieldElementType.IsValueType
                    && firstField.Offset.AsInt == 0
                    && ((mdType.GetElementSize().AsInt % firstFieldElementType.GetElementSize().AsInt) == 0);
        }
    }
}
