// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public struct MethodDefinition
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint _treatmentAndRowId;

        internal MethodDefinition(MetadataReader reader, uint treatmentAndRowId)
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

        private MethodDefTreatment Treatment
        {
            get { return (MethodDefTreatment)(_treatmentAndRowId >> TokenTypeIds.RowIdBitCount); }
        }

        private MethodDefinitionHandle Handle
        {
            get { return MethodDefinitionHandle.FromRowId(RowId); }
        }

        public StringHandle Name
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.MethodDefTable.GetName(Handle);
                }

                return GetProjectedName();
            }
        }

        public BlobHandle Signature
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.MethodDefTable.GetSignature(Handle);
                }

                return GetProjectedSignature();
            }
        }

        public int RelativeVirtualAddress
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.MethodDefTable.GetRva(Handle);
                }

                return GetProjectedRelativeVirtualAddress();
            }
        }

        public MethodAttributes Attributes
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.MethodDefTable.GetFlags(Handle);
                }

                return GetProjectedFlags();
            }
        }

        public MethodImplAttributes ImplAttributes
        {
            get
            {
                if (Treatment == 0)
                {
                    return _reader.MethodDefTable.GetImplFlags(Handle);
                }

                return GetProjectedImplFlags();
            }
        }

        public TypeDefinitionHandle GetDeclaringType()
        {
            return _reader.GetDeclaringType(Handle);
        }

        public ParameterHandleCollection GetParameters()
        {
            return new ParameterHandleCollection(_reader, Handle);
        }

        public GenericParameterHandleCollection GetGenericParameters()
        {
            return _reader.GenericParamTable.FindGenericParametersForMethod(Handle);
        }

        public MethodImport GetImport()
        {
            int implMapRid = _reader.ImplMapTable.FindImplForMethod(Handle);
            if (implMapRid == 0)
            {
                return default(MethodImport);
            }

            return _reader.ImplMapTable.GetImport(implMapRid);
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

        private StringHandle GetProjectedName()
        {
            if ((Treatment & MethodDefTreatment.KindMask) == MethodDefTreatment.DisposeMethod)
            {
                return StringHandle.FromVirtualIndex(StringHandle.VirtualIndex.Dispose);
            }

            return _reader.MethodDefTable.GetName(Handle);
        }

        private MethodAttributes GetProjectedFlags()
        {
            MethodAttributes flags = _reader.MethodDefTable.GetFlags(Handle);
            MethodDefTreatment treatment = Treatment;

            if ((treatment & MethodDefTreatment.KindMask) == MethodDefTreatment.HiddenInterfaceImplementation)
            {
                flags = (flags & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Private;
            }

            if ((treatment & MethodDefTreatment.MarkAbstractFlag) != 0)
            {
                flags |= MethodAttributes.Abstract;
            }

            if ((treatment & MethodDefTreatment.MarkPublicFlag) != 0)
            {
                flags = (flags & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
            }


            return flags | MethodAttributes.HideBySig;
        }

        private MethodImplAttributes GetProjectedImplFlags()
        {
            MethodImplAttributes flags = _reader.MethodDefTable.GetImplFlags(Handle);

            switch (Treatment & MethodDefTreatment.KindMask)
            {
                case MethodDefTreatment.DelegateMethod:
                    flags |= MethodImplAttributes.Runtime;
                    break;

                case MethodDefTreatment.DisposeMethod:
                case MethodDefTreatment.AttributeMethod:
                case MethodDefTreatment.InterfaceMethod:
                case MethodDefTreatment.HiddenInterfaceImplementation:
                case MethodDefTreatment.Other:
                    flags |= MethodImplAttributes.Runtime | MethodImplAttributes.InternalCall;
                    break;
            }

            return flags;
        }

        private BlobHandle GetProjectedSignature()
        {
            return _reader.MethodDefTable.GetSignature(Handle);
        }

        private int GetProjectedRelativeVirtualAddress()
        {
            return 0;
        }
        #endregion
    }
}