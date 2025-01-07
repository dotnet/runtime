// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            return ((CompilerTypeSystemContext)type.Context).GetClosestDefType(type);
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
        /// Gets the value of the field ordinal. Ordinals are computed by also including static fields, but excluding
        /// literal fields and fields with RVAs.
        /// </summary>
        public static int GetFieldOrdinal(this FieldDesc inputField)
        {
            // Make sure we are asking the question for a valid instance or static field
            Debug.Assert(!inputField.HasRva && !inputField.IsLiteral);

            int fieldOrdinal = 0;
            foreach (FieldDesc field in inputField.OwningType.GetFields())
            {
                // If this field does not contribute to layout, skip
                if (field.HasRva || field.IsLiteral)
                    continue;

                if (field == inputField)
                    return fieldOrdinal;

                fieldOrdinal++;
            }

            Debug.Assert(false);
            return -1;
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

        public static MethodDesc TryResolveConstraintMethodApprox(this TypeDesc constrainedType, TypeDesc interfaceType, MethodDesc interfaceMethod, out bool forceRuntimeLookup)
        {
            return TryResolveConstraintMethodApprox(constrainedType, interfaceType, interfaceMethod, out forceRuntimeLookup, ref Unsafe.NullRef<DefaultInterfaceMethodResolution>());
        }

        /// <summary>
        /// Attempts to resolve constrained call to <paramref name="interfaceMethod"/> into a concrete non-unboxing
        /// method on <paramref name="constrainedType"/>.
        /// The ability to resolve constraint methods is affected by the degree of code sharing we are performing
        /// for generic code.
        /// </summary>
        /// <returns>The resolved method or null if the constraint couldn't be resolved.</returns>
        public static MethodDesc TryResolveConstraintMethodApprox(this TypeDesc constrainedType, TypeDesc interfaceType, MethodDesc interfaceMethod, out bool forceRuntimeLookup, ref DefaultInterfaceMethodResolution staticResolution)
        {
            forceRuntimeLookup = false;

            bool isStaticVirtualMethod = interfaceMethod.Signature.IsStatic;

            // We can't resolve constraint calls effectively for reference types, and there's
            // not a lot of perf. benefit in doing it anyway.
            if (!constrainedType.IsValueType && (!isStaticVirtualMethod || constrainedType.IsCanonicalDefinitionType(CanonicalFormKind.Any)))
            {
                return null;
            }

            // Interface method may or may not be fully canonicalized here.
            // It would be canonical on the CoreCLR side so canonicalize here to keep the algorithms similar.
            Instantiation methodInstantiation = interfaceMethod.Instantiation;
            interfaceMethod = interfaceMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

            // 1. Find the (possibly generic) method that would implement the
            // constraint if we were making a call on a boxed value type.

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

                // Enumerate all potential interface instantiations
                int potentialMatchingInterfaces = 0;
                foreach (DefType potentialInterfaceType in canonType.RuntimeInterfaces)
                {
                    if (potentialInterfaceType.ConvertToCanonForm(CanonicalFormKind.Specific) ==
                        interfaceType.ConvertToCanonForm(CanonicalFormKind.Specific))
                    {
                        potentialMatchingInterfaces++;

                        // The below code is just trying to prevent one of the matches from requiring boxing
                        // It doesn't apply to static virtual methods.
                        if (isStaticVirtualMethod)
                            continue;

                        MethodDesc potentialInterfaceMethod = genInterfaceMethod;
                        if (potentialInterfaceMethod.OwningType != potentialInterfaceType)
                        {
                            potentialInterfaceMethod = context.GetMethodForInstantiatedType(
                                potentialInterfaceMethod.GetTypicalMethodDefinition(), (InstantiatedType)potentialInterfaceType);
                        }

                        method = canonType.ResolveInterfaceMethodToVirtualMethodOnType(potentialInterfaceMethod);

                        // See code:#TryResolveConstraintMethodApprox_DoNotReturnParentMethod
                        if (method != null && !method.OwningType.IsValueType)
                        {
                            // We explicitly wouldn't want to abort if we found a default implementation.
                            // The above resolution doesn't consider the default methods.
                            Debug.Assert(!method.OwningType.IsInterface);
                            return null;
                        }
                    }
                }

                Debug.Assert(potentialMatchingInterfaces != 0);

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
                            if (isStaticVirtualMethod)
                            {
                                method = constrainedType.ResolveVariantInterfaceMethodToStaticVirtualMethodOnType(exactInterfaceMethod);
                                if (method == null)
                                {
                                    staticResolution = constrainedType.ResolveVariantInterfaceMethodToDefaultImplementationOnType(exactInterfaceMethod, out method);
                                    if (staticResolution != DefaultInterfaceMethodResolution.DefaultImplementation)
                                        method = null;
                                }
                            }
                            else
                            {
                                method = constrainedType.ResolveVariantInterfaceMethodToVirtualMethodOnType(exactInterfaceMethod);
                            }
                            isExactMethodResolved = method != null;
                        }
                    }

                    if (!isExactMethodResolved)
                    {
                        // We couldn't resolve the interface statically
                        // Notify the caller that it should use runtime lookup
                        // Note that we can leave pMD incorrect, because we will use runtime lookup
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
                        if (isStaticVirtualMethod)
                        {
                            method = constrainedType.ResolveVariantInterfaceMethodToStaticVirtualMethodOnType(exactInterfaceMethod);
                            if (method == null)
                            {
                                staticResolution = constrainedType.ResolveVariantInterfaceMethodToDefaultImplementationOnType(exactInterfaceMethod, out method);
                                if (staticResolution != DefaultInterfaceMethodResolution.DefaultImplementation)
                                    method = null;
                            }
                        }
                        else
                        {
                            method = constrainedType.ResolveVariantInterfaceMethodToVirtualMethodOnType(exactInterfaceMethod);
                        }
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
                method = null;
            }

            if (method == null)
            {
                // Fall back to VSD
                return null;
            }

            //#TryResolveConstraintMethodApprox_DoNotReturnParentMethod
            // Only return a method if the value type itself declares the method,
            // otherwise we might get a method from Object or System.ValueType
            if (!isStaticVirtualMethod && !method.OwningType.IsValueType)
            {
                // Fall back to VSD
                return null;
            }

            // We've resolved the method, ignoring its generic method arguments
            // If the method is a generic method then go and get the instantiated descriptor
            if (methodInstantiation.Length != 0)
            {
                method = method.MakeInstantiatedMethod(methodInstantiation);
            }

            // It's difficult to discern what runtime determined form the interface method
            // is on later so fail the resolution if this would be that.
            // This is pretty conservative and can be narrowed down.
            if (method.IsCanonicalMethod(CanonicalFormKind.Any)
                && !method.OwningType.IsValueType)
            {
                Debug.Assert(method.Signature.IsStatic);
                return null;
            }

            Debug.Assert(method != null);

            return method;
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
