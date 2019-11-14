// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335
{
    internal readonly struct DocumentTableReader
    {
        internal readonly int NumberOfRows;

        private readonly bool _isGuidHeapRefSizeSmall;
        private readonly bool _isBlobHeapRefSizeSmall;

        private const int NameOffset = 0;
        private readonly int _hashAlgorithmOffset;
        private readonly int _hashOffset;
        private readonly int _languageOffset;

        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal DocumentTableReader(
            int numberOfRows,
            int guidHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            NumberOfRows = numberOfRows;
            _isGuidHeapRefSizeSmall = guidHeapRefSize == 2;
            _isBlobHeapRefSizeSmall = blobHeapRefSize == 2;

            _hashAlgorithmOffset = NameOffset + blobHeapRefSize;
            _hashOffset = _hashAlgorithmOffset + guidHeapRefSize;
            _languageOffset = _hashOffset + blobHeapRefSize;
            RowSize = _languageOffset + guidHeapRefSize;

            Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
        }

        internal DocumentNameBlobHandle GetName(DocumentHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return DocumentNameBlobHandle.FromOffset(Block.PeekHeapReference(rowOffset + NameOffset, _isBlobHeapRefSizeSmall));
        }

        internal GuidHandle GetHashAlgorithm(DocumentHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return GuidHandle.FromIndex(Block.PeekHeapReference(rowOffset + _hashAlgorithmOffset, _isGuidHeapRefSizeSmall));
        }

        internal BlobHandle GetHash(DocumentHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return BlobHandle.FromOffset(Block.PeekHeapReference(rowOffset + _hashOffset, _isBlobHeapRefSizeSmall));
        }

        internal GuidHandle GetLanguage(DocumentHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return GuidHandle.FromIndex(Block.PeekHeapReference(rowOffset + _languageOffset, _isGuidHeapRefSizeSmall));
        }
    }

    internal readonly struct MethodDebugInformationTableReader
    {
        internal readonly int NumberOfRows;

        private readonly bool _isDocumentRefSmall;
        private readonly bool _isBlobHeapRefSizeSmall;

        private const int DocumentOffset = 0;
        private readonly int _sequencePointsOffset;

        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal MethodDebugInformationTableReader(
            int numberOfRows,
            int documentRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            NumberOfRows = numberOfRows;
            _isDocumentRefSmall = documentRefSize == 2;
            _isBlobHeapRefSizeSmall = blobHeapRefSize == 2;

            _sequencePointsOffset = DocumentOffset + documentRefSize;
            RowSize = _sequencePointsOffset + blobHeapRefSize;

            Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
        }

        internal DocumentHandle GetDocument(MethodDebugInformationHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return DocumentHandle.FromRowId(Block.PeekReference(rowOffset + DocumentOffset, _isDocumentRefSmall));
        }

        internal BlobHandle GetSequencePoints(MethodDebugInformationHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return BlobHandle.FromOffset(Block.PeekHeapReference(rowOffset + _sequencePointsOffset, _isBlobHeapRefSizeSmall));
        }
    }

    internal readonly struct LocalScopeTableReader
    {
        internal readonly int NumberOfRows;

        private readonly bool _isMethodRefSmall;
        private readonly bool _isImportScopeRefSmall;
        private readonly bool _isLocalConstantRefSmall;
        private readonly bool _isLocalVariableRefSmall;

        private const int MethodOffset = 0;
        private readonly int _importScopeOffset;
        private readonly int _variableListOffset;
        private readonly int _constantListOffset;
        private readonly int _startOffsetOffset;
        private readonly int _lengthOffset;

        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal LocalScopeTableReader(
            int numberOfRows,
            bool declaredSorted,
            int methodRefSize,
            int importScopeRefSize,
            int localVariableRefSize,
            int localConstantRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            NumberOfRows = numberOfRows;
            _isMethodRefSmall = methodRefSize == 2;
            _isImportScopeRefSmall = importScopeRefSize == 2;
            _isLocalVariableRefSmall = localVariableRefSize == 2;
            _isLocalConstantRefSmall = localConstantRefSize == 2;

            _importScopeOffset = MethodOffset + methodRefSize;
            _variableListOffset = _importScopeOffset + importScopeRefSize;
            _constantListOffset = _variableListOffset + localVariableRefSize;
            _startOffsetOffset = _constantListOffset + localConstantRefSize;
            _lengthOffset = _startOffsetOffset + sizeof(uint);
            RowSize = _lengthOffset + sizeof(uint);

            Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);

            if (numberOfRows > 0 && !declaredSorted)
            {
                Throw.TableNotSorted(TableIndex.LocalScope);
            }
        }

        internal MethodDefinitionHandle GetMethod(int rowId)
        {
            int rowOffset = (rowId - 1) * RowSize;
            return MethodDefinitionHandle.FromRowId(Block.PeekReference(rowOffset + MethodOffset, _isMethodRefSmall));
        }

        internal ImportScopeHandle GetImportScope(LocalScopeHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return ImportScopeHandle.FromRowId(Block.PeekReference(rowOffset + _importScopeOffset, _isImportScopeRefSmall));
        }

        internal int GetVariableStart(int rowId)
        {
            int rowOffset = (rowId - 1) * RowSize;
            return Block.PeekReference(rowOffset + _variableListOffset, _isLocalVariableRefSmall);
        }

        internal int GetConstantStart(int rowId)
        {
            int rowOffset = (rowId - 1) * RowSize;
            return Block.PeekReference(rowOffset + _constantListOffset, _isLocalConstantRefSmall);
        }

        internal int GetStartOffset(int rowId)
        {
            int rowOffset = (rowId - 1) * RowSize;
            return Block.PeekInt32(rowOffset + _startOffsetOffset);
        }

        internal int GetLength(int rowId)
        {
            int rowOffset = (rowId - 1) * RowSize;
            return Block.PeekInt32(rowOffset + _lengthOffset);
        }

        internal int GetEndOffset(int rowId)
        {
            int rowOffset = (rowId - 1) * RowSize;

            long result =
                Block.PeekUInt32(rowOffset + _startOffsetOffset) +
                Block.PeekUInt32(rowOffset + _lengthOffset);

            if (unchecked((int)result) != result)
            {
                Throw.ValueOverflow();
            }

            return (int)result;
        }

        internal void GetLocalScopeRange(int methodDefRid, out int firstScopeRowId, out int lastScopeRowId)
        {
            int startRowNumber, endRowNumber;
            Block.BinarySearchReferenceRange(
                NumberOfRows,
                RowSize,
                MethodOffset,
                (uint)methodDefRid,
                _isMethodRefSmall,
                out startRowNumber,
                out endRowNumber
            );

            if (startRowNumber == -1)
            {
                firstScopeRowId = 1;
                lastScopeRowId = 0;
            }
            else
            {
                firstScopeRowId = startRowNumber + 1;
                lastScopeRowId = endRowNumber + 1;
            }
        }
    }

    internal readonly struct LocalVariableTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _isStringHeapRefSizeSmall;
        private readonly int _attributesOffset;
        private readonly int _indexOffset;
        private readonly int _nameOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal LocalVariableTableReader(
            int numberOfRows,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            NumberOfRows = numberOfRows;
            _isStringHeapRefSizeSmall = stringHeapRefSize == 2;

            _attributesOffset = 0;
            _indexOffset = _attributesOffset + sizeof(ushort);
            _nameOffset = _indexOffset + sizeof(ushort);
            RowSize = _nameOffset + stringHeapRefSize;

            Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
        }

        internal LocalVariableAttributes GetAttributes(LocalVariableHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return (LocalVariableAttributes)Block.PeekUInt16(rowOffset + _attributesOffset);
        }

        internal ushort GetIndex(LocalVariableHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return Block.PeekUInt16(rowOffset + _indexOffset);
        }

        internal StringHandle GetName(LocalVariableHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return StringHandle.FromOffset(Block.PeekHeapReference(rowOffset + _nameOffset, _isStringHeapRefSizeSmall));
        }
    }

    internal readonly struct LocalConstantTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _isStringHeapRefSizeSmall;
        private readonly bool _isBlobHeapRefSizeSmall;

        private const int NameOffset = 0;
        private readonly int _signatureOffset;

        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal LocalConstantTableReader(
            int numberOfRows,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            NumberOfRows = numberOfRows;
            _isStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _isBlobHeapRefSizeSmall = blobHeapRefSize == 2;

            _signatureOffset = NameOffset + stringHeapRefSize;
            RowSize = _signatureOffset + blobHeapRefSize;

            Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
        }

        internal StringHandle GetName(LocalConstantHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return StringHandle.FromOffset(Block.PeekHeapReference(rowOffset + NameOffset, _isStringHeapRefSizeSmall));
        }

        internal BlobHandle GetSignature(LocalConstantHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return BlobHandle.FromOffset(Block.PeekHeapReference(rowOffset + _signatureOffset, _isBlobHeapRefSizeSmall));
        }
    }

    internal readonly struct StateMachineMethodTableReader
    {
        internal readonly int NumberOfRows;

        private readonly bool _isMethodRefSizeSmall;

        private const int MoveNextMethodOffset = 0;
        private readonly int _kickoffMethodOffset;

        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal StateMachineMethodTableReader(
            int numberOfRows,
            bool declaredSorted,
            int methodRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            NumberOfRows = numberOfRows;
            _isMethodRefSizeSmall = methodRefSize == 2;

            _kickoffMethodOffset = methodRefSize;
            RowSize = _kickoffMethodOffset + methodRefSize;

            Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);

            if (numberOfRows > 0 && !declaredSorted)
            {
                Throw.TableNotSorted(TableIndex.StateMachineMethod);
            }
        }

        internal MethodDefinitionHandle FindKickoffMethod(int moveNextMethodRowId)
        {
            int foundRowNumber =
              this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                MoveNextMethodOffset,
                (uint)moveNextMethodRowId,
                _isMethodRefSizeSmall);

            if (foundRowNumber < 0)
            {
                return default(MethodDefinitionHandle);
            }

            return GetKickoffMethod(foundRowNumber + 1);
        }

        private MethodDefinitionHandle GetKickoffMethod(int rowId)
        {
            int rowOffset = (rowId - 1) * RowSize;
            return MethodDefinitionHandle.FromRowId(Block.PeekReference(rowOffset + _kickoffMethodOffset, _isMethodRefSizeSmall));
        }
    }

    internal readonly struct ImportScopeTableReader
    {
        internal readonly int NumberOfRows;

        private readonly bool _isImportScopeRefSizeSmall;
        private readonly bool _isBlobHeapRefSizeSmall;

        private const int ParentOffset = 0;
        private readonly int _importsOffset;

        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ImportScopeTableReader(
            int numberOfRows,
            int importScopeRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            NumberOfRows = numberOfRows;
            _isImportScopeRefSizeSmall = importScopeRefSize == 2;
            _isBlobHeapRefSizeSmall = blobHeapRefSize == 2;

            _importsOffset = ParentOffset + importScopeRefSize;
            RowSize = _importsOffset + blobHeapRefSize;

            Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
        }

        internal ImportScopeHandle GetParent(ImportScopeHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return ImportScopeHandle.FromRowId(Block.PeekReference(rowOffset + ParentOffset, _isImportScopeRefSizeSmall));
        }

        internal BlobHandle GetImports(ImportScopeHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return BlobHandle.FromOffset(Block.PeekHeapReference(rowOffset + _importsOffset, _isBlobHeapRefSizeSmall));
        }
    }

    internal readonly struct CustomDebugInformationTableReader
    {
        internal readonly int NumberOfRows;

        private readonly bool _isHasCustomDebugInformationRefSizeSmall;
        private readonly bool _isGuidHeapRefSizeSmall;
        private readonly bool _isBlobHeapRefSizeSmall;

        private const int ParentOffset = 0;
        private readonly int _kindOffset;
        private readonly int _valueOffset;

        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal CustomDebugInformationTableReader(
            int numberOfRows,
            bool declaredSorted,
            int hasCustomDebugInformationRefSize,
            int guidHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            NumberOfRows = numberOfRows;
            _isHasCustomDebugInformationRefSizeSmall = hasCustomDebugInformationRefSize == 2;
            _isGuidHeapRefSizeSmall = guidHeapRefSize == 2;
            _isBlobHeapRefSizeSmall = blobHeapRefSize == 2;

            _kindOffset = ParentOffset + hasCustomDebugInformationRefSize;
            _valueOffset = _kindOffset + guidHeapRefSize;
            RowSize = _valueOffset + blobHeapRefSize;

            Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);

            if (numberOfRows > 0 && !declaredSorted)
            {
                Throw.TableNotSorted(TableIndex.CustomDebugInformation);
            }
        }

        internal EntityHandle GetParent(CustomDebugInformationHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return HasCustomDebugInformationTag.ConvertToHandle(Block.PeekTaggedReference(rowOffset + ParentOffset, _isHasCustomDebugInformationRefSizeSmall));
        }

        internal GuidHandle GetKind(CustomDebugInformationHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return GuidHandle.FromIndex(Block.PeekHeapReference(rowOffset + _kindOffset, _isGuidHeapRefSizeSmall));
        }

        internal BlobHandle GetValue(CustomDebugInformationHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * RowSize;
            return BlobHandle.FromOffset(Block.PeekHeapReference(rowOffset + _valueOffset, _isBlobHeapRefSizeSmall));
        }

        internal void GetRange(EntityHandle parentHandle, out int firstImplRowId, out int lastImplRowId)
        {
            int startRowNumber, endRowNumber;

            Block.BinarySearchReferenceRange(
                NumberOfRows,
                RowSize,
                ParentOffset,
                HasCustomDebugInformationTag.ConvertToTag(parentHandle),
                _isHasCustomDebugInformationRefSizeSmall,
                out startRowNumber,
                out endRowNumber
            );

            if (startRowNumber == -1)
            {
                firstImplRowId = 1;
                lastImplRowId = 0;
            }
            else
            {
                firstImplRowId = startRowNumber + 1;
                lastImplRowId = endRowNumber + 1;
            }
        }
    }
}
