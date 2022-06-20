// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml;
using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;
using MethodDebugInformation = Internal.IL.MethodDebugInformation;

namespace ILCompiler
{
    public class FeatureSwitchManager : ILProvider
    {
        private readonly FeatureSwitchHashtable _hashtable;
        private readonly ILProvider _nestedILProvider;

        public FeatureSwitchManager(ILProvider nestedILProvider, IEnumerable<KeyValuePair<string, bool>> switchValues)
        {
            _nestedILProvider = nestedILProvider;
            _hashtable = new FeatureSwitchHashtable(new Dictionary<string, bool>(switchValues));
        }

        private BodySubstitution GetSubstitution(MethodDesc method)
        {
            if (method.GetTypicalMethodDefinition() is EcmaMethod ecmaMethod)
            {
                AssemblyFeatureInfo info = _hashtable.GetOrCreateValue(ecmaMethod.Module);
                if (info.BodySubstitutions != null && info.BodySubstitutions.TryGetValue(ecmaMethod, out BodySubstitution result))
                    return result;
            }

            return null;
        }

        private object GetSubstitution(FieldDesc field)
        {
            if (field.GetTypicalFieldDefinition() is EcmaField ecmaField)
            {
                AssemblyFeatureInfo info = _hashtable.GetOrCreateValue(ecmaField.Module);
                if (info.BodySubstitutions != null && info.FieldSubstitutions.TryGetValue(ecmaField, out object result))
                    return result;
            }

            return null;
        }

        public bool HasSubstitutedBody(MethodDesc method)
        {
            return GetSubstitution(method) != null;
        }

        public bool HasSubstitutedValue(FieldDesc field)
        {
            return GetSubstitution(field) != null;
        }

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            BodySubstitution substitution = GetSubstitution(method);
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
                    MethodIL newBodyDef = GetMethodILWithInlinedSubstitutions(resultDef);

                    // If we didn't rewrite the body, we can keep the existing result.
                    if (newBodyDef != resultDef)
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
        enum OpcodeFlags : byte
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
            // Last step is a sweep - we replace the tail of all unreachable blocks with "br $-2"
            // and nop out the rest. If the basic block is smaller than 2 bytes, we don't touch it.
            // We also eliminate any EH records that correspond to the stubbed out basic block.

            Debug.Assert(method.GetMethodILDefinition() == method);

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

            // Mark all reachable basic blocks
            //
            // We also do another round of basic block marking to mark beginning of visible basic blocks
            // after dead branch elimination. This allows us to limit the number of potential small basic blocks
            // that are not interesting (because no code jumps to them anymore), but could prevent us from
            // finishing the process. Unreachable basic blocks smaller than 2 bytes abort the substitution
            // inlining process because we can't neutralize them (turn them into an infinite loop).
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
                                offsetsToVisit.Push(ehRegion.FilterOffset);

                            offsetsToVisit.Push(ehRegion.HandlerOffset);

                            // RyuJIT is going to look at this basic block even though it's unreachable.
                            // Consider it visible so that we replace the tail with an endless loop.
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
                    else
                    {
                        reader.Skip(opcode);
                    }

