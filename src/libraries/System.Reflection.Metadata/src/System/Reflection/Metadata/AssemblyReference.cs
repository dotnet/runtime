// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public struct AssemblyReference
    {
        private readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use raw uint.
        private readonly uint treatmentAndRowId;

        private static readonly Version version_1_1_0_0 = new Version(1, 1, 0, 0);
        private static readonly Version version_4_0_0_0 = new Version(4, 0, 0, 0);

        internal AssemblyReference(MetadataReader reader, uint treatmentAndRowId)
        {
            Debug.Assert(reader != null);
            Debug.Assert(treatmentAndRowId != 0);

            // only virtual bit can be set in highest byte:
            Debug.Assert((treatmentAndRowId & ~(TokenTypeIds.VirtualTokenMask | TokenTypeIds.RIDMask)) == 0);

            this.reader = reader;
            this.treatmentAndRowId = treatmentAndRowId;
        }

        private uint RowId
        {
            get { return treatmentAndRowId & TokenTypeIds.RIDMask; }
        }

        private bool IsVirtual
        {
            get { return (treatmentAndRowId & TokenTypeIds.VirtualTokenMask) != 0; }
        }

        public Version Version
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualVersion();
                }

                // change mscorlib version:
                if (RowId == reader.WinMDMscorlibRef)
                {
                    return version_4_0_0_0;
                }

                return reader.AssemblyRefTable.GetVersion(RowId);
            }
        }

        public AssemblyFlags Flags
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualFlags();
                }

                return reader.AssemblyRefTable.GetFlags(RowId);
            }
        }

        public StringHandle Name
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualName();
                }

                return reader.AssemblyRefTable.GetName(RowId);
            }
        }

        public StringHandle Culture
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualCulture();
                }

                return reader.AssemblyRefTable.GetCulture(RowId);
            }
        }

        public BlobHandle PublicKeyOrToken
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualPublicKeyOrToken();
                }

                return reader.AssemblyRefTable.GetPublicKeyOrToken(RowId);
            }
        }

        public BlobHandle HashValue
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualHashValue();
                }

                return reader.AssemblyRefTable.GetHashValue(RowId);
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            if (IsVirtual)
            {
                return GetVirtualCustomAttributes();
            }

            return new CustomAttributeHandleCollection(reader, AssemblyReferenceHandle.FromRowId(RowId));
        }

        #region Virtual Rows
        private Version GetVirtualVersion()
        {
            switch ((AssemblyReferenceHandle.VirtualIndex)RowId)
            {
                case AssemblyReferenceHandle.VirtualIndex.System_Numerics_Vectors:
                    return version_1_1_0_0;
                default:
                    return version_4_0_0_0;
            }
        }

        private AssemblyFlags GetVirtualFlags()
        {
            // use flags from mscorlib ref (specifically PublicKey flag):
            return reader.AssemblyRefTable.GetFlags(reader.WinMDMscorlibRef);
        }

        private StringHandle GetVirtualName()
        {
            return StringHandle.FromVirtualIndex(GetVirtualNameIndex((AssemblyReferenceHandle.VirtualIndex)RowId));
        }

        private StringHandle.VirtualIndex GetVirtualNameIndex(AssemblyReferenceHandle.VirtualIndex index)
        {
            switch (index)
            {
                case AssemblyReferenceHandle.VirtualIndex.System_ObjectModel:
                    return StringHandle.VirtualIndex.System_ObjectModel;

                case AssemblyReferenceHandle.VirtualIndex.System_Runtime:
                    return StringHandle.VirtualIndex.System_Runtime;

                case AssemblyReferenceHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime:
                    return StringHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime;

                case AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime:
                    return StringHandle.VirtualIndex.System_Runtime_WindowsRuntime;

                case AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml:
                    return StringHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml;

                case AssemblyReferenceHandle.VirtualIndex.System_Numerics_Vectors:
                    return StringHandle.VirtualIndex.System_Numerics_Vectors;
            }

            Debug.Assert(false, "Unexpected virtual index value");
            return 0;
        }

        private StringHandle GetVirtualCulture()
        {
            return default(StringHandle);
        }

        private BlobHandle GetVirtualPublicKeyOrToken()
        {
            switch ((AssemblyReferenceHandle.VirtualIndex)RowId)
            {
                case AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime:
                case AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml:
                    // use key or token from mscorlib ref:
                    return reader.AssemblyRefTable.GetPublicKeyOrToken(reader.WinMDMscorlibRef);

                default:
                    // use contract assembly key or token:
                    var hasFullKey = (reader.AssemblyRefTable.GetFlags(reader.WinMDMscorlibRef) & AssemblyFlags.PublicKey) != 0;
                    return BlobHandle.FromVirtualIndex(hasFullKey ? BlobHandle.VirtualIndex.ContractPublicKey : BlobHandle.VirtualIndex.ContractPublicKeyToken, 0);
            }
        }

        private BlobHandle GetVirtualHashValue()
        {
            return default(BlobHandle);
        }

        private CustomAttributeHandleCollection GetVirtualCustomAttributes()
        {
            // return custom attributes applied on mscorlib ref
            return new CustomAttributeHandleCollection(reader, AssemblyReferenceHandle.FromRowId(reader.WinMDMscorlibRef));
        }
        #endregion
    }
}