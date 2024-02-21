// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ILCompiler.DependencyAnalysis;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;
using MethodDebugInformation = Internal.IL.MethodDebugInformation;

namespace ILCompiler
{
    public class SubstitutedILProvider : ILProvider
    {
        private readonly ILProvider _nestedILProvider;
        private readonly SubstitutionProvider _substitutionProvider;

        public SubstitutedILProvider(ILProvider nestedILProvider, SubstitutionProvider substitutionProvider)
        {
            _nestedILProvider = nestedILProvider;
            _substitutionProvider = substitutionProvider;
        }

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            BodySubstitution substitution = _substitutionProvider.GetSubstitution(method);
            if (substitution != null)
            {
                return substitution.EmitIL(method);
            }

            // BEGIN TEMPORARY WORKAROUND
            //
            // The following lines should just be:
            // return _nestedILProvider.GetMethodIL(method);
            // But we want to allow this to be used as a general-purpose IL provider.
            //
            // Rewriting all IL has compilation throughput hit we don't want.
            MethodIL result = _nestedILProvider.GetMethodIL(method);
            if (result != null)
            {
                var resultDef = result.GetMethodILDefinition();
                if (resultDef != result)
                {
                    MethodIL newBodyDef = GetMethodILWithInlinedSubstitutions(result);

                    // If we didn't rewrite the body, we can keep the existing result.
                    if (newBodyDef != result)
                        result = new InstantiatedMethodIL(method, newBodyDef);
                }
                else
                {
                    result = GetMethodILWithInlinedSubstitutions(result);
                }
            }
            return result;
            // END TEMPORARY WORKAROUND
        }

        // Flags that we track for each byte of the IL instruction stream.
        [Flags]
        private enum OpcodeFlags : byte
        {
            // This offset is an instruction boundary.
            InstructionStart = 0x1,

            // Beginning of a basic block in the original IL stream
            BasicBlockStart = 0x2,

            // End of a basic block in the original IL stream
            EndBasicBlock = 0x4,

            // Beginning of a basic block in the rewritten stream
            // (Lets us avoid seeing lots of small basic blocks within eliminated chunks.)
            VisibleBasicBlockStart = 0x10,

            // This is a potential SR.get_SomeResourceString call.
            GetResourceStringCall = 0x20,

            // The instruction at this offset is reachable
            Mark = 0x80,
        }

