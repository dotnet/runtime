// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Runtime.General;

using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;
using Internal.Runtime.CompilerServices;

using Internal.NativeFormat;
using Internal.TypeSystem;
using Internal.TypeSystem.NativeFormat;
using Internal.TypeSystem.NoMetadata;

namespace Internal.Runtime.TypeLoader
{
    public abstract class GenericDictionaryCell
    {
        internal abstract void Prepare(TypeBuilder builder);
        internal abstract IntPtr Create(TypeBuilder builder);
        internal unsafe virtual void WriteCellIntoDictionary(TypeBuilder typeBuilder, IntPtr* pDictionary, int slotIndex)
        {
            pDictionary[slotIndex] = Create(typeBuilder);
        }

        internal virtual IntPtr CreateLazyLookupCell(TypeBuilder builder, out IntPtr auxResult)
        {
            auxResult = IntPtr.Zero;
            return Create(builder);
        }

        // Helper method for nullable transform. Ideally, we would do the nullable transform upfront before
        // the types is build. Unfortunately, there does not seem to be easy way to test for Nullable<> type definition
        // without introducing type builder recursion
        private static RuntimeTypeHandle GetRuntimeTypeHandleWithNullableTransform(TypeBuilder builder, TypeDesc type)
        {
            RuntimeTypeHandle th = builder.GetRuntimeTypeHandle(type);
            if (RuntimeAugments.IsNullable(th))
                th = builder.GetRuntimeTypeHandle(((DefType)type).Instantiation[0]);
            return th;
        }

        private class PointerToOtherDictionarySlotCell : GenericDictionaryCell
        {
            internal uint OtherDictionarySlot;

            internal override void Prepare(TypeBuilder builder) { }
            internal override IntPtr Create(TypeBuilder builder)
            {
                // This api should never be called. The intention is that this cell is special
                // cased to have a value which is relative to other cells being emitted.
                throw new NotSupportedException();
            }

            internal unsafe override void WriteCellIntoDictionary(TypeBuilder typeBuilder, IntPtr* pDictionary, int slotIndex)
            {
                pDictionary[slotIndex] = new IntPtr(pDictionary + OtherDictionarySlot);
            }
        }

        public static GenericDictionaryCell CreateTypeHandleCell(TypeDesc type)
        {
            TypeHandleCell typeCell = new TypeHandleCell();
            typeCell.Type = type;
            return typeCell;
        }

        private class TypeHandleCell : GenericDictionaryCell
        {
            internal TypeDesc Type;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Canonical types do not have EETypes");

                builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                return builder.GetRuntimeTypeHandle(Type).ToIntPtr();
            }
        }

        private class UnwrapNullableTypeCell : GenericDictionaryCell
        {
            internal DefType Type;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Canonical types do not have EETypes");

                if (Type.IsNullable)
                {
                    Debug.Assert(Type.Instantiation.Length == 1);
                    builder.RegisterForPreparation(Type.Instantiation[0]);
                }
                else
                    builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                if (Type.IsNullable)
                    return builder.GetRuntimeTypeHandle(Type.Instantiation[0]).ToIntPtr();
                else
                    return builder.GetRuntimeTypeHandle(Type).ToIntPtr();
            }
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        public static GenericDictionaryCell CreateInterfaceCallCell(TypeDesc interfaceType, int slot)
        {
            InterfaceCallCell dispatchCell = new InterfaceCallCell();
            dispatchCell.InterfaceType = interfaceType;
            dispatchCell.Slot = slot;
            return dispatchCell;
        }
#endif

        private class InterfaceCallCell : GenericDictionaryCell
        {
            internal TypeDesc InterfaceType;
            internal int Slot;

            internal override void Prepare(TypeBuilder builder)
            {
                if (InterfaceType.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Unable to compute call information for a canonical interface");

                builder.RegisterForPreparation(InterfaceType);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                return RuntimeAugments.NewInterfaceDispatchCell(builder.GetRuntimeTypeHandle(InterfaceType), Slot);
            }
        }

#if FEATURE_UNIVERSAL_GENERICS
        /// <summary>
        /// Used for non-generic Direct Call Constrained Methods
        /// </summary>
        private class NonGenericDirectConstrainedMethodCell : GenericDictionaryCell
        {
            internal TypeDesc ConstraintType;
            internal TypeDesc ConstrainedMethodType;
            internal int ConstrainedMethodSlot;

            internal override void Prepare(TypeBuilder builder)
            {
                if (ConstraintType.IsCanonicalSubtype(CanonicalFormKind.Any) || ConstrainedMethodType.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Unable to compute call information for a canonical type/method.");

                builder.RegisterForPreparation(ConstraintType);
                builder.RegisterForPreparation(ConstrainedMethodType);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                return ConstrainedCallSupport.NonGenericConstrainedCallDesc.GetDirectConstrainedCallPtr(builder.GetRuntimeTypeHandle(ConstraintType),
                                                                                builder.GetRuntimeTypeHandle(ConstrainedMethodType),
                                                                                ConstrainedMethodSlot);
            }
        }

        /// <summary>
        /// Used for non-generic Constrained Methods
        /// </summary>
        private class NonGenericConstrainedMethodCell : GenericDictionaryCell
        {
            internal TypeDesc ConstraintType;
            internal TypeDesc ConstrainedMethodType;
            internal int ConstrainedMethodSlot;

            internal override void Prepare(TypeBuilder builder)
            {
                if (ConstraintType.IsCanonicalSubtype(CanonicalFormKind.Any) || ConstrainedMethodType.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Unable to compute call information for a canonical type/method.");

                builder.RegisterForPreparation(ConstraintType);
                builder.RegisterForPreparation(ConstrainedMethodType);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                return ConstrainedCallSupport.NonGenericConstrainedCallDesc.Get(builder.GetRuntimeTypeHandle(ConstraintType),
                                                                                builder.GetRuntimeTypeHandle(ConstrainedMethodType),
                                                                                ConstrainedMethodSlot);
            }
        }

        /// <summary>
        /// Used for generic Constrained Methods
        /// </summary>
        private class GenericConstrainedMethodCell : GenericDictionaryCell
        {
            internal TypeDesc ConstraintType;
            internal MethodDesc ConstrainedMethod;
            internal IntPtr MethodName;
            internal RuntimeSignature MethodSignature;

            internal override void Prepare(TypeBuilder builder)
            {
                if (ConstraintType.IsCanonicalSubtype(CanonicalFormKind.Any) || ConstrainedMethod.IsCanonicalMethod(CanonicalFormKind.Any))
                    Environment.FailFast("Unable to compute call information for a canonical type/method.");

                builder.RegisterForPreparation(ConstraintType);
                // Do not use builder.PrepareMethod here. That
                // would prepare the dictionary for the method,
                // and if the method is abstract, there is no
                // dictionary. Also, the dictionary is not necessary
                // to create the ldtoken.
                builder.RegisterForPreparation(ConstrainedMethod.OwningType);
                foreach (var type in ConstrainedMethod.Instantiation)
                    builder.RegisterForPreparation(type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                RuntimeTypeHandle[] genericArgHandles = ConstrainedMethod.HasInstantiation ?
                    builder.GetRuntimeTypeHandles(ConstrainedMethod.Instantiation) : null;

                RuntimeMethodHandle rmh = TypeLoaderEnvironment.Instance.GetRuntimeMethodHandleForComponents(
                    builder.GetRuntimeTypeHandle(ConstrainedMethod.OwningType),
                    MethodName,
                    MethodSignature,
                    genericArgHandles);

                return ConstrainedCallSupport.GenericConstrainedCallDesc.Get(builder.GetRuntimeTypeHandle(ConstraintType), rmh);
            }
        }
#endif

        private class StaticDataCell : GenericDictionaryCell
        {
            internal StaticDataKind DataKind;
            internal TypeDesc Type;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Unable to compute static field locations for a canonical type.");

                builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                RuntimeTypeHandle typeHandle = builder.GetRuntimeTypeHandle(Type);
                switch (DataKind)
                {
                    case StaticDataKind.NonGc:
                        return TypeLoaderEnvironment.Instance.TryGetNonGcStaticFieldData(typeHandle);

                    case StaticDataKind.Gc:
                        return TypeLoaderEnvironment.Instance.TryGetGcStaticFieldData(typeHandle);

                    default:
                        Debug.Assert(false);
                        return IntPtr.Zero;
                }
            }

