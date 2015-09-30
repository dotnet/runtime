// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct MethodBody
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use RowId.
        private readonly int _rowId;

        internal MethodBody(MetadataReader reader, MethodBodyHandle handle)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!handle.IsNil);

            _reader = reader;
            _rowId = handle.RowId;
        }

        private MethodBodyHandle Handle
        {
            get { return MethodBodyHandle.FromRowId(_rowId); }
        }

        /// <summary>
        /// Returns a blob encoding sequence points.
        /// </summary>
        public BlobHandle SequencePoints
        {
            get
            {
                return _reader.MethodBodyTable.GetSequencePoints(Handle);
            }
        }

        /// <summary>
        /// Returns local signature handle.
        /// </summary>
        public StandaloneSignatureHandle LocalSignature
        {
            get
            {
                if (SequencePoints.IsNil)
                {
                    return default(StandaloneSignatureHandle);
                }

                return StandaloneSignatureHandle.FromRowId(_reader.GetBlobReader(SequencePoints).ReadCompressedInteger());
            }
        }

        /// <summary>
        /// If the method is a MoveNext method of a state machine returns the kickoff method of the state machine, otherwise returns nil handle.
        /// </summary>
        public MethodDefinitionHandle GetStateMachineKickoffMethod()
        {
            return _reader.StateMachineMethodTable.FindKickoffMethod(_rowId);
        }
    }
}
