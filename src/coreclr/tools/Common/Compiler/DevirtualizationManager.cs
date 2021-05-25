// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

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
        public MethodDesc ResolveVirtualMethod(MethodDesc declMethod, TypeDesc implType)
        {
            Debug.Assert(declMethod.IsVirtual);

            // We're operating on virtual methods. This means that if implType is an array, we need
            // to get the type that has all the virtual methods provided by the class library.
            return ResolveVirtualMethod(declMethod, implType.GetClosestDefType());
        }

        protected virtual MethodDesc ResolveVirtualMethod(MethodDesc declMethod, DefType implType)
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
                                return null;
                            }
                        }
                    }
                }

                impl = implType.ResolveInterfaceMethodTarget(declMethod);
                if (impl != null)
                {
                    impl = implType.FindVirtualFunctionTargetMethodOnObjectType(impl);
                }
            }
            else
            {
                impl = implType.FindVirtualFunctionTargetMethodOnObjectType(declMethod);
                if (impl != null && (impl != declMethod))
                {
                    MethodDesc slotDefiningMethodImpl = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(impl);
                    MethodDesc slotDefiningMethodDecl = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(declMethod);

                    if (slotDefiningMethodImpl != slotDefiningMethodDecl)
                    {
                        // We cannot resolve virtual method in case the impl is a different slot from the declMethod
                        impl = null;
                    }
                }
            }

            return impl;
        }
    }
}