        public MethodIL GetMethodILWithInlinedSubstitutions(MethodIL method)
        {
            // This attempts to find all basic blocks that are unreachable after applying the substitutions.
            //
            // On a high level, we first find all the basic blocks and instruction boundaries in the IL stream.
            // This is tracked in a sidecar `flags` array that has flags for each byte of the IL stream.
            //
            // Once we have all the basic blocks and instruction boundaries, we do a marking phase to mark
            // the reachable blocks. We use substitutions to tell us what's unreachable. We consider conditional
            // branches "interesting" and whenever we see one, we seek backwards in the IL instruction stream
            // to find the instruction that feeds it. We make sure we don't cross the basic block boundary while
            // doing that. If the conditional instruction is fed by known values (either through the substitutions
            // or because it's an IL constant), we simulate the result of the comparison and only mark
            // the taken branch. We also mark any associated EH regions.
            //
            // The "seek backwards to find what feeds the comparison" only works for a couple known instructions
            // (load constant, call). It can't e.g. skip over arguments to the call.
            //
            // We then do a pass to compute the offsets of instructions in a new instruction stream, where
            // the unreachable basic blocks are replaced with an infinite loop (`br $-2`).
            // Because of this rewriting, all the offsets will shift.
            //
            // Last step is the actual rewriting: we copy instructions from the source basic block, remapping
            // offsets for jumps, and replacing parts that are unreachable with the infinite loop.
            //
            // We also attempt to rewrite calls to SR.SomeResourceString accessors with string
            // literals looked up from the managed resources.

            // Do not attempt to inline resource strings if we only want to use resource keys.
            // The optimizations are not compatible.
            bool shouldInlineResourceStrings = _substitutionProvider.ShouldInlineResourceStrings;

            ILExceptionRegion[] ehRegions = method.GetExceptionRegions();
            byte[] methodBytes = method.GetILBytes();
            OpcodeFlags[] flags = new OpcodeFlags[methodBytes.Length];

            // Offset 0 is the first basic block
            Stack<int> offsetsToVisit = new Stack<int>();
            offsetsToVisit.Push(0);

            // Basic blocks also start around EH regions
            foreach (ILExceptionRegion ehRegion in ehRegions)
            {
                if (ehRegion.Kind == ILExceptionRegionKind.Filter)
                    offsetsToVisit.Push(ehRegion.FilterOffset);

                offsetsToVisit.Push(ehRegion.HandlerOffset);
            }

            // Identify basic blocks and instruction boundaries
            while (offsetsToVisit.TryPop(out int offset))
            {
                // If this was already visited, we're done
                if (flags[offset] != 0)
                {
                    // Also mark as basic block start in case this was a target of a backwards branch.
                    flags[offset] |= OpcodeFlags.BasicBlockStart;
                    continue;
                }

                flags[offset] |= OpcodeFlags.BasicBlockStart;

                // Read until we reach the end of the basic block
                ILReader reader = new ILReader(methodBytes, offset);
                while (reader.HasNext)
                {
                    offset = reader.Offset;
                    flags[offset] |= OpcodeFlags.InstructionStart;
                    ILOpcode opcode = reader.ReadILOpcode();
                    if (opcode >= ILOpcode.br_s && opcode <= ILOpcode.blt_un
                        || opcode == ILOpcode.leave || opcode == ILOpcode.leave_s)
                    {
                        int destination = reader.ReadBranchDestination(opcode);
                        offsetsToVisit.Push(destination);

                        if (opcode != ILOpcode.leave && opcode != ILOpcode.leave_s
                            && opcode != ILOpcode.br && opcode != ILOpcode.br_s)
                        {
                            // Branches not tested for above are conditional and the flow falls through.
                            offsetsToVisit.Push(reader.Offset);
                        }

                        flags[offset] |= OpcodeFlags.EndBasicBlock;
                    }
                    else if (opcode == ILOpcode.ret
                        || opcode == ILOpcode.endfilter
                        || opcode == ILOpcode.endfinally
                        || opcode == ILOpcode.throw_
                        || opcode == ILOpcode.rethrow
                        || opcode == ILOpcode.jmp)
                    {
                        // Ends basic block.
                        flags[offset] |= OpcodeFlags.EndBasicBlock;

                        reader.Skip(opcode);
                    }
                    else if (opcode == ILOpcode.switch_)
                    {
                        uint count = reader.ReadILUInt32();
                        int jmpBase = reader.Offset + (int)(4 * count);
                        for (uint i = 0; i < count; i++)
                        {
                            int destination = (int)reader.ReadILUInt32() + jmpBase;
                            offsetsToVisit.Push(destination);
                        }
                        // We fall through to the next basic block.
                        offsetsToVisit.Push(reader.Offset);
                        flags[offset] |= OpcodeFlags.EndBasicBlock;
                    }
                    else
                    {
                        reader.Skip(opcode);
                    }

                    if ((flags[offset] & OpcodeFlags.EndBasicBlock) != 0)
                    {
                        if (reader.HasNext)
                        {
                            // If the bytes following this basic block are not reachable from anywhere,
                            // the sweeping step would consider them to be part of the last instruction
                            // of the current basic block because of how instruction boundaries are identified.
                            // We wouldn't NOP them out if the current basic block is reachable.
                            //
                            // That's a problem for RyuJIT because RyuJIT looks at these bytes for... reasons.
                            //
                            // We can just do the same thing as RyuJIT and consider those a basic block.
                            offsetsToVisit.Push(reader.Offset);
                        }
                        break;
                    }
                }
            }

            bool hasGetResourceStringCall = false;

            // Mark all reachable basic blocks
            //
            // We also do another round of basic block marking to mark beginning of visible basic blocks
            // after dead branch elimination. This allows us to limit the number of potential small basic blocks
            // that are not interesting (because no code jumps to them anymore).
            offsetsToVisit.Push(0);
            while (offsetsToVisit.TryPop(out int offset))
            {
                // Mark as a basic block visible after constant propagation.
                flags[offset] |= OpcodeFlags.VisibleBasicBlockStart;

                // If this was already marked, we're done.
                if ((flags[offset] & OpcodeFlags.Mark) != 0)
                    continue;

                ILReader reader = new ILReader(methodBytes, offset);
                while (reader.HasNext)
                {
                    offset = reader.Offset;
                    flags[offset] |= OpcodeFlags.Mark;
                    ILOpcode opcode = reader.ReadILOpcode();

                    // Mark any applicable EH blocks
                    foreach (ILExceptionRegion ehRegion in ehRegions)
                    {
                        int delta = offset - ehRegion.TryOffset;
                        if (delta >= 0 && delta < ehRegion.TryLength)
                        {
                            if (ehRegion.Kind == ILExceptionRegionKind.Filter)
                            {
                                offsetsToVisit.Push(ehRegion.FilterOffset);

                                // Filter must end with endfilter, so ensure we don't accidentally remove it.
                                // ECMA-335 dictates that filter block starts at FilterOffset and ends at HandlerOffset.
                                int expectedEndfilterLocation = ehRegion.HandlerOffset - 2;
                                bool isValidFilter = expectedEndfilterLocation >= 0
                                    && (flags[expectedEndfilterLocation] & OpcodeFlags.InstructionStart) != 0
                                    && methodBytes[expectedEndfilterLocation] == (byte)ILOpcode.prefix1
                                    && methodBytes[expectedEndfilterLocation + 1] == unchecked((byte)ILOpcode.endfilter);
                                if (isValidFilter)
                                {
                                    flags[expectedEndfilterLocation] |= OpcodeFlags.VisibleBasicBlockStart | OpcodeFlags.Mark;
                                }
                            }

                            offsetsToVisit.Push(ehRegion.HandlerOffset);

                            // RyuJIT is going to look at this basic block even though it's unreachable.
                            // Consider it visible so that we replace it with an endless loop.
                            int handlerEnd = ehRegion.HandlerOffset + ehRegion.HandlerLength;
                            if (handlerEnd < flags.Length)
                                flags[handlerEnd] |= OpcodeFlags.VisibleBasicBlockStart;
                        }
                    }

                    // All branches are relevant to basic block tracking
                    if (opcode == ILOpcode.brfalse || opcode == ILOpcode.brfalse_s
                        || opcode == ILOpcode.brtrue || opcode == ILOpcode.brtrue_s)
                    {
                        int destination = reader.ReadBranchDestination(opcode);
                        if (!TryGetConstantArgument(method, methodBytes, flags, offset, 0, out int constant))
                        {
                            // Can't get the constant - both branches are live.
                            offsetsToVisit.Push(destination);
                            offsetsToVisit.Push(reader.Offset);
                        }
                        else if ((constant == 0 && (opcode == ILOpcode.brfalse || opcode == ILOpcode.brfalse_s))
                            || (constant != 0 && (opcode == ILOpcode.brtrue || opcode == ILOpcode.brtrue_s)))
                        {
                            // Only the "branch taken" is live.
                            // The fallthrough marks the beginning of a visible (but not live) basic block.
                            offsetsToVisit.Push(destination);
                            flags[reader.Offset] |= OpcodeFlags.VisibleBasicBlockStart;
                        }
                        else
                        {
                            // Only fallthrough is live.
                            // The "brach taken" marks the beginning of a visible (but not live) basic block.
                            flags[destination] |= OpcodeFlags.VisibleBasicBlockStart;
                            offsetsToVisit.Push(reader.Offset);
                        }
                    }
                    else if (opcode == ILOpcode.beq || opcode == ILOpcode.beq_s
                        || opcode == ILOpcode.bne_un || opcode == ILOpcode.bne_un_s)
                    {
                        int destination = reader.ReadBranchDestination(opcode);
                        if (!TryGetConstantArgument(method, methodBytes, flags, offset, 0, out int left)
                            || !TryGetConstantArgument(method, methodBytes, flags, offset, 1, out int right))
                        {
                            // Can't get the constant - both branches are live.
                            offsetsToVisit.Push(destination);
                            offsetsToVisit.Push(reader.Offset);
                        }
                        else if ((left == right && (opcode == ILOpcode.beq || opcode == ILOpcode.beq_s)
                            || (left != right) && (opcode == ILOpcode.bne_un || opcode == ILOpcode.bne_un_s)))
                        {
                            // Only the "branch taken" is live.
                            // The fallthrough marks the beginning of a visible (but not live) basic block.
                            offsetsToVisit.Push(destination);
                            flags[reader.Offset] |= OpcodeFlags.VisibleBasicBlockStart;
                        }
                        else
                        {
                            // Only fallthrough is live.
                            // The "brach taken" marks the beginning of a visible (but not live) basic block.
                            flags[destination] |= OpcodeFlags.VisibleBasicBlockStart;
                            offsetsToVisit.Push(reader.Offset);
                        }
                    }
                    else if (opcode >= ILOpcode.br_s && opcode <= ILOpcode.blt_un
                        || opcode == ILOpcode.leave || opcode == ILOpcode.leave_s)
                    {
                        int destination = reader.ReadBranchDestination(opcode);
                        offsetsToVisit.Push(destination);
                        if (opcode != ILOpcode.leave && opcode != ILOpcode.leave_s
                            && opcode != ILOpcode.br && opcode != ILOpcode.br_s)
                        {
                            // Branches not tested for above are conditional and the flow falls through.
                            offsetsToVisit.Push(reader.Offset);
                        }
                        else
                        {
                            // RyuJIT is going to look at this basic block even though it's unreachable.
                            // Consider it visible so that we replace the tail with an endless loop.
                            if (reader.HasNext)
                                flags[reader.Offset] |= OpcodeFlags.VisibleBasicBlockStart;
                        }
                    }
                    else if (opcode == ILOpcode.switch_)
                    {
                        uint count = reader.ReadILUInt32();
                        int jmpBase = reader.Offset + (int)(4 * count);
                        for (uint i = 0; i < count; i++)
                        {
                            int destination = (int)reader.ReadILUInt32() + jmpBase;
                            offsetsToVisit.Push(destination);
                        }
                        offsetsToVisit.Push(reader.Offset);
                    }
                    else if (opcode == ILOpcode.ret
                        || opcode == ILOpcode.endfilter
                        || opcode == ILOpcode.endfinally
                        || opcode == ILOpcode.throw_
                        || opcode == ILOpcode.rethrow
                        || opcode == ILOpcode.jmp)
                    {
                        reader.Skip(opcode);

                        // RyuJIT is going to look at this basic block even though it's unreachable.
                        // Consider it visible so that we replace the tail with an endless loop.
                        if (reader.HasNext)
                            flags[reader.Offset] |= OpcodeFlags.VisibleBasicBlockStart;
                    }
                    else if (shouldInlineResourceStrings && opcode == ILOpcode.call)
                    {
                        var callee = method.GetObject(reader.ReadILToken(), NotFoundBehavior.ReturnNull) as EcmaMethod;
                        if (callee != null && callee.IsSpecialName && callee.OwningType is EcmaType calleeType
                            && calleeType.Name == InlineableStringsResourceNode.ResourceAccessorTypeName
                            && calleeType.Namespace == InlineableStringsResourceNode.ResourceAccessorTypeNamespace
                            && callee.Signature is { Length: 0, IsStatic: true }
                            && callee.Name.StartsWith("get_", StringComparison.Ordinal))
                        {
                            flags[offset] |= OpcodeFlags.GetResourceStringCall;
                            hasGetResourceStringCall = true;
                        }
                    }
                    else
                    {
                        reader.Skip(opcode);
                    }

                    if ((flags[offset] & OpcodeFlags.EndBasicBlock) != 0)
                        break;
                }
            }

            // Now sweep unreachable basic blocks
            bool hasUnmarkedInstruction = false;
            foreach (var flag in flags)
            {
                if ((flag & OpcodeFlags.InstructionStart) != 0 &&
                    (flag & OpcodeFlags.Mark) == 0)
                {
                    hasUnmarkedInstruction = true;
                }
            }

            if (!hasUnmarkedInstruction && !hasGetResourceStringCall)
                return method;

            // Maps instruction offsets in original method body to offsets in rewritten method body.
            var offsetMap = new int[methodBytes.Length];
#if DEBUG
            Array.Fill(offsetMap, -1);
#endif
            int srcPos = 0;
            int dstPos = 0;

            // Compute a map from original instruction offset to new instruction offsets
            while (srcPos < flags.Length)
            {
                bool marked = (flags[srcPos] & OpcodeFlags.Mark) != 0;

                // Go over all bytes in a single visible basic block
                int lastInstructionPos = srcPos;
                do
                {
                    if ((flags[srcPos] & OpcodeFlags.InstructionStart) != 0)
                    {
                        lastInstructionPos = srcPos;
                        offsetMap[srcPos] = dstPos;
                    }

                    if (marked)
                    {
                        dstPos++;
                    }

                    srcPos++;
                } while (srcPos < flags.Length && (flags[srcPos] & OpcodeFlags.VisibleBasicBlockStart) == 0);

                if (marked)
                {
                    // This was a marked visible basic block. If it ended in a short-form branch instruction,
                    // we need to do a size adjustment because the rewritten code doesn't use short-form
                    // instructions.
                    var reader = new ILReader(methodBytes, lastInstructionPos);
                    if (reader.HasNext)
                    {
                        ILOpcode opcode = reader.ReadILOpcode();
                        int adjustment = opcode switch
                        {
                            >= ILOpcode.br_s and <= ILOpcode.blt_un_s => 3,
                            ILOpcode.leave_s => 3,
                            _ => 0,
                        };
                        dstPos += adjustment;
                    }
                }
                else
                {
                    // This is a dead visible basic block. We're going to emit `br_s $-2`: reserve 2 bytes.
                    dstPos += 2;
                }
            }

            // Now generate the new body
            var newBody = new byte[dstPos];
            srcPos = 0;
            dstPos = 0;
            while (srcPos < flags.Length)
            {
                Debug.Assert((flags[srcPos] & OpcodeFlags.InstructionStart) != 0);
                Debug.Assert((flags[srcPos] & OpcodeFlags.VisibleBasicBlockStart) != 0);

                bool marked = (flags[srcPos] & OpcodeFlags.Mark) != 0;

                if (!marked)
                {
                    // Dead basic block: emit endless loop and skip the rest of the original code.
                    newBody[dstPos++] = (byte)ILOpCode.Br_s;
                    newBody[dstPos++] = unchecked((byte)-2);

                    do
                    {
                        srcPos++;
                    }
                    while (srcPos < flags.Length && (flags[srcPos] & OpcodeFlags.VisibleBasicBlockStart) == 0);
                }
                else
                {
                    // Live basic block: copy the original bytes
                    int lastInstructionPos = srcPos;
                    do
                    {
                        if ((flags[srcPos] & OpcodeFlags.InstructionStart) != 0)
                            lastInstructionPos = srcPos;

                        newBody[dstPos++] = methodBytes[srcPos++];
                    }
                    while (srcPos < flags.Length && (flags[srcPos] & OpcodeFlags.VisibleBasicBlockStart) == 0);

                    // If the basic block ended in a branch, we need to map the target offset to new offset.
                    // We'll also rewrite short-form instructions to their long forms.
                    var reader = new ILReader(methodBytes, lastInstructionPos);
                    if (reader.HasNext)
                    {
                        ILOpcode opcode = reader.ReadILOpcode();

                        if (opcode == ILOpcode.switch_)
                        {
                            uint count = reader.ReadILUInt32();
                            int srcJmpBase = reader.Offset + (int)(4 * count);
                            int dstJmpBase = dstPos;
                            dstPos -= (int)(sizeof(uint) * count);
                            for (uint i = 0; i < count; i++)
                            {
                                int dest = offsetMap[(int)reader.ReadILUInt32() + srcJmpBase];
                                BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(newBody, dstPos, sizeof(uint)), (uint)(dest - dstJmpBase));
                                dstPos += sizeof(uint);
                            }
                        }
                        else if (opcode is >= ILOpcode.br_s and <= ILOpcode.blt_un || opcode is ILOpcode.leave or ILOpcode.leave_s)
                        {
                            int dest = offsetMap[reader.ReadBranchDestination(opcode)];

                            if (opcode is >= ILOpcode.br_s and <= ILOpcode.blt_un_s || opcode == ILOpcode.leave_s)
                            {
                                dstPos -= 2;
                                if (opcode == ILOpcode.leave_s)
                                    opcode = ILOpcode.leave;
                                else
                                    opcode += ILOpcode.br - ILOpcode.br_s;
                            }
                            else
                            {
                                dstPos -= 5;
                            }

                            newBody[dstPos++] = (byte)opcode;
                            BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(newBody, dstPos, sizeof(uint)), (uint)(dest - (dstPos + sizeof(uint))));
                            dstPos += sizeof(uint);
                        }
                    }
                }
            }

            // EH regions with unmarked handlers belong to unmarked basic blocks
            // Need to eliminate them because they're not usable.
            // The rest need to have their offsets and lengths remapped.
            ArrayBuilder<ILExceptionRegion> newEHRegions = default(ArrayBuilder<ILExceptionRegion>);
            foreach (ILExceptionRegion ehRegion in ehRegions)
            {
                if ((flags[ehRegion.HandlerOffset] & OpcodeFlags.Mark) != 0)
                {
                    int tryOffset = offsetMap[ehRegion.TryOffset];
                    int handlerOffset = offsetMap[ehRegion.HandlerOffset];

                    var newEhRegion = new ILExceptionRegion(
                        ehRegion.Kind,
                        tryOffset,
                        offsetMap[ehRegion.TryOffset + ehRegion.TryLength] - tryOffset,
                        handlerOffset,
                        offsetMap[ehRegion.HandlerOffset + ehRegion.HandlerLength] - handlerOffset,
                        ehRegion.ClassToken,
                        ehRegion.Kind == ILExceptionRegionKind.Filter ? offsetMap[ehRegion.FilterOffset] : -1);

                    newEHRegions.Add(newEhRegion);
                }
            }

            // Remap debug information as well.
            MethodDebugInformation debugInfo = method.GetDebugInfo();
            IEnumerable<ILSequencePoint> oldSequencePoints = debugInfo?.GetSequencePoints();
            if (oldSequencePoints != null)
            {
                ArrayBuilder<ILSequencePoint> sequencePoints = default(ArrayBuilder<ILSequencePoint>);
                foreach (var sequencePoint in oldSequencePoints)
                {
                    if (sequencePoint.Offset < flags.Length && (flags[sequencePoint.Offset] & OpcodeFlags.Mark) != 0)
                    {
                        ILSequencePoint newSequencePoint = new ILSequencePoint(
                            offsetMap[sequencePoint.Offset], sequencePoint.Document, sequencePoint.LineNumber);
                        sequencePoints.Add(newSequencePoint);
                    }
                }

                debugInfo = new SubstitutedDebugInformation(debugInfo, sequencePoints.ToArray());
            }

            // We only optimize EcmaMethods because there we can find out the highest string token RID
            // in use.
            ArrayBuilder<string> newStrings = default;
            if (hasGetResourceStringCall && method.GetMethodILDefinition() is EcmaMethodIL ecmaMethodIL)
            {
                // We're going to inject new string tokens. Start where the last token of the module left off.
                // We don't need this token to be globally unique because all token resolution happens in the context
                // of a MethodIL and we're making a new one here. It just has to be unique to the MethodIL.
                int tokenRid = ecmaMethodIL.Module.MetadataReader.GetHeapSize(HeapIndex.UserString);

                for (int srcOffset = 0; srcOffset < flags.Length; srcOffset++)
                {
                    if ((flags[srcOffset] & OpcodeFlags.GetResourceStringCall) == 0)
                        continue;

                    int offset = offsetMap[srcOffset];

                    Debug.Assert(newBody[offset] == (byte)ILOpcode.call);
                    var getter = (EcmaMethod)method.GetObject(new ILReader(newBody, offset + 1).ReadILToken());

                    // If we can't get the string, this might be something else.
                    string resourceString = _substitutionProvider.GetResourceStringForAccessor(getter);
                    if (resourceString == null)
                        continue;

                    // If we ran out of tokens, we can't optimize anymore.
                    if (tokenRid > 0xFFFFFF)
                        continue;

                    newStrings.Add(resourceString);

                    // call and ldstr are both 5-byte instructions: opcode followed by a token.
                    newBody[offset] = (byte)ILOpcode.ldstr;
                    newBody[offset + 1] = (byte)tokenRid;
                    newBody[offset + 2] = (byte)(tokenRid >> 8);
                    newBody[offset + 3] = (byte)(tokenRid >> 16);
                    newBody[offset + 4] = TokenTypeString;

                    tokenRid++;
                }
            }

            return new SubstitutedMethodIL(method.GetMethodILDefinition(), newBody, newEHRegions.ToArray(), debugInfo, newStrings.ToArray());
        }

        private bool TryGetConstantArgument(MethodIL methodIL, byte[] body, OpcodeFlags[] flags, int offset, int argIndex, out int constant)
        {
            if ((flags[offset] & OpcodeFlags.BasicBlockStart) != 0)
            {
                constant = 0;
                return false;
            }

            for (int currentOffset = offset - 1; currentOffset >= 0; currentOffset--)
            {
                if ((flags[currentOffset] & OpcodeFlags.InstructionStart) == 0)
                    continue;

                ILReader reader = new ILReader(body, currentOffset);
                ILOpcode opcode = reader.ReadILOpcode();
                if (opcode == ILOpcode.call || opcode == ILOpcode.callvirt)
                {
                    MethodDesc method = (MethodDesc)methodIL.GetObject(reader.ReadILToken());
                    if (argIndex == 0)
                    {
                        BodySubstitution substitution = _substitutionProvider.GetSubstitution(method);
                        if (substitution != null && substitution.Value is int
                            && (opcode != ILOpcode.callvirt || !method.IsVirtual))
                        {
                            constant = (int)substitution.Value;
                            return true;
                        }
                        else if (method.IsIntrinsic && method.Name is "op_Inequality" or "op_Equality"
                            && method.OwningType is MetadataType mdType
                            && mdType.Name == "Type" && mdType.Namespace == "System" && mdType.Module == mdType.Context.SystemModule
                            && TryExpandTypeEquality(methodIL, body, flags, currentOffset, method.Name, out constant))
                        {
                            return true;
                        }
                        else if (method.IsIntrinsic && method.Name is "get_IsValueType"
                            && method.OwningType is MetadataType mdt
                            && mdt.Name == "Type" && mdt.Namespace == "System" && mdt.Module == mdt.Context.SystemModule
                            && TryExpandTypeIsValueType(methodIL, body, flags, currentOffset, out constant))
                        {
                            return true;
                        }
                        else
                        {
                            constant = 0;
                            return false;
                        }
                    }

                    argIndex--;

                    if (method.Signature.Length > 0 || !method.Signature.IsStatic)
                    {
                        // We don't know how to skip over the parameters
                        break;
                    }
                }
                else if (opcode == ILOpcode.ldsfld)
                {
                    FieldDesc field = (FieldDesc)methodIL.GetObject(reader.ReadILToken());
                    if (argIndex == 0)
                    {
                        object substitution = _substitutionProvider.GetSubstitution(field);
                        if (substitution is int)
                        {
                            constant = (int)substitution;
                            return true;
                        }
                        else
                        {
                            constant = 0;
                            return false;
                        }
                    }

                    argIndex--;
                }
                else if (opcode >= ILOpcode.ldc_i4_0 && opcode <= ILOpcode.ldc_i4_8)
                {
                    if (argIndex == 0)
                    {
                        constant = opcode - ILOpcode.ldc_i4_0;
                        return true;
                    }

                    argIndex--;
                }
                else if (opcode == ILOpcode.ldc_i4)
                {
                    if (argIndex == 0)
                    {
                        constant = (int)reader.ReadILUInt32();
                        return true;
                    }

                    argIndex--;
                }
                else if (opcode == ILOpcode.ldc_i4_s)
                {
                    if (argIndex == 0)
                    {
                        constant = (int)(sbyte)reader.ReadILByte();
                        return true;
                    }

                    argIndex--;
                }
                else if ((opcode == ILOpcode.ldloc || opcode == ILOpcode.ldloc_s ||
                    (opcode >= ILOpcode.ldloc_0 && opcode <= ILOpcode.ldloc_3)) &&
                    ((flags[currentOffset] & OpcodeFlags.BasicBlockStart) == 0))
                {
                    // Paired stloc/ldloc that the C# compiler generates in debug code?
                    int locIndex = opcode switch
                    {
                        ILOpcode.ldloc => reader.ReadILUInt16(),
                        ILOpcode.ldloc_s => reader.ReadILByte(),
                        _ => opcode - ILOpcode.ldloc_0,
                    };

                    for (int potentialStlocOffset = currentOffset - 1; potentialStlocOffset >= 0; potentialStlocOffset--)
                    {
                        if ((flags[potentialStlocOffset] & OpcodeFlags.InstructionStart) == 0)
                            continue;

                        ILReader nestedReader = new ILReader(body, potentialStlocOffset);
                        ILOpcode otherOpcode = nestedReader.ReadILOpcode();
                        if ((otherOpcode == ILOpcode.stloc || otherOpcode == ILOpcode.stloc_s ||
                            (otherOpcode >= ILOpcode.stloc_0 && otherOpcode <= ILOpcode.stloc_3))
                            && otherOpcode switch
                            {
                                ILOpcode.stloc => nestedReader.ReadILUInt16(),
                                ILOpcode.stloc_s => nestedReader.ReadILByte(),
                                _ => otherOpcode - ILOpcode.stloc_0,
                            } == locIndex)
                        {
                            // Move all the way to the stloc and resume looking for previous instruction.
                            currentOffset = potentialStlocOffset;
                            break;
                        }
                        else
                        {
                            constant = 0;
                            return false;
                        }
                    }
                }
                else if (opcode == ILOpcode.ceq)
                {
                    if (argIndex == 0)
                    {
                        if (!TryGetConstantArgument(methodIL, body, flags, currentOffset, 0, out int left)
                                || !TryGetConstantArgument(methodIL, body, flags, currentOffset, 1, out int right))
                        {
                            constant = 0;
                            return false;
                        }

                        constant = left == right ? 1 : 0;
                        return true;
                    }

                    // If we knew where the arguments to this end, we could resume looking from there.
                    // Punting for now.
                    Debug.Assert(argIndex != 0);
                    constant = 0;
                    return false;
                }
                else
                {
                    constant = 0;
                    return false;
                }

                if ((flags[currentOffset] & OpcodeFlags.BasicBlockStart) != 0)
                    break;
            }

            constant = 0;
            return false;
        }

        private static bool TryExpandTypeIsValueType(MethodIL methodIL, byte[] body, OpcodeFlags[] flags, int offset, out int constant)
        {
            // We expect to see a sequence:
            // ldtoken Foo
            // call GetTypeFromHandle
            // -> offset points here
            constant = 0;
            const int SequenceLength = 10;
            if (offset < SequenceLength)
                return false;

            if ((flags[offset - SequenceLength] & OpcodeFlags.InstructionStart) == 0)
                return false;

            ILReader reader = new ILReader(body, offset - SequenceLength);

            TypeDesc type = ReadLdToken(ref reader, methodIL, flags);
            if (type == null)
                return false;

            if (!ReadGetTypeFromHandle(ref reader, methodIL, flags))
                return false;

            // Dataflow runs on top of uninstantiated IL and we can't answer some questions there.
            // Unfortunately this means dataflow will still see code that the rest of the system
            // might have optimized away. It should not be a problem in practice.
            if (type.IsSignatureVariable)
                return false;

            constant = type.IsValueType ? 1 : 0;

            return true;
        }

        private static bool TryExpandTypeEquality(MethodIL methodIL, byte[] body, OpcodeFlags[] flags, int offset, string op, out int constant)
        {
            // We expect to see a sequence:
            // ldtoken Foo
            // call GetTypeFromHandle
            // ldtoken Bar
            // call GetTypeFromHandle
            // -> offset points here
            constant = 0;
            const int SequenceLength = 20;
            if (offset < SequenceLength)
                return false;

            if ((flags[offset - SequenceLength] & OpcodeFlags.InstructionStart) == 0)
                return false;

            ILReader reader = new ILReader(body, offset - SequenceLength);

            TypeDesc type1 = ReadLdToken(ref reader, methodIL, flags);
            if (type1 == null)
                return false;

            if (!ReadGetTypeFromHandle(ref reader, methodIL, flags))
                return false;

            TypeDesc type2 = ReadLdToken(ref reader, methodIL, flags);
            if (type1 == null)
                return false;

            if (!ReadGetTypeFromHandle(ref reader, methodIL, flags))
                return false;

            // Dataflow runs on top of uninstantiated IL and we can't answer some questions there.
            // Unfortunately this means dataflow will still see code that the rest of the system
            // might have optimized away. It should not be a problem in practice.
            if (type1.ContainsSignatureVariables() || type2.ContainsSignatureVariables())
                return false;

            bool? equality = TypeExtensions.CompareTypesForEquality(type1, type2);
            if (!equality.HasValue)
                return false;

            constant = equality.Value ? 1 : 0;

            if (op == "op_Inequality")
                constant ^= 1;

            return true;
        }

        private static TypeDesc ReadLdToken(ref ILReader reader, MethodIL methodIL, OpcodeFlags[] flags)
        {
            ILOpcode opcode = reader.ReadILOpcode();
            if (opcode != ILOpcode.ldtoken)
                return null;

            TypeDesc t = (TypeDesc)methodIL.GetObject(reader.ReadILToken());

            if ((flags[reader.Offset] & OpcodeFlags.BasicBlockStart) != 0)
                return null;

            return t;
        }

        private static bool ReadGetTypeFromHandle(ref ILReader reader, MethodIL methodIL, OpcodeFlags[] flags)
        {
            ILOpcode opcode = reader.ReadILOpcode();
            if (opcode != ILOpcode.call)
                return false;

            MethodDesc method = (MethodDesc)methodIL.GetObject(reader.ReadILToken());

            if (!method.IsIntrinsic || method.Name != "GetTypeFromHandle")
                return false;

            if ((flags[reader.Offset] & OpcodeFlags.BasicBlockStart) != 0)
                return false;

            return true;
        }

        private sealed class SubstitutedMethodIL : MethodIL
        {
            private readonly byte[] _body;
            private readonly ILExceptionRegion[] _ehRegions;
            private readonly MethodIL _wrappedMethodIL;
            private readonly MethodDebugInformation _debugInfo;
            private readonly string[] _newStrings;

            public SubstitutedMethodIL(MethodIL wrapped, byte[] body, ILExceptionRegion[] ehRegions, MethodDebugInformation debugInfo, string[] newStrings)
            {
                _wrappedMethodIL = wrapped;
                _body = body;
                _ehRegions = ehRegions;
                _debugInfo = debugInfo;
                _newStrings = newStrings;
            }

            public override MethodDesc OwningMethod => _wrappedMethodIL.OwningMethod;
            public override int MaxStack => _wrappedMethodIL.MaxStack;
            public override bool IsInitLocals => _wrappedMethodIL.IsInitLocals;
            public override ILExceptionRegion[] GetExceptionRegions() => _ehRegions;
            public override byte[] GetILBytes() => _body;
            public override LocalVariableDefinition[] GetLocals() => _wrappedMethodIL.GetLocals();
            public override object GetObject(int token, NotFoundBehavior notFoundBehavior)
            {
                // If this is a string token, it could be one of the new string tokens we injected.
                if ((token >>> 24) == TokenTypeString
                    && _wrappedMethodIL.GetMethodILDefinition() is EcmaMethodIL ecmaMethodIL)
                {
                    int rid = token & 0xFFFFFF;
                    int maxRealTokenRid = ecmaMethodIL.Module.MetadataReader.GetHeapSize(HeapIndex.UserString);
                    if (rid >= maxRealTokenRid)
                    {
                        // Yep, string injected by us.
                        return _newStrings[rid - maxRealTokenRid];
                    }
                }

                return _wrappedMethodIL.GetObject(token, notFoundBehavior);
            }
            public override MethodDebugInformation GetDebugInfo() => _debugInfo;
        }

        private sealed class SubstitutedDebugInformation : MethodDebugInformation
        {
            private readonly MethodDebugInformation _originalDebugInformation;
            private readonly ILSequencePoint[] _sequencePoints;

            public SubstitutedDebugInformation(MethodDebugInformation originalDebugInformation, ILSequencePoint[] newSequencePoints)
            {
                _originalDebugInformation = originalDebugInformation;
                _sequencePoints = newSequencePoints;
            }

            public override IEnumerable<Internal.IL.ILLocalVariable> GetLocalVariables() => _originalDebugInformation.GetLocalVariables();
            public override IEnumerable<string> GetParameterNames() => _originalDebugInformation.GetParameterNames();
            public override IEnumerable<ILSequencePoint> GetSequencePoints() => _sequencePoints;
        }

        private const int TokenTypeString = 0x70; // CorTokenType for strings
    }
}