            internal override unsafe IntPtr CreateLazyLookupCell(TypeBuilder builder, out IntPtr auxResult)
            {
                auxResult = IntPtr.Zero;
                return *(IntPtr*)Create(builder);
            }
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        public static GenericDictionaryCell CreateMethodDictionaryCell(InstantiatedMethod method)
        {
            MethodDictionaryCell methodCell = new MethodDictionaryCell();
            methodCell.GenericMethod = method;
            return methodCell;
        }
#endif

        private class MethodDictionaryCell : GenericDictionaryCell
        {
            internal InstantiatedMethod GenericMethod;

            internal unsafe override void Prepare(TypeBuilder builder)
            {
                if (GenericMethod.IsCanonicalMethod(CanonicalFormKind.Any))
                    Environment.FailFast("Method dictionaries of canonical methods do not exist");

                builder.PrepareMethod(GenericMethod);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                // TODO (USG): What if this method's instantiation is a non-shareable one (from a normal canonical
                // perspective) and there's an exact method pointer for the method in question, do we still
                // construct a method dictionary to be used with the universal canonical method implementation?
                Debug.Assert(GenericMethod.RuntimeMethodDictionary != IntPtr.Zero);
                return GenericMethod.RuntimeMethodDictionary;
            }
        }

        private class FieldLdTokenCell : GenericDictionaryCell
        {
            internal TypeDesc ContainingType;
            internal IntPtr FieldName;

            internal unsafe override void Prepare(TypeBuilder builder)
            {
                if (ContainingType.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Ldtoken is not permitted for a canonical field");

                builder.RegisterForPreparation(ContainingType);
            }

            internal override unsafe IntPtr Create(TypeBuilder builder)
            {
                RuntimeFieldHandle handle = TypeLoaderEnvironment.Instance.GetRuntimeFieldHandleForComponents(
                    builder.GetRuntimeTypeHandle(ContainingType),
                    FieldName);

                return *(IntPtr*)&handle;
            }
        }

        private class MethodLdTokenCell : GenericDictionaryCell
        {
            internal MethodDesc Method;
            internal IntPtr MethodName;
            internal RuntimeSignature MethodSignature;

            internal unsafe override void Prepare(TypeBuilder builder)
            {
                if (Method.IsCanonicalMethod(CanonicalFormKind.Any))
                    Environment.FailFast("Ldtoken is not permitted for a canonical method");

                // Do not use builder.PrepareMethod here. That
                // would prepare the dictionary for the method,
                // and if the method is abstract, there is no
                // dictionary. Also, the dictionary is not necessary
                // to create the ldtoken.
                builder.RegisterForPreparation(Method.OwningType);
                foreach (var type in Method.Instantiation)
                    builder.RegisterForPreparation(type);
            }

            internal override unsafe IntPtr Create(TypeBuilder builder)
            {
                RuntimeTypeHandle[] genericArgHandles = Method.HasInstantiation && !Method.IsMethodDefinition ?
                    builder.GetRuntimeTypeHandles(Method.Instantiation) : null;

                RuntimeMethodHandle handle = TypeLoaderEnvironment.Instance.GetRuntimeMethodHandleForComponents(
                    builder.GetRuntimeTypeHandle(Method.OwningType),
                    MethodName,
                    MethodSignature,
                    genericArgHandles);

                return *(IntPtr*)&handle;
            }
        }

#if FEATURE_UNIVERSAL_GENERICS
        private class TypeSizeCell : GenericDictionaryCell
        {
            internal TypeDesc Type;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Universal))
                    Environment.FailFast("Universal shared generics do not have a defined size");

                builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                if (Type.IsValueType)
                    return (IntPtr)RuntimeAugments.GetValueTypeSize(builder.GetRuntimeTypeHandle(Type));
                else
                    return (IntPtr)IntPtr.Size;
            }
        }
#endif

#if FEATURE_UNIVERSAL_GENERICS
        private class FieldOffsetCell : GenericDictionaryCell
        {
            internal DefType ContainingType;
            internal uint Ordinal;
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            internal FieldDesc Field;
#endif
            internal int Offset;

            internal unsafe override void Prepare(TypeBuilder builder)
            {
                if (ContainingType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                    Environment.FailFast("Universal shared generics do not have a defined size");

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                if (Field != null)
                    Offset = Field.Offset.AsInt;
                else
#endif
                    Offset = ContainingType.GetFieldByNativeLayoutOrdinal(Ordinal).Offset.AsInt;
            }

            internal override unsafe IntPtr Create(TypeBuilder builder)
            {
                return (IntPtr)Offset;
            }
        }

        private class VTableOffsetCell : GenericDictionaryCell
        {
            internal TypeDesc ContainingType;
            internal uint VTableSlot;
            private TypeDesc ContainingTypeTemplate;

            internal unsafe override void Prepare(TypeBuilder builder)
            {
                builder.RegisterForPreparation(ContainingType);
                ContainingTypeTemplate = ContainingType.ComputeTemplate();
            }

            //
            // This helper function will traverse the hierarchy of the containing type of this vtable offset cell, in parallel with
            // the hierarchy of its template type.
            // When traversing the template type hierarchy, we have 2 possibilities for the base type:
            //    - Fully universal canonical. USG types always have a dictionary slot, so if the dynamically created type does not share
            //      normal canonical code, we subtract 1 from the vtable offset (the dynamic type does not have a dictionary slot in that case)
            //    - Exact non-canonical type. In that case, we do not need to make any changes to the vtable offset (the binder/ILCompiler
            //      would have written the correct vtable offset, taking in the account the existance or non-existance of a dictionary slot.
            //
            private void AdjustVtableSlot(TypeDesc currentType, TypeDesc currentTemplateType, ref int vtableSlot)
            {
                TypeDesc baseType = currentType.BaseType;
                TypeDesc baseTemplateType = TypeBuilder.GetBaseTypeUsingRuntimeTypeHandle(currentTemplateType);

                Debug.Assert((baseType == null && baseTemplateType == null) || (baseType != null && baseTemplateType != null));

                // Compute the vtable layout for the current type starting with base types first
                if (baseType != null)
                    AdjustVtableSlot(baseType, baseTemplateType, ref vtableSlot);

                if (currentType.IsGeneric())
                {
                    if (!currentType.CanShareNormalGenericCode() && currentTemplateType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                        vtableSlot--;
                }
            }

            internal override unsafe IntPtr Create(TypeBuilder builder)
            {
                // Debug sanity check for the size of the MethodTable structure
                // just to ensure nothing of it gets reduced
                Debug.Assert(sizeof(MethodTable) == (IntPtr.Size == 8 ? 24 : 20));

                int result = (int)VTableSlot;

                // Check if the current type can share code with normal canonical
                // generic types. If not, then the vtable layout will not have a
                // slot for a dictionary pointer, and we need to adjust the slot number
                AdjustVtableSlot(ContainingType, ContainingTypeTemplate, ref result);
                Debug.Assert(result >= 0);

                return (IntPtr)(sizeof(MethodTable) + result * IntPtr.Size);
            }
        }
#endif

        private class AllocateObjectCell : GenericDictionaryCell
        {
            internal TypeDesc Type;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Canonical types cannot be allocated");

                builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                RuntimeTypeHandle th = GetRuntimeTypeHandleWithNullableTransform(builder, Type);
                return RuntimeAugments.GetAllocateObjectHelperForType(th);
            }

            internal override unsafe IntPtr CreateLazyLookupCell(TypeBuilder builder, out IntPtr auxResult)
            {
                RuntimeTypeHandle th = GetRuntimeTypeHandleWithNullableTransform(builder, Type);
                auxResult = th.ToIntPtr();
                return RuntimeAugments.GetAllocateObjectHelperForType(th);
            }
        }

        private class DefaultConstructorCell : GenericDictionaryCell
        {
            internal TypeDesc Type;

            internal override void Prepare(TypeBuilder builder)
            {
                builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                IntPtr result = TypeLoaderEnvironment.Instance.TryGetDefaultConstructorForType(Type);


                if (result == IntPtr.Zero)
                    result = RuntimeAugments.GetFallbackDefaultConstructor();
                return result;
            }
        }

        public static GenericDictionaryCell CreateIntPtrCell(IntPtr ptrValue)
        {
            IntPtrCell typeCell = new IntPtrCell();
            typeCell.Value = ptrValue;
            return typeCell;
        }

        private class IntPtrCell : GenericDictionaryCell
        {
            internal IntPtr Value;
            internal unsafe override void Prepare(TypeBuilder builder)
            {
            }

