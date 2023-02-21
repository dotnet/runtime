// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    /// <summary>
    /// TypeSystemContext that can interfact with the
    /// Redhawk runtime type system and native metadata
    /// </summary>
    public partial class TypeLoaderTypeSystemContext : TypeSystemContext
    {
        private static readonly NoMetadataRuntimeInterfacesAlgorithm s_noMetadataRuntimeInterfacesAlgorithm = new NoMetadataRuntimeInterfacesAlgorithm();
        private static readonly NativeLayoutInterfacesAlgorithm s_nativeLayoutInterfacesAlgorithm = new NativeLayoutInterfacesAlgorithm();

        public TypeLoaderTypeSystemContext(TargetDetails targetDetails) : base(targetDetails)
        {
        }

        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForDefType(DefType type)
        {
            if (type.RetrieveRuntimeTypeHandleIfPossible())
            {
                // If the type is already constructed, use the NoMetadataRuntimeInterfacesAlgorithm.
                // its more efficient than loading from native layout or metadata.
                return s_noMetadataRuntimeInterfacesAlgorithm;
            }
            else if (type.HasNativeLayout)
            {
                return s_nativeLayoutInterfacesAlgorithm;
            }
            return s_noMetadataRuntimeInterfacesAlgorithm;
        }

        protected internal sealed override bool IsIDynamicInterfaceCastableInterface(DefType type)
        {
            throw new NotImplementedException();
        }

        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForNonPointerArrayType(ArrayType type)
        {
            // At runtime, we're instantiating an Array<T> instantiation as the template, so we know we'll always have
            // a NativeLayoutInterfacesAlgorithm to work with
            return s_nativeLayoutInterfacesAlgorithm;
        }

        public override DefType GetWellKnownType(WellKnownType wellKnownType, bool throwIfNotFound = true)
        {
            switch (wellKnownType)
            {
                case WellKnownType.Void:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(void).TypeHandle);

                case WellKnownType.Boolean:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(bool).TypeHandle);

                case WellKnownType.Char:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(char).TypeHandle);

                case WellKnownType.SByte:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(sbyte).TypeHandle);

                case WellKnownType.Byte:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(byte).TypeHandle);

                case WellKnownType.Int16:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(short).TypeHandle);

                case WellKnownType.UInt16:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(ushort).TypeHandle);

                case WellKnownType.Int32:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(int).TypeHandle);

                case WellKnownType.UInt32:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(uint).TypeHandle);

                case WellKnownType.Int64:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(long).TypeHandle);

                case WellKnownType.UInt64:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(ulong).TypeHandle);

                case WellKnownType.IntPtr:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(IntPtr).TypeHandle);

                case WellKnownType.UIntPtr:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(UIntPtr).TypeHandle);

                case WellKnownType.Single:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(float).TypeHandle);

                case WellKnownType.Double:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(double).TypeHandle);

                case WellKnownType.ValueType:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(ValueType).TypeHandle);

                case WellKnownType.Enum:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(Enum).TypeHandle);

                case WellKnownType.Nullable:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(Nullable<>).TypeHandle);

                case WellKnownType.Object:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(object).TypeHandle);

                case WellKnownType.String:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(string).TypeHandle);

                case WellKnownType.Array:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(Array).TypeHandle);

                case WellKnownType.MulticastDelegate:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(MulticastDelegate).TypeHandle);

                case WellKnownType.RuntimeTypeHandle:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(RuntimeTypeHandle).TypeHandle);

                case WellKnownType.RuntimeMethodHandle:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(RuntimeMethodHandle).TypeHandle);

                case WellKnownType.RuntimeFieldHandle:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(RuntimeFieldHandle).TypeHandle);

                case WellKnownType.Exception:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(Exception).TypeHandle);

                default:
                    if (throwIfNotFound)
                        throw new TypeLoadException();
                    else
                        return null;
            }
        }

        protected internal override Instantiation ConvertInstantiationToCanonForm(Instantiation instantiation, CanonicalFormKind kind, out bool changed)
        {
            return StandardCanonicalizationAlgorithm.ConvertInstantiationToCanonForm(instantiation, kind, out changed);
        }

        protected internal override TypeDesc ConvertToCanon(TypeDesc typeToConvert, CanonicalFormKind kind)
        {
            return StandardCanonicalizationAlgorithm.ConvertToCanon(typeToConvert, kind);
        }

        protected internal override bool ComputeHasStaticConstructor(TypeDesc type)
        {
            // This assumes we can compute the information from a type definition
            // (`type` is going to be a definition here because that's how the type system is structured).
            // We don't maintain consistency for this at runtime. Different instantiations of
            // a single definition may or may not have a static constructor after AOT compilation.
            // Asking about this for a definition is an invalid question.
            // If this is ever needed, we need to restructure things in the common type system.
            throw new NotImplementedException();
        }

        public override bool SupportsUniversalCanon => false;
        public override bool SupportsCanon => true;
    }
}
