// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    public static class TypeSystemHelpers
    {
        public static bool IsWellKnownType(this TypeDesc type, WellKnownType wellKnownType)
        {
            return type == type.Context.GetWellKnownType(wellKnownType, false);
        }

        public static InstantiatedType MakeInstantiatedType(this MetadataType typeDef, Instantiation instantiation)
        {
            return typeDef.Context.GetInstantiatedType(typeDef, instantiation);
        }

        public static InstantiatedType MakeInstantiatedType(this MetadataType typeDef, params TypeDesc[] genericParameters)
        {
            return typeDef.Context.GetInstantiatedType(typeDef, new Instantiation(genericParameters));
        }


        public static InstantiatedMethod MakeInstantiatedMethod(this MethodDesc methodDef, Instantiation instantiation)
        {
            return methodDef.Context.GetInstantiatedMethod(methodDef, instantiation);
        }

        public static InstantiatedMethod MakeInstantiatedMethod(this MethodDesc methodDef, params TypeDesc[] genericParameters)
        {
            return methodDef.Context.GetInstantiatedMethod(methodDef, new Instantiation(genericParameters));
        }

        public static ArrayType MakeArrayType(this TypeDesc type)
        {
            return type.Context.GetArrayType(type);
        }

        /// <summary>
        /// Creates a multidimensional array type with the specified rank.
        /// To create a vector, use the <see cref="MakeArrayType(TypeDesc)"/> overload.
        /// </summary>
        public static ArrayType MakeArrayType(this TypeDesc type, int rank)
        {
            return type.Context.GetArrayType(type, rank);
        }

        public static ByRefType MakeByRefType(this TypeDesc type)
        {
            return type.Context.GetByRefType(type);
        }

        public static PointerType MakePointerType(this TypeDesc type)
        {
            return type.Context.GetPointerType(type);
        }

        public static TypeDesc GetParameterType(this TypeDesc type)
        {
            ParameterizedType paramType = (ParameterizedType)type;
            return paramType.ParameterType;
        }

        public static bool HasLayout(this MetadataType mdType)
        {
            return mdType.IsSequentialLayout || mdType.IsExplicitLayout;
        }

        public static LayoutInt GetElementSize(this TypeDesc type)
        {
            if (type.IsValueType)
            {
                return ((DefType)type).InstanceFieldSize;
            }
            else
            {
                return type.Context.Target.LayoutPointerSize;
            }
        }

        /// <summary>
        /// Gets the parameterless instance constructor on the specified type. To get the default constructor, use <see cref="TypeDesc.GetDefaultConstructor"/>.
        /// </summary>
        public static MethodDesc GetParameterlessConstructor(this TypeDesc type)
        {
            // TODO: Do we want check for specialname/rtspecialname? Maybe add another overload on GetMethod?
            var sig = new MethodSignature(0, 0, type.Context.GetWellKnownType(WellKnownType.Void), TypeDesc.EmptyTypes);
            return type.GetMethod(".ctor", sig);
        }

        public static bool HasExplicitOrImplicitDefaultConstructor(this TypeDesc type)
        {
            return type.IsValueType || type.GetDefaultConstructor() != null;
        }

        internal static MethodDesc FindMethodOnExactTypeWithMatchingTypicalMethod(this TypeDesc type, MethodDesc method)
        {
            MethodDesc methodTypicalDefinition = method.GetTypicalMethodDefinition();

            var instantiatedType = type as InstantiatedType;
            if (instantiatedType != null)
            {
                Debug.Assert(instantiatedType.GetTypeDefinition() == methodTypicalDefinition.OwningType);
                return method.Context.GetMethodForInstantiatedType(methodTypicalDefinition, instantiatedType);
            }
            else if (type.IsArray)
            {
                Debug.Assert(method.OwningType.IsArray);
                return ((ArrayType)type).GetArrayMethod(((ArrayMethod)method).Kind);
            }
            else
            {
                Debug.Assert(type == methodTypicalDefinition.OwningType);
                return methodTypicalDefinition;
            }
        }

        /// <summary>
        /// Returns method as defined on a non-generic base class or on a base
        /// instantiation.
        /// For example, If Foo&lt;T&gt; : Bar&lt;T&gt; and overrides method M,
        /// if method is Bar&lt;string&gt;.M(), then this returns Bar&lt;T&gt;.M()
        /// but if Foo : Bar&lt;string&gt;, then this returns Bar&lt;string&gt;.M()
        /// </summary>
        /// <param name="targetType">A potentially derived type</param>
        /// <param name="method">A base class's virtual method</param>
        public static MethodDesc FindMethodOnTypeWithMatchingTypicalMethod(this TypeDesc targetType, MethodDesc method)
        {
            // If method is nongeneric and on a nongeneric type, then it is the matching method
            if (!method.HasInstantiation && !method.OwningType.HasInstantiation)
            {
                return method;
            }

            // Since method is an instantiation that may or may not be the same as typeExamine's hierarchy,
            // find a matching base class on an open type and then work from the instantiation in typeExamine's
            // hierarchy
            TypeDesc typicalTypeOfTargetMethod = method.GetTypicalMethodDefinition().OwningType;
            TypeDesc targetOrBase = targetType;
            do
            {
                TypeDesc openTargetOrBase = targetOrBase;
                if (openTargetOrBase is InstantiatedType)
                {
                    openTargetOrBase = openTargetOrBase.GetTypeDefinition();
                }
                if (openTargetOrBase == typicalTypeOfTargetMethod)
                {
                    // Found an open match. Now find an equivalent method on the original target typeOrBase
                    MethodDesc matchingMethod = targetOrBase.FindMethodOnExactTypeWithMatchingTypicalMethod(method);
                    return matchingMethod;
                }
                targetOrBase = targetOrBase.BaseType;
            } while (targetOrBase != null);

            Debug.Fail("method has no related type in the type hierarchy of type");
            return null;
        }

        /// <summary>
        /// Attempts to resolve constrained call to <paramref name="interfaceMethod"/> into a concrete non-unboxing
        /// method on <paramref name="constrainedType"/>.
        /// The ability to resolve constraint methods is affected by the degree of code sharing we are performing
        /// for generic code.
        /// </summary>
        /// <returns>The resolved method or null if the constraint couldn't be resolved.</returns>
        public static MethodDesc TryResolveConstraintMethodApprox(this TypeDesc constrainedType, TypeDesc interfaceType, MethodDesc interfaceMethod, out bool forceRuntimeLookup)
        {
            forceRuntimeLookup = false;

            // We can't resolve constraint calls effectively for reference types, and there's
            // not a lot of perf. benefit in doing it anyway.
            if (!constrainedType.IsValueType)
            {
                return null;
            }

            // Non-virtual methods called through constraints simply resolve to the specified method without constraint resolution.
            if (!interfaceMethod.IsVirtual)
            {
                return null;
            }

            MethodDesc method;

            MethodDesc genInterfaceMethod = interfaceMethod.GetMethodDefinition();
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

                // TODO: this code assumes no shared generics
                Debug.Assert(interfaceType == interfaceMethod.OwningType);

                method = constrainedType.ResolveInterfaceMethodToVirtualMethodOnType(genInterfaceMethod);
            }
            else if (genInterfaceMethod.IsVirtual)
            {
                method = constrainedType.FindVirtualFunctionTargetMethodOnObjectType(genInterfaceMethod);
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
            if (!method.OwningType.IsValueType)
            {
                // Fall back to VSD
                return null;
            }

            // We've resolved the method, ignoring its generic method arguments
            // If the method is a generic method then go and get the instantiated descriptor
            if (interfaceMethod.HasInstantiation)
            {
                method = method.MakeInstantiatedMethod(interfaceMethod.Instantiation);
            }

            Debug.Assert(method != null);
            //assert(!pMD->IsUnboxingStub());

            return method;
        }

        /// <summary>
        /// Retrieves the namespace qualified name of a <see cref="DefType"/>.
        /// </summary>
        public static string GetFullName(this DefType metadataType)
        {
            string ns = metadataType.Namespace;
            return ns.Length > 0 ? string.Concat(ns, ".", metadataType.Name) : metadataType.Name;
        }

        /// <summary>
        /// Retrieves all methods on a type, including the ones injected by the type system context.
        /// </summary>
        public static IEnumerable<MethodDesc> GetAllMethods(this TypeDesc type)
        {
            return type.Context.GetAllMethods(type);
        }

        /// <summary>
        /// Retrieves all virtual methods on a type, including the ones injected by the type system context.
        /// </summary>
        public static IEnumerable<MethodDesc> GetAllVirtualMethods(this TypeDesc type)
        {
            return type.Context.GetAllVirtualMethods(type);
        }

        public static IEnumerable<MethodDesc> EnumAllVirtualSlots(this TypeDesc type)
        {
            return type.Context.GetVirtualMethodAlgorithmForType(type).ComputeAllVirtualSlots(type);
        }

        /// <summary>
        /// Resolves interface method '<paramref name="interfaceMethod"/>' to a method on '<paramref name="type"/>'
        /// that implements the the method.
        /// </summary>
        public static MethodDesc ResolveInterfaceMethodToVirtualMethodOnType(this TypeDesc type, MethodDesc interfaceMethod)
        {
            return type.Context.GetVirtualMethodAlgorithmForType(type).ResolveInterfaceMethodToVirtualMethodOnType(interfaceMethod, type);
        }

        public static MethodDesc ResolveVariantInterfaceMethodToVirtualMethodOnType(this TypeDesc type, MethodDesc interfaceMethod)
        {
            return type.Context.GetVirtualMethodAlgorithmForType(type).ResolveVariantInterfaceMethodToVirtualMethodOnType(interfaceMethod, type);
        }

        public static DefaultInterfaceMethodResolution ResolveInterfaceMethodToDefaultImplementationOnType(this TypeDesc type, MethodDesc interfaceMethod, out MethodDesc implMethod)
        {
            return type.Context.GetVirtualMethodAlgorithmForType(type).ResolveInterfaceMethodToDefaultImplementationOnType(interfaceMethod, type, out implMethod);
        }

        /// <summary>
        /// Resolves a virtual method call.
        /// </summary>
        public static MethodDesc FindVirtualFunctionTargetMethodOnObjectType(this TypeDesc type, MethodDesc targetMethod)
        {
            return type.Context.GetVirtualMethodAlgorithmForType(type).FindVirtualFunctionTargetMethodOnObjectType(targetMethod, type);
        }

        /// <summary>
        /// Creates an open instantiation of a type. Given Foo&lt;T&gt;, returns Foo&lt;!0&gt;.
        /// If the type is not generic, returns the <paramref name="type"/>.
        /// </summary>
        public static TypeDesc InstantiateAsOpen(this TypeDesc type)
        {
            if (!type.IsGenericDefinition)
            {
                Debug.Assert(!type.HasInstantiation);
                return type;
            }

            TypeSystemContext context = type.Context;

            var inst = new TypeDesc[type.Instantiation.Length];
            for (int i = 0; i < inst.Length; i++)
            {
                inst[i] = context.GetSignatureVariable(i, false);
            }

            return context.GetInstantiatedType((MetadataType)type, new Instantiation(inst));
        }

        /// <summary>
        /// Creates an open instantiation of a field. Given Foo&lt;T&gt;.Field, returns
        /// Foo&lt;!0&gt;.Field. If the owning type is not generic, returns the <paramref name="field"/>.
        /// </summary>
        public static FieldDesc InstantiateAsOpen(this FieldDesc field)
        {
            Debug.Assert(field.GetTypicalFieldDefinition() == field);

            TypeDesc owner = field.OwningType;

            if (owner.HasInstantiation)
            {
                var instantiatedOwner = (InstantiatedType)owner.InstantiateAsOpen();
                return field.Context.GetFieldForInstantiatedType(field, instantiatedOwner);
            }

            return field;
        }

        /// <summary>
        /// Creates an open instantiation of a method. Given Foo&lt;T&gt;.Method, returns
        /// Foo&lt;!0&gt;.Method. If the owning type is not generic, returns the <paramref name="method"/>.
        /// </summary>
        public static MethodDesc InstantiateAsOpen(this MethodDesc method)
        {
            Debug.Assert(method.IsMethodDefinition && !method.HasInstantiation);

            TypeDesc owner = method.OwningType;

            if (owner.HasInstantiation)
            {
                MetadataType instantiatedOwner = (MetadataType)owner.InstantiateAsOpen();
                return method.Context.GetMethodForInstantiatedType(method, (InstantiatedType)instantiatedOwner);
            }

            return method;
        }

        /// <summary>
        /// Scan the type and its base types for an implementation of an interface method. Returns null if no
        /// implementation is found.
        /// </summary>
        public static MethodDesc ResolveInterfaceMethodTarget(this TypeDesc thisType, MethodDesc interfaceMethodToResolve)
        {
            Debug.Assert(interfaceMethodToResolve.OwningType.IsInterface);

            MethodDesc result = null;
            TypeDesc currentType = thisType;
            do
            {
                result = currentType.ResolveInterfaceMethodToVirtualMethodOnType(interfaceMethodToResolve);
                currentType = currentType.BaseType;
            }
            while (result == null && currentType != null);

            return result;
        }

        /// <summary>
        /// Scan the type and its base types for an implementation of an interface method. Returns null if no
        /// implementation is found.
        /// </summary>
        public static MethodDesc ResolveInterfaceMethodTargetWithVariance(this TypeDesc thisType, MethodDesc interfaceMethodToResolve)
        {
            Debug.Assert(interfaceMethodToResolve.OwningType.IsInterface);

            MethodDesc result = null;
            TypeDesc currentType = thisType;
            do
            {
                result = currentType.ResolveVariantInterfaceMethodToVirtualMethodOnType(interfaceMethodToResolve);
                currentType = currentType.BaseType;
            }
            while (result == null && currentType != null);

            return result;
        }

        public static bool ContainsSignatureVariables(this TypeDesc thisType, bool treatGenericParameterLikeSignatureVariable = false)
        {
            switch (thisType.Category)
            {
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                case TypeFlags.ByRef:
                case TypeFlags.Pointer:
                    return ((ParameterizedType)thisType).ParameterType.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable);

                case TypeFlags.FunctionPointer:
                    MethodSignature pointerSignature = ((FunctionPointerType)thisType).Signature;

                    for (int i = 0; i < pointerSignature.Length; i++)
                        if (pointerSignature[i].ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable))
                            return true;

                    return pointerSignature.ReturnType.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable);

                case TypeFlags.SignatureMethodVariable:
                case TypeFlags.SignatureTypeVariable:
                    return true;

                case TypeFlags.GenericParameter:
                    if (treatGenericParameterLikeSignatureVariable)
                        return true;
                    // It is generally a bug to have instantiations over generic parameters
                    // in the system. Typical instantiations are represented as instantiations
                    // over own formals - so these should be signature variables instead.
                    throw new ArgumentException();

                default:
                    Debug.Assert(thisType is DefType);
                    foreach (TypeDesc arg in thisType.Instantiation)
                    {
                        if (arg.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable))
                            return true;
                    }

                    return false;
            }
        }

        /// <summary>
        /// Check if MethodImpl requires slot unification.
        /// </summary>
        /// <param name="method">Method to check</param>
        /// <returns>True when the method is marked with the PreserveBaseOverrides custom attribute, false otherwise.</returns>
        public static bool RequiresSlotUnification(this MethodDesc method)
        {
            if (method.HasCustomAttribute("System.Runtime.CompilerServices", "PreserveBaseOverridesAttribute"))
            {
#if DEBUG
                // We shouldn't be calling this for non-MethodImpls, so verify that the method being checked is really a MethodImpl
                MetadataType mdType = method.OwningType as MetadataType;
                if (mdType != null)
                {
                    bool isMethodImpl = false;
                    foreach (MethodImplRecord methodImplRecord in mdType.VirtualMethodImplsForType)
                    {
                        if (method == methodImplRecord.Body)
                        {
                            isMethodImpl = true;
                            break;
                        }
                    }
                    Debug.Assert(isMethodImpl);
                }
#endif
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether an object of type '<paramref name="type"/>' requires 8-byte alignment on
        /// 32bit ARM or 32bit Wasm architectures.
        /// </summary>
        public static bool RequiresAlign8(this TypeDesc type)
        {
            if (type.Context.Target.Architecture != TargetArchitecture.ARM && type.Context.Target.Architecture != TargetArchitecture.Wasm32)
            {
                return false;
            }

            if (type.IsArray)
            {
                var elementType = ((ArrayType)type).ElementType;
                if (elementType.IsValueType)
                {
                    var alignment = ((DefType)elementType).InstanceByteAlignment;
                    if (!alignment.IsIndeterminate && alignment.AsInt > 4)
                    {
                        return true;
                    }
                }
            }
            else if (type.IsDefType)
            {
                var alignment = ((DefType)type).InstanceByteAlignment;
                if (!alignment.IsIndeterminate && alignment.AsInt > 4)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