            internal unsafe override IntPtr Create(TypeBuilder builder)
            {
                return Value;
            }
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        public static GenericDictionaryCell CreateExactCallableMethodCell(MethodDesc method)
        {
            MethodCell methodCell = new MethodCell();
            methodCell.Method = method;
            if (!RuntimeSignatureHelper.TryCreate(method, out methodCell.MethodSignature))
            {
                Environment.FailFast("Unable to create method signature, for method reloc");
            }
            methodCell.ExactCallableAddressNeeded = true;
            return methodCell;
        }
#endif

        private class MethodCell : GenericDictionaryCell
        {
            internal MethodDesc Method;
            internal RuntimeSignature MethodSignature;
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            internal bool ExactCallableAddressNeeded;
#endif
            private bool _universalCanonImplementationOfCanonMethod;
            private MethodDesc _methodToUseForInstantiatingParameters;
            private IntPtr _exactFunctionPointer;

            internal unsafe override void Prepare(TypeBuilder builder)
            {
                _methodToUseForInstantiatingParameters = Method;

                IntPtr exactFunctionPointer;

                bool canUseRetrieveExactFunctionPointerIfPossible = false;

                // RetrieveExactFunctionPointerIfPossible always gets the unboxing stub if possible
                if (Method.UnboxingStub)
                    canUseRetrieveExactFunctionPointerIfPossible = true;
                else if (!Method.OwningType.IsValueType) // If the owning type isn't a valuetype, concerns about unboxing stubs are moot
                    canUseRetrieveExactFunctionPointerIfPossible = true;
                else if (TypeLoaderEnvironment.Instance.IsStaticMethodSignature(MethodSignature)) // Static methods don't have unboxing stub concerns
                    canUseRetrieveExactFunctionPointerIfPossible = true;

                if (canUseRetrieveExactFunctionPointerIfPossible &&
                    builder.RetrieveExactFunctionPointerIfPossible(Method, out exactFunctionPointer))
                {
                    // If we succeed in finding a non-shareable function pointer for this method, it means
                    // that we found a method body for it that was statically compiled. We'll use that body
                    // instead of the universal canonical method pointer
                    Debug.Assert(exactFunctionPointer != IntPtr.Zero &&
                                 exactFunctionPointer != Method.FunctionPointer &&
                                 exactFunctionPointer != Method.UsgFunctionPointer);

                    _exactFunctionPointer = exactFunctionPointer;
                }
                else
                {
                    // There is no exact function pointer available. This means that we'll have to
                    // build a method dictionary for the method instantiation, and use the shared canonical
                    // function pointer that was parsed from native layout.
                    _exactFunctionPointer = IntPtr.Zero;
                    builder.PrepareMethod(Method);

                    // Check whether we have already resolved a canonical or universal match
                    IntPtr addressToUse;
                    TypeLoaderEnvironment.MethodAddressType foundAddressType;
                    if (Method.FunctionPointer != IntPtr.Zero)
                    {
                        addressToUse = Method.FunctionPointer;
                        foundAddressType = TypeLoaderEnvironment.MethodAddressType.Canonical;
                    }
                    else if (Method.UsgFunctionPointer != IntPtr.Zero)
                    {
                        addressToUse = Method.UsgFunctionPointer;
                        foundAddressType = TypeLoaderEnvironment.MethodAddressType.UniversalCanonical;
                    }
                    else
                    {
                        // No previous match, new lookup is needed
                        IntPtr fnptr;
                        IntPtr unboxingStub;

                        MethodDesc searchMethod = Method;
                        if (Method.UnboxingStub)
                        {
                            // Find the function that isn't an unboxing stub, note the first parameter which is false
                            searchMethod = searchMethod.Context.ResolveGenericMethodInstantiation(false, (DefType)Method.OwningType, Method.NameAndSignature, Method.Instantiation, IntPtr.Zero, false);
                        }

                        if (!TypeLoaderEnvironment.TryGetMethodAddressFromMethodDesc(searchMethod, out fnptr, out unboxingStub, out foundAddressType))
                        {
                            Environment.FailFast("Unable to find method address for method:" + Method.ToString());
                        }

                        if (Method.UnboxingStub)
                        {
                            addressToUse = unboxingStub;
                        }
                        else
                        {
                            addressToUse = fnptr;
                        }

                        if (foundAddressType == TypeLoaderEnvironment.MethodAddressType.Canonical ||
                            foundAddressType == TypeLoaderEnvironment.MethodAddressType.UniversalCanonical)
                        {
                            // Cache the resolved canonical / universal pointer in the MethodDesc
                            // Actually it would simplify matters here if the MethodDesc held just one pointer
                            // and the lookup type enumeration value.
                            Method.SetFunctionPointer(
                                addressToUse,
                                foundAddressType == TypeLoaderEnvironment.MethodAddressType.UniversalCanonical);
                        }
                    }

                    // Look at the resolution type and check whether we can set up the ExactFunctionPointer upfront
                    switch (foundAddressType)
                    {
                        case TypeLoaderEnvironment.MethodAddressType.Exact:
                            _exactFunctionPointer = addressToUse;
                            break;
                        case TypeLoaderEnvironment.MethodAddressType.Canonical:
                            {
                                bool methodRequestedIsCanonical = Method.IsCanonicalMethod(CanonicalFormKind.Specific);
                                bool requestedMethodNeedsDictionaryWhenCalledAsCanonical = NeedsDictionaryParameterToCallCanonicalVersion(Method);

                                if (!requestedMethodNeedsDictionaryWhenCalledAsCanonical || methodRequestedIsCanonical)
                                {
                                    _exactFunctionPointer = addressToUse;
                                }
                                break;
                            }
#if FEATURE_UNIVERSAL_GENERICS
                        case TypeLoaderEnvironment.MethodAddressType.UniversalCanonical:
                            {
                                if (Method.IsCanonicalMethod(CanonicalFormKind.Universal) &&
                                    !NeedsDictionaryParameterToCallCanonicalVersion(Method) &&
                                    !UniversalGenericParameterLayout.MethodSignatureHasVarsNeedingCallingConventionConverter(
                                        Method.GetTypicalMethodDefinition().Signature))
                                {
                                    _exactFunctionPointer = addressToUse;
                                }
                                break;
                            }
#endif
                        default:
                            Environment.FailFast("Unexpected method address type");
                            return;
                    }

                    if (_exactFunctionPointer == IntPtr.Zero)
                    {
                        // We have exhausted exact resolution options so we must resort to calling
                        // convention conversion. Prepare the type parameters of the method so that
                        // the calling convention converter can have RuntimeTypeHandle's to work with.
                        // For canonical methods, convert paramters to their CanonAlike form
                        // as the Canonical RuntimeTypeHandle's are not permitted to exist.
                        Debug.Assert(!Method.IsCanonicalMethod(CanonicalFormKind.Universal));

                        bool methodRequestedIsCanonical = Method.IsCanonicalMethod(CanonicalFormKind.Specific);
                        MethodDesc canonAlikeForm = Method;
                        foreach (TypeDesc t in canonAlikeForm.Instantiation)
                        {
                            builder.PrepareType(t);
                        }
                        foreach (TypeDesc t in canonAlikeForm.OwningType.Instantiation)
                        {
                            builder.PrepareType(t);
                        }

                        if (!(Method.GetTypicalMethodDefinition() is RuntimeMethodDesc))
                        {
                            // Also, prepare all of the argument types as will be needed by the calling convention converter
                            MethodSignature signature = canonAlikeForm.Signature;
                            for (int i = 0; i < signature.Length; i++)
                            {
                                TypeDesc t = signature[i];
                                if (t is ByRefType)
                                    builder.PrepareType(((ByRefType)t).ParameterType);
                                else
                                    builder.PrepareType(t);
                            }
                            if (signature.ReturnType is ByRefType)
                                builder.PrepareType((ByRefType)signature.ReturnType);
                            else
                                builder.PrepareType(signature.ReturnType);
                        }

                        _universalCanonImplementationOfCanonMethod = methodRequestedIsCanonical;
                        _methodToUseForInstantiatingParameters = canonAlikeForm;
                    }
                }

                // By the time we reach here, we should always have a function pointer of some form
                Debug.Assert((_exactFunctionPointer != IntPtr.Zero) || (Method.FunctionPointer != IntPtr.Zero) || (Method.UsgFunctionPointer != IntPtr.Zero));
            }

