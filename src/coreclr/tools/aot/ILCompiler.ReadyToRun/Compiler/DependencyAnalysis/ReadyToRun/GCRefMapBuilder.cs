// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Internal.TypeSystem;

// The GCRef map is used to encode GC type of arguments for callsites. Logically, it is sequence <pos, token> where pos is
// position of the reference in the stack frame and token is type of GC reference (one of GCREFMAP_XXX values).
//
// - The encoding always starts at the byte boundary. The high order bit of each byte is used to signal end of the encoding
// stream. The last byte has the high order bit zero. It means that there are 7 useful bits in each byte.
// - "pos" is always encoded as delta from previous pos.
// - The basic encoding unit is two bits. Values 0, 1 and 2 are the common constructs (skip single slot, GC reference, interior
// pointer). Value 3 means that extended encoding follows.
// - The extended information is integer encoded in one or more four bit blocks. The high order bit of the four bit block is
// used to signal the end.
// - For x86, the encoding starts by size of the callee poped stack. The size is encoded using the same mechanism as above (two bit
// basic encoding, with extended encoding for large values).

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class GCRefMapBuilder
    {
        /// <summary>
        /// TargetDetails to use
        /// </summary>
        private readonly TargetDetails _target;

        /// <summary>
        /// Pending value, not yet written out
        /// </summary>
        private int _pendingByte;

        /// <summary>
        /// Number of bits in pending byte. Note that the trailing zero bits are not written out,
        /// so this can be more than 7.
        /// </summary>
        private int _bits;

        /// <summary>
        /// Current position
        /// </summary>
        private uint _pos;

        /// <summary>
        /// Builder for the generated data
        /// </summary>
        public ObjectDataBuilder Builder;

        /// <summary>
        /// Transition block characteristics for the given output architecture / target OS ABI.
        /// </summary>
        private readonly TransitionBlock _transitionBlock;

        public GCRefMapBuilder(TargetDetails target, bool relocsOnly)
        {
            _target = target;
            _pendingByte = 0;
            _bits = 0;
            _pos = 0;
            Builder = new ObjectDataBuilder(target, relocsOnly);
            _transitionBlock = TransitionBlock.FromTarget(target);
        }

        public void GetCallRefMap(MethodDesc method, bool isUnboxingStub)
        {
            TransitionBlock transitionBlock = TransitionBlock.FromTarget(method.Context.Target);

            MethodSignature signature = method.Signature;

            bool hasThis = (signature.Flags & MethodSignatureFlags.Static) == 0;

            // This pointer is omitted for string constructors
            bool fCtorOfVariableSizedObject = hasThis && method.OwningType.IsString && method.IsConstructor;
            if (fCtorOfVariableSizedObject)
                hasThis = false;

            bool isVarArg = false;
            TypeHandle returnType = new TypeHandle(signature.ReturnType);
            TypeHandle[] parameterTypes = new TypeHandle[signature.Length];
            for (int parameterIndex = 0; parameterIndex < parameterTypes.Length; parameterIndex++)
            {
                parameterTypes[parameterIndex] = new TypeHandle(signature[parameterIndex]);
            }
            CallingConventions callingConventions = (hasThis ? CallingConventions.ManagedInstance : CallingConventions.ManagedStatic);
            bool hasParamType = method.RequiresInstArg() && !isUnboxingStub;

            // On X86 the Array address method doesn't use IL stubs, and instead has a custom calling convention
            if ((method.Context.Target.Architecture == TargetArchitecture.X86) &&
                method.IsArrayAddressMethod())
            {
                hasParamType = true;
            }

            bool extraFunctionPointerArg = false;
            bool[] forcedByRefParams = new bool[parameterTypes.Length];
            bool skipFirstArg = false;
            bool extraObjectFirstArg = false;
            ArgIteratorData argIteratorData = new ArgIteratorData(hasThis, isVarArg, parameterTypes, returnType);

            ArgIterator argit = new ArgIterator(
                method.Context,
                argIteratorData,
                callingConventions,
                hasParamType,
                extraFunctionPointerArg,
                forcedByRefParams,
                skipFirstArg,
                extraObjectFirstArg);

            int nStackBytes = argit.SizeOfFrameArgumentArray();

            // Allocate a fake stack
            CORCOMPILE_GCREFMAP_TOKENS[] fakeStack = new CORCOMPILE_GCREFMAP_TOKENS[transitionBlock.SizeOfTransitionBlock + nStackBytes];

            // Fill it in
            FakeGcScanRoots(method, argit, fakeStack, isUnboxingStub);

            // Encode the ref map
            uint nStackSlots;
            if (_target.Architecture == TargetArchitecture.X86)
            {
                uint cbStackPop = argit.CbStackPop();
                WriteStackPop(cbStackPop / (uint)_target.PointerSize);

                nStackSlots = (uint)(nStackBytes / _target.PointerSize + _transitionBlock.NumArgumentRegisters);
            }
            else
            {
                nStackSlots = (uint)((transitionBlock.SizeOfTransitionBlock + nStackBytes - _transitionBlock.OffsetOfFirstGCRefMapSlot) / _target.PointerSize);
            }

            for (uint pos = 0; pos < nStackSlots; pos++)
            {
                int offset = _transitionBlock.OffsetFromGCRefMapPos(checked((int)pos));
                CORCOMPILE_GCREFMAP_TOKENS token = fakeStack[offset];

                if (token != CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_SKIP)
                {
                    WriteToken(pos, (byte)token);
                }
            }

            Flush();
        }

        /// <summary>
        /// Fill in the GC-relevant stack frame locations.
        /// </summary>
        private void FakeGcScanRoots(MethodDesc method, ArgIterator argit, CORCOMPILE_GCREFMAP_TOKENS[] frame, bool isUnboxingStub)
        {
            // Encode generic instantiation arg
            if (argit.HasParamType)
            {
                if (method.RequiresInstMethodDescArg())
                {
                    frame[argit.GetParamTypeArgOffset()] = CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_METHOD_PARAM;
                }
                else if (method.RequiresInstMethodTableArg())
                {
                    frame[argit.GetParamTypeArgOffset()] = CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_TYPE_PARAM;
                }
            }

            // If the function has a this pointer, add it to the mask
            if (argit.HasThis)
            {
                bool interior = method.OwningType.IsValueType && !isUnboxingStub;

                frame[_transitionBlock.ThisOffset] = (interior ? CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_INTERIOR : CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_REF);
            }

            if (argit.IsVarArg)
            {
                frame[argit.GetVASigCookieOffset()] = CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_VASIG_COOKIE;

                // We are done for varargs - the remaining arguments are reported via vasig cookie
                return;
            }

            // Also if the method has a return buffer, then it is the first argument, and could be an interior ref,
            // so always promote it.
            if (argit.HasRetBuffArg())
            {
                frame[_transitionBlock.GetRetBuffArgOffset(argit.HasThis)] = CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_INTERIOR;
            }

            //
            // Now iterate the arguments
            //

            // Cycle through the arguments, and call GcScanRoots for each
            int argIndex = 0;
            int argOffset;
            while ((argOffset = argit.GetNextOffset()) != TransitionBlock.InvalidOffset)
            {
                ArgLocDesc? argLocDescForStructInRegs = argit.GetArgLoc(argOffset);
                ArgDestination argDest = new ArgDestination(_transitionBlock, argOffset, argLocDescForStructInRegs);
                GcScanRoots(method.Signature[argIndex], in argDest, delta: 0, frame, topLevel: true);
                argIndex++;
            }
        }

        /// <summary>
        /// Report GC locations for a single method parameter.
        /// </summary>
        /// <param name="argIterator">ArgIterator to use for scanning</param>
        /// <param name="type">Parameter type</param>
        /// <param name="argDest">Location of the parameter</param>
        /// <param name="frame">Frame map to update by marking GC locations</param>
        /// <param name="topLevel">Indicates if the call is for a type or inner member</param>
        private void GcScanRoots(TypeDesc type, in ArgDestination argDest, int delta, CORCOMPILE_GCREFMAP_TOKENS[] frame, bool topLevel)
        {
            switch (type.Category)
            {
                // TYPE_GC_NONE
                case TypeFlags.Void:
                case TypeFlags.Boolean:
                case TypeFlags.Char:
                case TypeFlags.Byte:
                case TypeFlags.SByte:
                case TypeFlags.Int16:
                case TypeFlags.UInt16:
                case TypeFlags.Int32:
                case TypeFlags.UInt32:
                case TypeFlags.Int64:
                case TypeFlags.UInt64:
                case TypeFlags.Single:
                case TypeFlags.Double:
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                case TypeFlags.Enum:
                    break;

                // TYPE_GC_REF
                case TypeFlags.Class:
                case TypeFlags.Interface:
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                    argDest.GcMark(frame, delta, interior: false);
                    break;

                // TYPE_GC_BYREF
                case TypeFlags.ByRef:
                    argDest.GcMark(frame, delta, interior: true);
                    break;

                // TYPE_GC_OTHER
                case TypeFlags.ValueType:
                case TypeFlags.Nullable:
                    GcScanValueType(type, in argDest, delta, frame, topLevel);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void GcScanValueType(TypeDesc type, in ArgDestination argDest, int delta, CORCOMPILE_GCREFMAP_TOKENS[] frame, bool topLevel)
        {
            if (topLevel)
            {
                if (_transitionBlock.IsArgPassedByRef(new TypeHandle(type)))
                {
                    argDest.GcMark(frame, delta, interior: true);
                    return;
                }

                if (argDest.IsStructPassedInRegs())
                {
                    argDest.ReportPointersFromStructInRegisters(type, delta, frame);
                    return;
                }
            }

            Debug.Assert(type is MetadataType);
            MetadataType structType = (MetadataType)type;
            bool isInlineArray = structType.IsInlineArray;
            foreach (FieldDesc field in structType.GetFields())
            {
                if (field.IsStatic)
                    continue;

                if (isInlineArray)
                {
                    var elementSize = field.FieldType.GetElementSize().AsInt;
                    var totalSize = structType.InstanceFieldSize.AsInt;

                    for (int offset = 0; offset < totalSize; offset += elementSize)
                    {
                        GcScanRoots(field.FieldType, in argDest, delta + offset, frame, topLevel: false);
                    }

                    // there is only one formal instance field in an inline array
                    Debug.Assert(field.Offset.AsInt == 0);
                    break;
                }
                else
                {
                    GcScanRoots(field.FieldType, in argDest, delta + field.Offset.AsInt, frame, topLevel: false);
                }
            }
        }

        /// <summary>
        /// Append single bit to the stream
        /// </summary>
        /// <param name="bit"></param>
        private void AppendBit(uint bit)
        {
            if (bit != 0)
            {
                while (_bits >= 7)
                {
                    Builder.EmitByte((byte)(_pendingByte | 0x80));
                    _pendingByte = 0;
                    _bits -= 7;
                }

                _pendingByte |= (1 << _bits);
            }

            _bits++;
        }

        private void AppendTwoBit(uint bits)
        {
            AppendBit(bits & 1);
            AppendBit(bits >> 1);
        }

        private void AppendInt(uint val)
        {
            do
            {
                AppendBit(val & 1);
                AppendBit((val >> 1) & 1);
                AppendBit((val >> 2) & 1);

                val >>= 3;

                AppendBit((val != 0) ? 1u : 0u);
            }
            while (val != 0);
        }

        /// <summary>
        /// Emit stack pop into the stream (X86 only).
        /// </summary>
        /// <param name="stackPop">Stack pop value</param>
        public void WriteStackPop(uint stackPop)
        {
            if (stackPop < 3)
            {
                AppendTwoBit(stackPop);
            }
            else
            {
                AppendTwoBit(3);
                AppendInt((uint)(stackPop - 3));
            }
        }

        public void WriteToken(uint pos, uint gcRefMapToken)
        {
            uint posDelta = pos - _pos;
            _pos = pos + 1;

            if (posDelta != 0)
            {
                if (posDelta < 4)
                {
                    // Skipping by one slot at a time for small deltas produces smaller encoding.
                    while (posDelta > 0)
                    {
                        AppendTwoBit(0);
                        posDelta--;
                    }
                }
                else
                {
                    AppendTwoBit(3);
                    AppendInt((posDelta - 4) << 1);
                }
            }

            if (gcRefMapToken < 3)
            {
                AppendTwoBit(gcRefMapToken);
            }
            else
            {
                AppendTwoBit(3);
                AppendInt(((gcRefMapToken - 3) << 1) | 1);
            }
        }

        public void Flush()
        {
            if ((_pendingByte & 0x7F) != 0 || _pos == 0)
                Builder.EmitByte((byte)(_pendingByte & 0x7F));

            _pendingByte = 0;
            _bits = 0;

            _pos = 0;
        }
    }
}
