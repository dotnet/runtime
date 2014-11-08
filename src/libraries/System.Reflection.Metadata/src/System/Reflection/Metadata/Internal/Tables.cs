// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335
{
    internal struct ModuleTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly bool IsGUIDHeapRefSizeSmall;
        private readonly int GenerationOffset;
        private readonly int NameOffset;
        private readonly int MVIdOffset;
        private readonly int EnCIdOffset;
        private readonly int EnCBaseIdOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ModuleTableReader(
            uint numberOfRows,
            int stringHeapRefSize,
            int guidHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.IsGUIDHeapRefSizeSmall = guidHeapRefSize == 2;
            this.GenerationOffset = 0;
            this.NameOffset = this.GenerationOffset + sizeof(UInt16);
            this.MVIdOffset = this.NameOffset + stringHeapRefSize;
            this.EnCIdOffset = this.MVIdOffset + guidHeapRefSize;
            this.EnCBaseIdOffset = this.EnCIdOffset + guidHeapRefSize;
            this.RowSize = this.EnCBaseIdOffset + guidHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * (int)numberOfRows);
        }

        internal ushort GetGeneration()
        {
            Debug.Assert(NumberOfRows > 0);
            return this.Block.PeekUInt16(this.GenerationOffset);
        }

        internal StringHandle GetName()
        {
            Debug.Assert(NumberOfRows > 0);
            return StringHandle.FromIndex(this.Block.PeekReference(this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal GuidHandle GetMvid()
        {
            Debug.Assert(NumberOfRows > 0);
            return GuidHandle.FromIndex(this.Block.PeekReference(this.MVIdOffset, this.IsGUIDHeapRefSizeSmall));
        }

        internal GuidHandle GetEncId()
        {
            Debug.Assert(NumberOfRows > 0);
            return GuidHandle.FromIndex(this.Block.PeekReference(this.EnCIdOffset, this.IsGUIDHeapRefSizeSmall));
        }

        internal GuidHandle GetEncBaseId()
        {
            Debug.Assert(NumberOfRows > 0);
            return GuidHandle.FromIndex(this.Block.PeekReference(this.EnCBaseIdOffset, this.IsGUIDHeapRefSizeSmall));
        }
    }

    internal struct TypeRefTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsResolutionScopeRefSizeSmall;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly int ResolutionScopeOffset;
        private readonly int NameOffset;
        private readonly int NamespaceOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal TypeRefTableReader(
            uint numberOfRows,
            int resolutionScopeRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsResolutionScopeRefSizeSmall = resolutionScopeRefSize == 2;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.ResolutionScopeOffset = 0;
            this.NameOffset = this.ResolutionScopeOffset + resolutionScopeRefSize;
            this.NamespaceOffset = this.NameOffset + stringHeapRefSize;
            this.RowSize = this.NamespaceOffset + stringHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal Handle GetResolutionScope(TypeReferenceHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return ResolutionScopeTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.ResolutionScopeOffset, this.IsResolutionScopeRefSizeSmall));
        }

        internal StringHandle GetName(TypeReferenceHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetNamespace(TypeReferenceHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NamespaceOffset, this.IsStringHeapRefSizeSmall));
        }
    }

    internal struct TypeDefTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsFieldRefSizeSmall;
        private readonly bool IsMethodRefSizeSmall;
        private readonly bool IsTypeDefOrRefRefSizeSmall;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly int FlagsOffset;
        private readonly int NameOffset;
        private readonly int NamespaceOffset;
        private readonly int ExtendsOffset;
        private readonly int FieldListOffset;
        private readonly int MethodListOffset;
        internal readonly int RowSize;
        internal MemoryBlock Block;

        internal TypeDefTableReader(
            uint numberOfRows,
            int fieldRefSize,
            int methodRefSize,
            int typeDefOrRefRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.IsFieldRefSizeSmall = fieldRefSize == 2;
            this.IsMethodRefSizeSmall = methodRefSize == 2;
            this.IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.FlagsOffset = 0;
            this.NameOffset = this.FlagsOffset + sizeof(UInt32);
            this.NamespaceOffset = this.NameOffset + stringHeapRefSize;
            this.ExtendsOffset = this.NamespaceOffset + stringHeapRefSize;
            this.FieldListOffset = this.ExtendsOffset + typeDefOrRefRefSize;
            this.MethodListOffset = this.FieldListOffset + fieldRefSize;
            this.RowSize = this.MethodListOffset + methodRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal TypeAttributes GetFlags(TypeDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return (TypeAttributes)this.Block.PeekUInt32(rowOffset + this.FlagsOffset);
        }

        internal NamespaceDefinitionHandle GetNamespace(TypeDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return NamespaceDefinitionHandle.FromIndexOfFullName(this.Block.PeekReference(rowOffset + this.NamespaceOffset, this.IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetNamespaceString(TypeDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NamespaceOffset, this.IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetName(TypeDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal Handle GetExtends(TypeDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return TypeDefOrRefTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.ExtendsOffset, this.IsTypeDefOrRefRefSizeSmall));
        }

        internal uint GetFieldStart(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return this.Block.PeekReference(rowOffset + this.FieldListOffset, this.IsFieldRefSizeSmall);
        }

        internal uint GetMethodStart(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return this.Block.PeekReference(rowOffset + this.MethodListOffset, this.IsMethodRefSizeSmall);
        }

        internal TypeDefinitionHandle FindTypeContainingMethod(uint methodDefOrPtrRowId, int numberOfMethods)
        {
            uint numOfRows = this.NumberOfRows;
            int slot = this.Block.BinarySearchForSlot(numOfRows, this.RowSize, this.MethodListOffset, methodDefOrPtrRowId, this.IsMethodRefSizeSmall);
            uint row = (uint)(slot + 1);
            if (row == 0) return default(TypeDefinitionHandle);

            if (row > numOfRows)
            {
                if (methodDefOrPtrRowId <= numberOfMethods) return TypeDefinitionHandle.FromRowId(numOfRows);

                return default(TypeDefinitionHandle);
            }

            uint value = this.GetMethodStart(row);
            if (value == methodDefOrPtrRowId)
            {
                while (row < numOfRows)
                {
                    uint newRow = row + 1;
                    value = this.GetMethodStart(newRow);
                    if (value == methodDefOrPtrRowId)
                        row = newRow;
                    else
                        break;
                }
            }

            return TypeDefinitionHandle.FromRowId(row);
        }

        internal TypeDefinitionHandle FindTypeContainingField(uint fieldDefOrPtrRowId, int numberOfFields)
        {
            uint numOfRows = this.NumberOfRows;
            int slot = this.Block.BinarySearchForSlot(numOfRows, this.RowSize, this.FieldListOffset, fieldDefOrPtrRowId, this.IsFieldRefSizeSmall);
            uint row = (uint)(slot + 1);
            if (row == 0) return default(TypeDefinitionHandle);

            if (row > numOfRows)
            {
                if (fieldDefOrPtrRowId <= numberOfFields) return TypeDefinitionHandle.FromRowId(numOfRows);

                return default(TypeDefinitionHandle);
            }

            uint value = this.GetFieldStart(row);
            if (value == fieldDefOrPtrRowId)
            {
                while (row < numOfRows)
                {
                    uint newRow = row + 1;
                    value = this.GetFieldStart(newRow);
                    if (value == fieldDefOrPtrRowId)
                        row = newRow;
                    else
                        break;
                }
            }

            return TypeDefinitionHandle.FromRowId(row);
        }
    }

    internal struct FieldPtrTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsFieldTableRowRefSizeSmall;
        private readonly int FieldOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FieldPtrTableReader(
            uint numberOfRows,
            int fieldTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsFieldTableRowRefSizeSmall = fieldTableRowRefSize == 2;
            this.FieldOffset = 0;
            this.RowSize = this.FieldOffset + fieldTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal FieldDefinitionHandle GetFieldFor(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return FieldDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.FieldOffset, this.IsFieldTableRowRefSizeSmall));
        }

        internal uint GetRowIdForFieldDefRow(uint fieldDefRowId)
        {
            return (uint)(this.Block.LinearSearchReference(this.RowSize, this.FieldOffset, fieldDefRowId, this.IsFieldTableRowRefSizeSmall) + 1);
        }
    }

    internal struct FieldTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int FlagsOffset;
        private readonly int NameOffset;
        private readonly int SignatureOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FieldTableReader(
            uint numberOfRows,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.FlagsOffset = 0;
            this.NameOffset = this.FlagsOffset + sizeof(UInt16);
            this.SignatureOffset = this.NameOffset + stringHeapRefSize;
            this.RowSize = this.SignatureOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal StringHandle GetName(FieldDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal FieldAttributes GetFlags(FieldDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return (FieldAttributes)this.Block.PeekUInt16(rowOffset + this.FlagsOffset);
        }

        internal BlobHandle GetSignature(FieldDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.SignatureOffset, this.IsBlobHeapRefSizeSmall));
        }
    }

    internal struct MethodPtrTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsMethodTableRowRefSizeSmall;
        private readonly int MethodOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal MethodPtrTableReader(
            uint numberOfRows,
            int methodTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsMethodTableRowRefSizeSmall = methodTableRowRefSize == 2;
            this.MethodOffset = 0;
            this.RowSize = this.MethodOffset + methodTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        // returns a rid
        internal MethodDefinitionHandle GetMethodFor(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return MethodDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.MethodOffset, this.IsMethodTableRowRefSizeSmall)); ;
        }

        internal uint GetRowIdForMethodDefRow(uint methodDefRowId)
        {
            return (uint)(this.Block.LinearSearchReference(this.RowSize, this.MethodOffset, methodDefRowId, this.IsMethodTableRowRefSizeSmall) + 1);
        }
    }

    internal struct MethodTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsParamRefSizeSmall;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int RVAOffset;
        private readonly int ImplFlagsOffset;
        private readonly int FlagsOffset;
        private readonly int NameOffset;
        private readonly int SignatureOffset;
        private readonly int ParamListOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal MethodTableReader(
            uint numberOfRows,
            int paramRefSize,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsParamRefSizeSmall = paramRefSize == 2;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.RVAOffset = 0;
            this.ImplFlagsOffset = this.RVAOffset + sizeof(UInt32);
            this.FlagsOffset = this.ImplFlagsOffset + sizeof(UInt16);
            this.NameOffset = this.FlagsOffset + sizeof(UInt16);
            this.SignatureOffset = this.NameOffset + stringHeapRefSize;
            this.ParamListOffset = this.SignatureOffset + blobHeapRefSize;
            this.RowSize = this.ParamListOffset + paramRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal uint GetParamStart(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return this.Block.PeekReference(rowOffset + this.ParamListOffset, this.IsParamRefSizeSmall);
        }

        internal BlobHandle GetSignature(MethodDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.SignatureOffset, this.IsBlobHeapRefSizeSmall));
        }

        internal int GetRva(MethodDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return this.Block.PeekInt32(rowOffset + this.RVAOffset);
        }

        internal StringHandle GetName(MethodDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal MethodAttributes GetFlags(MethodDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return (MethodAttributes)this.Block.PeekUInt16(rowOffset + this.FlagsOffset);
        }

        internal MethodImplAttributes GetImplFlags(MethodDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return (MethodImplAttributes)this.Block.PeekUInt16(rowOffset + this.ImplFlagsOffset);
        }

        internal int GetNextRVA(
          int rva
        )
        {
            int nextRVA = int.MaxValue;
            int endOffset = (int)this.NumberOfRows * this.RowSize;
            for (int iterOffset = this.RVAOffset; iterOffset < endOffset; iterOffset += this.RowSize)
            {
                int currentRVA = this.Block.PeekInt32(iterOffset);
                if (currentRVA > rva && currentRVA < nextRVA)
                {
                    nextRVA = currentRVA;
                }
            }

            return nextRVA == int.MaxValue ? -1 : nextRVA;
        }
    }

    internal struct ParamPtrTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsParamTableRowRefSizeSmall;
        private readonly int ParamOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ParamPtrTableReader(
            uint numberOfRows,
            int paramTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsParamTableRowRefSizeSmall = paramTableRowRefSize == 2;
            this.ParamOffset = 0;
            this.RowSize = this.ParamOffset + paramTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal ParameterHandle GetParamFor(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return ParameterHandle.FromRowId(this.Block.PeekReference(rowOffset + this.ParamOffset, this.IsParamTableRowRefSizeSmall));
        }
    }

    internal struct ParamTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly int FlagsOffset;
        private readonly int SequenceOffset;
        private readonly int NameOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ParamTableReader(
            uint numberOfRows,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.FlagsOffset = 0;
            this.SequenceOffset = this.FlagsOffset + sizeof(UInt16);
            this.NameOffset = this.SequenceOffset + sizeof(UInt16);
            this.RowSize = this.NameOffset + stringHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal ParameterAttributes GetFlags(ParameterHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return (ParameterAttributes)this.Block.PeekUInt16(rowOffset + this.FlagsOffset);
        }

        internal ushort GetSequence(ParameterHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return this.Block.PeekUInt16(rowOffset + this.SequenceOffset);
        }

        internal StringHandle GetName(ParameterHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }
    }

    internal struct InterfaceImplTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsTypeDefTableRowRefSizeSmall;
        private readonly bool IsTypeDefOrRefRefSizeSmall;
        private readonly int ClassOffset;
        private readonly int InterfaceOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal InterfaceImplTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int typeDefTableRowRefSize,
            int typeDefOrRefRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            this.IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
            this.ClassOffset = 0;
            this.InterfaceOffset = this.ClassOffset + typeDefTableRowRefSize;
            this.RowSize = this.InterfaceOffset + typeDefOrRefRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.InterfaceImpl);
            }
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.ClassOffset, this.IsTypeDefTableRowRefSizeSmall);
        }

        internal void GetInterfaceImplRange(
            TypeDefinitionHandle typeDef,
            out int firstImplRowId,
            out int lastImplRowId)
        {
            uint typeDefRid = typeDef.RowId;

            int startRowNumber, endRowNumber;
            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                this.ClassOffset,
                typeDefRid,
                this.IsTypeDefTableRowRefSizeSmall,
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

        internal Handle GetInterface(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return TypeDefOrRefTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.InterfaceOffset, this.IsTypeDefOrRefRefSizeSmall));
        }
    }

    internal struct MemberRefTableReader
    {
        internal uint NumberOfRows;
        private readonly bool IsMemberRefParentRefSizeSmall;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int ClassOffset;
        private readonly int NameOffset;
        private readonly int SignatureOffset;
        internal readonly int RowSize;
        internal MemoryBlock Block;

        internal MemberRefTableReader(
            uint numberOfRows,
            int memberRefParentRefSize,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsMemberRefParentRefSizeSmall = memberRefParentRefSize == 2;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.ClassOffset = 0;
            this.NameOffset = this.ClassOffset + memberRefParentRefSize;
            this.SignatureOffset = this.NameOffset + stringHeapRefSize;
            this.RowSize = this.SignatureOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal BlobHandle GetSignature(MemberReferenceHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.SignatureOffset, this.IsBlobHeapRefSizeSmall));
        }

        internal StringHandle GetName(MemberReferenceHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal Handle GetClass(MemberReferenceHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return MemberRefParentTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.ClassOffset, this.IsMemberRefParentRefSizeSmall));
        }
    }

    internal struct ConstantTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsHasConstantRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int TypeOffset;
        private readonly int ParentOffset;
        private readonly int ValueOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ConstantTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int hasConstantRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsHasConstantRefSizeSmall = hasConstantRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.TypeOffset = 0;
            this.ParentOffset = this.TypeOffset + sizeof(Byte) + 1; // Alignment here (+1)...
            this.ValueOffset = this.ParentOffset + hasConstantRefSize;
            this.RowSize = this.ValueOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.Constant);
            }
        }

        internal ConstantTypeCode GetType(ConstantHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return (ConstantTypeCode)this.Block.PeekByte(rowOffset + this.TypeOffset);
        }

        internal BlobHandle GetValue(ConstantHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.ValueOffset, this.IsBlobHeapRefSizeSmall));
        }

        internal Handle GetParent(ConstantHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return HasConstantTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.ParentOffset, this.IsHasConstantRefSizeSmall));
        }

        internal ConstantHandle FindConstant(
          Handle parentToken
        )
        {
            int foundRowNumber =
              this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                this.ParentOffset,
                HasConstantTag.ConvertToTag(parentToken),
                this.IsHasConstantRefSizeSmall
            );
            return ConstantHandle.FromRowId((uint)(foundRowNumber + 1));
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.ParentOffset, this.IsHasConstantRefSizeSmall);
        }
    }

    internal struct CustomAttributeTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsHasCustomAttributeRefSizeSmall;
        private readonly bool IsCustomAttriubuteTypeRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int ParentOffset;
        private readonly int TypeOffset;
        private readonly int ValueOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        // row ids in the CustomAttribute table sorted by parents
        internal readonly uint[] PtrTable;

        internal CustomAttributeTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int hasCustomAttributeRefSize,
            int customAttributeTypeRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsHasCustomAttributeRefSizeSmall = hasCustomAttributeRefSize == 2;
            this.IsCustomAttriubuteTypeRefSizeSmall = customAttributeTypeRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.ParentOffset = 0;
            this.TypeOffset = this.ParentOffset + hasCustomAttributeRefSize;
            this.ValueOffset = this.TypeOffset + customAttributeTypeRefSize;
            this.RowSize = this.ValueOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
            this.PtrTable = null;

            if (!declaredSorted && !CheckSorted())
            {
                this.PtrTable = this.Block.BuildPtrTable(
                    (int)numberOfRows,
                    this.RowSize,
                    this.ParentOffset,
                    this.IsHasCustomAttributeRefSizeSmall);
            }
        }

        internal Handle GetParent(CustomAttributeHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return HasCustomAttributeTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.ParentOffset, this.IsHasCustomAttributeRefSizeSmall));
        }

        internal Handle GetConstructor(CustomAttributeHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return CustomAttributeTypeTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.TypeOffset, this.IsCustomAttriubuteTypeRefSizeSmall));
        }

        internal BlobHandle GetValue(CustomAttributeHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.ValueOffset, this.IsBlobHeapRefSizeSmall));
        }

        private uint GetParentTag(int index)
        {
            return this.Block.PeekReference(index * this.RowSize + this.ParentOffset, this.IsHasCustomAttributeRefSizeSmall);
        }

        internal void GetAttributeRange(Handle parentHandle, out int firstImplRowId, out int lastImplRowId)
        {
            int startRowNumber, endRowNumber;

            if (this.PtrTable != null)
            {
                this.Block.BinarySearchReferenceRange(
                    this.PtrTable,
                    this.RowSize,
                    this.ParentOffset,
                    HasCustomAttributeTag.ConvertToTag(parentHandle),
                    this.IsHasCustomAttributeRefSizeSmall,
                    out startRowNumber,
                    out endRowNumber
                );
            }
            else
            {
                this.Block.BinarySearchReferenceRange(
                    this.NumberOfRows,
                    this.RowSize,
                    this.ParentOffset,
                    HasCustomAttributeTag.ConvertToTag(parentHandle),
                    this.IsHasCustomAttributeRefSizeSmall,
                    out startRowNumber,
                    out endRowNumber
                );
            }

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

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.ParentOffset, this.IsHasCustomAttributeRefSizeSmall);
        }
    }

    internal struct FieldMarshalTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsHasFieldMarshalRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int ParentOffset;
        private readonly int NativeTypeOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FieldMarshalTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int hasFieldMarshalRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsHasFieldMarshalRefSizeSmall = hasFieldMarshalRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.ParentOffset = 0;
            this.NativeTypeOffset = this.ParentOffset + hasFieldMarshalRefSize;
            this.RowSize = this.NativeTypeOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.FieldMarshal);
            }
        }

        internal Handle GetParent(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return HasFieldMarshalTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.ParentOffset, this.IsHasFieldMarshalRefSizeSmall));
        }

        internal BlobHandle GetNativeType(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NativeTypeOffset, this.IsBlobHeapRefSizeSmall));
        }

        internal uint FindFieldMarshalRowId(Handle handle)
        {
            int foundRowNumber =
              this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                this.ParentOffset,
                HasFieldMarshalTag.ConvertToTag(handle),
                this.IsHasFieldMarshalRefSizeSmall
            );
            return (uint)(foundRowNumber + 1);
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.ParentOffset, this.IsHasFieldMarshalRefSizeSmall);
        }
    }

    internal struct DeclSecurityTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsHasDeclSecurityRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int ActionOffset;
        private readonly int ParentOffset;
        private readonly int PermissionSetOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal DeclSecurityTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int hasDeclSecurityRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsHasDeclSecurityRefSizeSmall = hasDeclSecurityRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.ActionOffset = 0;
            this.ParentOffset = this.ActionOffset + sizeof(UInt16);
            this.PermissionSetOffset = this.ParentOffset + hasDeclSecurityRefSize;
            this.RowSize = this.PermissionSetOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.DeclSecurity);
            }
        }

        internal DeclarativeSecurityAction GetAction(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return (DeclarativeSecurityAction)this.Block.PeekUInt16(rowOffset + this.ActionOffset);
        }

        internal Handle GetParent(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return HasDeclSecurityTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.ParentOffset, this.IsHasDeclSecurityRefSizeSmall));
        }

        internal BlobHandle GetPermissionSet(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.PermissionSetOffset, this.IsBlobHeapRefSizeSmall));
        }

        internal void GetAttributeRange(Handle parentToken, out int firstImplRowId, out int lastImplRowId)
        {
            int startRowNumber, endRowNumber;

            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                this.ParentOffset,
                HasDeclSecurityTag.ConvertToTag(parentToken),
                this.IsHasDeclSecurityRefSizeSmall,
                out startRowNumber,
                out endRowNumber);

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

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.ParentOffset, this.IsHasDeclSecurityRefSizeSmall);
        }
    }

    internal struct ClassLayoutTableReader
    {
        internal uint NumberOfRows;
        private readonly bool IsTypeDefTableRowRefSizeSmall;
        private readonly int PackagingSizeOffset;
        private readonly int ClassSizeOffset;
        private readonly int ParentOffset;
        internal readonly int RowSize;
        internal MemoryBlock Block;

        internal ClassLayoutTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int typeDefTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            this.PackagingSizeOffset = 0;
            this.ClassSizeOffset = this.PackagingSizeOffset + sizeof(UInt16);
            this.ParentOffset = this.ClassSizeOffset + sizeof(UInt32);
            this.RowSize = this.ParentOffset + typeDefTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.ClassLayout);
            }
        }

        internal TypeDefinitionHandle GetParent(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.ParentOffset, this.IsTypeDefTableRowRefSizeSmall));
        }

        internal ushort GetPackingSize(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return this.Block.PeekUInt16(rowOffset + this.PackagingSizeOffset);
        }

        internal uint GetClassSize(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + this.ClassSizeOffset);
        }

        // Returns RowId (0 means we there is no record in this table corresponding to the specified type).
        internal uint FindRow(TypeDefinitionHandle typeDef)
        {
            return (uint)(1 + this.Block.BinarySearchReference(this.NumberOfRows, this.RowSize, this.ParentOffset, typeDef.RowId, this.IsTypeDefTableRowRefSizeSmall));
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.ParentOffset, this.IsTypeDefTableRowRefSizeSmall);
        }
    }

    internal struct FieldLayoutTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsFieldTableRowRefSizeSmall;
        private readonly int OffsetOffset;
        private readonly int FieldOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FieldLayoutTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int fieldTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.IsFieldTableRowRefSizeSmall = fieldTableRowRefSize == 2;
            this.OffsetOffset = 0;
            this.FieldOffset = this.OffsetOffset + sizeof(UInt32);
            this.RowSize = this.FieldOffset + fieldTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.FieldLayout);
            }
        }

        /// <summary>
        /// Returns field offset for given field RowId, or -1 if not available. 
        /// </summary>
        internal uint FindFieldLayoutRowId(FieldDefinitionHandle handle)
        {
            int rowNumber =
              this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                this.FieldOffset,
                handle.RowId,
                this.IsFieldTableRowRefSizeSmall
            );

            return (uint)(rowNumber + 1);
        }

        internal uint GetOffset(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + this.OffsetOffset);
        }

        internal FieldDefinitionHandle GetField(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return FieldDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.FieldOffset, this.IsFieldTableRowRefSizeSmall));
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.FieldOffset, this.IsFieldTableRowRefSizeSmall);
        }
    }

    internal struct StandAloneSigTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int SignatureOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal StandAloneSigTableReader(
            uint numberOfRows,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.SignatureOffset = 0;
            this.RowSize = this.SignatureOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal BlobHandle GetSignature(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.SignatureOffset, this.IsBlobHeapRefSizeSmall));
        }
    }

    internal struct EventMapTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsTypeDefTableRowRefSizeSmall;
        private readonly bool IsEventRefSizeSmall;
        private readonly int ParentOffset;
        private readonly int EventListOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal EventMapTableReader(
            uint numberOfRows,
            int typeDefTableRowRefSize,
            int eventRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            this.IsEventRefSizeSmall = eventRefSize == 2;
            this.ParentOffset = 0;
            this.EventListOffset = this.ParentOffset + typeDefTableRowRefSize;
            this.RowSize = this.EventListOffset + eventRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal uint FindEventMapRowIdFor(TypeDefinitionHandle typeDef)
        {
            // We do a linear scan here because we dont have these tables sorted
            // TODO: We can scan the table to see if it is sorted and use binary search if so.
            // Also, the compilers should make sure it's sorted.
            int rowNumber =
              this.Block.LinearSearchReference(
                this.RowSize,
                this.ParentOffset,
                typeDef.RowId,
                this.IsTypeDefTableRowRefSizeSmall
            );
            return (uint)(rowNumber + 1);
        }

        internal TypeDefinitionHandle GetParentType(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.ParentOffset, this.IsTypeDefTableRowRefSizeSmall));
        }

        internal uint GetEventListStartFor(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return this.Block.PeekReference(rowOffset + this.EventListOffset, this.IsEventRefSizeSmall);
        }
    }

    internal struct EventPtrTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsEventTableRowRefSizeSmall;
        private readonly int EventOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal EventPtrTableReader(
            uint numberOfRows,
            int eventTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsEventTableRowRefSizeSmall = eventTableRowRefSize == 2;
            this.EventOffset = 0;
            this.RowSize = this.EventOffset + eventTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal EventDefinitionHandle GetEventFor(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return EventDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.EventOffset, this.IsEventTableRowRefSizeSmall));
        }
    }

    internal struct EventTableReader
    {
        internal uint NumberOfRows;
        private readonly bool IsTypeDefOrRefRefSizeSmall;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly int FlagsOffset;
        private readonly int NameOffset;
        private readonly int EventTypeOffset;
        internal readonly int RowSize;
        internal MemoryBlock Block;

        internal EventTableReader(
            uint numberOfRows,
            int typeDefOrRefRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.FlagsOffset = 0;
            this.NameOffset = this.FlagsOffset + sizeof(UInt16);
            this.EventTypeOffset = this.NameOffset + stringHeapRefSize;
            this.RowSize = this.EventTypeOffset + typeDefOrRefRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal EventAttributes GetFlags(EventDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return (EventAttributes)this.Block.PeekUInt16(rowOffset + this.FlagsOffset);
        }

        internal StringHandle GetName(EventDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal Handle GetEventType(EventDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return TypeDefOrRefTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.EventTypeOffset, this.IsTypeDefOrRefRefSizeSmall));
        }
    }

    internal struct PropertyMapTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsTypeDefTableRowRefSizeSmall;
        private readonly bool IsPropertyRefSizeSmall;
        private readonly int ParentOffset;
        private readonly int PropertyListOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal PropertyMapTableReader(
            uint numberOfRows,
            int typeDefTableRowRefSize,
            int propertyRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            this.IsPropertyRefSizeSmall = propertyRefSize == 2;
            this.ParentOffset = 0;
            this.PropertyListOffset = this.ParentOffset + typeDefTableRowRefSize;
            this.RowSize = this.PropertyListOffset + propertyRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal uint FindPropertyMapRowIdFor(TypeDefinitionHandle typeDef)
        {
            // We do a linear scan here because we dont have these tables sorted.
            // TODO: We can scan the table to see if it is sorted and use binary search if so.
            // Also, the compilers should make sure it's sorted.
            int rowNumber =
              this.Block.LinearSearchReference(
                this.RowSize,
                this.ParentOffset,
                typeDef.RowId,
                this.IsTypeDefTableRowRefSizeSmall
            );
            return (uint)(rowNumber + 1);
        }

        internal TypeDefinitionHandle GetParentType(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.ParentOffset, this.IsTypeDefTableRowRefSizeSmall));
        }

        internal uint GetPropertyListStartFor(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            uint propertyList = this.Block.PeekReference(rowOffset + this.PropertyListOffset, this.IsPropertyRefSizeSmall);
            return propertyList;
        }
    }

    internal struct PropertyPtrTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsPropertyTableRowRefSizeSmall;
        private readonly int PropertyOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal PropertyPtrTableReader(
            uint numberOfRows,
            int propertyTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsPropertyTableRowRefSizeSmall = propertyTableRowRefSize == 2;
            this.PropertyOffset = 0;
            this.RowSize = this.PropertyOffset + propertyTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal PropertyDefinitionHandle GetPropertyFor(
          int rowId
        )
        // ^ requires rowId <= this.NumberOfRows;
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return PropertyDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.PropertyOffset, this.IsPropertyTableRowRefSizeSmall));
        }
    }

    internal struct PropertyTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int FlagsOffset;
        private readonly int NameOffset;
        private readonly int SignatureOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal PropertyTableReader(
            uint numberOfRows,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.FlagsOffset = 0;
            this.NameOffset = this.FlagsOffset + sizeof(UInt16);
            this.SignatureOffset = this.NameOffset + stringHeapRefSize;
            this.RowSize = this.SignatureOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal PropertyAttributes GetFlags(PropertyDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return (PropertyAttributes)this.Block.PeekUInt16(rowOffset + this.FlagsOffset);
        }

        internal StringHandle GetName(PropertyDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal BlobHandle GetSignature(PropertyDefinitionHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.SignatureOffset, this.IsBlobHeapRefSizeSmall));
        }
    }

    internal struct MethodSemanticsTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsMethodTableRowRefSizeSmall;
        private readonly bool IsHasSemanticRefSizeSmall;
        private readonly int SemanticsFlagOffset;
        private readonly int MethodOffset;
        private readonly int AssociationOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal MethodSemanticsTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int methodTableRowRefSize,
            int hasSemanticRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsMethodTableRowRefSizeSmall = methodTableRowRefSize == 2;
            this.IsHasSemanticRefSizeSmall = hasSemanticRefSize == 2;
            this.SemanticsFlagOffset = 0;
            this.MethodOffset = this.SemanticsFlagOffset + sizeof(UInt16);
            this.AssociationOffset = this.MethodOffset + methodTableRowRefSize;
            this.RowSize = this.AssociationOffset + hasSemanticRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.MethodSemantics);
            }
        }

        internal MethodDefinitionHandle GetMethod(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return MethodDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.MethodOffset, this.IsMethodTableRowRefSizeSmall));
        }

        internal MethodSemanticsAttributes GetSemantics(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return (MethodSemanticsAttributes)this.Block.PeekUInt16(rowOffset + this.SemanticsFlagOffset);
        }

        internal Handle GetAssociation(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return HasSemanticsTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.AssociationOffset, this.IsHasSemanticRefSizeSmall));
        }

        // returns rowID
        internal uint FindSemanticMethodsForEvent(EventDefinitionHandle eventDef, out ushort methodCount)
        {
            methodCount = 0;
            uint searchCodedTag = HasSemanticsTag.ConvertEventHandleToTag(eventDef);
            return this.BinarySearchTag(searchCodedTag, ref methodCount);
        }

        internal uint FindSemanticMethodsForProperty(PropertyDefinitionHandle propertyDef, out ushort methodCount)
        {
            methodCount = 0;
            uint searchCodedTag = HasSemanticsTag.ConvertPropertyHandleToTag(propertyDef);
            return this.BinarySearchTag(searchCodedTag, ref methodCount);
        }

        private uint BinarySearchTag(uint searchCodedTag, ref ushort methodCount)
        {
            int startRowNumber, endRowNumber;
            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                this.AssociationOffset,
                searchCodedTag,
                this.IsHasSemanticRefSizeSmall,
                out startRowNumber,
                out endRowNumber
            );

            if (startRowNumber == -1)
            {
                methodCount = 0;
                return 0;
            }

            methodCount = (ushort)(endRowNumber - startRowNumber + 1);
            return (uint)(startRowNumber + 1);
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.AssociationOffset, this.IsHasSemanticRefSizeSmall);
        }
    }

    internal struct MethodImplTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsTypeDefTableRowRefSizeSmall;
        private readonly bool IsMethodDefOrRefRefSizeSmall;
        private readonly int ClassOffset;
        private readonly int MethodBodyOffset;
        private readonly int MethodDeclarationOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal MethodImplTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int typeDefTableRowRefSize,
            int methodDefOrRefRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            this.IsMethodDefOrRefRefSizeSmall = methodDefOrRefRefSize == 2;
            this.ClassOffset = 0;
            this.MethodBodyOffset = this.ClassOffset + typeDefTableRowRefSize;
            this.MethodDeclarationOffset = this.MethodBodyOffset + methodDefOrRefRefSize;
            this.RowSize = this.MethodDeclarationOffset + methodDefOrRefRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.MethodImpl);
            }
        }

        internal TypeDefinitionHandle GetClass(MethodImplementationHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.ClassOffset, this.IsTypeDefTableRowRefSizeSmall));
        }

        internal Handle GetMethodBody(MethodImplementationHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return MethodDefOrRefTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.MethodBodyOffset, this.IsMethodDefOrRefRefSizeSmall));
        }

        internal Handle GetMethodDeclaration(MethodImplementationHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return MethodDefOrRefTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.MethodDeclarationOffset, this.IsMethodDefOrRefRefSizeSmall));
        }

        internal void GetMethodImplRange(
            TypeDefinitionHandle typeDef,
            out int firstImplRowId,
            out int lastImplRowId)
        {
            uint typeDefRid = typeDef.RowId;

            int startRowNumber, endRowNumber;
            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                this.ClassOffset,
                typeDefRid,
                this.IsTypeDefTableRowRefSizeSmall,
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

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.ClassOffset, this.IsTypeDefTableRowRefSizeSmall);
        }
    }

    internal struct ModuleRefTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly int NameOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ModuleRefTableReader(
            uint numberOfRows,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.NameOffset = 0;
            this.RowSize = this.NameOffset + stringHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal StringHandle GetName(ModuleReferenceHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }
    }

    internal struct TypeSpecTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int SignatureOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal TypeSpecTableReader(
            uint numberOfRows,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.SignatureOffset = 0;
            this.RowSize = this.SignatureOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal BlobHandle GetSignature(TypeSpecificationHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.SignatureOffset, this.IsBlobHeapRefSizeSmall));
        }
    }

    internal struct ImplMapTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsModuleRefTableRowRefSizeSmall;
        private readonly bool IsMemberForwardRowRefSizeSmall;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly int FlagsOffset;
        private readonly int MemberForwardedOffset;
        private readonly int ImportNameOffset;
        private readonly int ImportScopeOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ImplMapTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int moduleRefTableRowRefSize,
            int memberForwardedRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsModuleRefTableRowRefSizeSmall = moduleRefTableRowRefSize == 2;
            this.IsMemberForwardRowRefSizeSmall = memberForwardedRefSize == 2;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.FlagsOffset = 0;
            this.MemberForwardedOffset = this.FlagsOffset + sizeof(UInt16);
            this.ImportNameOffset = this.MemberForwardedOffset + memberForwardedRefSize;
            this.ImportScopeOffset = this.ImportNameOffset + stringHeapRefSize;
            this.RowSize = this.ImportScopeOffset + moduleRefTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.ImplMap);
            }
        }

        internal MethodImport this[uint rowId]  // This is 1 based...
        {
            get
            {
                int rowOffset = (int)(rowId - 1) * this.RowSize;
                var pInvokeMapFlags = (MethodImportAttributes)Block.PeekUInt16(rowOffset + this.FlagsOffset);
                var importName = StringHandle.FromIndex(Block.PeekReference(rowOffset + this.ImportNameOffset, this.IsStringHeapRefSizeSmall));
                var importScope = ModuleReferenceHandle.FromRowId(Block.PeekReference(rowOffset + this.ImportScopeOffset, this.IsModuleRefTableRowRefSizeSmall));
                return new MethodImport(pInvokeMapFlags, importName, importScope);
            }
        }

        internal Handle GetMemberForwarded(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return MemberForwardedTag.ConvertToToken(Block.PeekTaggedReference(rowOffset + this.MemberForwardedOffset, this.IsMemberForwardRowRefSizeSmall));
        }

        internal uint FindImplForMethod(MethodDefinitionHandle methodDef)
        {
            uint searchCodedTag = MemberForwardedTag.ConvertMethodDefToTag(methodDef);
            return this.BinarySearchTag(searchCodedTag);
        }

        private uint BinarySearchTag(uint searchCodedTag)
        {
            int foundRowNumber =
              this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                this.MemberForwardedOffset,
                searchCodedTag,
                this.IsMemberForwardRowRefSizeSmall
            );
            return (uint)(foundRowNumber + 1);
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.MemberForwardedOffset, this.IsMemberForwardRowRefSizeSmall);
        }
    }

    internal struct FieldRVATableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsFieldTableRowRefSizeSmall;
        private readonly int RVAOffset;
        private readonly int FieldOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FieldRVATableReader(
            uint numberOfRows,
            bool declaredSorted,
            int fieldTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsFieldTableRowRefSizeSmall = fieldTableRowRefSize == 2;
            this.RVAOffset = 0;
            this.FieldOffset = this.RVAOffset + sizeof(UInt32);
            this.RowSize = this.FieldOffset + fieldTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.FieldRva);
            }
        }

        internal int GetRVA(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return Block.PeekInt32(rowOffset + this.RVAOffset);
        }

        internal uint FindFieldRVARowId(uint fieldDefRowId)
        {
            int foundRowNumber = Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                this.FieldOffset,
                fieldDefRowId,
                this.IsFieldTableRowRefSizeSmall
            );

            return (uint)(foundRowNumber + 1);
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.FieldOffset, this.IsFieldTableRowRefSizeSmall);
        }
    }

    internal struct EnCLogTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly int TokenOffset;
        private readonly int FuncCodeOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal EnCLogTableReader(
            uint numberOfRows,
            MemoryBlock containingBlock,
            int containingBlockOffset,
            MetadataStreamKind metadataStreamKind)
        {
            // EnC tables are not allowed in a compressed stream.
            // However when asked for a snapshot of the current metadata after an EnC change has been applied 
            // the CLR includes the EnCLog table into the snapshot (but not EnCMap). We pretend EnCLog is empty.
            this.NumberOfRows = (metadataStreamKind == MetadataStreamKind.Compressed) ? 0 : numberOfRows;

            this.TokenOffset = 0;
            this.FuncCodeOffset = this.TokenOffset + sizeof(uint);
            this.RowSize = this.FuncCodeOffset + sizeof(uint);
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal uint GetToken(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + this.TokenOffset);
        }