            private bool NeedsDictionaryParameterToCallCanonicalVersion(MethodDesc method)
            {
                if (Method.HasInstantiation)
                    return true;

                if (!Method.OwningType.HasInstantiation)
                    return false;

                if (Method is NoMetadataMethodDesc)
                {
                    // If the method does not have metadata, use the NameAndSignature property which should work in that case.
                    if (TypeLoaderEnvironment.Instance.IsStaticMethodSignature(Method.NameAndSignature.Signature))
                        return true;
                }
                else
                {
                    // Otherwise, use the MethodSignature
                    if (Method.Signature.IsStatic)
                        return true;
                }

                return Method.OwningType.IsValueType && !Method.UnboxingStub;
            }

            internal unsafe override IntPtr Create(TypeBuilder builder)
            {
                if (_exactFunctionPointer != IntPtr.Zero)
                {
                    // We are done... we don't need to create any unboxing stubs or calling convertion translation
                    // thunks for exact non-shareable method instantiations
                    return _exactFunctionPointer;
                }

                Debug.Assert(Method.Instantiation.Length > 0 || Method.OwningType.HasInstantiation);

                IntPtr methodDictionary = IntPtr.Zero;

                if (!_universalCanonImplementationOfCanonMethod)
                {
                    methodDictionary = Method.Instantiation.Length > 0 ?
                        ((InstantiatedMethod)Method).RuntimeMethodDictionary :
                        builder.GetRuntimeTypeHandle(Method.OwningType).ToIntPtr();
                }

                if (Method.FunctionPointer != IntPtr.Zero)
                {
                    if (Method.Instantiation.Length > 0
                        || TypeLoaderEnvironment.Instance.IsStaticMethodSignature(MethodSignature)
                        || (Method.OwningType.IsValueType && !Method.UnboxingStub))
                    {
                        Debug.Assert(methodDictionary != IntPtr.Zero);
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                        if (this.ExactCallableAddressNeeded)
                        {
                            // In this case we need to build an instantiating stub
                            return BuildCallingConventionConverter(builder, Method.FunctionPointer, methodDictionary, false);
                        }
                        else
#endif
                        {
                            return FunctionPointerOps.GetGenericMethodFunctionPointer(Method.FunctionPointer, methodDictionary);
                        }
                    }
                    else
                    {
                        return Method.FunctionPointer;
                    }
                }
#if FEATURE_UNIVERSAL_GENERICS
                else if (Method.UsgFunctionPointer != IntPtr.Zero)
                {
                    return BuildCallingConventionConverter(builder, Method.UsgFunctionPointer, methodDictionary, true);
                }
#endif

                Debug.Fail("UNREACHABLE");
                return IntPtr.Zero;
            }

#if FEATURE_UNIVERSAL_GENERICS
            private IntPtr BuildCallingConventionConverter(TypeBuilder builder, IntPtr pointerToUse, IntPtr dictionary, bool usgConverter)
            {
                RuntimeTypeHandle[] typeArgs = Empty<RuntimeTypeHandle>.Array;
                RuntimeTypeHandle[] methodArgs = Empty<RuntimeTypeHandle>.Array;

                typeArgs = builder.GetRuntimeTypeHandles(_methodToUseForInstantiatingParameters.OwningType.Instantiation);

                bool containingTypeIsValueType = _methodToUseForInstantiatingParameters.OwningType.IsValueType;
                bool genericMethod = (_methodToUseForInstantiatingParameters.Instantiation.Length > 0);

                methodArgs = builder.GetRuntimeTypeHandles(_methodToUseForInstantiatingParameters.Instantiation);

                Debug.Assert(!MethodSignature.IsNativeLayoutSignature || (MethodSignature.NativeLayoutSignature() != IntPtr.Zero));

                CallConverterThunk.ThunkKind thunkKind = default(CallConverterThunk.ThunkKind);
                if (usgConverter)
                {
                    if (genericMethod || containingTypeIsValueType)
                    {
                        if (Method.UnboxingStub)
                        {
                            if (dictionary == IntPtr.Zero)
                                Environment.FailFast("Need standard to generic non-instantiating unboxing stub thunk kind");
                            else
                                thunkKind = CallConverterThunk.ThunkKind.StandardUnboxingAndInstantiatingGeneric;
                        }
                        else
                        {
                            if (dictionary == IntPtr.Zero)
                                thunkKind = CallConverterThunk.ThunkKind.StandardToGenericPassthruInstantiating;
                            else
                                thunkKind = CallConverterThunk.ThunkKind.StandardToGenericInstantiating;
                        }
                    }
                    else
                    {
                        if (dictionary == IntPtr.Zero)
                            thunkKind = CallConverterThunk.ThunkKind.StandardToGenericPassthruInstantiatingIfNotHasThis;
                        else
                            thunkKind = CallConverterThunk.ThunkKind.StandardToGenericInstantiatingIfNotHasThis;
                    }
                }
                else
                {
                    thunkKind = CallConverterThunk.ThunkKind.StandardToStandardInstantiating;
                }

                IntPtr thunkPtr = CallConverterThunk.MakeThunk(
                    thunkKind,
                    Method.UsgFunctionPointer,
                    MethodSignature,
                    dictionary,
                    typeArgs,
                    methodArgs);

                Debug.Assert(thunkPtr != IntPtr.Zero);
                return thunkPtr;
            }
#endif
        }

        private class CastingCell : GenericDictionaryCell
        {
            internal TypeDesc Type;
            internal bool Throwing;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Canonical types do not have EETypes");

                builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                RuntimeTypeHandle th = GetRuntimeTypeHandleWithNullableTransform(builder, Type);
                return RuntimeAugments.GetCastingHelperForType(th, Throwing);
            }

            internal override unsafe IntPtr CreateLazyLookupCell(TypeBuilder builder, out IntPtr auxResult)
            {
                RuntimeTypeHandle th = GetRuntimeTypeHandleWithNullableTransform(builder, Type);
                auxResult = th.ToIntPtr();
                return RuntimeAugments.GetCastingHelperForType(th, Throwing);
            }
        }

        private class AllocateArrayCell : GenericDictionaryCell
        {
            internal TypeDesc Type;

            internal override void Prepare(TypeBuilder builder)
            {
                if (Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                    Environment.FailFast("Canonical types do not have EETypes");

                builder.RegisterForPreparation(Type);
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                return RuntimeAugments.GetAllocateArrayHelperForType(builder.GetRuntimeTypeHandle(Type));
            }

            internal override unsafe IntPtr CreateLazyLookupCell(TypeBuilder builder, out IntPtr auxResult)
            {
                auxResult = builder.GetRuntimeTypeHandle(Type).ToIntPtr();
                return Create(builder);
            }
        }

#if FEATURE_UNIVERSAL_GENERICS
        private class CallingConventionConverterCell : GenericDictionaryCell
        {
            internal NativeFormat.CallingConventionConverterKind Flags;
            internal RuntimeSignature Signature;
            internal Instantiation MethodArgs;
            internal Instantiation TypeArgs;

            internal override void Prepare(TypeBuilder builder)
            {
                if (!MethodArgs.IsNull)
                {
                    foreach (TypeDesc t in MethodArgs)
                    {
                        if (t.IsCanonicalSubtype(CanonicalFormKind.Any))
                            Environment.FailFast("Canonical types do not have EETypes");
                    }
                }
                if (!TypeArgs.IsNull)
                {
                    foreach (TypeDesc t in TypeArgs)
                    {
                        if (t.IsCanonicalSubtype(CanonicalFormKind.Any))
                            Environment.FailFast("Canonical types do not have EETypes");
                    }
                }
            }

            internal override IntPtr Create(TypeBuilder builder)
            {
                RuntimeTypeHandle[] typeArgs = null;
                RuntimeTypeHandle[] methodArgs = null;

                if (!MethodArgs.IsNull)
                {
                    methodArgs = builder.GetRuntimeTypeHandles(MethodArgs);
                }

                if (!TypeArgs.IsNull)
                {
                    typeArgs = builder.GetRuntimeTypeHandles(TypeArgs);
                }

                CallConverterThunk.ThunkKind thunkKind;

                switch (Flags)
                {
                    case NativeFormat.CallingConventionConverterKind.NoInstantiatingParam:
                        thunkKind = CallConverterThunk.ThunkKind.GenericToStandardWithTargetPointerArg;
                        break;

                    case NativeFormat.CallingConventionConverterKind.HasInstantiatingParam:
                        thunkKind = CallConverterThunk.ThunkKind.GenericToStandardWithTargetPointerArgAndParamArg;
                        break;

                    case NativeFormat.CallingConventionConverterKind.MaybeInstantiatingParam:
                        thunkKind = CallConverterThunk.ThunkKind.GenericToStandardWithTargetPointerArgAndMaybeParamArg;
                        break;

                    default:
                        throw new NotSupportedException();
                }

                IntPtr result = CallConverterThunk.MakeThunk(thunkKind, IntPtr.Zero, Signature, IntPtr.Zero, typeArgs, methodArgs);
                return result;
            }
        }
#endif

