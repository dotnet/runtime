// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public readonly struct TypeDefinition
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint _treatmentAndRowId;

        internal TypeDefinition(MetadataReader reader, uint treatmentAndRowId)
        {
            Debug.Assert(reader != null);
            Debug.Assert(treatmentAndRowId != 0);

            _reader = reader;
            _treatmentAndRowId = treatmentAndRowId;
        }

        private int RowId
        {
            get { return (int)(_treatmentAndRowId & TokenTypeIds.RIDMask); }
        }

        private TypeDefTreatment Treatment
        {
            get { return (TypeDefTreatment)(_treatmentAndRowId >> TokenTypeIds.RowIdBitCount); }
        }

        private TypeDefinitionHandle Handle
        {
            get { return TypeDefinitionHandle.FromRowId(RowId); }
        }

        public TypeAttributes Attributes
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.TypeDefTable.GetFlags(Handle);
                }

                return GetProjectedFlags();
            }
        }

        /// <summary>
        /// Name of the type.
        /// </summary>
        public StringHandle Name
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.TypeDefTable.GetName(Handle);
                }

                return GetProjectedName();
            }
        }

        /// <summary>
        /// Full name of the namespace where the type is defined, or nil if the type is nested or defined in a root namespace.
        /// </summary>
        public StringHandle Namespace
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.TypeDefTable.GetNamespace(Handle);
                }

                return GetProjectedNamespaceString();
            }
        }

        /// <summary>
        /// The definition handle of the namespace where the type is defined, or nil if the type is nested or defined in a root namespace.
        /// </summary>
        public NamespaceDefinitionHandle NamespaceDefinition
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.TypeDefTable.GetNamespaceDefinition(Handle);
                }

                return GetProjectedNamespace();
            }
        }

        /// <summary>
        /// The base type of the type definition: either
        /// <see cref="TypeSpecificationHandle"/>, <see cref="TypeReferenceHandle"/> or <see cref="TypeDefinitionHandle"/>.
        /// </summary>
        public EntityHandle BaseType
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.TypeDefTable.GetExtends(Handle);
                }

                return GetProjectedBaseType();
            }
        }

        public TypeLayout GetLayout()
        {
            int classLayoutRowId = _reader.ClassLayoutTable.FindRow(Handle);
            if (classLayoutRowId == 0)
            {
                // NOTE: We don't need a bool/TryGetLayout because zero also means use default:
                //
                // Spec:
                //  ClassSize of zero does not mean the class has zero size. It means that no .size directive was specified
                //  at definition time, in which case, the actual size is calculated from the field types, taking account of
                //  packing size (default or specified) and natural alignment on the target, runtime platform.
                //
                // PackingSize shall be one of {0, 1, 2, 4, 8, 16, 32, 64, 128}. (0 means use
                // the default pack size for the platform on which the application is
                // running.)

                return default(TypeLayout);
            }

            uint size = _reader.ClassLayoutTable.GetClassSize(classLayoutRowId);

            // The spec doesn't limit the size to 31bit. It only limits the size to 1MB if Parent is a value type.
            // It however doesn't make much sense to define classes with >2GB size. So in order to keep the API
            // clean of unsigned ints we impose the limit.
            if (unchecked((int)size) != size)
            {
                throw new BadImageFormatException(SR.InvalidTypeSize);
            }

            int packingSize = _reader.ClassLayoutTable.GetPackingSize(classLayoutRowId);
            return new TypeLayout((int)size, packingSize);
        }

        /// <summary>
        /// Returns the enclosing type of a specified nested type or nil handle if the type is not nested.
        /// </summary>
        public TypeDefinitionHandle GetDeclaringType()
        {
            return _reader.NestedClassTable.FindEnclosingType(Handle);
        }

        public GenericParameterHandleCollection GetGenericParameters()
        {
            return _reader.GenericParamTable.FindGenericParametersForType(Handle);
        }

        public MethodDefinitionHandleCollection GetMethods()
        {
            return new MethodDefinitionHandleCollection(_reader, Handle);
        }

        public FieldDefinitionHandleCollection GetFields()
        {
            return new FieldDefinitionHandleCollection(_reader, Handle);
        }

        public PropertyDefinitionHandleCollection GetProperties()
        {
            return new PropertyDefinitionHandleCollection(_reader, Handle);
        }

        public EventDefinitionHandleCollection GetEvents()
        {
            return new EventDefinitionHandleCollection(_reader, Handle);
        }

        /// <summary>
        /// Returns an array of types nested in the specified type.
        /// </summary>
        public ImmutableArray<TypeDefinitionHandle> GetNestedTypes()
        {
            return _reader.GetNestedTypes(Handle);
        }

        public MethodImplementationHandleCollection GetMethodImplementations()
        {
            return new MethodImplementationHandleCollection(_reader, Handle);
        }

        public InterfaceImplementationHandleCollection GetInterfaceImplementations()
        {
            return new InterfaceImplementationHandleCollection(_reader, Handle);
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, Handle);
        }

        public DeclarativeSecurityAttributeHandleCollection GetDeclarativeSecurityAttributes()
        {
            return new DeclarativeSecurityAttributeHandleCollection(_reader, Handle);
        }

        #region Projections

        private TypeAttributes GetProjectedFlags()
        {
            var flags = _reader.TypeDefTable.GetFlags(Handle);
            var treatment = Treatment;

            switch (treatment & TypeDefTreatment.KindMask)
            {
                case TypeDefTreatment.NormalNonAttribute:
                    flags |= TypeAttributes.WindowsRuntime | TypeAttributes.Import;
                    break;

                case TypeDefTreatment.NormalAttribute:
                    flags |= TypeAttributes.WindowsRuntime | TypeAttributes.Sealed;
                    break;

                case TypeDefTreatment.UnmangleWinRTName:
                    flags = flags & ~TypeAttributes.SpecialName | TypeAttributes.Public;
                    break;

                case TypeDefTreatment.PrefixWinRTName:
                    flags = flags & ~TypeAttributes.Public | TypeAttributes.Import;
                    break;

                case TypeDefTreatment.RedirectedToClrType:
                    flags = flags & ~TypeAttributes.Public | TypeAttributes.Import;
                    break;

                case TypeDefTreatment.RedirectedToClrAttribute:
                    flags &= ~TypeAttributes.Public;
                    break;
            }

            if ((treatment & TypeDefTreatment.MarkAbstractFlag) != 0)
            {
                flags |= TypeAttributes.Abstract;
            }

            if ((treatment & TypeDefTreatment.MarkInternalFlag) != 0)
            {
                flags &= ~TypeAttributes.Public;
            }

            return flags;
        }

        private StringHandle GetProjectedName()
        {
            var name = _reader.TypeDefTable.GetName(Handle);

            switch (Treatment & TypeDefTreatment.KindMask)
            {
                case TypeDefTreatment.UnmangleWinRTName:
                    return name.SuffixRaw(MetadataReader.ClrPrefix.Length);

                case TypeDefTreatment.PrefixWinRTName:
                    return name.WithWinRTPrefix();
            }

            return name;
        }

        private NamespaceDefinitionHandle GetProjectedNamespace()
        {
            // NOTE: NamespaceDefinitionHandle currently relies on never having virtual values. If this ever gets projected
            //       to a virtual namespace name, then that assumption will need to be removed.

            // no change:
            return _reader.TypeDefTable.GetNamespaceDefinition(Handle);
        }

        private StringHandle GetProjectedNamespaceString()
        {
            // no change:
            return _reader.TypeDefTable.GetNamespace(Handle);
        }

        private EntityHandle GetProjectedBaseType()
        {
            // no change:
            return _reader.TypeDefTable.GetExtends(Handle);
        }

        #endregion
    }
}
