// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using CORINFO_DEVIRTUALIZATION_DETAIL = Internal.JitInterface.CORINFO_DEVIRTUALIZATION_DETAIL;
using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Manages devirtualization behaviors. Devirtualization is the process of converting
    /// virtual calls to direct calls in cases where we can compute the result of a virtual
    /// lookup at compile time.
    /// </summary>
    public class DevirtualizationManager
    {
        /// <summary>
        /// Returns true if <paramref name="type"/> cannot be the base class of any other
        /// type.
        /// </summary>
        public virtual bool IsEffectivelySealed(TypeDesc type)
        {
            switch (type.Category)
            {
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                case TypeFlags.ByRef:
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                    return true;

                default:
                    Debug.Assert(type.IsDefType);
                    var metadataType = (MetadataType)type;
                    return metadataType.IsSealed || metadataType.IsModuleType;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="method"/> cannot be overriden by any other method.
        /// </summary>
        public virtual bool IsEffectivelySealed(MethodDesc method)
        {
            return method.IsFinal || IsEffectivelySealed(method.OwningType);
        }

        /// <summary>
        /// Attempts to resolve the <paramref name="declMethod"/> virtual method into
        /// a method on <paramref name="implType"/> that implements the declaring method.
        /// Returns null if this is not possible.
        /// </summary>
        /// <remarks>
        /// Note that if <paramref name="implType"/> is a value type, the result of the resolution
        /// might have to be treated as an unboxing thunk by the caller.
        /// </remarks>
        public MethodDesc ResolveVirtualMethod(MethodDesc declMethod, TypeDesc implType, ref CORINFO_DEVIRTUALIZATION_DETAIL devirtualizationDetail)
        {
            Debug.Assert(declMethod.IsVirtual);

            // We're operating on virtual methods. This means that if implType is an array, we need
            // to get the type that has all the virtual methods provided by the class library.
            return ResolveVirtualMethod(declMethod, implType.GetClosestDefType(), ref devirtualizationDetail);
        }

        protected virtual MethodDesc ResolveVirtualMethod(MethodDesc declMethod, DefType implType, ref CORINFO_DEVIRTUALIZATION_DETAIL devirtualizationDetail)
        {
            MethodDesc impl;

            if (declMethod.OwningType.IsInterface)
            {
                if (declMethod.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any) || implType.IsCanonicalSubtype(CanonicalFormKind.Any))
                {
                    DefType[] implTypeRuntimeInterfaces = implType.RuntimeInterfaces;
                    int canonicallyMatchingInterfacesFound = 0;
                    DefType canonicalInterfaceType = (DefType)declMethod.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific);
                    for (int i = 0; i < implTypeRuntimeInterfaces.Length; i++)
                    {
                        DefType runtimeInterface = implTypeRuntimeInterfaces[i];
                        if (canonicalInterfaceType.HasSameTypeDefinition(runtimeInterface) &&
                            runtimeInterface.ConvertToCanonForm(CanonicalFormKind.Specific) == canonicalInterfaceType)
                        {
                            canonicallyMatchingInterfacesFound++;
                            if (canonicallyMatchingInterfacesFound > 1)
                            {
                                // We cannot resolve the interface as we don't know with exact enough detail which interface
                                // of multiple possible interfaces is being called.
                                devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_MULTIPLE_IMPL;
                                return null;
                            }
                        }
                    }
                }

                if (!implType.CanCastTo(declMethod.OwningType))
                {
                    devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_FAILED_CAST;
                    return null;
                }

                impl = implType.ResolveInterfaceMethodTarget(declMethod);
                if (impl != null)
                {
                    impl = implType.FindVirtualFunctionTargetMethodOnObjectType(impl);
                }
            }
            else
            {
                // The derived class should be a subclass of the the base class.
                // this check is perfomed via typedef checking instead of casting, as we accept canon methods calling exact types
                TypeDesc checkType;
                for (checkType = implType; checkType != null && !checkType.HasSameTypeDefinition(declMethod.OwningType); checkType = checkType.BaseType)
                { }

                if (checkType == null)
                {
                    // The derived class should be a subclass of the the base class.
                    devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_FAILED_SUBCLASS;
                    return null;
                }

                impl = implType.FindVirtualFunctionTargetMethodOnObjectType(declMethod);
                if (impl != null && (impl != declMethod))
                {
                    MethodDesc slotDefiningMethodImpl = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(impl);
                    MethodDesc slotDefiningMethodDecl = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(declMethod);

                    if (slotDefiningMethodImpl != slotDefiningMethodDecl)
                    {
                        // If the derived method's slot does not match the vtable slot,
                        // bail on devirtualization, as the method was installed into
                        // the vtable slot via an explicit override and even if the
                        // method is final, the slot may not be.
                        //
                        // Note the jit could still safely devirtualize if it had an exact
                        // class, but such cases are likely rare.
                        devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_FAILED_SLOT;
                        impl = null;
                    }
                }
            }

            return impl;
        }
    }
}
