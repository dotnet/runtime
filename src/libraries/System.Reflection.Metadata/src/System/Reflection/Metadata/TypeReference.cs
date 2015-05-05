// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public struct TypeReference
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint _treatmentAndRowId;

        internal TypeReference(MetadataReader reader, uint treatmentAndRowId)
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

        private TypeRefTreatment Treatment
        {
            get { return (TypeRefTreatment)(_treatmentAndRowId >> TokenTypeIds.RowIdBitCount); }
        }

        private TypeReferenceHandle Handle
        {
            get { return TypeReferenceHandle.FromRowId(RowId); }
        }

        /// <summary>
        /// Resolution scope in which the target type is defined and is uniquely identified by the specified <see cref="Namespace"/> and <see cref="Name"/>.
        /// </summary>
        /// <remarks>
        /// Resolution scope can be one of the following handles:
        /// <list type="bullet">
        /// <item><description><see cref="TypeReferenceHandle"/> of the enclosing type, if the target type is a nested type.</description></item>
        /// <item><description><see cref="ModuleReferenceHandle"/>, if the target type is defined in another module within the same assembly as this one.</description></item>
        /// <item><description><see cref="EntityHandle.ModuleDefinition"/>, if the target type is defined in the current module. This should not occur in a CLI compressed metadata module.</description></item>
        /// <item><description><see cref="AssemblyReferenceHandle"/>, if the target type is defined in a different assembly from the current module.</description></item>
        /// <item><description>Nil handle if the target type must be resolved by searching the <see cref="MetadataReader.ExportedTypes"/> for a matching <see cref="Namespace"/> and <see cref="Name"/>.</description></item>
        /// </list>
        /// </remarks>
        public EntityHandle ResolutionScope
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.TypeRefTable.GetResolutionScope(Handle);
                }

                return GetProjectedResolutionScope();
            }
        }

        /// <summary>
        /// Name of the target type.
        /// </summary>
        public StringHandle Name
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.TypeRefTable.GetName(Handle);
                }

                return GetProjectedName();
            }
        }

        /// <summary>
        /// Full name of the namespace where the target type is defined, or nil if the type is nested or defined in a root namespace.
        /// </summary>
        public StringHandle Namespace
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.TypeRefTable.GetNamespace(Handle);
                }

                return GetProjectedNamespace();
            }
        }

        #region Projections

        private EntityHandle GetProjectedResolutionScope()
        {
            switch (Treatment)
            {
                case TypeRefTreatment.SystemAttribute:
                case TypeRefTreatment.SystemDelegate:
                    return AssemblyReferenceHandle.FromVirtualIndex(AssemblyReferenceHandle.VirtualIndex.System_Runtime);

                case TypeRefTreatment.UseProjectionInfo:
                    return MetadataReader.GetProjectedAssemblyRef((int)RowId);
            }

            Debug.Assert(false, "Unknown TypeRef treatment");
            return default(AssemblyReferenceHandle);
        }

        private StringHandle GetProjectedName()
        {
            if (Treatment == TypeRefTreatment.UseProjectionInfo)
            {
                return MetadataReader.GetProjectedName((int)RowId);
            }
            else
            {
                return _reader.TypeRefTable.GetName(Handle);
            }
        }

        private StringHandle GetProjectedNamespace()
        {
            switch (Treatment)
            {
                case TypeRefTreatment.SystemAttribute:
                case TypeRefTreatment.SystemDelegate:
                    return StringHandle.FromVirtualIndex(StringHandle.VirtualIndex.System);

                case TypeRefTreatment.UseProjectionInfo:
                    return MetadataReader.GetProjectedNamespace((int)RowId);
            }

            Debug.Assert(false, "Unknown TypeRef treatment");
            return default(StringHandle);
        }
        #endregion
    }
}