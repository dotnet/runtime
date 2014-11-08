// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public struct MethodDefinition
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly uint treatmentAndRowId;

        internal MethodDefinition(MetadataReader reader, uint treatmentAndRowId)
        {
            Debug.Assert(reader != null);
            Debug.Assert(treatmentAndRowId != 0);

            this.reader = reader;
            this.treatmentAndRowId = treatmentAndRowId;
        }

        private uint RowId
        {
            get { return treatmentAndRowId & TokenTypeIds.RIDMask; }
        }

        private MethodDefTreatment Treatment
        {
            get { return (MethodDefTreatment)(treatmentAndRowId >> TokenTypeIds.RowIdBitCount); }
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
                    return reader.MethodDefTable.GetName(Handle);
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
                    return reader.MethodDefTable.GetSignature(Handle);
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
                    return reader.MethodDefTable.GetRva(Handle);
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
                    return reader.MethodDefTable.GetFlags(Handle);
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
                    return reader.MethodDefTable.GetImplFlags(Handle);
                }

                return GetProjectedImplFlags();
            }
        }

        public TypeDefinitionHandle GetDeclaringType()
        {
            return reader.GetDeclaringType(Handle);
        }

        public ParameterHandleCollection GetParameters()
        {
            return new ParameterHandleCollection(reader, Handle);
        }

        public GenericParameterHandleCollection GetGenericParameters()
        {
            return reader.GenericParamTable.FindGenericParametersForMethod(Handle);
        }

        public MethodImport GetImport()
        {
            uint implMapRid = reader.ImplMapTable.FindImplForMethod(Handle);
            if (implMapRid == 0)
            {
                return default(MethodImport);
            }

            return reader.ImplMapTable[implMapRid];
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }

        public DeclarativeSecurityAttributeHandleCollection GetDeclarativeSecurityAttributes()
        {
            return new DeclarativeSecurityAttributeHandleCollection(reader, Handle);
        }

        #region Projections

        private StringHandle GetProjectedName()
        {
            if ((Treatment & MethodDefTreatment.KindMask) == MethodDefTreatment.DisposeMethod)
            {
                return StringHandle.FromVirtualIndex(StringHandle.VirtualIndex.Dispose);
            }

            return reader.MethodDefTable.GetName(Handle);
        }

        private MethodAttributes GetProjectedFlags()
        {
            MethodAttributes flags = reader.MethodDefTable.GetFlags(Handle);
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
            MethodImplAttributes flags = reader.MethodDefTable.GetImplFlags(Handle);

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
            return reader.MethodDefTable.GetSignature(Handle);
        }

        private int GetProjectedRelativeVirtualAddress()
        {
            return 0;
        }
        #endregion
    }
}