                    if ((flags[offset] & OpcodeFlags.EndBasicBlock) != 0)
                        break;
                }
            }

            // Now sweep unreachable basic blocks by replacing them with nops
            bool hasUnmarkedIntructions = false;
            foreach (var flag in flags)
            {
                if ((flag & OpcodeFlags.InstructionStart) != 0 &&
                    (flag & OpcodeFlags.Mark) == 0)
                {
                    hasUnmarkedIntructions = true;
                }
            }

            if (!hasUnmarkedIntructions)
                return method;

            byte[] newBody = (byte[])methodBytes.Clone();
            int position = 0;
            while (position < newBody.Length)
            {
                Debug.Assert((flags[position] & OpcodeFlags.InstructionStart) != 0);
                Debug.Assert((flags[position] & OpcodeFlags.VisibleBasicBlockStart) != 0);

                bool erase = (flags[position] & OpcodeFlags.Mark) == 0;

                int basicBlockStart = position;
                do
                {
                    if (erase)
                        newBody[position] = (byte)ILOpCode.Nop;
                    position++;
                } while (position < newBody.Length && (flags[position] & OpcodeFlags.VisibleBasicBlockStart) == 0);

                // If we had to nop out this basic block, we need to neutralize it by appending
                // an infinite loop ("br $-2").
                // We append instead of prepend because RyuJIT's importer has trouble with junk unreachable bytes.
                if (erase)
                {
                    if (position - basicBlockStart < 2)
                    {
                        // We cannot neutralize the basic block, so better leave the method alone.
                        // The control would fall through to the next basic block.
                        return method;
                    }

                    newBody[position - 2] = (byte)ILOpCode.Br_s;
                    newBody[position - 1] = unchecked((byte)-2);
                }
            }

            // EH regions with unmarked handlers belong to unmarked basic blocks
            // Need to eliminate them because they're not usable.
            ArrayBuilder<ILExceptionRegion> newEHRegions = new ArrayBuilder<ILExceptionRegion>();
            foreach (ILExceptionRegion ehRegion in ehRegions)
            {
                if ((flags[ehRegion.HandlerOffset] & OpcodeFlags.Mark) != 0)
                {
                    newEHRegions.Add(ehRegion);
                }
            }

            // Existing debug information might not match new instruction boundaries (plus there's little point
            // in generating debug information for NOPs) - generate new debug information by filtering
            // out the sequence points associated with nopped out instructions.
            MethodDebugInformation debugInfo = method.GetDebugInfo();
            IEnumerable<ILSequencePoint> oldSequencePoints = debugInfo?.GetSequencePoints();
            if (oldSequencePoints != null)
            {
                ArrayBuilder<ILSequencePoint> sequencePoints = new ArrayBuilder<ILSequencePoint>();
                foreach (var sequencePoint in oldSequencePoints)
                {
                    if (sequencePoint.Offset < flags.Length && (flags[sequencePoint.Offset] & OpcodeFlags.Mark) != 0)
                    {
                        sequencePoints.Add(sequencePoint);
                    }
                }

                debugInfo = new SubstitutedDebugInformation(debugInfo, sequencePoints.ToArray());
            }

            return new SubstitutedMethodIL(method, newBody, newEHRegions.ToArray(), debugInfo);
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
                        BodySubstitution substitution = GetSubstitution(method);
                        if (substitution != null && substitution.Value is int
                            && (opcode != ILOpcode.callvirt || !method.IsVirtual))
                        {
                            constant = (int)substitution.Value;
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
                        object substitution = GetSubstitution(field);
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

        private class SubstitutedMethodIL : MethodIL
        {
            private readonly byte[] _body;
            private readonly ILExceptionRegion[] _ehRegions;
            private readonly MethodIL _wrappedMethodIL;
            private readonly MethodDebugInformation _debugInfo;

            public SubstitutedMethodIL(MethodIL wrapped, byte[] body, ILExceptionRegion[] ehRegions, MethodDebugInformation debugInfo)
            {
                _wrappedMethodIL = wrapped;
                _body = body;
                _ehRegions = ehRegions;
                _debugInfo = debugInfo;
            }

            public override MethodDesc OwningMethod => _wrappedMethodIL.OwningMethod;
            public override int MaxStack => _wrappedMethodIL.MaxStack;
            public override bool IsInitLocals => _wrappedMethodIL.IsInitLocals;
            public override ILExceptionRegion[] GetExceptionRegions() => _ehRegions;
            public override byte[] GetILBytes() => _body;
            public override LocalVariableDefinition[] GetLocals() => _wrappedMethodIL.GetLocals();
            public override object GetObject(int token, NotFoundBehavior notFoundBehavior) => _wrappedMethodIL.GetObject(token, notFoundBehavior);
            public override MethodDebugInformation GetDebugInfo() => _debugInfo;
        }

        private class SubstitutedDebugInformation : MethodDebugInformation
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

        private class FeatureSwitchHashtable : LockFreeReaderHashtable<EcmaModule, AssemblyFeatureInfo>
        {
            private readonly Dictionary<string, bool> _switchValues;

            public FeatureSwitchHashtable(Dictionary<string, bool> switchValues)
            {
                _switchValues = switchValues;
            }

            protected override bool CompareKeyToValue(EcmaModule key, AssemblyFeatureInfo value) => key == value.Module;
            protected override bool CompareValueToValue(AssemblyFeatureInfo value1, AssemblyFeatureInfo value2) => value1.Module == value2.Module;
            protected override int GetKeyHashCode(EcmaModule key) => key.GetHashCode();
            protected override int GetValueHashCode(AssemblyFeatureInfo value) => value.Module.GetHashCode();

            protected override AssemblyFeatureInfo CreateValueFromKey(EcmaModule key)
            {
                return new AssemblyFeatureInfo(key, _switchValues);
            }
        }

        private class AssemblyFeatureInfo
        {
            public EcmaModule Module { get; }

            public Dictionary<MethodDesc, BodySubstitution> BodySubstitutions { get; }
            public Dictionary<FieldDesc, object> FieldSubstitutions { get; }

            public AssemblyFeatureInfo(EcmaModule module, IReadOnlyDictionary<string, bool> featureSwitchValues)
            {
                Module = module;

                PEMemoryBlock resourceDirectory = module.PEReader.GetSectionData(module.PEReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);

                foreach (var resourceHandle in module.MetadataReader.ManifestResources)
                {
                    ManifestResource resource = module.MetadataReader.GetManifestResource(resourceHandle);

                    // Don't try to process linked resources or resources in other assemblies
                    if (!resource.Implementation.IsNil)
                    {
                        continue;
                    }

                    string resourceName = module.MetadataReader.GetString(resource.Name);
                    if (resourceName == "ILLink.Substitutions.xml")
                    {
                        BlobReader reader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
                        int length = (int)reader.ReadUInt32();

                        UnmanagedMemoryStream ms;
                        unsafe
                        {
                            ms = new UnmanagedMemoryStream(reader.CurrentPointer, length);
                        }

                        (BodySubstitutions, FieldSubstitutions) = BodySubstitutionsParser.GetSubstitutions(module.Context, ms, resource, module, "name", featureSwitchValues);
                    }
                }
            }
        }
    }
}