        internal static unsafe GenericDictionaryCell[] BuildDictionary(TypeBuilder typeBuilder, NativeLayoutInfoLoadContext nativeLayoutInfoLoadContext, NativeParser parser)
        {
            uint parserStartOffset = parser.Offset;

            uint count = parser.GetSequenceCount();
            Debug.Assert(count > 0);

            TypeLoaderLogger.WriteLine("Parsing dictionary layout @ " + parserStartOffset.LowLevelToString() + " (" + count.LowLevelToString() + " entries)");

            GenericDictionaryCell[] dictionary = new GenericDictionaryCell[count];

            for (uint i = 0; i < count; i++)
            {
                TypeLoaderLogger.WriteLine("  -> DictionaryCell[" + i.LowLevelToString() + "] = ");

                dictionary[i] = ParseAndCreateCell(nativeLayoutInfoLoadContext, ref parser);
            }

            for (uint i = 0; i < count; i++)
                dictionary[i].Prepare(typeBuilder);

            return dictionary;
        }

        internal static unsafe GenericDictionaryCell[] BuildFloatingDictionary(TypeBuilder typeBuilder, NativeLayoutInfoLoadContext nativeLayoutInfoLoadContext, NativeParser parser, out int floatingVersionCellIndex, out int floatingVersionInLayout)
        {
            //
            // The format of a dictionary that has a floating portion is as follows:
            //
            // "Fixed" portion:
            //      - First slot is a pointer to the first cell of the "floating" portion
            //      - Followed by N various dictionary lookup cells
            // "Floating" portion:
            //      - Cell containing the version number of the floating portion
            //      - Followed by N various dictionary lookup cells
            //

            floatingVersionCellIndex = floatingVersionInLayout = -1;

            uint count = parser.GetSequenceCount();
            Debug.Assert(count > 1);

            GenericDictionaryCell cell = ParseAndCreateCell(nativeLayoutInfoLoadContext, ref parser);
            if (!(cell is PointerToOtherDictionarySlotCell))
            {
                // This is not a dictionary layout that has a floating portion
                Debug.Fail("Unreachable: we should never reach here if the target dictionary does not have a floating layout");
                return null;
            }

            PointerToOtherDictionarySlotCell pointerToCell = (PointerToOtherDictionarySlotCell)cell;
            floatingVersionCellIndex = (int)pointerToCell.OtherDictionarySlot;
            Debug.Assert(count > pointerToCell.OtherDictionarySlot);

            GenericDictionaryCell[] dictionary = new GenericDictionaryCell[count - pointerToCell.OtherDictionarySlot];

            for (uint i = 1; i < pointerToCell.OtherDictionarySlot; i++)
            {
                // Parse and discard the fixed dictionary cells. We only need to build the cells of the floating portion
                ParseAndCreateCell(nativeLayoutInfoLoadContext, ref parser);
            }

            for (uint i = pointerToCell.OtherDictionarySlot; i < count; i++)
            {
                TypeLoaderLogger.WriteLine("  -> FloatingDictionaryCell[" + (i - pointerToCell.OtherDictionarySlot).LowLevelToString() + "] (" + i.LowLevelToString() + " in all) = ");

                cell = ParseAndCreateCell(nativeLayoutInfoLoadContext, ref parser);

                if ((i == pointerToCell.OtherDictionarySlot) && (cell is IntPtrCell))
                {
                    // The first cell in the floating portion should always be the version number
                    floatingVersionInLayout = (int)((IntPtrCell)cell).Value;
                }

                dictionary[i - pointerToCell.OtherDictionarySlot] = cell;
            }

            for (uint i = pointerToCell.OtherDictionarySlot; i < count; i++)
                dictionary[i - pointerToCell.OtherDictionarySlot].Prepare(typeBuilder);

            return dictionary;
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        /// <summary>
        /// Build an array of GenericDictionaryCell from a NativeParser stream that has the appropriate metadata
        /// Return null if there are no cells to describe
        /// </summary>
        internal static unsafe GenericDictionaryCell[] BuildDictionaryFromMetadataTokensAndContext(TypeBuilder typeBuilder, NativeParser parser, NativeFormatMetadataUnit nativeMetadataUnit, FixupCellMetadataResolver resolver)
        {
            uint parserStartOffset = parser.Offset;

            uint count = parser.GetSequenceCount();

            // An empty dictionary isn't interesting
            if (count == 0)
                return null;

            Debug.Assert(count > 0);
            TypeLoaderLogger.WriteLine("Parsing dictionary layout @ " + parserStartOffset.LowLevelToString() + " (" + count.LowLevelToString() + " entries)");

            GenericDictionaryCell[] dictionary = new GenericDictionaryCell[count];

            for (uint i = 0; i < count; i++)
            {
                MetadataFixupKind fixupKind = (MetadataFixupKind)parser.GetUInt8();
                Internal.Metadata.NativeFormat.Handle token = parser.GetUnsigned().AsHandle();
                Internal.Metadata.NativeFormat.Handle token2 = new Internal.Metadata.NativeFormat.Handle();

                switch (fixupKind)
                {
                    case MetadataFixupKind.GenericConstrainedMethod:
                    case MetadataFixupKind.NonGenericConstrainedMethod:
                    case MetadataFixupKind.NonGenericDirectConstrainedMethod:
                        token2 = parser.GetUnsigned().AsHandle();
                        break;
                }
                GenericDictionaryCell cell = CreateCellFromFixupKindAndToken(fixupKind, resolver, token, token2);
                cell.Prepare(typeBuilder);
                dictionary[i] = cell;
            }

            return dictionary;
        }
#endif

        private static TypeDesc TransformNullable(TypeDesc type)
        {
            DefType typeAsDefType = type as DefType;
            if (typeAsDefType != null && typeAsDefType.Instantiation.Length == 1 && typeAsDefType.GetTypeDefinition().RuntimeTypeHandle.Equals(typeof(Nullable<>).TypeHandle))
                return typeAsDefType.Instantiation[0];
            return type;
        }

#if FEATURE_UNIVERSAL_GENERICS
        private static int ComputeConstrainedMethodSlot(MethodDesc constrainedMethod)
        {
            if (constrainedMethod.OwningType.IsInterface)
            {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                ushort slot;
                if (!LazyVTableResolver.TryGetInterfaceSlotNumberFromMethod(constrainedMethod, out slot))
                    throw new BadImageFormatException();

                return slot;
#else
                Environment.FailFast("Unable to resolve constrained call");
                return 0;
#endif
            }
            else if (constrainedMethod.OwningType == constrainedMethod.Context.GetWellKnownType(WellKnownType.Object))
            {
                if (constrainedMethod.Name == "ToString")
                    return ConstrainedCallSupport.NonGenericConstrainedCallDesc.s_ToStringSlot;
                else if (constrainedMethod.Name == "GetHashCode")
                    return ConstrainedCallSupport.NonGenericConstrainedCallDesc.s_GetHashCodeSlot;
                else if (constrainedMethod.Name == "Equals")
                    return ConstrainedCallSupport.NonGenericConstrainedCallDesc.s_EqualsSlot;
            }

            Environment.FailFast("unable to construct constrained method slot from constrained method");
            return -1;
        }
#endif

        internal static GenericDictionaryCell CreateMethodCell(MethodDesc method, bool exactCallableAddressNeeded)
        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            var nativeFormatMethod = (TypeSystem.NativeFormat.NativeFormatMethod)method.GetTypicalMethodDefinition();

            return new MethodCell
            {
                ExactCallableAddressNeeded = exactCallableAddressNeeded,
                Method = method,
                MethodSignature = RuntimeSignature.CreateFromMethodHandle(nativeFormatMethod.MetadataUnit.RuntimeModule, nativeFormatMethod.Handle.ToInt())
            };
#else
            Environment.FailFast("Creating a methodcell from a MethodDesc only supported in the presence of metadata based type loading.");
            return null;
#endif
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        /// <summary>
        /// Create a single cell to resolve based on a MetadataFixupKind, and the matching tokens
        /// </summary>
        internal static GenericDictionaryCell CreateCellFromFixupKindAndToken(MetadataFixupKind kind, FixupCellMetadataResolver metadata, Internal.Metadata.NativeFormat.Handle token, Internal.Metadata.NativeFormat.Handle token2)
        {
            GenericDictionaryCell cell;

            switch (kind)
            {
                case MetadataFixupKind.TypeHandle:
                    {
                        var type = metadata.GetType(token);
                        TypeLoaderLogger.WriteLine("TypeHandle: " + type.ToString());

                        cell = new TypeHandleCell() { Type = type };
                    }
                    break;

                case MetadataFixupKind.ArrayOfTypeHandle:
                    {
                        var type = metadata.GetType(token);
                        var arrayType = type.Context.GetArrayType(type);
                        TypeLoaderLogger.WriteLine("TypeHandle: " + arrayType.ToString());

                        cell = new TypeHandleCell() { Type = arrayType };
                    }
                    break;

                case MetadataFixupKind.VirtualCallDispatch:
                    {
                        var method = metadata.GetMethod(token);


                        var containingType = method.OwningType;
                        if (containingType.IsInterface)
                        {
                            ushort slot;
                            if (!LazyVTableResolver.TryGetInterfaceSlotNumberFromMethod(method, out slot))
                            {
                                Environment.FailFast("Unable to get interface slot while resolving InterfaceCall dictionary cell");
                            }

                            TypeLoaderLogger.WriteLine("InterfaceCall: " + containingType.ToString() + ", slot #" + ((int)slot).LowLevelToString());

                            cell = new InterfaceCallCell() { InterfaceType = containingType, Slot = (int)slot };
                        }
                        else
                        {
                            // TODO! Implement virtual dispatch cell creation
                            throw NotImplemented.ByDesign;
                        }
                    }
                    break;

                case MetadataFixupKind.MethodDictionary:
                    {
                        var genericMethod = metadata.GetMethod(token);
                        TypeLoaderLogger.WriteLine("MethodDictionary: " + genericMethod.ToString());

                        cell = new MethodDictionaryCell { GenericMethod = (InstantiatedMethod)genericMethod };
                    }
                    break;

                case MetadataFixupKind.GcStaticData:
                    {
                        var type = metadata.GetType(token);
                        var staticDataKind = StaticDataKind.Gc;
                        TypeLoaderLogger.WriteLine("StaticData (" + (staticDataKind == StaticDataKind.Gc ? "Gc" : "NonGc") + ": " + type.ToString());

                        cell = new StaticDataCell() { DataKind = staticDataKind, Type = type };
                    }
                    break;

                case MetadataFixupKind.NonGcStaticData:
                    {
                        var type = metadata.GetType(token);
                        var staticDataKind = StaticDataKind.NonGc;
                        TypeLoaderLogger.WriteLine("StaticData (" + (staticDataKind == StaticDataKind.Gc ? "Gc" : "NonGc") + ": " + type.ToString());

                        cell = new StaticDataCell() { DataKind = staticDataKind, Type = type };
                    }
                    break;

                case MetadataFixupKind.DirectGcStaticData:
                    {
                        var type = metadata.GetType(token);
                        var staticDataKind = StaticDataKind.Gc;
                        TypeLoaderLogger.WriteLine("Direct StaticData (" + (staticDataKind == StaticDataKind.Gc ? "Gc" : "NonGc") + ": " + type.ToString());

                        cell = new StaticDataCell() { DataKind = staticDataKind, Type = type };
                    }
                    break;

                case MetadataFixupKind.DirectNonGcStaticData:
                    {
                        var type = metadata.GetType(token);
                        var staticDataKind = StaticDataKind.NonGc;
                        TypeLoaderLogger.WriteLine("Direct StaticData (" + (staticDataKind == StaticDataKind.Gc ? "Gc" : "NonGc") + ": " + type.ToString());

                        cell = new StaticDataCell() { DataKind = staticDataKind, Type = type };
                    }
                    break;

                case MetadataFixupKind.UnwrapNullableType:
                    {
                        var type = metadata.GetType(token);
                        TypeLoaderLogger.WriteLine("UnwrapNullableType of: " + type.ToString());

                        if (type is DefType)
                            cell = new UnwrapNullableTypeCell() { Type = (DefType)type };
                        else
                            cell = new TypeHandleCell() { Type = type };
                    }
                    break;

                case MetadataFixupKind.FieldLdToken:
                    {
                        var field = metadata.GetField(token);

                        TypeLoaderLogger.WriteLine("LdToken on: " + field.ToString());
                        IntPtr fieldName = TypeLoaderEnvironment.Instance.GetNativeFormatStringForString(field.Name);
                        cell = new FieldLdTokenCell() { FieldName = fieldName, ContainingType = field.OwningType };
                    }
                    break;

                case MetadataFixupKind.MethodLdToken:
                    {
                        var method = metadata.GetMethod(token);
                        TypeLoaderLogger.WriteLine("LdToken on: " + method.ToString());
                        var nativeFormatMethod = (TypeSystem.NativeFormat.NativeFormatMethod)method.GetTypicalMethodDefinition();

                        cell = new MethodLdTokenCell
                        {
                            Method = method,
                            MethodName = IntPtr.Zero,
                            MethodSignature = RuntimeSignature.CreateFromMethodHandle(nativeFormatMethod.MetadataUnit.RuntimeModule, nativeFormatMethod.Handle.ToInt())
                        };
                    }
                    break;

                case MetadataFixupKind.TypeSize:
                    {
                        var type = metadata.GetType(token);
                        TypeLoaderLogger.WriteLine("TypeSize: " + type.ToString());

                        cell = new TypeSizeCell() { Type = type };
                    }
                    break;

                case MetadataFixupKind.FieldOffset:
                    {
                        var field = metadata.GetField(token);
                        TypeLoaderLogger.WriteLine("FieldOffset: " + field.ToString());

                        cell = new FieldOffsetCell() { Field = field };
                    }
                    break;

                case MetadataFixupKind.AllocateObject:
                    {
                        var type = metadata.GetType(token);
                        TypeLoaderLogger.WriteLine("AllocateObject on: " + type.ToString());

                        cell = new AllocateObjectCell { Type = type };
                    }
                    break;

                case MetadataFixupKind.DefaultConstructor:
                    {
                        var type = metadata.GetType(token);
                        TypeLoaderLogger.WriteLine("DefaultConstructor on: " + type.ToString());

                        cell = new DefaultConstructorCell { Type = type };
                    }
                    break;

                case MetadataFixupKind.UnboxingStubMethod:
                    {
                        var method = metadata.GetMethod(token);
                        TypeLoaderLogger.WriteLine("Unboxing Stub Method: " + method.ToString());
                        if (method.OwningType.IsValueType)
                        {
                            // If an unboxing stub could exists, that's actually what we want
                            method = method.Context.ResolveGenericMethodInstantiation(true/* get the unboxing stub */, method.OwningType.GetClosestDefType(), method.NameAndSignature, method.Instantiation, IntPtr.Zero, false);
                        }

                        var nativeFormatMethod = (TypeSystem.NativeFormat.NativeFormatMethod)method.GetTypicalMethodDefinition();

                        cell = new MethodCell
                        {
                            Method = method,
                            MethodSignature = RuntimeSignature.CreateFromMethodHandle(nativeFormatMethod.MetadataUnit.RuntimeModule, nativeFormatMethod.Handle.ToInt())
                        };
                    }
                    break;

                case MetadataFixupKind.Method:
                    {
                        var method = metadata.GetMethod(token);
                        TypeLoaderLogger.WriteLine("Method: " + method.ToString());
                        var nativeFormatMethod = (TypeSystem.NativeFormat.NativeFormatMethod)method.GetTypicalMethodDefinition();

                        cell = new MethodCell
                        {
                            Method = method,
                            MethodSignature = RuntimeSignature.CreateFromMethodHandle(nativeFormatMethod.MetadataUnit.RuntimeModule, nativeFormatMethod.Handle.ToInt())
                        };
                    }
                    break;

                case MetadataFixupKind.CallableMethod:
                    {
                        var method = metadata.GetMethod(token);
                        TypeLoaderLogger.WriteLine("CallableMethod: " + method.ToString());
                        var nativeFormatMethod = (TypeSystem.NativeFormat.NativeFormatMethod)method.GetTypicalMethodDefinition();

                        cell = new MethodCell
                        {
                            ExactCallableAddressNeeded = true,
                            Method = method,
                            MethodSignature = RuntimeSignature.CreateFromMethodHandle(nativeFormatMethod.MetadataUnit.RuntimeModule, nativeFormatMethod.Handle.ToInt())
                        };
                    }
                    break;

                case MetadataFixupKind.NonGenericDirectConstrainedMethod:
                    {
                        var constraintType = metadata.GetType(token);
                        var method = metadata.GetMethod(token2);
                        var constrainedMethodType = method.OwningType;
                        var constrainedMethodSlot = ComputeConstrainedMethodSlot(method);

                        TypeLoaderLogger.WriteLine("NonGenericDirectConstrainedMethod: " + constraintType.ToString() + " Method:" + method.ToString() + " Consisting of " + constrainedMethodType.ToString() + ", slot #" + constrainedMethodSlot.LowLevelToString());

                        cell = new NonGenericDirectConstrainedMethodCell()
                        {
                            ConstraintType = constraintType,
                            ConstrainedMethodType = constrainedMethodType,
                            ConstrainedMethodSlot = (int)constrainedMethodSlot
                        };
                    }
                    break;

                case MetadataFixupKind.NonGenericConstrainedMethod:
                    {
                        var constraintType = metadata.GetType(token);
                        var method = metadata.GetMethod(token2);
                        var constrainedMethodType = method.OwningType;
                        var constrainedMethodSlot = ComputeConstrainedMethodSlot(method);

                        TypeLoaderLogger.WriteLine("NonGenericConstrainedMethod: " + constraintType.ToString() + " Method:" + method.ToString() + " Consisting of " + constrainedMethodType.ToString() + ", slot #" + constrainedMethodSlot.LowLevelToString());

                        cell = new NonGenericConstrainedMethodCell()
                        {
                            ConstraintType = constraintType,
                            ConstrainedMethodType = constrainedMethodType,
                            ConstrainedMethodSlot = (int)constrainedMethodSlot
                        };
                    }
                    break;

                case MetadataFixupKind.GenericConstrainedMethod:
                    {
                        var constraintType = metadata.GetType(token);
                        var method = metadata.GetMethod(token2);
                        var nativeFormatMethod = (TypeSystem.NativeFormat.NativeFormatMethod)method.GetTypicalMethodDefinition();


                        TypeLoaderLogger.WriteLine("GenericConstrainedMethod: " + constraintType.ToString() + " Method " + method.ToString());

                        cell = new GenericConstrainedMethodCell()
                        {
                            ConstraintType = constraintType,
                            ConstrainedMethod = method,
                            MethodName = IntPtr.Zero,
                            MethodSignature = RuntimeSignature.CreateFromMethodHandle(nativeFormatMethod.MetadataUnit.RuntimeModule, nativeFormatMethod.Handle.ToInt())
                        };
                    }
                    break;

                case MetadataFixupKind.IsInst:
                case MetadataFixupKind.CastClass:
                    {
                        var type = metadata.GetType(token);

                        TypeLoaderLogger.WriteLine("Casting on: " + type.ToString());

                        cell = new CastingCell { Type = type, Throwing = (kind == MetadataFixupKind.CastClass) };
                    }
                    break;

                case MetadataFixupKind.AllocateArray:
                    {
                        var type = metadata.GetType(token);

                        TypeLoaderLogger.WriteLine("AllocateArray on: " + type.ToString());

                        cell = new AllocateArrayCell { Type = type };
                    }
                    break;

                case MetadataFixupKind.CheckArrayElementType:
                    {
                        var type = metadata.GetType(token);

                        TypeLoaderLogger.WriteLine("CheckArrayElementType on: " + type.ToString());

                        cell = new CheckArrayElementTypeCell { Type = type };
                    }
                    break;

                case MetadataFixupKind.CallingConventionConverter_NoInstantiatingParam:
                case MetadataFixupKind.CallingConventionConverter_MaybeInstantiatingParam:
                case MetadataFixupKind.CallingConventionConverter_HasInstantiatingParam:
                    {
                        CallingConventionConverterKind converterKind;
                        switch (kind)
                        {
                            case MetadataFixupKind.CallingConventionConverter_NoInstantiatingParam:
                                converterKind = CallingConventionConverterKind.NoInstantiatingParam;
                                break;
                            case MetadataFixupKind.CallingConventionConverter_MaybeInstantiatingParam:
                                converterKind = CallingConventionConverterKind.MaybeInstantiatingParam;
                                break;
                            case MetadataFixupKind.CallingConventionConverter_HasInstantiatingParam:
                                converterKind = CallingConventionConverterKind.HasInstantiatingParam;
                                break;
                            default:
                                Environment.FailFast("Unknown converter kind");
                                throw new BadImageFormatException();
                        }

                        cell = new CallingConventionConverterCell
                        {
                            Flags = converterKind,
                            Signature = metadata.GetSignature(token),
                            MethodArgs = metadata.MethodInstantiation,
                            TypeArgs = metadata.TypeInstantiation
                        };

#if TYPE_LOADER_TRACE
                        TypeLoaderLogger.WriteLine("CallingConventionConverter on: ");
                        TypeLoaderLogger.WriteLine("     -> Flags: " + ((int)converterKind).LowLevelToString());
                        TypeLoaderLogger.WriteLine("     -> Signature: " + token.ToInt().LowLevelToString());
                        for (int i = 0; i < metadata.TypeInstantiation.Length; i++)
                            TypeLoaderLogger.WriteLine("     -> TypeArg[" + i.LowLevelToString() + "]: " + metadata.TypeInstantiation[i]);
                        for (int i = 0; i < metadata.MethodInstantiation.Length; i++)
                            TypeLoaderLogger.WriteLine("     -> MethodArg[" + i.LowLevelToString() + "]: " + metadata.MethodInstantiation[i]);
#endif
                    }
                    break;

                default:
                    Environment.FailFast("Unknown fixup kind");
                    // Throw here so that the compiler won't complain.
                    throw new BadImageFormatException();
            }

            return cell;
        }
#endif

        internal static GenericDictionaryCell ParseAndCreateCell(NativeLayoutInfoLoadContext nativeLayoutInfoLoadContext, ref NativeParser parser)
        {
            GenericDictionaryCell cell;

            var kind = parser.GetFixupSignatureKind();
            switch (kind)
            {
                case FixupSignatureKind.TypeHandle:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        TypeLoaderLogger.WriteLine("TypeHandle: " + type.ToString());

                        cell = new TypeHandleCell() { Type = type };
                    }
                    break;

                case FixupSignatureKind.InterfaceCall:
                    {
                        var interfaceType = nativeLayoutInfoLoadContext.GetType(ref parser);
                        var slot = parser.GetUnsigned();
                        TypeLoaderLogger.WriteLine("InterfaceCall: " + interfaceType.ToString() + ", slot #" + slot.LowLevelToString());

                        cell = new InterfaceCallCell() { InterfaceType = interfaceType, Slot = (int)slot };
                    }
                    break;

                case FixupSignatureKind.MethodDictionary:
                    {
                        var genericMethod = nativeLayoutInfoLoadContext.GetMethod(ref parser);
                        Debug.Assert(genericMethod.Instantiation.Length > 0);
                        TypeLoaderLogger.WriteLine("MethodDictionary: " + genericMethod.ToString());

                        cell = new MethodDictionaryCell { GenericMethod = (InstantiatedMethod)genericMethod };
                    }
                    break;

                case FixupSignatureKind.StaticData:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        StaticDataKind staticDataKind = (StaticDataKind)parser.GetUnsigned();
                        TypeLoaderLogger.WriteLine("StaticData (" + (staticDataKind == StaticDataKind.Gc ? "Gc" : "NonGc") + ": " + type.ToString());

                        cell = new StaticDataCell() { DataKind = staticDataKind, Type = type };
                    }
                    break;

                case FixupSignatureKind.UnwrapNullableType:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        TypeLoaderLogger.WriteLine("UnwrapNullableType of: " + type.ToString());

                        if (type is DefType)
                            cell = new UnwrapNullableTypeCell() { Type = (DefType)type };
                        else
                            cell = new TypeHandleCell() { Type = type };
                    }
                    break;

