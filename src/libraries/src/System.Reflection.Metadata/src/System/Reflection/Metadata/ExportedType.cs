// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct ExportedType
    {
        internal readonly MetadataReader reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        internal readonly uint rowId;

        internal ExportedType(MetadataReader reader, uint rowId)
        {
            Debug.Assert(reader != null);
            Debug.Assert(rowId != 0);

            this.reader = reader;
            this.rowId = rowId;
        }

        private ExportedTypeHandle Handle
        {
            get { return ExportedTypeHandle.FromRowId(rowId); }
        }

        public TypeAttributes Attributes
        {
            get { return reader.ExportedTypeTable.GetFlags(rowId); }
        }

        public bool IsForwarder
        {
            get { return Attributes.IsForwarder() && Implementation.Kind == HandleKind.AssemblyReference; }
        }

        public StringHandle Name
        {
            get { return reader.ExportedTypeTable.GetTypeName(rowId); }
        }

        public NamespaceDefinitionHandle Namespace
        {
            get
            {
                // NOTE: NamespaceDefinitionHandle currently relies on never having virtual values. If this ever gets projected
                //       to a virtual namespace name, then that assumption will need to be removed.
                return reader.ExportedTypeTable.GetTypeNamespace(rowId);
            }
        }

        /// <summary>
        /// Handle to resolve the implementation of the target type.
        /// </summary>
        /// <returns>
        /// <list type="bullet">
        /// <item><description><see cref="AssemblyFileHandle"/> representing another module in the assembly.</description></item>
        /// <item><description><see cref="AssemblyReferenceHandle"/> representing another assembly if <see cref="IsForwarder"/> is true.</description></item>
        /// <item><description><see cref="ExportedTypeHandle"/> representing the declaring exported type in which this was is nested.</description></item>
        /// </list>
        /// </returns>
        public Handle Implementation
        {
            get
            {
                return reader.ExportedTypeTable.GetImplementation(rowId);
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }
    }
}