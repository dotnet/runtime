// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335
{
    public sealed class ControlFlowBuilder
    {
        // internal for testing:
        internal readonly struct BranchInfo
        {
            internal readonly int ILOffset;
            internal readonly LabelHandle Label;
            private readonly byte _opCode;

            internal ILOpCode OpCode => (ILOpCode)_opCode;

            internal BranchInfo(int ilOffset, LabelHandle label, ILOpCode opCode)
            {
                ILOffset = ilOffset;
                Label = label;
                _opCode = (byte)opCode;
            }

            internal bool IsShortBranchDistance(ImmutableArray<int>.Builder labels, out int distance)
            {
                const int shortBranchSize = 2;
                const int longBranchSize = 5;

                int labelTargetOffset = labels[Label.Id - 1];
                if (labelTargetOffset < 0)
                {
                    Throw.InvalidOperation_LabelNotMarked(Label.Id);
                }

                distance = labelTargetOffset - (ILOffset + shortBranchSize);
                if (unchecked((sbyte)distance) == distance)
                {
                    return true;
                }

                distance = labelTargetOffset - (ILOffset + longBranchSize);
                return false;
            }
        }

        internal readonly struct ExceptionHandlerInfo
        {
            public readonly ExceptionRegionKind Kind;
            public readonly LabelHandle TryStart, TryEnd, HandlerStart, HandlerEnd, FilterStart;
            public readonly EntityHandle CatchType;

            public ExceptionHandlerInfo(
                ExceptionRegionKind kind,
                LabelHandle tryStart, 
                LabelHandle tryEnd,
                LabelHandle handlerStart, 
                LabelHandle handlerEnd, 
                LabelHandle filterStart, 
                EntityHandle catchType)
            {
                Kind = kind;
                TryStart = tryStart;
                TryEnd = tryEnd;
                HandlerStart = handlerStart;
                HandlerEnd = handlerEnd;
                FilterStart = filterStart;
                CatchType = catchType;
            }
        }

        private readonly ImmutableArray<BranchInfo>.Builder _branches;
        private readonly ImmutableArray<int>.Builder _labels;
        private ImmutableArray<ExceptionHandlerInfo>.Builder _lazyExceptionHandlers;

        public ControlFlowBuilder()
        {
            _branches = ImmutableArray.CreateBuilder<BranchInfo>();
            _labels = ImmutableArray.CreateBuilder<int>();
        }

        internal void Clear()
        {
            _branches.Clear();
            _labels.Clear();
            _lazyExceptionHandlers?.Clear();
        }

        internal LabelHandle AddLabel()
        {
            _labels.Add(-1);
            return new LabelHandle(_labels.Count);
        }

        internal void AddBranch(int ilOffset, LabelHandle label, ILOpCode opCode)
        {
            Debug.Assert(ilOffset >= 0);
            Debug.Assert(_branches.Count == 0 || ilOffset > _branches.Last().ILOffset);
            ValidateLabel(label, nameof(label));
            _branches.Add(new BranchInfo(ilOffset, label, opCode));
        }

        internal void MarkLabel(int ilOffset, LabelHandle label)
        {
            Debug.Assert(ilOffset >= 0);
            ValidateLabel(label, nameof(label));
            _labels[label.Id - 1] = ilOffset;
        }

        private int GetLabelOffsetChecked(LabelHandle label)
        {
            int offset = _labels[label.Id - 1];
            if (offset < 0)
            {
                Throw.InvalidOperation_LabelNotMarked(label.Id);
            }

            return offset;
        }

        private void ValidateLabel(LabelHandle label, string parameterName)
        {
            if (label.IsNil)
            {
                Throw.ArgumentNull(parameterName);
            }

            if (label.Id > _labels.Count)
            {
                Throw.LabelDoesntBelongToBuilder(parameterName);
            }
        }

        /// <summary>
        /// Adds finally region.
        /// </summary>
        /// <param name="tryStart">Label marking the first instruction of the try block.</param>
        /// <param name="tryEnd">Label marking the instruction immediately following the try block.</param>
        /// <param name="handlerStart">Label marking the first instruction of the handler.</param>
        /// <param name="handlerEnd">Label marking the instruction immediately following the handler.</param>
        /// <returns>Encoder for the next clause.</returns>
        /// <exception cref="ArgumentException">A label was not defined by an instruction encoder this builder is associated with.</exception>
        /// <exception cref="ArgumentNullException">A label has default value.</exception>
        public void AddFinallyRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd) => 
            AddExceptionRegion(ExceptionRegionKind.Finally, tryStart, tryEnd, handlerStart, handlerEnd);

        /// <summary>
        /// Adds fault region.
        /// </summary>
        /// <param name="tryStart">Label marking the first instruction of the try block.</param>
        /// <param name="tryEnd">Label marking the instruction immediately following the try block.</param>
        /// <param name="handlerStart">Label marking the first instruction of the handler.</param>
        /// <param name="handlerEnd">Label marking the instruction immediately following the handler.</param>
        /// <exception cref="ArgumentException">A label was not defined by an instruction encoder this builder is associated with.</exception>
        /// <exception cref="ArgumentNullException">A label has default value.</exception>
        public void AddFaultRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd) =>
            AddExceptionRegion(ExceptionRegionKind.Fault, tryStart, tryEnd, handlerStart, handlerEnd);

        /// <summary>
        /// Adds catch region.
        /// </summary>
        /// <param name="tryStart">Label marking the first instruction of the try block.</param>
        /// <param name="tryEnd">Label marking the instruction immediately following the try block.</param>
        /// <param name="handlerStart">Label marking the first instruction of the handler.</param>
        /// <param name="handlerEnd">Label marking the instruction immediately following the handler.</param>
        /// <param name="catchType">The type of exception to be caught: <see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/> or <see cref="TypeSpecificationHandle"/>.</param>
        /// <exception cref="ArgumentException">A label was not defined by an instruction encoder this builder is associated with.</exception>
        /// <exception cref="ArgumentException"><paramref name="catchType"/> is not a valid type handle.</exception>
        /// <exception cref="ArgumentNullException">A label has default value.</exception>
        public void AddCatchRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd, EntityHandle catchType)
        {
            if (!ExceptionRegionEncoder.IsValidCatchTypeHandle(catchType))
            {
                Throw.InvalidArgument_Handle(nameof(catchType));
            }

            AddExceptionRegion(ExceptionRegionKind.Catch, tryStart, tryEnd, handlerStart, handlerEnd, catchType: catchType);
        }

        /// <summary>
        /// Adds catch region.
        /// </summary>
        /// <param name="tryStart">Label marking the first instruction of the try block.</param>
        /// <param name="tryEnd">Label marking the instruction immediately following the try block.</param>
        /// <param name="handlerStart">Label marking the first instruction of the handler.</param>
        /// <param name="handlerEnd">Label marking the instruction immediately following the handler.</param>
        /// <param name="filterStart">Label marking the first instruction of the filter block.</param>
        /// <exception cref="ArgumentException">A label was not defined by an instruction encoder this builder is associated with.</exception>
        /// <exception cref="ArgumentNullException">A label has default value.</exception>
        public void AddFilterRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd, LabelHandle filterStart)
        {
            ValidateLabel(filterStart, nameof(filterStart));
            AddExceptionRegion(ExceptionRegionKind.Filter, tryStart, tryEnd, handlerStart, handlerEnd, filterStart: filterStart);
        }

        private void AddExceptionRegion(
            ExceptionRegionKind kind, 
            LabelHandle tryStart, 
            LabelHandle tryEnd, 
            LabelHandle handlerStart, 
            LabelHandle handlerEnd, 
            LabelHandle filterStart = default(LabelHandle),
            EntityHandle catchType = default(EntityHandle))
        {
            ValidateLabel(tryStart, nameof(tryStart));
            ValidateLabel(tryEnd, nameof(tryEnd));
            ValidateLabel(handlerStart, nameof(handlerStart));
            ValidateLabel(handlerEnd, nameof(handlerEnd));

            if (_lazyExceptionHandlers == null)
            {
                _lazyExceptionHandlers = ImmutableArray.CreateBuilder<ExceptionHandlerInfo>();
            }

            _lazyExceptionHandlers.Add(new ExceptionHandlerInfo(kind, tryStart, tryEnd, handlerStart, handlerEnd, filterStart, catchType));
        }

        // internal for testing:
        internal IEnumerable<BranchInfo> Branches => _branches;

        // internal for testing:
        internal IEnumerable<int> Labels => _labels;

        internal int BranchCount => _branches.Count;

        internal int ExceptionHandlerCount => _lazyExceptionHandlers?.Count ?? 0;

        /// <exception cref="InvalidOperationException" />
        internal void CopyCodeAndFixupBranches(BlobBuilder srcBuilder, BlobBuilder dstBuilder)
        {
            var branch = _branches[0];
            int branchIndex = 0;

            // offset within the source builder
            int srcOffset = 0;
            
            // current offset within the current source blob
            int srcBlobOffset = 0;

            foreach (Blob srcBlob in srcBuilder.GetBlobs())
            {
                Debug.Assert(
                    srcBlobOffset == 0 || 
                    srcBlobOffset == 1 && srcBlob.Buffer[0] == 0xff ||
                    srcBlobOffset == 4 && srcBlob.Buffer[0] == 0xff && srcBlob.Buffer[1] == 0xff && srcBlob.Buffer[2] == 0xff && srcBlob.Buffer[3] == 0xff);

                while (true)
                {
                    // copy bytes preceding the next branch, or till the end of the blob:
                    int chunkSize = Math.Min(branch.ILOffset - srcOffset, srcBlob.Length - srcBlobOffset);
                    dstBuilder.WriteBytes(srcBlob.Buffer, srcBlobOffset, chunkSize);
                    srcOffset += chunkSize;
                    srcBlobOffset += chunkSize;

                    // there is no branch left in the blob:
                    if (srcBlobOffset == srcBlob.Length)
                    {
                        srcBlobOffset = 0;
                        break;
                    }

                    Debug.Assert(srcBlob.Buffer[srcBlobOffset] == (byte)branch.OpCode);

                    int operandSize = branch.OpCode.GetBranchOperandSize();
                    bool isShortInstruction = operandSize == 1;

                    // Note: the 4B operand is contiguous since we wrote it via BlobBuilder.WriteInt32()
                    Debug.Assert(
                        srcBlobOffset + 1 == srcBlob.Length || 
                        (isShortInstruction ? 
                           srcBlob.Buffer[srcBlobOffset + 1] == 0xff :
                           BitConverter.ToUInt32(srcBlob.Buffer, srcBlobOffset + 1) == 0xffffffff));

                    // write branch opcode:
                    dstBuilder.WriteByte(srcBlob.Buffer[srcBlobOffset]);

                    // write branch operand:
                    int branchDistance;
                    bool isShortDistance = branch.IsShortBranchDistance(_labels, out branchDistance);

                    if (isShortInstruction && !isShortDistance)
                    {
                        // We could potentially implement algortihm that automatically fixes up the branch instructions as well to accomodate bigger distances,
                        // however an optimal algorithm would be rather complex (something like: calculate topological ordering of crossing branch instructions 
                        // and then use fixed point to eliminate cycles). If the caller doesn't care about optimal IL size they can use long branches whenever the 
                        // distance is unknown upfront. If they do they probably already implement more sophisticad algorithm for IL layout optimization already. 
                        throw new InvalidOperationException(SR.Format(SR.DistanceBetweenInstructionAndLabelTooBig, branch.OpCode, srcOffset, branchDistance));
                    }

                    if (isShortInstruction)
                    {
                        dstBuilder.WriteSByte((sbyte)branchDistance);
                    }
                    else
                    {
                        dstBuilder.WriteInt32(branchDistance);
                    }

                    srcOffset += sizeof(byte) + operandSize;

                    // next branch:
                    branchIndex++;
                    if (branchIndex == _branches.Count)
                    {
                        branch = new BranchInfo(int.MaxValue, default(LabelHandle), 0);
                    }
                    else
                    {
                        branch = _branches[branchIndex];
                    }

                    // the branch starts at the very end and its operand is in the next blob:
                    if (srcBlobOffset == srcBlob.Length - 1)
                    {
                        srcBlobOffset = operandSize;
                        break;
                    }

                    // skip fake branch instruction:
                    srcBlobOffset += sizeof(byte) + operandSize;
                }
            }
        }

        internal void SerializeExceptionTable(BlobBuilder builder)
        {
            if (_lazyExceptionHandlers == null || _lazyExceptionHandlers.Count == 0)
            {
                return;
            }

            var regionEncoder = ExceptionRegionEncoder.SerializeTableHeader(builder, _lazyExceptionHandlers.Count, HasSmallExceptionRegions());

            foreach (var handler in _lazyExceptionHandlers)
            {
                // Note that labels have been validated when added to the handler list,
                // they might not have been marked though.

                int tryStart = GetLabelOffsetChecked(handler.TryStart);
                int tryEnd = GetLabelOffsetChecked(handler.TryEnd);
                int handlerStart = GetLabelOffsetChecked(handler.HandlerStart);
                int handlerEnd = GetLabelOffsetChecked(handler.HandlerEnd);

                if (tryStart > tryEnd)
                {
                    Throw.InvalidOperation(SR.Format(SR.InvalidExceptionRegionBounds, tryStart, tryEnd));
                }

                if (handlerStart > handlerEnd)
                {
                    Throw.InvalidOperation(SR.Format(SR.InvalidExceptionRegionBounds, handlerStart, handlerEnd));
                }

                int catchTokenOrOffset;
                switch (handler.Kind)
                {
                    case ExceptionRegionKind.Catch:
                        catchTokenOrOffset = MetadataTokens.GetToken(handler.CatchType);
                        break;

                    case ExceptionRegionKind.Filter:
                        catchTokenOrOffset = GetLabelOffsetChecked(handler.FilterStart);
                        break;

                    default:
                        catchTokenOrOffset = 0;
                        break;
                }

                regionEncoder.AddUnchecked(
                    handler.Kind,
                    tryStart, 
                    tryEnd - tryStart,
                    handlerStart,
                    handlerEnd - handlerStart,
                    catchTokenOrOffset);
            }
        }

        private bool HasSmallExceptionRegions()
        {
            Debug.Assert(_lazyExceptionHandlers != null);

            if (!ExceptionRegionEncoder.IsSmallRegionCount(_lazyExceptionHandlers.Count))
            {
                return false;
            }

            foreach (var handler in _lazyExceptionHandlers)
            {
                if (!ExceptionRegionEncoder.IsSmallExceptionRegionFromBounds(GetLabelOffsetChecked(handler.TryStart), GetLabelOffsetChecked(handler.TryEnd)) ||
                    !ExceptionRegionEncoder.IsSmallExceptionRegionFromBounds(GetLabelOffsetChecked(handler.HandlerStart), GetLabelOffsetChecked(handler.HandlerEnd)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