                case FixupSignatureKind.FieldLdToken:
                    {
                        NativeParser ldtokenSigParser = parser.GetParserFromRelativeOffset();

                        var type = nativeLayoutInfoLoadContext.GetType(ref ldtokenSigParser);
                        IntPtr fieldNameSig = ldtokenSigParser.Reader.OffsetToAddress(ldtokenSigParser.Offset);
                        TypeLoaderLogger.WriteLine("LdToken on: " + type.ToString() + "." + ldtokenSigParser.GetString());

                        cell = new FieldLdTokenCell() { FieldName = fieldNameSig, ContainingType = type };
                    }
                    break;

                case FixupSignatureKind.MethodLdToken:
                    {
                        NativeParser ldtokenSigParser = parser.GetParserFromRelativeOffset();

                        RuntimeSignature methodNameSig;
                        RuntimeSignature methodSig;
                        var method = nativeLayoutInfoLoadContext.GetMethod(ref ldtokenSigParser, out methodNameSig, out methodSig);
                        TypeLoaderLogger.WriteLine("LdToken on: " + method.OwningType.ToString() + "::" + method.NameAndSignature.Name);

                        cell = new MethodLdTokenCell
                        {
                            Method = method,
                            MethodName = methodNameSig.NativeLayoutSignature(),
                            MethodSignature = methodSig
                        };
                    }
                    break;

#if FEATURE_UNIVERSAL_GENERICS
                case FixupSignatureKind.TypeSize:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        TypeLoaderLogger.WriteLine("TypeSize: " + type.ToString());