#pragma warning disable 618 // Edit and continue API marked obsolete to give us more time to refactor
        internal EditAndContinueOperation GetFuncCode(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return (EditAndContinueOperation)this.Block.PeekUInt32(rowOffset + this.FuncCodeOffset);
        }
    }

    internal struct EnCMapTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly int TokenOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal EnCMapTableReader(
            uint numberOfRows,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.TokenOffset = 0;
            this.RowSize = this.TokenOffset + sizeof(uint);
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal uint GetToken(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + this.TokenOffset);
        }
    }
#pragma warning restore 618 // Edit and continue API marked obsolete to give us more time to refactor

    internal struct AssemblyTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int HashAlgIdOffset;
        private readonly int MajorVersionOffset;
        private readonly int MinorVersionOffset;
        private readonly int BuildNumberOffset;
        private readonly int RevisionNumberOffset;
        private readonly int FlagsOffset;
        private readonly int PublicKeyOffset;
        private readonly int NameOffset;
        private readonly int CultureOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal AssemblyTableReader(
            uint numberOfRows,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            // NOTE: obfuscated assemblies may have more than one row in Assembly table,
            //       we ignore all rows but the first one
            this.NumberOfRows = numberOfRows > 1 ? 1 : numberOfRows;

            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.HashAlgIdOffset = 0;
            this.MajorVersionOffset = this.HashAlgIdOffset + sizeof(UInt32);
            this.MinorVersionOffset = this.MajorVersionOffset + sizeof(UInt16);
            this.BuildNumberOffset = this.MinorVersionOffset + sizeof(UInt16);
            this.RevisionNumberOffset = this.BuildNumberOffset + sizeof(UInt16);
            this.FlagsOffset = this.RevisionNumberOffset + sizeof(UInt16);
            this.PublicKeyOffset = this.FlagsOffset + sizeof(UInt32);
            this.NameOffset = this.PublicKeyOffset + blobHeapRefSize;
            this.CultureOffset = this.NameOffset + stringHeapRefSize;
            this.RowSize = this.CultureOffset + stringHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal AssemblyHashAlgorithm GetHashAlgorithm()
        {
            Debug.Assert(NumberOfRows == 1);
            return (AssemblyHashAlgorithm)this.Block.PeekUInt32(this.HashAlgIdOffset);
        }

        internal Version GetVersion()
        {
            Debug.Assert(NumberOfRows == 1);
            return new Version(
                this.Block.PeekUInt16(this.MajorVersionOffset),
                this.Block.PeekUInt16(this.MinorVersionOffset),
                this.Block.PeekUInt16(this.BuildNumberOffset),
                this.Block.PeekUInt16(this.RevisionNumberOffset));
        }

        internal AssemblyFlags GetFlags()
        {
            Debug.Assert(NumberOfRows == 1);
            return (AssemblyFlags)this.Block.PeekUInt32(this.FlagsOffset);
        }

        internal BlobHandle GetPublicKey()
        {
            Debug.Assert(NumberOfRows == 1);
            return BlobHandle.FromIndex(this.Block.PeekReference(this.PublicKeyOffset, this.IsBlobHeapRefSizeSmall));
        }

        internal StringHandle GetName()
        {
            Debug.Assert(NumberOfRows == 1);
            return StringHandle.FromIndex(this.Block.PeekReference(this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetCulture()
        {
            Debug.Assert(NumberOfRows == 1);
            return StringHandle.FromIndex(this.Block.PeekReference(this.CultureOffset, this.IsStringHeapRefSizeSmall));
        }
    }

    internal struct AssemblyProcessorTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly int ProcessorOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal AssemblyProcessorTableReader(
            uint numberOfRows,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.ProcessorOffset = 0;
            this.RowSize = this.ProcessorOffset + sizeof(UInt32);
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }
    }

    internal struct AssemblyOSTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly int OSPlatformIdOffset;
        private readonly int OSMajorVersionIdOffset;
        private readonly int OSMinorVersionIdOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal AssemblyOSTableReader(
            uint numberOfRows,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.OSPlatformIdOffset = 0;
            this.OSMajorVersionIdOffset = this.OSPlatformIdOffset + sizeof(UInt32);
            this.OSMinorVersionIdOffset = this.OSMajorVersionIdOffset + sizeof(UInt32);
            this.RowSize = this.OSMinorVersionIdOffset + sizeof(UInt32);
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }
    }

    internal struct AssemblyRefTableReader
    {
        /// <summary>
        /// In CLI metadata equal to the actual number of entries in AssemblyRef table.
        /// In WinMD metadata it includes synthesized AssemblyRefs in addition.
        /// </summary>
        internal readonly int NumberOfNonVirtualRows;
        internal readonly int NumberOfVirtualRows;

        private readonly bool IsStringHeapRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int MajorVersionOffset;
        private readonly int MinorVersionOffset;
        private readonly int BuildNumberOffset;
        private readonly int RevisionNumberOffset;
        private readonly int FlagsOffset;
        private readonly int PublicKeyOrTokenOffset;
        private readonly int NameOffset;
        private readonly int CultureOffset;
        private readonly int HashValueOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal AssemblyRefTableReader(
            int numberOfRows,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset,
            MetadataKind metadataKind)
        {
            this.NumberOfNonVirtualRows = numberOfRows;
            this.NumberOfVirtualRows = (metadataKind == MetadataKind.Ecma335) ? 0 : (int)AssemblyReferenceHandle.VirtualIndex.Count;

            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.MajorVersionOffset = 0;
            this.MinorVersionOffset = this.MajorVersionOffset + sizeof(UInt16);
            this.BuildNumberOffset = this.MinorVersionOffset + sizeof(UInt16);
            this.RevisionNumberOffset = this.BuildNumberOffset + sizeof(UInt16);
            this.FlagsOffset = this.RevisionNumberOffset + sizeof(UInt16);
            this.PublicKeyOrTokenOffset = this.FlagsOffset + sizeof(UInt32);
            this.NameOffset = this.PublicKeyOrTokenOffset + blobHeapRefSize;
            this.CultureOffset = this.NameOffset + stringHeapRefSize;
            this.HashValueOffset = this.CultureOffset + stringHeapRefSize;
            this.RowSize = this.HashValueOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal Version GetVersion(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return new Version(
                this.Block.PeekUInt16(rowOffset + this.MajorVersionOffset),
                this.Block.PeekUInt16(rowOffset + this.MinorVersionOffset),
                this.Block.PeekUInt16(rowOffset + this.BuildNumberOffset),
                this.Block.PeekUInt16(rowOffset + this.RevisionNumberOffset));
        }

        internal AssemblyFlags GetFlags(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return (AssemblyFlags)this.Block.PeekUInt32(rowOffset + this.FlagsOffset);
        }

        internal BlobHandle GetPublicKeyOrToken(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.PublicKeyOrTokenOffset, this.IsBlobHeapRefSizeSmall));
        }

        internal StringHandle GetName(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetCulture(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.CultureOffset, this.IsStringHeapRefSizeSmall));
        }

        internal BlobHandle GetHashValue(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.HashValueOffset, this.IsBlobHeapRefSizeSmall));
        }
    }

    internal struct AssemblyRefProcessorTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsAssemblyRefTableRowSizeSmall;
        private readonly int ProcessorOffset;
        private readonly int AssemblyRefOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal AssemblyRefProcessorTableReader(
            uint numberOfRows,
            int assembyRefTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsAssemblyRefTableRowSizeSmall = assembyRefTableRowRefSize == 2;
            this.ProcessorOffset = 0;
            this.AssemblyRefOffset = this.ProcessorOffset + sizeof(UInt32);
            this.RowSize = this.AssemblyRefOffset + assembyRefTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }
    }

    internal struct AssemblyRefOSTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsAssemblyRefTableRowRefSizeSmall;
        private readonly int OSPlatformIdOffset;
        private readonly int OSMajorVersionIdOffset;
        private readonly int OSMinorVersionIdOffset;
        private readonly int AssemblyRefOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal AssemblyRefOSTableReader(
            uint numberOfRows,
            int assembyRefTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.IsAssemblyRefTableRowRefSizeSmall = assembyRefTableRowRefSize == 2;
            this.OSPlatformIdOffset = 0;
            this.OSMajorVersionIdOffset = this.OSPlatformIdOffset + sizeof(UInt32);
            this.OSMinorVersionIdOffset = this.OSMajorVersionIdOffset + sizeof(UInt32);
            this.AssemblyRefOffset = this.OSMinorVersionIdOffset + sizeof(UInt32);
            this.RowSize = this.AssemblyRefOffset + assembyRefTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }
    }

    internal struct FileTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int FlagsOffset;
        private readonly int NameOffset;
        private readonly int HashValueOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FileTableReader(
            uint numberOfRows,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.FlagsOffset = 0;
            this.NameOffset = this.FlagsOffset + sizeof(UInt32);
            this.HashValueOffset = this.NameOffset + stringHeapRefSize;
            this.RowSize = this.HashValueOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal BlobHandle GetHashValue(AssemblyFileHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.HashValueOffset, this.IsBlobHeapRefSizeSmall));
        }

        internal uint GetFlags(AssemblyFileHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + this.FlagsOffset);
        }

        internal StringHandle GetName(AssemblyFileHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }
    }

    internal struct ExportedTypeTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsImplementationRefSizeSmall;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly int FlagsOffset;
        private readonly int TypeDefIdOffset;
        private readonly int TypeNameOffset;
        private readonly int TypeNamespaceOffset;
        private readonly int ImplementationOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ExportedTypeTableReader(
            uint numberOfRows,
            int implementationRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsImplementationRefSizeSmall = implementationRefSize == 2;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.FlagsOffset = 0;
            this.TypeDefIdOffset = this.FlagsOffset + sizeof(UInt32);
            this.TypeNameOffset = this.TypeDefIdOffset + sizeof(UInt32);
            this.TypeNamespaceOffset = this.TypeNameOffset + stringHeapRefSize;
            this.ImplementationOffset = this.TypeNamespaceOffset + stringHeapRefSize;
            this.RowSize = this.ImplementationOffset + implementationRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal StringHandle GetTypeName(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.TypeNameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetTypeNamespaceString(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.TypeNamespaceOffset, this.IsStringHeapRefSizeSmall));
        }

        internal NamespaceDefinitionHandle GetTypeNamespace(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return NamespaceDefinitionHandle.FromIndexOfFullName(this.Block.PeekReference(rowOffset + this.TypeNamespaceOffset, this.IsStringHeapRefSizeSmall));
        }

        internal Handle GetImplementation(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return ImplementationTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.ImplementationOffset, this.IsImplementationRefSizeSmall));
        }

        internal TypeAttributes GetFlags(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return (TypeAttributes)this.Block.PeekUInt32(rowOffset + this.FlagsOffset);
        }

        internal uint GetTypeDefId(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + this.TypeDefIdOffset);
        }

        internal uint GetNamespace(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            uint typeNamespace = this.Block.PeekReference(rowOffset + this.TypeNamespaceOffset, this.IsStringHeapRefSizeSmall);
            return typeNamespace;
        }
    }

    internal struct ManifestResourceTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsImplementationRefSizeSmall;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly int OffsetOffset;
        private readonly int FlagsOffset;
        private readonly int NameOffset;
        private readonly int ImplementationOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ManifestResourceTableReader(
            uint numberOfRows,
            int implementationRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsImplementationRefSizeSmall = implementationRefSize == 2;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.OffsetOffset = 0;
            this.FlagsOffset = this.OffsetOffset + sizeof(UInt32);
            this.NameOffset = this.FlagsOffset + sizeof(UInt32);
            this.ImplementationOffset = this.NameOffset + stringHeapRefSize;
            this.RowSize = this.ImplementationOffset + implementationRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal StringHandle GetName(ManifestResourceHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal Handle GetImplementation(ManifestResourceHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return ImplementationTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.ImplementationOffset, this.IsImplementationRefSizeSmall));
        }

        internal uint GetOffset(ManifestResourceHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + this.OffsetOffset);
        }

        internal ManifestResourceAttributes GetFlags(ManifestResourceHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return (ManifestResourceAttributes)this.Block.PeekUInt32(rowOffset + this.FlagsOffset);
        }
    }

    internal struct NestedClassTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsTypeDefTableRowRefSizeSmall;
        private readonly int NestedClassOffset;
        private readonly int EnclosingClassOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal NestedClassTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int typeDefTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            this.IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            this.NestedClassOffset = 0;
            this.EnclosingClassOffset = this.NestedClassOffset + typeDefTableRowRefSize;
            this.RowSize = this.EnclosingClassOffset + typeDefTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.NestedClass);
            }
        }

        internal TypeDefinitionHandle GetNestedClass(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.NestedClassOffset, this.IsTypeDefTableRowRefSizeSmall));
        }

        internal TypeDefinitionHandle GetEnclosingClass(uint rowId)
        {
            int rowOffset = (int)(rowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + this.EnclosingClassOffset, this.IsTypeDefTableRowRefSizeSmall));
        }

        internal TypeDefinitionHandle FindEnclosingType(TypeDefinitionHandle nestedTypeDef)
        {
            int rowNumber =
              this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                this.NestedClassOffset,
                nestedTypeDef.RowId,
                this.IsTypeDefTableRowRefSizeSmall);

            if (rowNumber == -1)
                return default(TypeDefinitionHandle);

            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowNumber * this.RowSize + this.EnclosingClassOffset, this.IsTypeDefTableRowRefSizeSmall));
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.NestedClassOffset, this.IsTypeDefTableRowRefSizeSmall);
        }
    }

    internal struct GenericParamTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsTypeOrMethodDefRefSizeSmall;
        private readonly bool IsStringHeapRefSizeSmall;
        private readonly int NumberOffset;
        private readonly int FlagsOffset;
        private readonly int OwnerOffset;
        private readonly int NameOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal GenericParamTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int typeOrMethodDefRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.IsTypeOrMethodDefRefSizeSmall = typeOrMethodDefRefSize == 2;
            this.IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            this.NumberOffset = 0;
            this.FlagsOffset = this.NumberOffset + sizeof(UInt16);
            this.OwnerOffset = this.FlagsOffset + sizeof(UInt16);
            this.NameOffset = this.OwnerOffset + typeOrMethodDefRefSize;
            this.RowSize = this.NameOffset + stringHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.GenericParam);
            }
        }

        internal ushort GetNumber(GenericParameterHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return this.Block.PeekUInt16(rowOffset + this.NumberOffset);
        }

        internal GenericParameterAttributes GetFlags(GenericParameterHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return (GenericParameterAttributes)this.Block.PeekUInt16(rowOffset + this.FlagsOffset);
        }

        internal StringHandle GetName(GenericParameterHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return StringHandle.FromIndex(this.Block.PeekReference(rowOffset + this.NameOffset, this.IsStringHeapRefSizeSmall));
        }

        internal Handle GetOwner(GenericParameterHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return TypeOrMethodDefTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.OwnerOffset, this.IsTypeOrMethodDefRefSizeSmall));
        }

        internal GenericParameterHandleCollection FindGenericParametersForType(TypeDefinitionHandle typeDef)
        {
            ushort count = 0;
            uint searchCodedTag = TypeOrMethodDefTag.ConvertTypeDefRowIdToTag(typeDef);
            int startRid = (int)this.BinarySearchTag(searchCodedTag, ref count);

            return new GenericParameterHandleCollection(startRid, count);
        }

        internal GenericParameterHandleCollection FindGenericParametersForMethod(MethodDefinitionHandle methodDef)
        {
            ushort count = 0;
            uint searchCodedTag = TypeOrMethodDefTag.ConvertMethodDefToTag(methodDef);
            int startRid = (int)this.BinarySearchTag(searchCodedTag, ref count);

            return new GenericParameterHandleCollection(startRid, count);
        }

        private uint BinarySearchTag(uint searchCodedTag, ref ushort genericParamCount)
        {
            int startRowNumber, endRowNumber;
            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                this.OwnerOffset,
                searchCodedTag,
                this.IsTypeOrMethodDefRefSizeSmall,
                out startRowNumber,
                out endRowNumber);

            if (startRowNumber == -1)
            {
                genericParamCount = 0;
                return 0;
            }

            genericParamCount = (ushort)(endRowNumber - startRowNumber + 1);
            return (uint)(startRowNumber + 1);
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.OwnerOffset, this.IsTypeOrMethodDefRefSizeSmall);
        }
    }

    internal struct MethodSpecTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsMethodDefOrRefRefSizeSmall;
        private readonly bool IsBlobHeapRefSizeSmall;
        private readonly int MethodOffset;
        private readonly int InstantiationOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal MethodSpecTableReader(
            uint numberOfRows,
            int methodDefOrRefRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.IsMethodDefOrRefRefSizeSmall = methodDefOrRefRefSize == 2;
            this.IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            this.MethodOffset = 0;
            this.InstantiationOffset = this.MethodOffset + methodDefOrRefRefSize;
            this.RowSize = this.InstantiationOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));
        }

        internal Handle GetMethod(MethodSpecificationHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return MethodDefOrRefTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.MethodOffset, this.IsMethodDefOrRefRefSizeSmall));
        }

        internal BlobHandle GetInstantiation(MethodSpecificationHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromIndex(this.Block.PeekReference(rowOffset + this.InstantiationOffset, this.IsBlobHeapRefSizeSmall));
        }
    }

    internal struct GenericParamConstraintTableReader
    {
        internal readonly uint NumberOfRows;
        private readonly bool IsGenericParamTableRowRefSizeSmall;
        private readonly bool IsTypeDefOrRefRefSizeSmall;
        private readonly int OwnerOffset;
        private readonly int ConstraintOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal GenericParamConstraintTableReader(
            uint numberOfRows,
            bool declaredSorted,
            int genericParamTableRowRefSize,
            int typeDefOrRefRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            this.IsGenericParamTableRowRefSizeSmall = genericParamTableRowRefSize == 2;
            this.IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
            this.OwnerOffset = 0;
            this.ConstraintOffset = this.OwnerOffset + genericParamTableRowRefSize;
            this.RowSize = this.ConstraintOffset + typeDefOrRefRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, (int)(this.RowSize * numberOfRows));

            if (!declaredSorted && !CheckSorted())
            {
                MetadataReader.ThrowTableNotSorted(TableIndex.GenericParamConstraint);
            }
        }

        internal GenericParameterConstraintHandleCollection FindConstraintsForGenericParam(GenericParameterHandle genericParameter)
        {
            int startRowNumber, endRowNumber;
            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                this.OwnerOffset,
                genericParameter.RowId,
                this.IsGenericParamTableRowRefSizeSmall,
                out startRowNumber,
                out endRowNumber);

            if (startRowNumber == -1)
            {
                return default(GenericParameterConstraintHandleCollection);
            }

            return new GenericParameterConstraintHandleCollection(
                firstRowId: startRowNumber + 1,
                count: (ushort)(endRowNumber - startRowNumber + 1));
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, this.OwnerOffset, this.IsGenericParamTableRowRefSizeSmall);
        }

        internal Handle GetConstraint(GenericParameterConstraintHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return TypeDefOrRefTag.ConvertToToken(this.Block.PeekTaggedReference(rowOffset + this.ConstraintOffset, this.IsTypeDefOrRefRefSizeSmall));
        }

        internal GenericParameterHandle GetOwner(GenericParameterConstraintHandle handle)
        {
            int rowOffset = (int)(handle.RowId - 1) * this.RowSize;
            return GenericParameterHandle.FromRowId(this.Block.PeekReference(rowOffset + this.OwnerOffset, this.IsGenericParamTableRowRefSizeSmall));
        }
    }
}