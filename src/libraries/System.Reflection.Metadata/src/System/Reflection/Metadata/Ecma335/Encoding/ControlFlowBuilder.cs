// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.Metadata.Ecma335
{
    public sealed class ControlFlowBuilder
    {
        // internal for testing:
        internal readonly struct BranchInfo
        {
            // The offset to the label operand inside the instruction.
            internal readonly int OperandOffset;
            internal readonly LabelHandle Label;
            // Label offsets are calculated from the end of the instruction that contains them.
            // This value contains the displacement from the start of the label operand
            // to the end of the instruction. It is equal to one on short branches,
            // four on long branches and bigger on the switch instruction.
            private readonly int _instructionEndDisplacement;

            // The following two fields are used for error reporting and tests.

            // The offset to the start of the instruction.
            internal readonly int ILOffset;
            internal readonly ILOpCode OpCode;

            internal bool IsShortBranch => _instructionEndDisplacement == 1;
            internal int OperandSize => Math.Min(_instructionEndDisplacement, 4);

            internal BranchInfo(int operandOffset, LabelHandle label, int instructionEndDisplacement, int ilOffset, ILOpCode opCode)
            {
                OperandOffset = operandOffset;
                Label = label;
                _instructionEndDisplacement = instructionEndDisplacement;
                ILOffset = ilOffset;
                OpCode = opCode;
            }

            internal int GetBranchDistance(List<int> labels)
            {
                int labelTargetOffset = labels[Label.Id - 1];
                if (labelTargetOffset < 0)
                {
                    Throw.InvalidOperation_LabelNotMarked(Label.Id);
                }

                int distance = labelTargetOffset - (OperandOffset + _instructionEndDisplacement);

                if (IsShortBranch && unchecked((sbyte)distance) != distance)
                {
                    // We could potentially implement algorithm that automatically fixes up branch instructions to accommodate for bigger distances (short vs long),
                    // however an optimal algorithm would be rather complex (something like: calculate topological ordering of crossing branch instructions
                    // and then use fixed point to eliminate cycles). If the caller doesn't care about optimal IL size they can use long branches whenever the
                    // distance is unknown upfront. If they do they probably implement more sophisticated algorithm for IL layout optimization already.
                    throw new InvalidOperationException(SR.Format(SR.DistanceBetweenInstructionAndLabelTooBig, OpCode, ILOffset, distance));
                }

                return distance;
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

        private readonly List<BranchInfo> _branches;
        private readonly List<int> _labels;
        private List<ExceptionHandlerInfo>? _lazyExceptionHandlers;

        public ControlFlowBuilder()
        {
            _branches = new List<BranchInfo>();
            _labels = new List<int>();
        }

        /// <summary>
        /// Clears the object's internal state, allowing the same instance to be reused.
        /// </summary>
        public void Clear()
        {
            _branches.Clear();
            _labels.Clear();
            _lazyExceptionHandlers?.Clear();
            RemainingSwitchBranches = 0;
        }

        internal LabelHandle AddLabel()
        {
            ValidateNotInSwitch();
            _labels.Add(-1);
            return new LabelHandle(_labels.Count);
        }

        internal void AddBranch(int operandOffset, LabelHandle label, int instructionEndDisplacement, int ilOffset, ILOpCode opCode)
        {
            Debug.Assert(operandOffset >= 0);
            Debug.Assert(_branches.Count == 0 || operandOffset > _branches[_branches.Count - 1].OperandOffset);
            ValidateLabel(label, nameof(label));
#if DEBUG
            switch (instructionEndDisplacement)
            {
                case 1:
                    Debug.Assert(opCode.GetBranchOperandSize() == 1);
                    break;
                case 4:
                    Debug.Assert(opCode == ILOpCode.Switch || opCode.GetBranchOperandSize() == 4);
                    break;
                default:
                    Debug.Assert(instructionEndDisplacement > 4 && instructionEndDisplacement % 4 == 0 && opCode == ILOpCode.Switch);
                    break;
            }
#endif
            _branches.Add(new BranchInfo(operandOffset, label, instructionEndDisplacement, ilOffset, opCode));
        }

        internal void MarkLabel(int ilOffset, LabelHandle label)
        {
            Debug.Assert(ilOffset >= 0);
            ValidateNotInSwitch();
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
            ValidateNotInSwitch();

            _lazyExceptionHandlers ??= new List<ExceptionHandlerInfo>();

            _lazyExceptionHandlers.Add(new ExceptionHandlerInfo(kind, tryStart, tryEnd, handlerStart, handlerEnd, filterStart, catchType));
        }

        // internal for testing:
        internal IEnumerable<BranchInfo> Branches => _branches;

        // internal for testing:
        internal IEnumerable<int> Labels => _labels;

        internal int BranchCount => _branches.Count;

        internal int ExceptionHandlerCount => _lazyExceptionHandlers?.Count ?? 0;

        internal int RemainingSwitchBranches { get; set; }

        internal void ValidateNotInSwitch()
        {
            if (RemainingSwitchBranches > 0)
            {
                Throw.InvalidOperation(SR.SwitchInstructionEncoderTooFewBranches);
            }
        }

        internal void SwitchBranchAdded()
        {
            if (RemainingSwitchBranches == 0)
            {
                Throw.InvalidOperation(SR.SwitchInstructionEncoderTooManyBranches);
            }
            RemainingSwitchBranches--;
        }

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
                    int chunkSize = Math.Min(branch.OperandOffset - srcOffset, srcBlob.Length - srcBlobOffset);
                    dstBuilder.WriteBytes(srcBlob.Buffer, srcBlobOffset, chunkSize);
                    srcOffset += chunkSize;
                    srcBlobOffset += chunkSize;

                    // there is no branch left in the blob:
                    if (srcBlobOffset == srcBlob.Length)
                    {
                        srcBlobOffset = 0;
                        break;
                    }

                    int operandSize = branch.OperandSize;
                    bool isShortInstruction = branch.IsShortBranch;

                    // Note: the 4B operand is contiguous since we wrote it via BlobBuilder.WriteInt32()
                    Debug.Assert(
                        srcBlobOffset == srcBlob.Length ||
                        (isShortInstruction ?
                           srcBlob.Buffer[srcBlobOffset] == 0xff :
                           BitConverter.ToUInt32(srcBlob.Buffer, srcBlobOffset) == 0xffffffff));

                    int branchDistance = branch.GetBranchDistance(_labels);

                    // write branch operand:
                    if (isShortInstruction)
                    {
                        dstBuilder.WriteSByte((sbyte)branchDistance);
                    }
                    else
                    {
                        dstBuilder.WriteInt32(branchDistance);
                    }

                    srcOffset += operandSize;

                    // next branch:
                    branchIndex++;
                    if (branchIndex == _branches.Count)
                    {
                        // We have processed all branches. The MaxValue will cause the rest
                        // of the IL stream to be directly copied to the destination blob.
                        branch = new BranchInfo(operandOffset: int.MaxValue, label: default,
                            instructionEndDisplacement: default, ilOffset: default, opCode: default);
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

                    // skip fake branch operand:
                    srcBlobOffset += operandSize;
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

                int catchTokenOrOffset = handler.Kind switch
                {
                    ExceptionRegionKind.Catch => MetadataTokens.GetToken(handler.CatchType),
                    ExceptionRegionKind.Filter => GetLabelOffsetChecked(handler.FilterStart),
                    _ => 0,
                };

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