                        cell = new TypeSizeCell() { Type = type };
                    }
                    break;

                case FixupSignatureKind.FieldOffset:
                    {
                        var type = (DefType)nativeLayoutInfoLoadContext.GetType(ref parser);
                        uint ordinal = parser.GetUnsigned();
                        TypeLoaderLogger.WriteLine("FieldOffset on: " + type.ToString());

                        cell = new FieldOffsetCell() { Ordinal = ordinal, ContainingType = type };
                    }
                    break;

                case FixupSignatureKind.VTableOffset:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        var vtableSlot = parser.GetUnsigned();
                        TypeLoaderLogger.WriteLine("VTableOffset on: " + type.ToString() + ", slot: " + vtableSlot.LowLevelToString());

                        cell = new VTableOffsetCell() { ContainingType = type, VTableSlot = vtableSlot };
                    }
                    break;
#endif

                case FixupSignatureKind.AllocateObject:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        TypeLoaderLogger.WriteLine("AllocateObject on: " + type.ToString());

                        cell = new AllocateObjectCell { Type = type };
                    }
                    break;

                case FixupSignatureKind.DefaultConstructor:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);
                        TypeLoaderLogger.WriteLine("DefaultConstructor on: " + type.ToString());

                        cell = new DefaultConstructorCell { Type = type };
                    }
                    break;

                case FixupSignatureKind.Method:
                    {
                        RuntimeSignature methodSig;
                        RuntimeSignature methodNameSig;
                        var method = nativeLayoutInfoLoadContext.GetMethod(ref parser, out methodNameSig, out methodSig);
                        TypeLoaderLogger.WriteLine("Method: " + method.ToString());

                        cell = new MethodCell
                        {
                            Method = method,
                            MethodSignature = methodSig
                        };
                    }
                    break;

