// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335
{
    internal readonly struct ModuleTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly bool _IsGUIDHeapRefSizeSmall;
        private readonly int _GenerationOffset;
        private readonly int _NameOffset;
        private readonly int _MVIdOffset;
        private readonly int _EnCIdOffset;
        private readonly int _EnCBaseIdOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ModuleTableReader(
            int numberOfRows,
            int stringHeapRefSize,
            int guidHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _IsGUIDHeapRefSizeSmall = guidHeapRefSize == 2;
            _GenerationOffset = 0;
            _NameOffset = _GenerationOffset + sizeof(ushort);
            _MVIdOffset = _NameOffset + stringHeapRefSize;
            _EnCIdOffset = _MVIdOffset + guidHeapRefSize;
            _EnCBaseIdOffset = _EnCIdOffset + guidHeapRefSize;
            this.RowSize = _EnCBaseIdOffset + guidHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal ushort GetGeneration()
        {
            Debug.Assert(NumberOfRows > 0);
            return this.Block.PeekUInt16(_GenerationOffset);
        }

        internal StringHandle GetName()
        {
            Debug.Assert(NumberOfRows > 0);
            return StringHandle.FromOffset(this.Block.PeekHeapReference(_NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal GuidHandle GetMvid()
        {
            Debug.Assert(NumberOfRows > 0);
            return GuidHandle.FromIndex(this.Block.PeekHeapReference(_MVIdOffset, _IsGUIDHeapRefSizeSmall));
        }

        internal GuidHandle GetEncId()
        {
            Debug.Assert(NumberOfRows > 0);
            return GuidHandle.FromIndex(this.Block.PeekHeapReference(_EnCIdOffset, _IsGUIDHeapRefSizeSmall));
        }

        internal GuidHandle GetEncBaseId()
        {
            Debug.Assert(NumberOfRows > 0);
            return GuidHandle.FromIndex(this.Block.PeekHeapReference(_EnCBaseIdOffset, _IsGUIDHeapRefSizeSmall));
        }
    }

    internal readonly struct TypeRefTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsResolutionScopeRefSizeSmall;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly int _ResolutionScopeOffset;
        private readonly int _NameOffset;
        private readonly int _NamespaceOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal TypeRefTableReader(
            int numberOfRows,
            int resolutionScopeRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsResolutionScopeRefSizeSmall = resolutionScopeRefSize == 2;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _ResolutionScopeOffset = 0;
            _NameOffset = _ResolutionScopeOffset + resolutionScopeRefSize;
            _NamespaceOffset = _NameOffset + stringHeapRefSize;
            this.RowSize = _NamespaceOffset + stringHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal EntityHandle GetResolutionScope(TypeReferenceHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return ResolutionScopeTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _ResolutionScopeOffset, _IsResolutionScopeRefSizeSmall));
        }

        internal StringHandle GetName(TypeReferenceHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetNamespace(TypeReferenceHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NamespaceOffset, _IsStringHeapRefSizeSmall));
        }
    }

    internal struct TypeDefTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsFieldRefSizeSmall;
        private readonly bool _IsMethodRefSizeSmall;
        private readonly bool _IsTypeDefOrRefRefSizeSmall;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly int _FlagsOffset;
        private readonly int _NameOffset;
        private readonly int _NamespaceOffset;
        private readonly int _ExtendsOffset;
        private readonly int _FieldListOffset;
        private readonly int _MethodListOffset;
        internal readonly int RowSize;
        internal MemoryBlock Block;

        internal TypeDefTableReader(
            int numberOfRows,
            int fieldRefSize,
            int methodRefSize,
            int typeDefOrRefRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsFieldRefSizeSmall = fieldRefSize == 2;
            _IsMethodRefSizeSmall = methodRefSize == 2;
            _IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _FlagsOffset = 0;
            _NameOffset = _FlagsOffset + sizeof(uint);
            _NamespaceOffset = _NameOffset + stringHeapRefSize;
            _ExtendsOffset = _NamespaceOffset + stringHeapRefSize;
            _FieldListOffset = _ExtendsOffset + typeDefOrRefRefSize;
            _MethodListOffset = _FieldListOffset + fieldRefSize;
            this.RowSize = _MethodListOffset + methodRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal TypeAttributes GetFlags(TypeDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return (TypeAttributes)this.Block.PeekUInt32(rowOffset + _FlagsOffset);
        }

        internal NamespaceDefinitionHandle GetNamespaceDefinition(TypeDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return NamespaceDefinitionHandle.FromFullNameOffset(this.Block.PeekHeapReference(rowOffset + _NamespaceOffset, _IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetNamespace(TypeDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NamespaceOffset, _IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetName(TypeDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal EntityHandle GetExtends(TypeDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return TypeDefOrRefTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _ExtendsOffset, _IsTypeDefOrRefRefSizeSmall));
        }

        internal int GetFieldStart(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekReference(rowOffset + _FieldListOffset, _IsFieldRefSizeSmall);
        }

        internal int GetMethodStart(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekReference(rowOffset + _MethodListOffset, _IsMethodRefSizeSmall);
        }

        internal TypeDefinitionHandle FindTypeContainingMethod(int methodDefOrPtrRowId, int numberOfMethods)
        {
            int numOfRows = this.NumberOfRows;
            int slot = this.Block.BinarySearchForSlot(numOfRows, this.RowSize, _MethodListOffset, (uint)methodDefOrPtrRowId, _IsMethodRefSizeSmall);
            int row = slot + 1;
            if (row == 0)
            {
                return default(TypeDefinitionHandle);
            }

            if (row > numOfRows)
            {
                if (methodDefOrPtrRowId <= numberOfMethods)
                {
                    return TypeDefinitionHandle.FromRowId(numOfRows);
                }

                return default(TypeDefinitionHandle);
            }

            int value = this.GetMethodStart(row);
            if (value == methodDefOrPtrRowId)
            {
                while (row < numOfRows)
                {
                    int newRow = row + 1;
                    value = this.GetMethodStart(newRow);
                    if (value == methodDefOrPtrRowId)
                    {
                        row = newRow;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return TypeDefinitionHandle.FromRowId(row);
        }

        internal TypeDefinitionHandle FindTypeContainingField(int fieldDefOrPtrRowId, int numberOfFields)
        {
            int numOfRows = this.NumberOfRows;
            int slot = this.Block.BinarySearchForSlot(numOfRows, this.RowSize, _FieldListOffset, (uint)fieldDefOrPtrRowId, _IsFieldRefSizeSmall);
            int row = slot + 1;
            if (row == 0)
            {
                return default(TypeDefinitionHandle);
            }

            if (row > numOfRows)
            {
                if (fieldDefOrPtrRowId <= numberOfFields)
                {
                    return TypeDefinitionHandle.FromRowId(numOfRows);
                }

                return default(TypeDefinitionHandle);
            }

            int value = this.GetFieldStart(row);
            if (value == fieldDefOrPtrRowId)
            {
                while (row < numOfRows)
                {
                    int newRow = row + 1;
                    value = this.GetFieldStart(newRow);
                    if (value == fieldDefOrPtrRowId)
                    {
                        row = newRow;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return TypeDefinitionHandle.FromRowId(row);
        }
    }

    internal readonly struct FieldPtrTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsFieldTableRowRefSizeSmall;
        private readonly int _FieldOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FieldPtrTableReader(
            int numberOfRows,
            int fieldTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsFieldTableRowRefSizeSmall = fieldTableRowRefSize == 2;
            _FieldOffset = 0;
            this.RowSize = _FieldOffset + fieldTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal FieldDefinitionHandle GetFieldFor(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return FieldDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _FieldOffset, _IsFieldTableRowRefSizeSmall));
        }

        internal int GetRowIdForFieldDefRow(int fieldDefRowId)
        {
            return this.Block.LinearSearchReference(this.RowSize, _FieldOffset, (uint)fieldDefRowId, _IsFieldTableRowRefSizeSmall) + 1;
        }
    }

    internal readonly struct FieldTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _FlagsOffset;
        private readonly int _NameOffset;
        private readonly int _SignatureOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FieldTableReader(
            int numberOfRows,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _FlagsOffset = 0;
            _NameOffset = _FlagsOffset + sizeof(ushort);
            _SignatureOffset = _NameOffset + stringHeapRefSize;
            this.RowSize = _SignatureOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal StringHandle GetName(FieldDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal FieldAttributes GetFlags(FieldDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return (FieldAttributes)this.Block.PeekUInt16(rowOffset + _FlagsOffset);
        }

        internal BlobHandle GetSignature(FieldDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _SignatureOffset, _IsBlobHeapRefSizeSmall));
        }
    }

    internal readonly struct MethodPtrTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsMethodTableRowRefSizeSmall;
        private readonly int _MethodOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal MethodPtrTableReader(
            int numberOfRows,
            int methodTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsMethodTableRowRefSizeSmall = methodTableRowRefSize == 2;
            _MethodOffset = 0;
            this.RowSize = _MethodOffset + methodTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        // returns a rid
        internal MethodDefinitionHandle GetMethodFor(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return MethodDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _MethodOffset, _IsMethodTableRowRefSizeSmall));
        }

        internal int GetRowIdForMethodDefRow(int methodDefRowId)
        {
            return this.Block.LinearSearchReference(this.RowSize, _MethodOffset, (uint)methodDefRowId, _IsMethodTableRowRefSizeSmall) + 1;
        }
    }

    internal readonly struct MethodTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsParamRefSizeSmall;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _RvaOffset;
        private readonly int _ImplFlagsOffset;
        private readonly int _FlagsOffset;
        private readonly int _NameOffset;
        private readonly int _SignatureOffset;
        private readonly int _ParamListOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal MethodTableReader(
            int numberOfRows,
            int paramRefSize,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsParamRefSizeSmall = paramRefSize == 2;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _RvaOffset = 0;
            _ImplFlagsOffset = _RvaOffset + sizeof(uint);
            _FlagsOffset = _ImplFlagsOffset + sizeof(ushort);
            _NameOffset = _FlagsOffset + sizeof(ushort);
            _SignatureOffset = _NameOffset + stringHeapRefSize;
            _ParamListOffset = _SignatureOffset + blobHeapRefSize;
            this.RowSize = _ParamListOffset + paramRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal int GetParamStart(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekReference(rowOffset + _ParamListOffset, _IsParamRefSizeSmall);
        }

        internal BlobHandle GetSignature(MethodDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _SignatureOffset, _IsBlobHeapRefSizeSmall));
        }

        internal int GetRva(MethodDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return this.Block.PeekInt32(rowOffset + _RvaOffset);
        }

        internal StringHandle GetName(MethodDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal MethodAttributes GetFlags(MethodDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return (MethodAttributes)this.Block.PeekUInt16(rowOffset + _FlagsOffset);
        }

        internal MethodImplAttributes GetImplFlags(MethodDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return (MethodImplAttributes)this.Block.PeekUInt16(rowOffset + _ImplFlagsOffset);
        }
    }

    internal readonly struct ParamPtrTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsParamTableRowRefSizeSmall;
        private readonly int _ParamOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ParamPtrTableReader(
            int numberOfRows,
            int paramTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsParamTableRowRefSizeSmall = paramTableRowRefSize == 2;
            _ParamOffset = 0;
            this.RowSize = _ParamOffset + paramTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal ParameterHandle GetParamFor(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return ParameterHandle.FromRowId(this.Block.PeekReference(rowOffset + _ParamOffset, _IsParamTableRowRefSizeSmall));
        }
    }

    internal readonly struct ParamTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly int _FlagsOffset;
        private readonly int _SequenceOffset;
        private readonly int _NameOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ParamTableReader(
            int numberOfRows,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _FlagsOffset = 0;
            _SequenceOffset = _FlagsOffset + sizeof(ushort);
            _NameOffset = _SequenceOffset + sizeof(ushort);
            this.RowSize = _NameOffset + stringHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal ParameterAttributes GetFlags(ParameterHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return (ParameterAttributes)this.Block.PeekUInt16(rowOffset + _FlagsOffset);
        }

        internal ushort GetSequence(ParameterHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return this.Block.PeekUInt16(rowOffset + _SequenceOffset);
        }

        internal StringHandle GetName(ParameterHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }
    }

    internal readonly struct InterfaceImplTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsTypeDefTableRowRefSizeSmall;
        private readonly bool _IsTypeDefOrRefRefSizeSmall;
        private readonly int _ClassOffset;
        private readonly int _InterfaceOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal InterfaceImplTableReader(
            int numberOfRows,
            bool declaredSorted,
            int typeDefTableRowRefSize,
            int typeDefOrRefRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            _IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
            _ClassOffset = 0;
            _InterfaceOffset = _ClassOffset + typeDefTableRowRefSize;
            this.RowSize = _InterfaceOffset + typeDefOrRefRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.InterfaceImpl);
            }
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _ClassOffset, _IsTypeDefTableRowRefSizeSmall);
        }

        internal void GetInterfaceImplRange(
            TypeDefinitionHandle typeDef,
            out int firstImplRowId,
            out int lastImplRowId)
        {
            int typeDefRid = typeDef.RowId;

            int startRowNumber, endRowNumber;
            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                _ClassOffset,
                (uint)typeDefRid,
                _IsTypeDefTableRowRefSizeSmall,
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

        internal EntityHandle GetInterface(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return TypeDefOrRefTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _InterfaceOffset, _IsTypeDefOrRefRefSizeSmall));
        }
    }

    internal struct MemberRefTableReader
    {
        internal int NumberOfRows;
        private readonly bool _IsMemberRefParentRefSizeSmall;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _ClassOffset;
        private readonly int _NameOffset;
        private readonly int _SignatureOffset;
        internal readonly int RowSize;
        internal MemoryBlock Block;

        internal MemberRefTableReader(
            int numberOfRows,
            int memberRefParentRefSize,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsMemberRefParentRefSizeSmall = memberRefParentRefSize == 2;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _ClassOffset = 0;
            _NameOffset = _ClassOffset + memberRefParentRefSize;
            _SignatureOffset = _NameOffset + stringHeapRefSize;
            this.RowSize = _SignatureOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal BlobHandle GetSignature(MemberReferenceHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _SignatureOffset, _IsBlobHeapRefSizeSmall));
        }

        internal StringHandle GetName(MemberReferenceHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal EntityHandle GetClass(MemberReferenceHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return MemberRefParentTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _ClassOffset, _IsMemberRefParentRefSizeSmall));
        }
    }

    internal readonly struct ConstantTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsHasConstantRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _TypeOffset;
        private readonly int _ParentOffset;
        private readonly int _ValueOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ConstantTableReader(
            int numberOfRows,
            bool declaredSorted,
            int hasConstantRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsHasConstantRefSizeSmall = hasConstantRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _TypeOffset = 0;
            _ParentOffset = _TypeOffset + sizeof(byte) + 1; // Alignment here (+1)...
            _ValueOffset = _ParentOffset + hasConstantRefSize;
            this.RowSize = _ValueOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.Constant);
            }
        }

        internal ConstantTypeCode GetType(ConstantHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return (ConstantTypeCode)this.Block.PeekByte(rowOffset + _TypeOffset);
        }

        internal BlobHandle GetValue(ConstantHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _ValueOffset, _IsBlobHeapRefSizeSmall));
        }

        internal EntityHandle GetParent(ConstantHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return HasConstantTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _ParentOffset, _IsHasConstantRefSizeSmall));
        }

        internal ConstantHandle FindConstant(EntityHandle parentHandle)
        {
            int foundRowNumber =
              this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                _ParentOffset,
                HasConstantTag.ConvertToTag(parentHandle),
                _IsHasConstantRefSizeSmall);

            return ConstantHandle.FromRowId(foundRowNumber + 1);
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _ParentOffset, _IsHasConstantRefSizeSmall);
        }
    }

    internal readonly struct CustomAttributeTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsHasCustomAttributeRefSizeSmall;
        private readonly bool _IsCustomAttributeTypeRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _ParentOffset;
        private readonly int _TypeOffset;
        private readonly int _ValueOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        // row ids in the CustomAttribute table sorted by parents
        internal readonly int[] PtrTable;

        internal CustomAttributeTableReader(
            int numberOfRows,
            bool declaredSorted,
            int hasCustomAttributeRefSize,
            int customAttributeTypeRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsHasCustomAttributeRefSizeSmall = hasCustomAttributeRefSize == 2;
            _IsCustomAttributeTypeRefSizeSmall = customAttributeTypeRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _ParentOffset = 0;
            _TypeOffset = _ParentOffset + hasCustomAttributeRefSize;
            _ValueOffset = _TypeOffset + customAttributeTypeRefSize;
            this.RowSize = _ValueOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
            this.PtrTable = null;

            if (!declaredSorted && !CheckSorted())
            {
                this.PtrTable = this.Block.BuildPtrTable(
                    numberOfRows,
                    this.RowSize,
                    _ParentOffset,
                    _IsHasCustomAttributeRefSizeSmall);
            }
        }

        internal EntityHandle GetParent(CustomAttributeHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return HasCustomAttributeTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _ParentOffset, _IsHasCustomAttributeRefSizeSmall));
        }

        internal EntityHandle GetConstructor(CustomAttributeHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return CustomAttributeTypeTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _TypeOffset, _IsCustomAttributeTypeRefSizeSmall));
        }

        internal BlobHandle GetValue(CustomAttributeHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _ValueOffset, _IsBlobHeapRefSizeSmall));
        }

        internal void GetAttributeRange(EntityHandle parentHandle, out int firstImplRowId, out int lastImplRowId)
        {
            int startRowNumber, endRowNumber;

            if (this.PtrTable != null)
            {
                this.Block.BinarySearchReferenceRange(
                    this.PtrTable,
                    this.RowSize,
                    _ParentOffset,
                    HasCustomAttributeTag.ConvertToTag(parentHandle),
                    _IsHasCustomAttributeRefSizeSmall,
                    out startRowNumber,
                    out endRowNumber
                );
            }
            else
            {
                this.Block.BinarySearchReferenceRange(
                    this.NumberOfRows,
                    this.RowSize,
                    _ParentOffset,
                    HasCustomAttributeTag.ConvertToTag(parentHandle),
                    _IsHasCustomAttributeRefSizeSmall,
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
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _ParentOffset, _IsHasCustomAttributeRefSizeSmall);
        }
    }

    internal readonly struct FieldMarshalTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsHasFieldMarshalRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _ParentOffset;
        private readonly int _NativeTypeOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FieldMarshalTableReader(
            int numberOfRows,
            bool declaredSorted,
            int hasFieldMarshalRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsHasFieldMarshalRefSizeSmall = hasFieldMarshalRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _ParentOffset = 0;
            _NativeTypeOffset = _ParentOffset + hasFieldMarshalRefSize;
            this.RowSize = _NativeTypeOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.FieldMarshal);
            }
        }

        internal EntityHandle GetParent(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return HasFieldMarshalTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _ParentOffset, _IsHasFieldMarshalRefSizeSmall));
        }

        internal BlobHandle GetNativeType(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NativeTypeOffset, _IsBlobHeapRefSizeSmall));
        }

        internal int FindFieldMarshalRowId(EntityHandle handle)
        {
            int foundRowNumber =
              this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                _ParentOffset,
                HasFieldMarshalTag.ConvertToTag(handle),
                _IsHasFieldMarshalRefSizeSmall);

            return foundRowNumber + 1;
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _ParentOffset, _IsHasFieldMarshalRefSizeSmall);
        }
    }

    internal readonly struct DeclSecurityTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsHasDeclSecurityRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _ActionOffset;
        private readonly int _ParentOffset;
        private readonly int _PermissionSetOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal DeclSecurityTableReader(
            int numberOfRows,
            bool declaredSorted,
            int hasDeclSecurityRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsHasDeclSecurityRefSizeSmall = hasDeclSecurityRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _ActionOffset = 0;
            _ParentOffset = _ActionOffset + sizeof(ushort);
            _PermissionSetOffset = _ParentOffset + hasDeclSecurityRefSize;
            this.RowSize = _PermissionSetOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.DeclSecurity);
            }
        }

        internal DeclarativeSecurityAction GetAction(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return (DeclarativeSecurityAction)this.Block.PeekUInt16(rowOffset + _ActionOffset);
        }

        internal EntityHandle GetParent(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return HasDeclSecurityTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _ParentOffset, _IsHasDeclSecurityRefSizeSmall));
        }

        internal BlobHandle GetPermissionSet(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _PermissionSetOffset, _IsBlobHeapRefSizeSmall));
        }

        internal void GetAttributeRange(EntityHandle parentToken, out int firstImplRowId, out int lastImplRowId)
        {
            int startRowNumber, endRowNumber;

            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                _ParentOffset,
                HasDeclSecurityTag.ConvertToTag(parentToken),
                _IsHasDeclSecurityRefSizeSmall,
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
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _ParentOffset, _IsHasDeclSecurityRefSizeSmall);
        }
    }

    internal struct ClassLayoutTableReader
    {
        internal int NumberOfRows;
        private readonly bool _IsTypeDefTableRowRefSizeSmall;
        private readonly int _PackagingSizeOffset;
        private readonly int _ClassSizeOffset;
        private readonly int _ParentOffset;
        internal readonly int RowSize;
        internal MemoryBlock Block;

        internal ClassLayoutTableReader(
            int numberOfRows,
            bool declaredSorted,
            int typeDefTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            _PackagingSizeOffset = 0;
            _ClassSizeOffset = _PackagingSizeOffset + sizeof(ushort);
            _ParentOffset = _ClassSizeOffset + sizeof(uint);
            this.RowSize = _ParentOffset + typeDefTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.ClassLayout);
            }
        }

        internal TypeDefinitionHandle GetParent(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _ParentOffset, _IsTypeDefTableRowRefSizeSmall));
        }

        internal ushort GetPackingSize(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekUInt16(rowOffset + _PackagingSizeOffset);
        }

        internal uint GetClassSize(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + _ClassSizeOffset);
        }

        // Returns RowId (0 means we there is no record in this table corresponding to the specified type).
        internal int FindRow(TypeDefinitionHandle typeDef)
        {
            return 1 + this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize, 
                _ParentOffset, 
                (uint)typeDef.RowId,
                _IsTypeDefTableRowRefSizeSmall);
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _ParentOffset, _IsTypeDefTableRowRefSizeSmall);
        }
    }

    internal readonly struct FieldLayoutTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsFieldTableRowRefSizeSmall;
        private readonly int _OffsetOffset;
        private readonly int _FieldOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FieldLayoutTableReader(
            int numberOfRows,
            bool declaredSorted,
            int fieldTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsFieldTableRowRefSizeSmall = fieldTableRowRefSize == 2;
            _OffsetOffset = 0;
            _FieldOffset = _OffsetOffset + sizeof(uint);
            this.RowSize = _FieldOffset + fieldTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.FieldLayout);
            }
        }

        /// <summary>
        /// Returns field offset for given field RowId, or -1 if not available. 
        /// </summary>
        internal int FindFieldLayoutRowId(FieldDefinitionHandle handle)
        {
            int rowNumber =
              this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                _FieldOffset,
                (uint)handle.RowId,
                _IsFieldTableRowRefSizeSmall);

            return rowNumber + 1;
        }

        internal uint GetOffset(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + _OffsetOffset);
        }

        internal FieldDefinitionHandle GetField(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return FieldDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _FieldOffset, _IsFieldTableRowRefSizeSmall));
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _FieldOffset, _IsFieldTableRowRefSizeSmall);
        }
    }

    internal readonly struct StandAloneSigTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _SignatureOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal StandAloneSigTableReader(
            int numberOfRows,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _SignatureOffset = 0;
            this.RowSize = _SignatureOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal BlobHandle GetSignature(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _SignatureOffset, _IsBlobHeapRefSizeSmall));
        }
    }

    internal readonly struct EventMapTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsTypeDefTableRowRefSizeSmall;
        private readonly bool _IsEventRefSizeSmall;
        private readonly int _ParentOffset;
        private readonly int _EventListOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal EventMapTableReader(
            int numberOfRows,
            int typeDefTableRowRefSize,
            int eventRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            _IsEventRefSizeSmall = eventRefSize == 2;
            _ParentOffset = 0;
            _EventListOffset = _ParentOffset + typeDefTableRowRefSize;
            this.RowSize = _EventListOffset + eventRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal int FindEventMapRowIdFor(TypeDefinitionHandle typeDef)
        {
            // We do a linear scan here because we don't have these tables sorted
            // TODO: We can scan the table to see if it is sorted and use binary search if so.
            // Also, the compilers should make sure it's sorted.
            int rowNumber = this.Block.LinearSearchReference(
                this.RowSize,
                _ParentOffset,
                (uint)typeDef.RowId,
                _IsTypeDefTableRowRefSizeSmall);

            return rowNumber + 1;
        }

        internal TypeDefinitionHandle GetParentType(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _ParentOffset, _IsTypeDefTableRowRefSizeSmall));
        }

        internal int GetEventListStartFor(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekReference(rowOffset + _EventListOffset, _IsEventRefSizeSmall);
        }
    }

    internal readonly struct EventPtrTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsEventTableRowRefSizeSmall;
        private readonly int _EventOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal EventPtrTableReader(
            int numberOfRows,
            int eventTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsEventTableRowRefSizeSmall = eventTableRowRefSize == 2;
            _EventOffset = 0;
            this.RowSize = _EventOffset + eventTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal EventDefinitionHandle GetEventFor(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return EventDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _EventOffset, _IsEventTableRowRefSizeSmall));
        }
    }

    internal struct EventTableReader
    {
        internal int NumberOfRows;
        private readonly bool _IsTypeDefOrRefRefSizeSmall;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly int _FlagsOffset;
        private readonly int _NameOffset;
        private readonly int _EventTypeOffset;
        internal readonly int RowSize;
        internal MemoryBlock Block;

        internal EventTableReader(
            int numberOfRows,
            int typeDefOrRefRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _FlagsOffset = 0;
            _NameOffset = _FlagsOffset + sizeof(ushort);
            _EventTypeOffset = _NameOffset + stringHeapRefSize;
            this.RowSize = _EventTypeOffset + typeDefOrRefRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal EventAttributes GetFlags(EventDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return (EventAttributes)this.Block.PeekUInt16(rowOffset + _FlagsOffset);
        }

        internal StringHandle GetName(EventDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal EntityHandle GetEventType(EventDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return TypeDefOrRefTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _EventTypeOffset, _IsTypeDefOrRefRefSizeSmall));
        }
    }

    internal readonly struct PropertyMapTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsTypeDefTableRowRefSizeSmall;
        private readonly bool _IsPropertyRefSizeSmall;
        private readonly int _ParentOffset;
        private readonly int _PropertyListOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal PropertyMapTableReader(
            int numberOfRows,
            int typeDefTableRowRefSize,
            int propertyRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            _IsPropertyRefSizeSmall = propertyRefSize == 2;
            _ParentOffset = 0;
            _PropertyListOffset = _ParentOffset + typeDefTableRowRefSize;
            this.RowSize = _PropertyListOffset + propertyRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal int FindPropertyMapRowIdFor(TypeDefinitionHandle typeDef)
        {
            // We do a linear scan here because we don't have these tables sorted.
            // TODO: We can scan the table to see if it is sorted and use binary search if so.
            // Also, the compilers should make sure it's sorted.
            int rowNumber =
              this.Block.LinearSearchReference(
                this.RowSize,
                _ParentOffset,
                (uint)typeDef.RowId,
                _IsTypeDefTableRowRefSizeSmall);

            return rowNumber + 1;
        }

        internal TypeDefinitionHandle GetParentType(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _ParentOffset, _IsTypeDefTableRowRefSizeSmall));
        }

        internal int GetPropertyListStartFor(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekReference(rowOffset + _PropertyListOffset, _IsPropertyRefSizeSmall);
        }
    }

    internal readonly struct PropertyPtrTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsPropertyTableRowRefSizeSmall;
        private readonly int _PropertyOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal PropertyPtrTableReader(
            int numberOfRows,
            int propertyTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsPropertyTableRowRefSizeSmall = propertyTableRowRefSize == 2;
            _PropertyOffset = 0;
            this.RowSize = _PropertyOffset + propertyTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal PropertyDefinitionHandle GetPropertyFor(
          int rowId
        )
        // ^ requires rowId <= this.NumberOfRows;
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return PropertyDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _PropertyOffset, _IsPropertyTableRowRefSizeSmall));
        }
    }

    internal readonly struct PropertyTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _FlagsOffset;
        private readonly int _NameOffset;
        private readonly int _SignatureOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal PropertyTableReader(
            int numberOfRows,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _FlagsOffset = 0;
            _NameOffset = _FlagsOffset + sizeof(ushort);
            _SignatureOffset = _NameOffset + stringHeapRefSize;
            this.RowSize = _SignatureOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal PropertyAttributes GetFlags(PropertyDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return (PropertyAttributes)this.Block.PeekUInt16(rowOffset + _FlagsOffset);
        }

        internal StringHandle GetName(PropertyDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal BlobHandle GetSignature(PropertyDefinitionHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _SignatureOffset, _IsBlobHeapRefSizeSmall));
        }
    }

    internal readonly struct MethodSemanticsTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsMethodTableRowRefSizeSmall;
        private readonly bool _IsHasSemanticRefSizeSmall;
        private readonly int _SemanticsFlagOffset;
        private readonly int _MethodOffset;
        private readonly int _AssociationOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal MethodSemanticsTableReader(
            int numberOfRows,
            bool declaredSorted,
            int methodTableRowRefSize,
            int hasSemanticRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsMethodTableRowRefSizeSmall = methodTableRowRefSize == 2;
            _IsHasSemanticRefSizeSmall = hasSemanticRefSize == 2;
            _SemanticsFlagOffset = 0;
            _MethodOffset = _SemanticsFlagOffset + sizeof(ushort);
            _AssociationOffset = _MethodOffset + methodTableRowRefSize;
            this.RowSize = _AssociationOffset + hasSemanticRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.MethodSemantics);
            }
        }

        internal MethodDefinitionHandle GetMethod(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return MethodDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _MethodOffset, _IsMethodTableRowRefSizeSmall));
        }

        internal MethodSemanticsAttributes GetSemantics(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return (MethodSemanticsAttributes)this.Block.PeekUInt16(rowOffset + _SemanticsFlagOffset);
        }

        internal EntityHandle GetAssociation(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return HasSemanticsTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _AssociationOffset, _IsHasSemanticRefSizeSmall));
        }

        // returns rowID
        internal int FindSemanticMethodsForEvent(EventDefinitionHandle eventDef, out ushort methodCount)
        {
            methodCount = 0;
            uint searchCodedTag = HasSemanticsTag.ConvertEventHandleToTag(eventDef);
            return this.BinarySearchTag(searchCodedTag, ref methodCount);
        }

        internal int FindSemanticMethodsForProperty(PropertyDefinitionHandle propertyDef, out ushort methodCount)
        {
            methodCount = 0;
            uint searchCodedTag = HasSemanticsTag.ConvertPropertyHandleToTag(propertyDef);
            return this.BinarySearchTag(searchCodedTag, ref methodCount);
        }

        private int BinarySearchTag(uint searchCodedTag, ref ushort methodCount)
        {
            int startRowNumber, endRowNumber;
            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                _AssociationOffset,
                searchCodedTag,
                _IsHasSemanticRefSizeSmall,
                out startRowNumber,
                out endRowNumber
            );

            if (startRowNumber == -1)
            {
                methodCount = 0;
                return 0;
            }

            methodCount = (ushort)(endRowNumber - startRowNumber + 1);
            return startRowNumber + 1;
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _AssociationOffset, _IsHasSemanticRefSizeSmall);
        }
    }

    internal readonly struct MethodImplTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsTypeDefTableRowRefSizeSmall;
        private readonly bool _IsMethodDefOrRefRefSizeSmall;
        private readonly int _ClassOffset;
        private readonly int _MethodBodyOffset;
        private readonly int _MethodDeclarationOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal MethodImplTableReader(
            int numberOfRows,
            bool declaredSorted,
            int typeDefTableRowRefSize,
            int methodDefOrRefRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            _IsMethodDefOrRefRefSizeSmall = methodDefOrRefRefSize == 2;
            _ClassOffset = 0;
            _MethodBodyOffset = _ClassOffset + typeDefTableRowRefSize;
            _MethodDeclarationOffset = _MethodBodyOffset + methodDefOrRefRefSize;
            this.RowSize = _MethodDeclarationOffset + methodDefOrRefRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.MethodImpl);
            }
        }

        internal TypeDefinitionHandle GetClass(MethodImplementationHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _ClassOffset, _IsTypeDefTableRowRefSizeSmall));
        }

        internal EntityHandle GetMethodBody(MethodImplementationHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return MethodDefOrRefTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _MethodBodyOffset, _IsMethodDefOrRefRefSizeSmall));
        }

        internal EntityHandle GetMethodDeclaration(MethodImplementationHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return MethodDefOrRefTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _MethodDeclarationOffset, _IsMethodDefOrRefRefSizeSmall));
        }

        internal void GetMethodImplRange(
            TypeDefinitionHandle typeDef,
            out int firstImplRowId,
            out int lastImplRowId)
        {
            int startRowNumber, endRowNumber;
            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                _ClassOffset,
                (uint)typeDef.RowId,
                _IsTypeDefTableRowRefSizeSmall,
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
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _ClassOffset, _IsTypeDefTableRowRefSizeSmall);
        }
    }

    internal readonly struct ModuleRefTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly int _NameOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ModuleRefTableReader(
            int numberOfRows,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _NameOffset = 0;
            this.RowSize = _NameOffset + stringHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal StringHandle GetName(ModuleReferenceHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }
    }

    internal readonly struct TypeSpecTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _SignatureOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal TypeSpecTableReader(
            int numberOfRows,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _SignatureOffset = 0;
            this.RowSize = _SignatureOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal BlobHandle GetSignature(TypeSpecificationHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _SignatureOffset, _IsBlobHeapRefSizeSmall));
        }
    }

    internal readonly struct ImplMapTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsModuleRefTableRowRefSizeSmall;
        private readonly bool _IsMemberForwardRowRefSizeSmall;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly int _FlagsOffset;
        private readonly int _MemberForwardedOffset;
        private readonly int _ImportNameOffset;
        private readonly int _ImportScopeOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ImplMapTableReader(
            int numberOfRows,
            bool declaredSorted,
            int moduleRefTableRowRefSize,
            int memberForwardedRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsModuleRefTableRowRefSizeSmall = moduleRefTableRowRefSize == 2;
            _IsMemberForwardRowRefSizeSmall = memberForwardedRefSize == 2;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _FlagsOffset = 0;
            _MemberForwardedOffset = _FlagsOffset + sizeof(ushort);
            _ImportNameOffset = _MemberForwardedOffset + memberForwardedRefSize;
            _ImportScopeOffset = _ImportNameOffset + stringHeapRefSize;
            this.RowSize = _ImportScopeOffset + moduleRefTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.ImplMap);
            }
        }

        internal MethodImport GetImport(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            var pInvokeMapFlags = (MethodImportAttributes)Block.PeekUInt16(rowOffset + _FlagsOffset);
            var importName = StringHandle.FromOffset(Block.PeekHeapReference(rowOffset + _ImportNameOffset, _IsStringHeapRefSizeSmall));
            var importScope = ModuleReferenceHandle.FromRowId(Block.PeekReference(rowOffset + _ImportScopeOffset, _IsModuleRefTableRowRefSizeSmall));
            return new MethodImport(pInvokeMapFlags, importName, importScope);
        }

        internal EntityHandle GetMemberForwarded(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return MemberForwardedTag.ConvertToHandle(Block.PeekTaggedReference(rowOffset + _MemberForwardedOffset, _IsMemberForwardRowRefSizeSmall));
        }

        internal int FindImplForMethod(MethodDefinitionHandle methodDef)
        {
            uint searchCodedTag = MemberForwardedTag.ConvertMethodDefToTag(methodDef);
            return this.BinarySearchTag(searchCodedTag);
        }

        private int BinarySearchTag(uint searchCodedTag)
        {
            int foundRowNumber =
              this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                _MemberForwardedOffset,
                searchCodedTag,
                _IsMemberForwardRowRefSizeSmall);

            return foundRowNumber + 1;
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _MemberForwardedOffset, _IsMemberForwardRowRefSizeSmall);
        }
    }

    internal readonly struct FieldRVATableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsFieldTableRowRefSizeSmall;
        private readonly int _RvaOffset;
        private readonly int _FieldOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FieldRVATableReader(
            int numberOfRows,
            bool declaredSorted,
            int fieldTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsFieldTableRowRefSizeSmall = fieldTableRowRefSize == 2;
            _RvaOffset = 0;
            _FieldOffset = _RvaOffset + sizeof(uint);
            this.RowSize = _FieldOffset + fieldTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.FieldRva);
            }
        }

        internal int GetRva(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return Block.PeekInt32(rowOffset + _RvaOffset);
        }

        internal int FindFieldRvaRowId(int fieldDefRowId)
        {
            int foundRowNumber = Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                _FieldOffset,
                (uint)fieldDefRowId,
                _IsFieldTableRowRefSizeSmall);

            return foundRowNumber + 1;
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _FieldOffset, _IsFieldTableRowRefSizeSmall);
        }
    }

    internal readonly struct EnCLogTableReader
    {
        internal readonly int NumberOfRows;
        private readonly int _TokenOffset;
        private readonly int _FuncCodeOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal EnCLogTableReader(
            int numberOfRows,
            MemoryBlock containingBlock,
            int containingBlockOffset,
            MetadataStreamKind metadataStreamKind)
        {
            // EnC tables are not allowed in a compressed stream.
            // However when asked for a snapshot of the current metadata after an EnC change has been applied 
            // the CLR includes the EnCLog table into the snapshot (but not EnCMap). We pretend EnCLog is empty.
            this.NumberOfRows = (metadataStreamKind == MetadataStreamKind.Compressed) ? 0 : numberOfRows;

            _TokenOffset = 0;
            _FuncCodeOffset = _TokenOffset + sizeof(uint);
            this.RowSize = _FuncCodeOffset + sizeof(uint);
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal uint GetToken(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + _TokenOffset);
        }

        internal EditAndContinueOperation GetFuncCode(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return (EditAndContinueOperation)this.Block.PeekUInt32(rowOffset + _FuncCodeOffset);
        }
    }

    internal readonly struct EnCMapTableReader
    {
        internal readonly int NumberOfRows;
        private readonly int _TokenOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal EnCMapTableReader(
            int numberOfRows,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _TokenOffset = 0;
            this.RowSize = _TokenOffset + sizeof(uint);
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal uint GetToken(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + _TokenOffset);
        }
    }

    internal readonly struct AssemblyTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _HashAlgIdOffset;
        private readonly int _MajorVersionOffset;
        private readonly int _MinorVersionOffset;
        private readonly int _BuildNumberOffset;
        private readonly int _RevisionNumberOffset;
        private readonly int _FlagsOffset;
        private readonly int _PublicKeyOffset;
        private readonly int _NameOffset;
        private readonly int _CultureOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal AssemblyTableReader(
            int numberOfRows,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            // NOTE: obfuscated assemblies may have more than one row in Assembly table,
            //       we ignore all rows but the first one
            this.NumberOfRows = numberOfRows > 1 ? 1 : numberOfRows;

            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _HashAlgIdOffset = 0;
            _MajorVersionOffset = _HashAlgIdOffset + sizeof(uint);
            _MinorVersionOffset = _MajorVersionOffset + sizeof(ushort);
            _BuildNumberOffset = _MinorVersionOffset + sizeof(ushort);
            _RevisionNumberOffset = _BuildNumberOffset + sizeof(ushort);
            _FlagsOffset = _RevisionNumberOffset + sizeof(ushort);
            _PublicKeyOffset = _FlagsOffset + sizeof(uint);
            _NameOffset = _PublicKeyOffset + blobHeapRefSize;
            _CultureOffset = _NameOffset + stringHeapRefSize;
            this.RowSize = _CultureOffset + stringHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal AssemblyHashAlgorithm GetHashAlgorithm()
        {
            Debug.Assert(NumberOfRows == 1);
            return (AssemblyHashAlgorithm)this.Block.PeekUInt32(_HashAlgIdOffset);
        }

        internal Version GetVersion()
        {
            Debug.Assert(NumberOfRows == 1);
            return new Version(
                this.Block.PeekUInt16(_MajorVersionOffset),
                this.Block.PeekUInt16(_MinorVersionOffset),
                this.Block.PeekUInt16(_BuildNumberOffset),
                this.Block.PeekUInt16(_RevisionNumberOffset));
        }

        internal AssemblyFlags GetFlags()
        {
            Debug.Assert(NumberOfRows == 1);
            return (AssemblyFlags)this.Block.PeekUInt32(_FlagsOffset);
        }

        internal BlobHandle GetPublicKey()
        {
            Debug.Assert(NumberOfRows == 1);
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(_PublicKeyOffset, _IsBlobHeapRefSizeSmall));
        }

        internal StringHandle GetName()
        {
            Debug.Assert(NumberOfRows == 1);
            return StringHandle.FromOffset(this.Block.PeekHeapReference(_NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetCulture()
        {
            Debug.Assert(NumberOfRows == 1);
            return StringHandle.FromOffset(this.Block.PeekHeapReference(_CultureOffset, _IsStringHeapRefSizeSmall));
        }
    }

    internal readonly struct AssemblyProcessorTableReader
    {
        internal readonly int NumberOfRows;
        private readonly int _ProcessorOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal AssemblyProcessorTableReader(
            int numberOfRows,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _ProcessorOffset = 0;
            this.RowSize = _ProcessorOffset + sizeof(uint);
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }
    }

    internal readonly struct AssemblyOSTableReader
    {
        internal readonly int NumberOfRows;
        private readonly int _OSPlatformIdOffset;
        private readonly int _OSMajorVersionIdOffset;
        private readonly int _OSMinorVersionIdOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal AssemblyOSTableReader(
            int numberOfRows,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _OSPlatformIdOffset = 0;
            _OSMajorVersionIdOffset = _OSPlatformIdOffset + sizeof(uint);
            _OSMinorVersionIdOffset = _OSMajorVersionIdOffset + sizeof(uint);
            this.RowSize = _OSMinorVersionIdOffset + sizeof(uint);
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }
    }

    internal readonly struct AssemblyRefTableReader
    {
        /// <summary>
        /// In CLI metadata equal to the actual number of entries in AssemblyRef table.
        /// In WinMD metadata it includes synthesized AssemblyRefs in addition.
        /// </summary>
        internal readonly int NumberOfNonVirtualRows;
        internal readonly int NumberOfVirtualRows;

        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _MajorVersionOffset;
        private readonly int _MinorVersionOffset;
        private readonly int _BuildNumberOffset;
        private readonly int _RevisionNumberOffset;
        private readonly int _FlagsOffset;
        private readonly int _PublicKeyOrTokenOffset;
        private readonly int _NameOffset;
        private readonly int _CultureOffset;
        private readonly int _HashValueOffset;
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

            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _MajorVersionOffset = 0;
            _MinorVersionOffset = _MajorVersionOffset + sizeof(ushort);
            _BuildNumberOffset = _MinorVersionOffset + sizeof(ushort);
            _RevisionNumberOffset = _BuildNumberOffset + sizeof(ushort);
            _FlagsOffset = _RevisionNumberOffset + sizeof(ushort);
            _PublicKeyOrTokenOffset = _FlagsOffset + sizeof(uint);
            _NameOffset = _PublicKeyOrTokenOffset + blobHeapRefSize;
            _CultureOffset = _NameOffset + stringHeapRefSize;
            _HashValueOffset = _CultureOffset + stringHeapRefSize;
            this.RowSize = _HashValueOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal Version GetVersion(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return new Version(
                this.Block.PeekUInt16(rowOffset + _MajorVersionOffset),
                this.Block.PeekUInt16(rowOffset + _MinorVersionOffset),
                this.Block.PeekUInt16(rowOffset + _BuildNumberOffset),
                this.Block.PeekUInt16(rowOffset + _RevisionNumberOffset));
        }

        internal AssemblyFlags GetFlags(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return (AssemblyFlags)this.Block.PeekUInt32(rowOffset + _FlagsOffset);
        }

        internal BlobHandle GetPublicKeyOrToken(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _PublicKeyOrTokenOffset, _IsBlobHeapRefSizeSmall));
        }

        internal StringHandle GetName(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetCulture(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _CultureOffset, _IsStringHeapRefSizeSmall));
        }

        internal BlobHandle GetHashValue(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _HashValueOffset, _IsBlobHeapRefSizeSmall));
        }
    }

    internal readonly struct AssemblyRefProcessorTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsAssemblyRefTableRowSizeSmall;
        private readonly int _ProcessorOffset;
        private readonly int _AssemblyRefOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal AssemblyRefProcessorTableReader(
            int numberOfRows,
            int assemblyRefTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsAssemblyRefTableRowSizeSmall = assemblyRefTableRowRefSize == 2;
            _ProcessorOffset = 0;
            _AssemblyRefOffset = _ProcessorOffset + sizeof(uint);
            this.RowSize = _AssemblyRefOffset + assemblyRefTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }
    }

    internal readonly struct AssemblyRefOSTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsAssemblyRefTableRowRefSizeSmall;
        private readonly int _OSPlatformIdOffset;
        private readonly int _OSMajorVersionIdOffset;
        private readonly int _OSMinorVersionIdOffset;
        private readonly int _AssemblyRefOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal AssemblyRefOSTableReader(
            int numberOfRows,
            int assemblyRefTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsAssemblyRefTableRowRefSizeSmall = assemblyRefTableRowRefSize == 2;
            _OSPlatformIdOffset = 0;
            _OSMajorVersionIdOffset = _OSPlatformIdOffset + sizeof(uint);
            _OSMinorVersionIdOffset = _OSMajorVersionIdOffset + sizeof(uint);
            _AssemblyRefOffset = _OSMinorVersionIdOffset + sizeof(uint);
            this.RowSize = _AssemblyRefOffset + assemblyRefTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }
    }

    internal readonly struct FileTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _FlagsOffset;
        private readonly int _NameOffset;
        private readonly int _HashValueOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal FileTableReader(
            int numberOfRows,
            int stringHeapRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _FlagsOffset = 0;
            _NameOffset = _FlagsOffset + sizeof(uint);
            _HashValueOffset = _NameOffset + stringHeapRefSize;
            this.RowSize = _HashValueOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal BlobHandle GetHashValue(AssemblyFileHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _HashValueOffset, _IsBlobHeapRefSizeSmall));
        }

        internal uint GetFlags(AssemblyFileHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + _FlagsOffset);
        }

        internal StringHandle GetName(AssemblyFileHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }
    }

    internal readonly struct ExportedTypeTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsImplementationRefSizeSmall;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly int _FlagsOffset;
        private readonly int _TypeDefIdOffset;
        private readonly int _TypeNameOffset;
        private readonly int _TypeNamespaceOffset;
        private readonly int _ImplementationOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ExportedTypeTableReader(
            int numberOfRows,
            int implementationRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsImplementationRefSizeSmall = implementationRefSize == 2;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _FlagsOffset = 0;
            _TypeDefIdOffset = _FlagsOffset + sizeof(uint);
            _TypeNameOffset = _TypeDefIdOffset + sizeof(uint);
            _TypeNamespaceOffset = _TypeNameOffset + stringHeapRefSize;
            _ImplementationOffset = _TypeNamespaceOffset + stringHeapRefSize;
            this.RowSize = _ImplementationOffset + implementationRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal StringHandle GetTypeName(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _TypeNameOffset, _IsStringHeapRefSizeSmall));
        }

        internal StringHandle GetTypeNamespaceString(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _TypeNamespaceOffset, _IsStringHeapRefSizeSmall));
        }

        internal NamespaceDefinitionHandle GetTypeNamespace(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return NamespaceDefinitionHandle.FromFullNameOffset(this.Block.PeekHeapReference(rowOffset + _TypeNamespaceOffset, _IsStringHeapRefSizeSmall));
        }

        internal EntityHandle GetImplementation(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return ImplementationTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _ImplementationOffset, _IsImplementationRefSizeSmall));
        }

        internal TypeAttributes GetFlags(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return (TypeAttributes)this.Block.PeekUInt32(rowOffset + _FlagsOffset);
        }

        internal int GetTypeDefId(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekInt32(rowOffset + _TypeDefIdOffset);
        }

        internal int GetNamespace(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return this.Block.PeekReference(rowOffset + _TypeNamespaceOffset, _IsStringHeapRefSizeSmall);
        }
    }

    internal readonly struct ManifestResourceTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsImplementationRefSizeSmall;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly int _OffsetOffset;
        private readonly int _FlagsOffset;
        private readonly int _NameOffset;
        private readonly int _ImplementationOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal ManifestResourceTableReader(
            int numberOfRows,
            int implementationRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsImplementationRefSizeSmall = implementationRefSize == 2;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _OffsetOffset = 0;
            _FlagsOffset = _OffsetOffset + sizeof(uint);
            _NameOffset = _FlagsOffset + sizeof(uint);
            _ImplementationOffset = _NameOffset + stringHeapRefSize;
            this.RowSize = _ImplementationOffset + implementationRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal StringHandle GetName(ManifestResourceHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal EntityHandle GetImplementation(ManifestResourceHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return ImplementationTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _ImplementationOffset, _IsImplementationRefSizeSmall));
        }

        internal uint GetOffset(ManifestResourceHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return this.Block.PeekUInt32(rowOffset + _OffsetOffset);
        }

        internal ManifestResourceAttributes GetFlags(ManifestResourceHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return (ManifestResourceAttributes)this.Block.PeekUInt32(rowOffset + _FlagsOffset);
        }
    }

    internal readonly struct NestedClassTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsTypeDefTableRowRefSizeSmall;
        private readonly int _NestedClassOffset;
        private readonly int _EnclosingClassOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal NestedClassTableReader(
            int numberOfRows,
            bool declaredSorted,
            int typeDefTableRowRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset
        )
        {
            this.NumberOfRows = numberOfRows;
            _IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
            _NestedClassOffset = 0;
            _EnclosingClassOffset = _NestedClassOffset + typeDefTableRowRefSize;
            this.RowSize = _EnclosingClassOffset + typeDefTableRowRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.NestedClass);
            }
        }

        internal TypeDefinitionHandle GetNestedClass(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _NestedClassOffset, _IsTypeDefTableRowRefSizeSmall));
        }

        internal TypeDefinitionHandle GetEnclosingClass(int rowId)
        {
            int rowOffset = (rowId - 1) * this.RowSize;
            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowOffset + _EnclosingClassOffset, _IsTypeDefTableRowRefSizeSmall));
        }

        internal TypeDefinitionHandle FindEnclosingType(TypeDefinitionHandle nestedTypeDef)
        {
            int rowNumber =
              this.Block.BinarySearchReference(
                this.NumberOfRows,
                this.RowSize,
                _NestedClassOffset,
                (uint)nestedTypeDef.RowId,
                _IsTypeDefTableRowRefSizeSmall);

            if (rowNumber == -1)
            {
                return default(TypeDefinitionHandle);
            }

            return TypeDefinitionHandle.FromRowId(this.Block.PeekReference(rowNumber * this.RowSize + _EnclosingClassOffset, _IsTypeDefTableRowRefSizeSmall));
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _NestedClassOffset, _IsTypeDefTableRowRefSizeSmall);
        }
    }

    internal readonly struct GenericParamTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsTypeOrMethodDefRefSizeSmall;
        private readonly bool _IsStringHeapRefSizeSmall;
        private readonly int _NumberOffset;
        private readonly int _FlagsOffset;
        private readonly int _OwnerOffset;
        private readonly int _NameOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal GenericParamTableReader(
            int numberOfRows,
            bool declaredSorted,
            int typeOrMethodDefRefSize,
            int stringHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsTypeOrMethodDefRefSizeSmall = typeOrMethodDefRefSize == 2;
            _IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
            _NumberOffset = 0;
            _FlagsOffset = _NumberOffset + sizeof(ushort);
            _OwnerOffset = _FlagsOffset + sizeof(ushort);
            _NameOffset = _OwnerOffset + typeOrMethodDefRefSize;
            this.RowSize = _NameOffset + stringHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.GenericParam);
            }
        }

        internal ushort GetNumber(GenericParameterHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return this.Block.PeekUInt16(rowOffset + _NumberOffset);
        }

        internal GenericParameterAttributes GetFlags(GenericParameterHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return (GenericParameterAttributes)this.Block.PeekUInt16(rowOffset + _FlagsOffset);
        }

        internal StringHandle GetName(GenericParameterHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return StringHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _NameOffset, _IsStringHeapRefSizeSmall));
        }

        internal EntityHandle GetOwner(GenericParameterHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return TypeOrMethodDefTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _OwnerOffset, _IsTypeOrMethodDefRefSizeSmall));
        }

        internal GenericParameterHandleCollection FindGenericParametersForType(TypeDefinitionHandle typeDef)
        {
            ushort count = 0;
            uint searchCodedTag = TypeOrMethodDefTag.ConvertTypeDefRowIdToTag(typeDef);
            int startRid = this.BinarySearchTag(searchCodedTag, ref count);

            return new GenericParameterHandleCollection(startRid, count);
        }

        internal GenericParameterHandleCollection FindGenericParametersForMethod(MethodDefinitionHandle methodDef)
        {
            ushort count = 0;
            uint searchCodedTag = TypeOrMethodDefTag.ConvertMethodDefToTag(methodDef);
            int startRid = this.BinarySearchTag(searchCodedTag, ref count);

            return new GenericParameterHandleCollection(startRid, count);
        }

        private int BinarySearchTag(uint searchCodedTag, ref ushort genericParamCount)
        {
            int startRowNumber, endRowNumber;
            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                _OwnerOffset,
                searchCodedTag,
                _IsTypeOrMethodDefRefSizeSmall,
                out startRowNumber,
                out endRowNumber);

            if (startRowNumber == -1)
            {
                genericParamCount = 0;
                return 0;
            }

            genericParamCount = (ushort)(endRowNumber - startRowNumber + 1);
            return startRowNumber + 1;
        }

        private bool CheckSorted()
        {
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _OwnerOffset, _IsTypeOrMethodDefRefSizeSmall);
        }
    }

    internal readonly struct MethodSpecTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsMethodDefOrRefRefSizeSmall;
        private readonly bool _IsBlobHeapRefSizeSmall;
        private readonly int _MethodOffset;
        private readonly int _InstantiationOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal MethodSpecTableReader(
            int numberOfRows,
            int methodDefOrRefRefSize,
            int blobHeapRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsMethodDefOrRefRefSizeSmall = methodDefOrRefRefSize == 2;
            _IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
            _MethodOffset = 0;
            _InstantiationOffset = _MethodOffset + methodDefOrRefRefSize;
            this.RowSize = _InstantiationOffset + blobHeapRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
        }

        internal EntityHandle GetMethod(MethodSpecificationHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return MethodDefOrRefTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _MethodOffset, _IsMethodDefOrRefRefSizeSmall));
        }

        internal BlobHandle GetInstantiation(MethodSpecificationHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return BlobHandle.FromOffset(this.Block.PeekHeapReference(rowOffset + _InstantiationOffset, _IsBlobHeapRefSizeSmall));
        }
    }

    internal readonly struct GenericParamConstraintTableReader
    {
        internal readonly int NumberOfRows;
        private readonly bool _IsGenericParamTableRowRefSizeSmall;
        private readonly bool _IsTypeDefOrRefRefSizeSmall;
        private readonly int _OwnerOffset;
        private readonly int _ConstraintOffset;
        internal readonly int RowSize;
        internal readonly MemoryBlock Block;

        internal GenericParamConstraintTableReader(
            int numberOfRows,
            bool declaredSorted,
            int genericParamTableRowRefSize,
            int typeDefOrRefRefSize,
            MemoryBlock containingBlock,
            int containingBlockOffset)
        {
            this.NumberOfRows = numberOfRows;
            _IsGenericParamTableRowRefSizeSmall = genericParamTableRowRefSize == 2;
            _IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
            _OwnerOffset = 0;
            _ConstraintOffset = _OwnerOffset + genericParamTableRowRefSize;
            this.RowSize = _ConstraintOffset + typeDefOrRefRefSize;
            this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);

            if (!declaredSorted && !CheckSorted())
            {
                Throw.TableNotSorted(TableIndex.GenericParamConstraint);
            }
        }

        internal GenericParameterConstraintHandleCollection FindConstraintsForGenericParam(GenericParameterHandle genericParameter)
        {
            int startRowNumber, endRowNumber;
            this.Block.BinarySearchReferenceRange(
                this.NumberOfRows,
                this.RowSize,
                _OwnerOffset,
                (uint)genericParameter.RowId,
                _IsGenericParamTableRowRefSizeSmall,
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
            return this.Block.IsOrderedByReferenceAscending(this.RowSize, _OwnerOffset, _IsGenericParamTableRowRefSizeSmall);
        }

        internal EntityHandle GetConstraint(GenericParameterConstraintHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return TypeDefOrRefTag.ConvertToHandle(this.Block.PeekTaggedReference(rowOffset + _ConstraintOffset, _IsTypeDefOrRefRefSizeSmall));
        }

        internal GenericParameterHandle GetOwner(GenericParameterConstraintHandle handle)
        {
            int rowOffset = (handle.RowId - 1) * this.RowSize;
            return GenericParameterHandle.FromRowId(this.Block.PeekReference(rowOffset + _OwnerOffset, _IsGenericParamTableRowRefSizeSmall));
        }
    }
}