#if FEATURE_UNIVERSAL_GENERICS
                case FixupSignatureKind.NonGenericDirectConstrainedMethod:
                    {
                        var constraintType = nativeLayoutInfoLoadContext.GetType(ref parser);
                        var constrainedMethodType = nativeLayoutInfoLoadContext.GetType(ref parser);
                        var constrainedMethodSlot = parser.GetUnsigned();
                        TypeLoaderLogger.WriteLine("NonGenericDirectConstrainedMethod: " + constraintType.ToString() + " Method " + constrainedMethodType.ToString() + ", slot #" + constrainedMethodSlot.LowLevelToString());

                        cell = new NonGenericDirectConstrainedMethodCell()
                        {
                            ConstraintType = constraintType,
                            ConstrainedMethodType = constrainedMethodType,
                            ConstrainedMethodSlot = (int)constrainedMethodSlot
                        };
                    }
                    break;

                case FixupSignatureKind.NonGenericConstrainedMethod:
                    {
                        var constraintType = nativeLayoutInfoLoadContext.GetType(ref parser);
                        var constrainedMethodType = nativeLayoutInfoLoadContext.GetType(ref parser);
                        var constrainedMethodSlot = parser.GetUnsigned();
                        TypeLoaderLogger.WriteLine("NonGenericConstrainedMethod: " + constraintType.ToString() + " Method " + constrainedMethodType.ToString() + ", slot #" + constrainedMethodSlot.LowLevelToString());

                        cell = new NonGenericConstrainedMethodCell()
                        {
                            ConstraintType = constraintType,
                            ConstrainedMethodType = constrainedMethodType,
                            ConstrainedMethodSlot = (int)constrainedMethodSlot
                        };
                    }
                    break;

                case FixupSignatureKind.GenericConstrainedMethod:
                    {
                        var constraintType = nativeLayoutInfoLoadContext.GetType(ref parser);

                        NativeParser ldtokenSigParser = parser.GetParserFromRelativeOffset();
                        RuntimeSignature methodNameSig;
                        RuntimeSignature methodSig;
                        var method = nativeLayoutInfoLoadContext.GetMethod(ref ldtokenSigParser, out methodNameSig, out methodSig);

                        TypeLoaderLogger.WriteLine("GenericConstrainedMethod: " + constraintType.ToString() + " Method " + method.OwningType.ToString() + "::" + method.NameAndSignature.Name);

                        cell = new GenericConstrainedMethodCell()
                        {
                            ConstraintType = constraintType,
                            ConstrainedMethod = method,
                            MethodName = methodNameSig.NativeLayoutSignature(),
                            MethodSignature = methodSig
                        };
                    }
                    break;
#endif

                case FixupSignatureKind.IsInst:
                case FixupSignatureKind.CastClass:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);

                        TypeLoaderLogger.WriteLine("Casting on: " + type.ToString());

                        cell = new CastingCell { Type = type, Throwing = (kind == FixupSignatureKind.CastClass) };
                    }
                    break;

                case FixupSignatureKind.AllocateArray:
                    {
                        var type = nativeLayoutInfoLoadContext.GetType(ref parser);

                        TypeLoaderLogger.WriteLine("AllocateArray on: " + type.ToString());

                        cell = new AllocateArrayCell { Type = type };
                    }
                    break;

#if FEATURE_UNIVERSAL_GENERICS
                case FixupSignatureKind.CallingConventionConverter:
                    {
                        CallingConventionConverterKind flags = (CallingConventionConverterKind)parser.GetUnsigned();
                        NativeParser sigParser = parser.GetParserFromRelativeOffset();
                        RuntimeSignature signature = RuntimeSignature.CreateFromNativeLayoutSignature(nativeLayoutInfoLoadContext._module.Handle, sigParser.Offset);

                        TypeLoaderLogger.WriteLine("CallingConventionConverter: Flags=" + ((int)flags).LowLevelToString() + " Signature=" + signature.NativeLayoutSignature().LowLevelToString());

                        cell = new CallingConventionConverterCell
                        {
                            Flags = flags,
                            Signature = signature,
                            MethodArgs = nativeLayoutInfoLoadContext._methodArgumentHandles,
                            TypeArgs = nativeLayoutInfoLoadContext._typeArgumentHandles
                        };
                    }
                    break;
#endif

                case FixupSignatureKind.NotYetSupported:
                case FixupSignatureKind.ThreadStaticIndex:
                    TypeLoaderLogger.WriteLine("Valid dictionary entry, but not yet supported by the TypeLoader!");
                    throw new TypeBuilder.MissingTemplateException();

                case FixupSignatureKind.PointerToOtherSlot:
                    cell = new PointerToOtherDictionarySlotCell
                    {
                        OtherDictionarySlot = parser.GetUnsigned()
                    };
                    TypeLoaderLogger.WriteLine("PointerToOtherSlot: " + ((PointerToOtherDictionarySlotCell)cell).OtherDictionarySlot.LowLevelToString());
                    break;

                case FixupSignatureKind.IntValue:
                    cell = new IntPtrCell
                    {
                        Value = new IntPtr((int)parser.GetUnsigned())
                    };
                    TypeLoaderLogger.WriteLine("IntValue: " + ((IntPtrCell)cell).Value.LowLevelToString());
                    break;

                default:
                    parser.ThrowBadImageFormatException();
                    cell = null;
                    break;
            }

            return cell;
        }
    }
}
