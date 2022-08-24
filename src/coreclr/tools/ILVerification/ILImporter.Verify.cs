// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILVerify;

namespace Internal.IL
{
    class VerificationException : Exception
    {
        public VerificationException()
        {
        }
    }

    class LocalVerificationException : VerificationException
    {
        public LocalVerificationException()
        {
        }
    }

    class VerifierException : Exception
    {
        internal VerifierException(string message) : base(message)
        {
        }
    }

    partial class ILImporter
    {
        readonly MethodDesc _method;
        readonly MethodSignature _methodSignature;
        readonly TypeSystemContext _typeSystemContext;

        readonly TypeDesc _thisType;

        readonly MethodIL _methodIL;
        readonly byte[] _ilBytes;
        readonly LocalVariableDefinition[] _locals;

        readonly bool _initLocals;
        readonly int _maxStack;

        bool[] _instructionBoundaries; // For IL verification

        static readonly StackValue[] s_emptyStack = new StackValue[0];
        StackValue[] _stack = s_emptyStack;
        int _stackTop = 0;

        bool _isThisInitialized;
        bool _modifiesThisPtr;
        bool _trackObjCtorState;

        bool[] _validTargetOffsets;

        int? _delegateCreateStart;

        class ExceptionRegion
        {
            public ILExceptionRegion ILRegion;
        };
        ExceptionRegion[] _exceptionRegions;

        [Flags]
        enum Prefix
        {
            ReadOnly    = 0x01,
            Unaligned   = 0x02,
            Volatile    = 0x04,
            Tail        = 0x08,
            Constrained = 0x10,
            No          = 0x20,
        }
        Prefix _pendingPrefix;
        TypeDesc _constrained;

        int _currentInstructionOffset;

        class BasicBlock
        {
            // Common fields
            public enum ImportState : byte
            {
                Unmarked,
                IsPending,
                WasVerified
            }

            public BasicBlock Next;

            public int StartOffset;
            public ImportState State = ImportState.Unmarked;

            public StackValue[] EntryStack;
            public bool IsThisInitialized = false;

            public bool TryStart;
            public bool FilterStart;
            public bool HandlerStart;

            //these point to the direct enclosing items in _exceptionRegions
            public int? TryIndex;
            public int? HandlerIndex;
            public int? FilterIndex;

            // Used for Backward Branch Constraint
            public bool HasPredecessorWithLowerOffset = false;

            public int ErrorCount
            {
                get;
                private set;
            }
            public void IncrementErrorCount()
            {
                ErrorCount++;
            }
        };

        void EmptyTheStack() => _stackTop = 0;

        void Push(StackValue value)
        {
            FatalCheck(_stackTop < _maxStack, VerifierError.StackOverflow);

            if (_stackTop >= _stack.Length)
                Array.Resize(ref _stack, 2 * _stackTop + 3);
            _stack[_stackTop++] = value;
        }

        StackValue Pop(bool allowUninitThis = false)
        {
            FatalCheck(_stackTop > 0, VerifierError.StackUnderflow);

            var stackValue = _stack[--_stackTop];

            if (!allowUninitThis)
                Check(!_trackObjCtorState || !stackValue.IsThisPtr || _isThisInitialized, VerifierError.UninitStack, stackValue);

            return stackValue;
        }

        public ILImporter(MethodDesc method, MethodIL methodIL)
        {
            _typeSystemContext = method.Context;

            // Instantiate method and its owning type
            var instantiatedType = method.OwningType;
            var instantiatedMethod = method;
            if (instantiatedType.HasInstantiation)
            {
                Instantiation genericTypeInstantiation = InstantiatedGenericParameter.CreateGenericTypeInstantiaton(instantiatedType.Instantiation);
                instantiatedType = _typeSystemContext.GetInstantiatedType((MetadataType)instantiatedType, genericTypeInstantiation);
                instantiatedMethod = _typeSystemContext.GetMethodForInstantiatedType(instantiatedMethod.GetTypicalMethodDefinition(), (InstantiatedType)instantiatedType);
            }

            if (instantiatedMethod.HasInstantiation)
            {
                Instantiation genericMethodInstantiation = InstantiatedGenericParameter.CreateGenericMethodInstantiation(
                    instantiatedType.Instantiation, instantiatedMethod.Instantiation);
                instantiatedMethod = _typeSystemContext.GetInstantiatedMethod(instantiatedMethod, genericMethodInstantiation);
            }
            _method = instantiatedMethod;

            _methodSignature = _method.Signature;
            _methodIL = method == instantiatedMethod ? methodIL : new InstantiatedMethodIL(instantiatedMethod, methodIL);

            // Determine this type
            if (!_method.Signature.IsStatic)
            {
                _thisType = instantiatedType;

                // ECMA-335 II.13.3 Methods of value types, P. 164:
                // ... By contrast, instance and virtual methods of value types shall be coded to expect a
                // managed pointer(see Partition I) to an unboxed instance of the value type. ...
                if (_thisType.IsValueType)
                    _thisType = _thisType.MakeByRefType();
            }

            _initLocals = _methodIL.IsInitLocals;

            _maxStack = _methodIL.MaxStack;

            _isThisInitialized = false;
            _trackObjCtorState = !_methodSignature.IsStatic && _method.IsConstructor && !method.OwningType.IsValueType;

            _ilBytes = _methodIL.GetILBytes();
            _locals = _methodIL.GetLocals();

            var ilExceptionRegions = _methodIL.GetExceptionRegions();
            _exceptionRegions = new ExceptionRegion[ilExceptionRegions.Length];
            for (int i = 0; i < ilExceptionRegions.Length; i++)
            {
                _exceptionRegions[i] = new ExceptionRegion() { ILRegion = ilExceptionRegions[i] };
            }
        }

        public Action<ErrorArgument[], VerifierError> ReportVerificationError { set; private get; }

        public bool SanityChecks { set; private get; }

        public void Verify()
        {
            _instructionBoundaries = new bool[_ilBytes.Length];

            FindBasicBlocks();
            FindEnclosingExceptionRegions();
            InitialPass();
            ImportBasicBlocks();
        }

        private void FindEnclosingExceptionRegions()
        {
            for (int i = 0; i < _basicBlocks.Length; i++)
            {
                if (_basicBlocks[i] == null)
                    continue;

                var basicBlock = _basicBlocks[i];
                var offset = basicBlock.StartOffset;

                for (int j = 0; j < _exceptionRegions.Length; j++)
                {
                    var r = _exceptionRegions[j].ILRegion;
                    // Check if offset is within the range [TryOffset, TryOffset + TryLength[
                    if (r.TryOffset <= offset && offset < r.TryOffset + r.TryLength)
                    {
                        if (!basicBlock.TryIndex.HasValue)
                        {
                            basicBlock.TryIndex = j;
                        }
                        else
                        {
                            var currentlySelected = _exceptionRegions[basicBlock.TryIndex.Value].ILRegion;
                            var probeItem = _exceptionRegions[j].ILRegion;

                            if (currentlySelected.TryOffset < probeItem.TryOffset &&
                                currentlySelected.TryOffset + currentlySelected.TryLength > probeItem.TryOffset + probeItem.TryLength)
                            {
                                basicBlock.TryIndex = j;
                            }
                        }
                    }
                    // Check if offset is within the range [HandlerOffset, HandlerOffset + HandlerLength[
                    if (r.HandlerOffset <= offset && offset < r.HandlerOffset + r.HandlerLength)
                    {
                        if (!basicBlock.HandlerIndex.HasValue)
                        {
                            basicBlock.HandlerIndex = j;
                        }
                        else
                        {
                            var currentlySelected = _exceptionRegions[basicBlock.HandlerIndex.Value].ILRegion;
                            var probeItem = _exceptionRegions[j].ILRegion;

                            if (currentlySelected.HandlerOffset < probeItem.HandlerOffset &&
                                currentlySelected.HandlerOffset + currentlySelected.HandlerLength > probeItem.HandlerOffset + probeItem.HandlerLength)
                            {
                                basicBlock.HandlerIndex = j;
                            }
                        }
                    }
                    // Check if offset is within the range [FilterOffset, HandlerOffset[
                    if (r.FilterOffset != -1 && r.FilterOffset <= offset && offset < r.HandlerOffset)
                    {
                        if (!basicBlock.FilterIndex.HasValue)
                        {
                            basicBlock.FilterIndex = j;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the method's il modifies the this pointer and builds up the
        /// array of valid target offsets.
        /// </summary>
        private void InitialPass()
        {
            FatalCheck(_ilBytes.Length > 0, VerifierError.CodeSizeZero);

            _modifiesThisPtr = false;
            _validTargetOffsets = new bool[_ilBytes.Length];

            bool previousWasPrefix = false;

            _currentOffset = 0;

            while (_currentOffset < _ilBytes.Length)
            {
                if (!previousWasPrefix) // The instruction following a prefix is not a valid branch target.
                    _validTargetOffsets[_currentOffset] = true;

                ILOpcode opCode = (ILOpcode)ReadILByte();

                previousWasPrefix = false;
            again:
                switch (opCode)
                {
                    // Check this pointer modification
                    case ILOpcode.starg_s:
                    case ILOpcode.ldarga_s:
                        if (ReadILByte() == 0)
                        {
                            _modifiesThisPtr = true;
                            break;
                        }
                        break;
                    case ILOpcode.starg:
                    case ILOpcode.ldarga:
                        if (ReadILUInt16() == 0)
                        {
                            _modifiesThisPtr = true;
                            break;
                        }
                        break;

                    // Keep track of prefixes
                    case ILOpcode.unaligned:
                    case ILOpcode.no:
                        previousWasPrefix = true;
                        SkipIL(1);
                        continue;
                    case ILOpcode.constrained:
                        previousWasPrefix = true;
                        SkipIL(4);
                        continue;
                    case ILOpcode.tail:
                    case ILOpcode.volatile_:
                    case ILOpcode.readonly_:
                        previousWasPrefix = true;
                        continue;

                    // Check for block predecessors with lower il offset
                    case ILOpcode.br:
                    case ILOpcode.leave:
                        MarkPredecessorWithLowerOffset((int)ReadILUInt32());
                        continue;
                    case ILOpcode.brfalse:
                    case ILOpcode.brtrue:
                    case ILOpcode.beq:
                    case ILOpcode.bge:
                    case ILOpcode.bgt:
                    case ILOpcode.ble:
                    case ILOpcode.blt:
                    case ILOpcode.bne_un:
                    case ILOpcode.bge_un:
                    case ILOpcode.bgt_un:
                    case ILOpcode.ble_un:
                    case ILOpcode.blt_un:
                        MarkPredecessorWithLowerOffset((int)ReadILUInt32());
                        break;
                    case ILOpcode.br_s:
                    case ILOpcode.leave_s:
                        MarkPredecessorWithLowerOffset((sbyte)ReadILByte());
                        continue;
                    case ILOpcode.brfalse_s:
                    case ILOpcode.brtrue_s:
                    case ILOpcode.beq_s:
                    case ILOpcode.bge_s:
                    case ILOpcode.bgt_s:
                    case ILOpcode.ble_s:
                    case ILOpcode.blt_s:
                    case ILOpcode.bne_un_s:
                    case ILOpcode.bge_un_s:
                    case ILOpcode.bgt_un_s:
                    case ILOpcode.ble_un_s:
                    case ILOpcode.blt_un_s:
                        MarkPredecessorWithLowerOffset((sbyte)ReadILByte());
                        break;
                    case ILOpcode.switch_:
                        {
                            uint count = ReadILUInt32();
                            int[] jmpDeltas = new int[count];
                            for (uint i = 0; i < count; i++)
                                jmpDeltas[i] = (int)ReadILUInt32();

                            foreach (int delta in jmpDeltas)
                                MarkPredecessorWithLowerOffset(delta);
                        }
                        break;

                    // Skip all other Opcodes
                    case ILOpcode.ret:
                    case ILOpcode.throw_:
                    case ILOpcode.rethrow:
                    case ILOpcode.endfinally:
                    case ILOpcode.endfilter:
                        continue;
                    case ILOpcode.ldarg_s:
                    case ILOpcode.ldloc_s:
                    case ILOpcode.ldloca_s:
                    case ILOpcode.stloc_s:
                    case ILOpcode.ldc_i4_s:
                        SkipIL(1);
                        break;
                    case ILOpcode.ldarg:
                    case ILOpcode.ldloc:
                    case ILOpcode.ldloca:
                    case ILOpcode.stloc:
                        SkipIL(2);
                        break;
                    case ILOpcode.ldc_i4:
                    case ILOpcode.ldc_r4:
                    case ILOpcode.call:
                    case ILOpcode.calli:
                    case ILOpcode.callvirt:
                    case ILOpcode.cpobj:
                    case ILOpcode.ldobj:
                    case ILOpcode.ldstr:
                    case ILOpcode.newobj:
                    case ILOpcode.castclass:
                    case ILOpcode.isinst:
                    case ILOpcode.unbox:
                    case ILOpcode.ldfld:
                    case ILOpcode.ldflda:
                    case ILOpcode.stfld:
                    case ILOpcode.ldsfld:
                    case ILOpcode.ldsflda:
                    case ILOpcode.stsfld:
                    case ILOpcode.stobj:
                    case ILOpcode.box:
                    case ILOpcode.newarr:
                    case ILOpcode.ldelema:
                    case ILOpcode.ldelem:
                    case ILOpcode.stelem:
                    case ILOpcode.unbox_any:
                    case ILOpcode.refanyval:
                    case ILOpcode.mkrefany:
                    case ILOpcode.ldtoken:
                    case ILOpcode.ldftn:
                    case ILOpcode.ldvirtftn:
                    case ILOpcode.initobj:
                    case ILOpcode.sizeof_:
                        SkipIL(4);
                        break;
                    case ILOpcode.jmp:
                        SkipIL(4);
                        continue;
                    case ILOpcode.ldc_i8:
                    case ILOpcode.ldc_r8:
                        SkipIL(8);
                        break;
                    case ILOpcode.prefix1:
                        opCode = (ILOpcode)(0x100 + ReadILByte());
                        goto again;
                    default:
                        break;
                }

                if (_currentOffset < _basicBlocks.Length)
                {
                    var fallthrough = _basicBlocks[_currentOffset];
                    if (fallthrough != null)
                        MarkPredecessorWithLowerOffset(0);
                }
            }
        }

        void MarkPredecessorWithLowerOffset(int delta)
        {
            if (delta >= 0)
                _basicBlocks[_currentOffset + delta].HasPredecessorWithLowerOffset = true;
        }

        void AbortBasicBlockVerification()
        {
            throw new LocalVerificationException();
        }

        void AbortMethodVerification()
        {
            throw new VerificationException();
        }

        // Check whether the condition is true. If not, terminate the verification of current method.
        void FatalCheck(bool cond, VerifierError error)
        {
            if (!Check(cond, error))
                AbortMethodVerification();
        }

        // Check whether the condition is true. If not, terminate the verification of current method.
        void FatalCheck(bool cond, VerifierError error, StackValue found)
        {
            if (!Check(cond, error, found))
                AbortMethodVerification();
        }

        // Check whether the condition is true. If not, terminate the verification of current method.
        void FatalCheck(bool cond, VerifierError error, StackValue found, StackValue expected)
        {
            if (!Check(cond, error, found, expected))
                AbortMethodVerification();
        }

        // If not, report verification error and continue verification.
        void VerificationError(VerifierError error)
        {
            if (_currentBasicBlock != null)
                _currentBasicBlock.IncrementErrorCount();

            var args = new ErrorArgument[]
            {
                new ErrorArgument("Offset", _currentInstructionOffset)
            };
            ReportVerificationError(args, error);
        }

        void VerificationError(VerifierError error, object found)
        {
            if (_currentBasicBlock != null)
                _currentBasicBlock.IncrementErrorCount();

            var args = new ErrorArgument[]
            {
                new ErrorArgument("Offset", _currentInstructionOffset),
                new ErrorArgument("Found", found.ToString())
            };
            ReportVerificationError(args, error);
        }

        void VerificationError(VerifierError error, object found, object expected)
        {
            if (_currentBasicBlock != null)
                _currentBasicBlock.IncrementErrorCount();

            var args = new ErrorArgument[]
            {
                new ErrorArgument("Offset", _currentInstructionOffset),
                new ErrorArgument("Found", found.ToString()),
                new ErrorArgument("Expected", expected.ToString())
            };
            ReportVerificationError(args, error);
        }

        // Check whether the condition is true. If not, report verification error and continue verification.
        bool Check(bool cond, VerifierError error)
        {
            if (!cond)
                VerificationError(error);
            return cond;
        }

        bool Check(bool cond, VerifierError error, StackValue found)
        {
            if (!cond)
                VerificationError(error, found);
            return cond;
        }

        bool Check(bool cond, VerifierError error, StackValue found, StackValue expected)
        {
            if (!cond)
                VerificationError(error, found, expected);
            return cond;
        }

        void CheckIsNumeric(StackValue value)
        {
            if (!Check(StackValueKind.Int32 <= value.Kind && value.Kind <= StackValueKind.Float,
                VerifierError.ExpectedNumericType, value))
            {
                AbortBasicBlockVerification();
            }
        }

        void CheckIsInteger(StackValue value)
        {
            if (!Check(StackValueKind.Int32 <= value.Kind && value.Kind <= StackValueKind.NativeInt,
                VerifierError.ExpectedIntegerType, value))
            {
                AbortBasicBlockVerification();
            }
        }

        void CheckIsIndex(StackValue value)
        {
            if (!Check(value.Kind == StackValueKind.Int32 || value.Kind == StackValueKind.NativeInt,
                VerifierError.StackUnexpected /* TODO: ExpectedIndex */, value))
            {
                AbortBasicBlockVerification();
            }
        }

        void CheckIsByRef(StackValue value)
        {
            if (!Check(value.Kind == StackValueKind.ByRef, VerifierError.StackByRef, value))
            {
                AbortBasicBlockVerification();
            }
        }

        void CheckIsArray(StackValue value)
        {
            Check((value.Kind == StackValueKind.ObjRef) && ((value.Type == null) || value.Type.IsSzArray),
                VerifierError.ExpectedArray /* , value */);
        }

        void CheckIsAssignable(StackValue src, StackValue dst, VerifierError error = VerifierError.StackUnexpected)
        {
            if (!IsAssignable(src, dst))
                VerificationError(error, src, dst);
        }

        private void CheckIsValidLeaveTarget(BasicBlock src, BasicBlock target)
        {
            if (!_validTargetOffsets[target.StartOffset])
            {
                VerificationError(VerifierError.BadJumpTarget);
                return;
            }

            // If the source is within filter, target shall be within the same
            if (src.FilterIndex.HasValue && src.FilterIndex != target.FilterIndex)
            {
                VerificationError(VerifierError.LeaveOutOfFilter);
            }

            // If the source is within fault handler or finally handler, target shall be within the same
            if (src.HandlerIndex.HasValue && src.HandlerIndex != target.HandlerIndex)
            {
                var regionKind = _exceptionRegions[src.HandlerIndex.Value].ILRegion.Kind;
                if (regionKind == ILExceptionRegionKind.Fault)
                    VerificationError(VerifierError.LeaveOutOfFault);
                else if (regionKind == ILExceptionRegionKind.Finally)
                    VerificationError(VerifierError.LeaveOutOfFinally);
            }

            // If the source is within a try block, target shall be within the same or an enclosing try block
            // or the first instruction of a disjoint try block
            // or not within any try block
            bool invalidLeaveIntoTry = false;
            if (src.TryIndex.HasValue && src.TryIndex != target.TryIndex)
            {
                if (target.TryIndex.HasValue)
                {
                    ref var srcRegion = ref _exceptionRegions[src.TryIndex.Value].ILRegion;
                    ref var targetRegion = ref _exceptionRegions[target.TryIndex.Value].ILRegion;

                    // Target is not enclosing source
                    if (targetRegion.TryOffset > srcRegion.TryOffset ||
                        src.StartOffset >= targetRegion.TryOffset + targetRegion.TryLength)
                    {
                        // Target is not first instruction
                        if (target.StartOffset != targetRegion.TryOffset)
                        {
                            VerificationError(VerifierError.LeaveIntoTry);
                            invalidLeaveIntoTry = true;
                        }
                        else if (srcRegion.TryOffset <= targetRegion.TryOffset &&
                            srcRegion.TryOffset + srcRegion.TryLength > targetRegion.TryOffset) // Source is enclosing target
                        {
                            if (!IsDirectChildRegion(src, target))
                            {
                                VerificationError(VerifierError.LeaveIntoTry);
                                invalidLeaveIntoTry = true;
                            }
                        }
                        else if (!IsDisjointTryBlock(ref targetRegion, ref srcRegion))
                        {
                            VerificationError(VerifierError.LeaveIntoTry);
                            invalidLeaveIntoTry = true;
                        }
                    }
                }
            }

            // If the source is within a catch or filtered handler, target shall be within same catch or filtered handler
            // or within the associated try block
            // or within a try block enclosing the catch / filtered handler
            // or the first instruction of a disjoint try block
            // or not within any try block
            if (src.HandlerIndex.HasValue && src.HandlerIndex != target.HandlerIndex)
            {
                if (target.TryIndex.HasValue)
                {
                    ref var srcRegion = ref _exceptionRegions[src.HandlerIndex.Value].ILRegion;
                    ref var targetRegion = ref _exceptionRegions[target.TryIndex.Value].ILRegion;

                    // If target is not associated try block, and not enclosing srcRegion
                    if (target.TryIndex != src.HandlerIndex &&
                        (targetRegion.TryOffset > srcRegion.HandlerOffset ||
                        targetRegion.TryOffset + targetRegion.TryLength < srcRegion.HandlerOffset))
                    {
                        // If target is not first instruction of try, or not a direct sibling
                        if (target.StartOffset != targetRegion.TryOffset || !IsDisjointTryBlock(ref targetRegion, ref srcRegion))
                            VerificationError(VerifierError.LeaveIntoTry);
                    }
                }
            }

            // If the target is within a filter or handler, source shall be within same
            if (target.HandlerIndex.HasValue && src.HandlerIndex != target.HandlerIndex)
            {
                ref var targetRegion = ref _exceptionRegions[target.HandlerIndex.Value].ILRegion;
                // If target region is not enclosing source
                if (targetRegion.HandlerOffset > src.StartOffset || targetRegion.HandlerOffset + targetRegion.HandlerLength < src.StartOffset)
                    VerificationError(VerifierError.LeaveIntoHandler);
            }
            if (target.FilterIndex.HasValue && src.FilterIndex != target.FilterIndex)
            {
                ref var targetRegion = ref _exceptionRegions[target.FilterIndex.Value].ILRegion;
                var filterLength = targetRegion.HandlerOffset - targetRegion.FilterOffset;

                // If target region is not enclosing source
                if (targetRegion.FilterOffset > src.StartOffset || targetRegion.FilterOffset + filterLength < src.StartOffset)
                    VerificationError(VerifierError.LeaveIntoFilter);
            }

            // If the target is within a try block (except first instruction), source shall be within same
            // or within associated handler
            if (!invalidLeaveIntoTry && target.TryIndex.HasValue && src.TryIndex != target.TryIndex)
            {
                ref var targetRegion = ref _exceptionRegions[target.TryIndex.Value].ILRegion;

                if (target.StartOffset != targetRegion.TryOffset && // Not first instruction
                    (!src.HandlerIndex.HasValue || src.HandlerIndex != target.TryIndex) && // Not associated handler
                    (targetRegion.TryOffset > src.StartOffset || targetRegion.TryOffset + targetRegion.TryLength < src.StartOffset)) // Target region does not enclose source
                    VerificationError(VerifierError.LeaveIntoTry);
            }
        }

        bool IsValidBranchTarget(BasicBlock src, BasicBlock target, bool isFallthrough, bool reportErrors = true)
        {
            if (!_validTargetOffsets[target.StartOffset])
            {
                if (reportErrors)
                    VerificationError(VerifierError.BadJumpTarget);
                return false;
            }

            bool isValid = true;

            if (src.TryIndex != target.TryIndex)
            {
                if (src.TryIndex == null)
                {
                    // Branching to first instruction of try-block is valid
                    if (target.StartOffset != _exceptionRegions[target.TryIndex.Value].ILRegion.TryOffset || !IsDirectChildRegion(src, target))
                    {
                        if (reportErrors)
                        {
                            Debug.Assert(!isFallthrough); // This should not be reachable by fallthrough
                            VerificationError(VerifierError.BranchIntoTry);
                        }
                        isValid = false;
                    }
                }
                else if (target.TryIndex == null)
                {
                    if (reportErrors)
                    {
                        if (isFallthrough)
                            VerificationError(VerifierError.FallthroughException);
                        else
                            VerificationError(VerifierError.BranchOutOfTry);
                    }
                    isValid = false;
                }
                else
                {
                    ref var srcRegion = ref _exceptionRegions[src.TryIndex.Value].ILRegion;
                    ref var targetRegion = ref _exceptionRegions[target.TryIndex.Value].ILRegion;
                    // If target is inside source region
                    if (srcRegion.TryOffset <= targetRegion.TryOffset &&
                        target.StartOffset < srcRegion.TryOffset + srcRegion.TryLength)
                    {
                        // Only branching to first instruction of try-block is valid
                        if (target.StartOffset != targetRegion.TryOffset || !IsDirectChildRegion(src, target))
                        {
                            if (reportErrors)
                            {
                                Debug.Assert(!isFallthrough); // This should not be reachable by fallthrough
                                VerificationError(VerifierError.BranchIntoTry);
                            }
                            isValid = false;
                        }
                    }
                    else
                    {
                        if (reportErrors)
                        {
                            if (isFallthrough)
                                VerificationError(VerifierError.FallthroughException);
                            else
                                VerificationError(VerifierError.BranchOutOfTry);
                        }
                        isValid = false;
                    }
                }
            }

            if (src.FilterIndex != target.FilterIndex)
            {
                if (src.FilterIndex == null)
                {
                    if (reportErrors)
                    {
                        if (isFallthrough)
                            VerificationError(VerifierError.FallthroughIntoFilter);
                        else
                            VerificationError(VerifierError.BranchIntoFilter);
                    }
                    isValid = false;
                }
                else if (target.HandlerIndex == null)
                {
                    if (reportErrors)
                    {
                        if (isFallthrough)
                            VerificationError(VerifierError.FallthroughException);
                        else
                            VerificationError(VerifierError.BranchOutOfFilter);
                    }
                    isValid = false;
                }
                else
                {
                    ref var srcRegion = ref _exceptionRegions[src.FilterIndex.Value].ILRegion;
                    ref var targetRegion = ref _exceptionRegions[target.FilterIndex.Value].ILRegion;
                    if (srcRegion.FilterOffset <= targetRegion.FilterOffset)
                    {
                        if (reportErrors)
                        {
                            if (isFallthrough)
                                VerificationError(VerifierError.FallthroughIntoFilter);
                            else
                                VerificationError(VerifierError.BranchIntoFilter);
                        }
                        isValid = false;
                    }
                    else
                    {
                        if (reportErrors)
                        {
                            if (isFallthrough)
                                VerificationError(VerifierError.FallthroughException);
                            else
                                VerificationError(VerifierError.BranchOutOfFilter);
                        }
                        isValid = false;
                    }
                }
            }

            if (src.HandlerIndex != target.HandlerIndex)
            {
                if (src.HandlerIndex == null)
                {
                    if (reportErrors)
                    {
                        if (isFallthrough)
                            VerificationError(VerifierError.FallthroughIntoHandler);
                        else
                            VerificationError(VerifierError.BranchIntoHandler);
                    }
                    isValid = false;
                }
                else if (target.HandlerIndex == null)
                {
                    if (reportErrors)
                    {
                        if (isFallthrough)
                            VerificationError(VerifierError.FallthroughException);
                        else
                        {
                            if (_exceptionRegions[src.HandlerIndex.Value].ILRegion.Kind == ILExceptionRegionKind.Finally)
                                VerificationError(VerifierError.BranchOutOfFinally);
                            else
                                VerificationError(VerifierError.BranchOutOfHandler);
                        }
                    }
                    isValid = false;
                }
                else
                {
                    ref var srcRegion = ref _exceptionRegions[src.HandlerIndex.Value].ILRegion;
                    ref var targetRegion = ref _exceptionRegions[target.HandlerIndex.Value].ILRegion;
                    if (srcRegion.HandlerOffset <= targetRegion.HandlerOffset)
                    {
                        if (reportErrors)
                        {
                            if (isFallthrough)
                                VerificationError(VerifierError.FallthroughIntoHandler);
                            else
                                VerificationError(VerifierError.BranchIntoHandler);
                        }
                        isValid = false;
                    }
                    else
                    {
                        if (reportErrors)
                        {
                            if (isFallthrough)
                                VerificationError(VerifierError.FallthroughException);
                            else
                            {
                                if (srcRegion.Kind == ILExceptionRegionKind.Finally)
                                    VerificationError(VerifierError.BranchOutOfFinally);
                                else
                                    VerificationError(VerifierError.BranchOutOfHandler);
                            }
                        }
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        /// <summary>
        /// Checks whether the given enclosed try block is a direct child try-region of
        /// the given enclosing try block.
        /// </summary>
        /// <param name="enclosingBlock">The block enclosing the try block given by <paramref name="enclosedBlock"/>.</param>
        /// <param name="enclosedBlock">The block to check whether it is a direct child try-region of <paramref name="enclosingBlock"/>.</param>
        /// <returns>True if <paramref name="enclosedBlock"/> is a direct child try region of <paramref name="enclosingBlock"/>.</returns>
        bool IsDirectChildRegion(BasicBlock enclosingBlock, BasicBlock enclosedBlock)
        {
            ref var enclosedRegion = ref _exceptionRegions[enclosedBlock.TryIndex.Value].ILRegion;

            // Walk from enclosed try start backwards and check each BasicBlock whether it is a try-start
            for (int i = enclosedRegion.TryOffset - 1; i > enclosingBlock.StartOffset; --i)
            {
                var block = _basicBlocks[i];
                if (block == null)
                    continue;

                if (block.TryStart && block.TryIndex != enclosingBlock.TryIndex)
                {
                    ref var blockRegion = ref _exceptionRegions[block.TryIndex.Value].ILRegion;
                    // blockRegion is actually enclosing enclosedRegion
                    if (blockRegion.TryOffset + blockRegion.TryLength > enclosedRegion.TryOffset)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether the try block <paramref name="disjoint"/> is a disjoint try block relative to <paramref name="source"/>.
        /// </summary>
        /// <returns>True if <paramref name="disjoint"/> is a disjoint try block relative to <paramref name="source"/>.</returns>
        bool IsDisjointTryBlock(ref ILExceptionRegion disjoint, ref ILExceptionRegion source)
        {
            if (source.TryOffset <= disjoint.TryOffset && source.TryOffset + source.TryLength >= disjoint.TryOffset + disjoint.TryLength)
            {
                // source is enclosing disjoint
                return false;
            }

            // Walk from disjoint region backwards and check for enclosing exception regions.
            for (int i = disjoint.TryOffset - 1; i >= 0; --i)
            {
                var block = _basicBlocks[i];
                if (block == null)
                    continue;

                if (block.TryStart)
                {
                    ref var blockRegion = ref _exceptionRegions[block.TryIndex.Value].ILRegion;
                    // blockRegion is enclosing disjoint, but not source
                    if (blockRegion.TryOffset + blockRegion.TryLength > disjoint.TryOffset &&
                        (blockRegion.TryOffset > source.TryOffset || blockRegion.TryOffset + blockRegion.TryLength <= source.TryOffset))
                        return false;
                }

                if (block.HandlerStart)
                {
                    ref var blockRegion = ref _exceptionRegions[block.HandlerIndex.Value].ILRegion;
                    // blockRegion is enclosing secondRegion, but not source
                    if (blockRegion.HandlerOffset + blockRegion.HandlerLength > disjoint.TryOffset &&
                        (blockRegion.HandlerOffset > source.TryOffset || blockRegion.HandlerOffset + blockRegion.HandlerLength <= source.TryOffset))
                        return false;
                }

                if (block.FilterStart)
                {
                    ref var blockRegion = ref _exceptionRegions[block.FilterIndex.Value].ILRegion;
                    // blockRegion is enclosing secondRegion, but not source
                    var filterLength = blockRegion.HandlerOffset - blockRegion.FilterOffset;
                    if (blockRegion.FilterOffset + filterLength > disjoint.TryOffset &&
                        (blockRegion.FilterOffset > source.TryOffset || blockRegion.FilterOffset + filterLength <= source.TryOffset))
                        return false;
                }
            }

            return true;
        }

        // For now, match PEVerify type formating to make it easy to compare with baseline
        static string TypeToStringForIsAssignable(TypeDesc type)
        {
            switch (type.Category)
            {
                case TypeFlags.Boolean: return "Boolean";
                case TypeFlags.Char: return "Char";
                case TypeFlags.SByte:
                case TypeFlags.Byte: return "Byte";
                case TypeFlags.Int16:
                case TypeFlags.UInt16: return "Short";
                case TypeFlags.Int32:
                case TypeFlags.UInt32: return "Int32";
                case TypeFlags.Int64:
                case TypeFlags.UInt64: return "Long";
                case TypeFlags.Single: return "Single";
                case TypeFlags.Double: return "Double";
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr: return "Native Int";
            }

            return StackValue.CreateFromType(type).ToString();
        }

        void CheckIsAssignable(TypeDesc src, TypeDesc dst)
        {
            if (!IsAssignable(src, dst))
            {
                VerificationError(VerifierError.StackUnexpected, TypeToStringForIsAssignable(src), TypeToStringForIsAssignable(dst));
            }
        }

        void CheckIsArrayElementCompatibleWith(TypeDesc src, TypeDesc dst)
        {
            if (!IsAssignable(src, dst, true))
            {
                // TODO: Better error message
                VerificationError(VerifierError.StackUnexpected, TypeToStringForIsAssignable(src));
            }
        }

        void CheckIsPointerElementCompatibleWith(TypeDesc src, TypeDesc dst)
        {
            if (!(src == dst || IsSameReducedType(src, dst)))
            {
                // TODO: Better error message
                // VerificationError(VerifierError.StackUnexpected, TypeToStringForIsAssignable(src), TypeToStringForIsAssignable(dst));
                VerificationError(VerifierError.StackUnexpectedArrayType, TypeToStringForIsAssignable(src));
            }
        }

        void CheckIsObjRef(TypeDesc type)
        {
            if (!IsAssignable(type, GetWellKnownType(WellKnownType.Object), false))
            {
                // TODO: Better error message
                VerificationError(VerifierError.StackUnexpected, TypeToStringForIsAssignable(type));
            }
        }

        void CheckIsObjRef(StackValue value)
        {
            if (value.Kind != StackValueKind.ObjRef)
                VerificationError(VerifierError.StackObjRef, value);
        }

        private void CheckIsNotPointer(TypeDesc type)
        {
            if (type.IsPointer)
                VerificationError(VerifierError.UnmanagedPointer);
        }

        void CheckIsComparable(StackValue a, StackValue b, ILOpcode op)
        {
            if (!IsBinaryComparable(a, b, op))
            {
                VerificationError(VerifierError.StackUnexpected, a, b);
            }
        }

        void CheckDelegateCreation(StackValue ftn, StackValue obj)
        {
            if (!_delegateCreateStart.HasValue)
            {
                VerificationError(VerifierError.DelegatePattern);
                return;
            }

            int delegateStart = _delegateCreateStart.Value;

            if (_currentInstructionOffset - delegateStart == 6) // ldftn <tok> takes 6 bytes
            {
                if (GetOpcodeAt(delegateStart) != ILOpcode.ldftn)
                {
                    VerificationError(VerifierError.DelegatePattern);
                    return;
                }
                else
                {
                    // See "Rules for non-virtual call to a non-final virtual method" in ImportCall
                    if (ftn.Method.IsVirtual && !ftn.Method.IsFinal && !obj.IsBoxedValueType)
                    {
                        var methodTypeDef = ftn.Method.OwningType.GetTypeDefinition() as MetadataType; // Method is always considered final if owning type is sealed
                        if (methodTypeDef == null || !methodTypeDef.IsSealed)
                            Check(obj.IsThisPtr && !_modifiesThisPtr, VerifierError.LdftnNonFinalVirtual);
                    }
                }
            }
            else if (_currentInstructionOffset - _delegateCreateStart == 7) // dup, ldvirtftn <tok> takes 7 bytes
            {
                if (GetOpcodeAt(delegateStart) != ILOpcode.dup ||
                    GetOpcodeAt(delegateStart + 1) != ILOpcode.ldvirtftn)
                {
                    VerificationError(VerifierError.DelegatePattern);
                    return;
                }
            }
            else
                VerificationError(VerifierError.DelegatePattern);
        }

        bool IsDelegateAssignable(MethodDesc targetMethod, TypeDesc delegateType, TypeDesc firstArg)
        {
            var invokeMethod = delegateType.GetMethod("Invoke", null);
            if (invokeMethod == null)
                return false;

            var targetSignature = targetMethod.Signature;
            var invokeSignature = invokeMethod.Signature;

            // Compare calling convention ignoring distinction between static and instance
            if ((targetSignature.Flags & ~MethodSignatureFlags.Static) != (invokeSignature.Flags & ~MethodSignatureFlags.Static))
                return false;

            // Compare return type
            if (!IsAssignable(targetSignature.ReturnType, invokeSignature.ReturnType))
                return false;

            int totalTargetArgs = targetSignature.Length + (targetSignature.IsStatic ? 0 : 1);

            // Compare signature parameters

            bool isOpenDelegate;
            if (totalTargetArgs == invokeSignature.Length)
            {
                // All arguments provided by invoke, delegate must be open.
                isOpenDelegate = true;
            }
            else if (totalTargetArgs == invokeSignature.Length + 1)
            {
                // One too few arguments provided by invoke, delegate must be closed.
                isOpenDelegate = false;
            }
            else
            {
                return false;
            }

            // An open static delegate which takes no arguments. In that case we're done.
            if (totalTargetArgs == 0)
            {
                Debug.Assert(isOpenDelegate);
                return true;
            }

            int consumedArgs = 0;

            TypeDesc firstInvokeArg;
            if (isOpenDelegate)
            {
                // If we're looking at an open delegate but the caller has provided a target it's not a match.
                if (firstArg != null)
                    return false;

                firstInvokeArg = invokeSignature[0];
                consumedArgs++;
            }
            else
            {
                // If we're looking at a closed delegate but the caller has not provided a target it's not a match.
                if (firstArg == null)
                    return false;

                firstInvokeArg = firstArg;
            }

            TypeDesc firstTargetArg;
            if (targetSignature.IsStatic)
            {
                // Checked above
                Debug.Assert(targetSignature.Length != 0);

                // The first argument for a static method is the first fixed arg.
                firstTargetArg = targetSignature[0];
                consumedArgs--;
            }
            else
            {
                // The type of the first argument to an instance method is from the method type.
                firstTargetArg = targetMethod.OwningType;

                // If the delegate is open and the target method is on a value type or primitive then the first argument of the invoke
                // method must be a reference to that type. So make the type we got from the reference to a ref. (We don't need to
                // do this for the closed instance case because there we got the invocation side type from the first arg passed in, i.e.
                // ref was stripped from it implicitly).
                if (isOpenDelegate && firstTargetArg.IsValueType)
                    firstTargetArg = firstTargetArg.MakeByRefType();
            }

            if (!IsAssignable(firstInvokeArg, firstTargetArg))
                return false;

            // We better have same number of remaining args
            if (invokeSignature.Length - consumedArgs != targetSignature.Length)
                return false;

            for (int i = isOpenDelegate ? 1 : 0; i < invokeSignature.Length; i++)
            {
                if (!IsAssignable(invokeSignature[i], targetSignature[i - consumedArgs]))
                    return false;
            }

            return true;
        }

        ILOpcode GetOpcodeAt(int instructionOffset)
        {
            var opCode = (ILOpcode)_ilBytes[instructionOffset];
            if (opCode == ILOpcode.prefix1)
                opCode = (ILOpcode)(0x100 + _ilBytes[instructionOffset + 1]);

            return opCode;
        }

        void Unverifiable()
        {
            VerificationError(VerifierError.Unverifiable);
        }

        TypeDesc GetWellKnownType(WellKnownType wellKnownType)
        {
            return _typeSystemContext.GetWellKnownType(wellKnownType);
        }

        void HandleTokenResolveException(int token)
        {
            var args = new ErrorArgument[]
             {
                new ErrorArgument("Offset", _currentInstructionOffset),
                new ErrorArgument("Token", token)
             };
            ReportVerificationError(args, VerifierError.TokenResolve);
            AbortBasicBlockVerification();
        }

        Object ResolveToken(int token)
        {
            Object tokenObj = null;
            try
            {
                tokenObj = _methodIL.GetObject(token);
            }
            catch (BadImageFormatException)
            {
                HandleTokenResolveException(token);
            }
            catch (ArgumentException)
            {
                HandleTokenResolveException(token);
            }
            return tokenObj;
        }

        TypeDesc ResolveTypeToken(int token)
        {
            object tokenObj = ResolveToken(token);
            FatalCheck(tokenObj is TypeDesc, VerifierError.ExpectedTypeToken);
            return (TypeDesc)tokenObj;
        }

        FieldDesc ResolveFieldToken(int token)
        {
            object tokenObj = ResolveToken(token);
            FatalCheck(tokenObj is FieldDesc, VerifierError.ExpectedFieldToken);
            return (FieldDesc)tokenObj;
        }

        MethodDesc ResolveMethodToken(int token)
        {
            object tokenObj = ResolveToken(token);
            FatalCheck(tokenObj is MethodDesc, VerifierError.ExpectedMethodToken);
            return (MethodDesc)tokenObj;
        }

        void MarkInstructionBoundary()
        {
            _instructionBoundaries[_currentOffset] = true;
        }

        void StartImportingInstruction()
        {
            _currentInstructionOffset = _currentOffset;
        }

        void EndImportingInstruction()
        {
            CheckPendingPrefix(_pendingPrefix);
            ClearPendingPrefix(_pendingPrefix); // Make sure prefix is cleared
        }

        void StartImportingBasicBlock(BasicBlock basicBlock)
        {
            _delegateCreateStart = null;
            _isThisInitialized = basicBlock.IsThisInitialized;

            if (basicBlock.TryStart)
            {
                Check(basicBlock.EntryStack == null || basicBlock.EntryStack.Length == 0, VerifierError.TryNonEmptyStack);

                for (int i = 0; i < _exceptionRegions.Length; i++)
                {
                    var r = _exceptionRegions[i];

                    if (basicBlock.StartOffset != r.ILRegion.TryOffset)
                        continue;

                    if (r.ILRegion.Kind == ILExceptionRegionKind.Filter)
                    {
                        var filterBlock = _basicBlocks[r.ILRegion.FilterOffset];
                        PropagateThisState(basicBlock, filterBlock);
                        MarkBasicBlock(filterBlock);
                    }

                    var handlerBlock = _basicBlocks[r.ILRegion.HandlerOffset];
                    PropagateThisState(basicBlock, handlerBlock);
                    MarkBasicBlock(handlerBlock);
                }
            }

            if (basicBlock.FilterStart || basicBlock.HandlerStart)
            {
                ExceptionRegion r;
                if (basicBlock.HandlerIndex.HasValue)
                {
                    r = _exceptionRegions[basicBlock.HandlerIndex.Value];
                }
                else if (basicBlock.FilterIndex.HasValue)
                {
                    r = _exceptionRegions[basicBlock.FilterIndex.Value];
                }
                else
                {
                    Debug.Fail("Block marked as filter / handler start but no filter / handler index set.");
                    return;
                }

                if (r.ILRegion.Kind == ILExceptionRegionKind.Filter || r.ILRegion.Kind == ILExceptionRegionKind.Catch)
                {
                    // stack must uninit or 1 (exception object)
                    Check(basicBlock.EntryStack == null || basicBlock.EntryStack.Length == 1, VerifierError.FilterOrCatchUnexpectedStack);

                    if (basicBlock.EntryStack == null)
                        basicBlock.EntryStack = new StackValue[1];

                    if (r.ILRegion.Kind == ILExceptionRegionKind.Filter)
                    {
                        basicBlock.EntryStack[0] = StackValue.CreateObjRef(GetWellKnownType(WellKnownType.Object));
                    }
                    else
                    if (r.ILRegion.Kind == ILExceptionRegionKind.Catch)
                    {
                        var exceptionType = ResolveTypeToken(r.ILRegion.ClassToken);
                        Check(!exceptionType.IsByRef, VerifierError.CatchByRef);
                        basicBlock.EntryStack[0] = StackValue.CreateObjRef(exceptionType);

                        if (SanityChecks && basicBlock.EntryStack[0] != StackValue.CreateObjRef(GetWellKnownType(WellKnownType.Object)))
                        {
                            CheckIsAssignable(basicBlock.EntryStack[0], StackValue.CreateObjRef(GetWellKnownType(WellKnownType.Exception)),
                                VerifierError.ThrowOrCatchOnlyExceptionType);
                        }
                    }
                }
                else
                {
                    // stack must be uninit or empty
                    Check(basicBlock.EntryStack == null || basicBlock.EntryStack.Length == 0, VerifierError.FinOrFaultNonEmptyStack);
                    if (basicBlock.EntryStack == null)
                        basicBlock.EntryStack = s_emptyStack;
                }
            }

            if (basicBlock.EntryStack?.Length > 0)
            {
                if (!basicBlock.TryStart && !basicBlock.HandlerStart && !basicBlock.FilterStart)
                {
                    // ECMA III 1.7.5 Backward Branch Constraints
                    // if stack is not empty at beginning of this block,
                    // there must exist a predecessor block with lower IL offset.
                    Check(basicBlock.HasPredecessorWithLowerOffset, VerifierError.BackwardBranch);
                }

                // Copy stack state
                if (_stack == null || _stack.Length < basicBlock.EntryStack.Length)
                    Array.Resize(ref _stack, basicBlock.EntryStack.Length);
                Array.Copy(basicBlock.EntryStack, _stack, basicBlock.EntryStack.Length);
                _stackTop = basicBlock.EntryStack.Length;
            }
            else
            {
                _stackTop = 0;
            }
        }

        void EndImportingBasicBlock(BasicBlock basicBlock)
        {
            basicBlock.State = BasicBlock.ImportState.WasVerified;
        }

        void ImportNop()
        {
            // Always verifiable
        }

        void ImportBreak()
        {
            // Always verifiable
        }

        TypeDesc GetVarType(int index, bool argument)
        {
            if (argument)
            {
                if (_thisType != null)
                {
                    if (index == 0)
                        return _thisType;
                    index--;
                }
                FatalCheck(index < _methodSignature.Length, VerifierError.UnrecognizedArgumentNumber);
                return _methodSignature[index];
            }
            else
            {
                FatalCheck(index < _locals.Length, VerifierError.UnrecognizedLocalNumber);
                return _locals[index].Type;
            }
        }

        void ImportLoadVar(int index, bool argument)
        {
            var varType = GetVarType(index, argument);

            if (!argument)
                Check(_initLocals, VerifierError.InitLocals);

            CheckIsNotPointer(varType);

            var stackValue = StackValue.CreateFromType(varType);
            if (index == 0 && argument && _thisType != null)
            {
                Debug.Assert(varType == _thisType);
                stackValue.SetIsThisPtr();
            }

            Push(stackValue);
        }

        void ImportStoreVar(int index, bool argument)
        {
            var varType = GetVarType(index, argument);

            var value = Pop();

            if (_trackObjCtorState && !_isThisInitialized)
                Check(index != 0 || !argument, VerifierError.ThisUninitStore);

            CheckIsAssignable(value, StackValue.CreateFromType(varType));
        }

        void ImportAddressOfVar(int index, bool argument)
        {
            var varType = GetVarType(index, argument);

            if (!argument)
                Check(_initLocals, VerifierError.InitLocals);

            Check(!varType.IsByRef, VerifierError.ByrefOfByref);

            var stackValue = StackValue.CreateByRef(varType);
            if (index == 0 && argument && _thisType != null)
            {
                Debug.Assert(varType == _thisType);
                stackValue.SetIsThisPtr();

                Check(!_trackObjCtorState || _isThisInitialized, VerifierError.ThisUninitStore);
            }

            Push(stackValue);
        }

        void ImportDup()
        {
            var value = Pop(allowUninitThis: true);

            // this could be the beginning of a delegate create
            _delegateCreateStart = _currentInstructionOffset;

            Push(value);
            Push(value);
        }

        void ImportPop()
        {
            Pop(allowUninitThis: true);
        }

        void ImportJmp(int token)
        {
            Unverifiable();

            var method = ResolveMethodToken(token);
        }

        void ImportCasting(ILOpcode opcode, int token)
        {
            var type = ResolveTypeToken(token);

            var value = Pop();

            CheckIsObjRef(value);

            Check(_method.OwningType.CanAccess(type), VerifierError.TypeAccess);

            Push(StackValue.CreateObjRef(type));
        }

        void ImportCall(ILOpcode opcode, int token)
        {
            FatalCheck(opcode != ILOpcode.calli, VerifierError.Unverifiable);

            TypeDesc constrained = null;
            bool tailCall = false;

            if (opcode != ILOpcode.newobj)
            {
                if (HasPendingPrefix(Prefix.Constrained) && opcode == ILOpcode.callvirt)
                {
                    ClearPendingPrefix(Prefix.Constrained);
                    constrained = _constrained;
                }

                if (HasPendingPrefix(Prefix.Tail))
                {
                    ClearPendingPrefix(Prefix.Tail);
                    tailCall = true;
                }
            }

            // TODO: VarArgs
            // if (sig.isVarArg())
            //      eeGetCallSiteSig(memberRef, getCurrentModuleHandle(), getCurrentContext(), &sig, false);

            MethodDesc method = ResolveMethodToken(token);

            MethodSignature sig = method.Signature;

            TypeDesc methodType = sig.IsStatic ? null : method.OwningType;

            if (opcode == ILOpcode.callvirt)
            {
                Check(methodType != null, VerifierError.CallVirtOnStatic);
                Check(!methodType.IsValueType, VerifierError.CallVirtOnValueType);
            }
            else if (opcode != ILOpcode.newobj)
            {
                EcmaMethod ecmaMethod = method.GetTypicalMethodDefinition() as EcmaMethod;
                if (ecmaMethod != null)
                    Check(!ecmaMethod.IsAbstract, VerifierError.CallAbstract);
            }

            if (opcode == ILOpcode.newobj && methodType.IsDelegate)
            {
                Check(sig.Length == 2, VerifierError.DelegateCtor);
                var declaredObj = StackValue.CreateFromType(sig[0]);
                var declaredFtn = StackValue.CreateFromType(sig[1]);

                Check(declaredFtn.Kind == StackValueKind.NativeInt, VerifierError.DelegateCtorSigI, declaredFtn);

                var actualFtn = Pop();
                var actualObj = Pop();

                Check(actualFtn.IsMethod, VerifierError.StackMethod);

                CheckIsAssignable(actualObj, declaredObj);
                Check(actualObj.Kind == StackValueKind.ObjRef, VerifierError.DelegateCtorSigO, actualObj);

                CheckDelegateCreation(actualFtn, actualObj);

                if (!IsDelegateAssignable(actualFtn.Method, methodType, actualObj.Type))
                    VerificationError(VerifierError.DelegateCtor);
            }
            else
            {
                for (int i = sig.Length - 1; i >= 0; i--)
                {
                    var actual = Pop(allowUninitThis: true);
                    var declared = StackValue.CreateFromType(sig[i]);

                    CheckIsAssignable(actual, declared);

                    // check that the argument is not a byref for tailcalls
                    if (tailCall)
                        Check(!IsByRefLike(declared), VerifierError.TailByRef, declared);
                }
            }

            TypeDesc instance = null;

            if (opcode == ILOpcode.newobj)
            {
                Check(method.IsConstructor, VerifierError.CtorExpected);
                if (sig.IsStatic || methodType == null || method.IsAbstract)
                {
                    VerificationError(VerifierError.CtorSig);
                }
                else
                {
                    if (methodType.IsArray)
                    {
                        var arrayType = (ArrayType)methodType;
                        Check(!IsByRefLike(StackValue.CreateFromType(arrayType.ElementType)), VerifierError.ArrayByRef);
                    }
                    else
                    {
                        var metadataType = (MetadataType)methodType;
                        Check(!metadataType.IsAbstract, VerifierError.NewobjAbstractClass);
                    }
                }
            }
            else
            if (methodType != null)
            {
                var actualThis = Pop(allowUninitThis: true);
                instance = actualThis.Type;
                var declaredThis = methodType.IsValueType ?
                    StackValue.CreateByRef(methodType) : StackValue.CreateObjRef(methodType);

                // If this is a call to the base class .ctor, set thisPtr Init for this block.
                if (method.IsConstructor)
                {
                    if (_trackObjCtorState && actualThis.IsThisPtr &&
                        (methodType == _thisType || methodType == _thisType.BaseType)) // Call to overloaded ctor or base ctor
                    {
                        _isThisInitialized = true;
                    }
                    else
                    {
                        // Allow direct calls to value type constructors
                        Check(actualThis.Kind == StackValueKind.ByRef && actualThis.Type.IsValueType, VerifierError.CallCtor);
                    }
                }

                if (constrained != null)
                {
                    Check(actualThis.Kind == StackValueKind.ByRef, VerifierError.ConstrainedCallWithNonByRefThis);

                    // We just dereference this and test for equality
                    //todo: improve error - "this type mismatch with constrained type operand"
                    Check(actualThis.Type == constrained, VerifierError.StackUnexpected);

                    // Now pretend the this type is the boxed constrained type, for the sake of subsequent checks
                    actualThis = StackValue.CreateObjRef(constrained);
                }

                // To support direct calls on readonly byrefs, just pretend declaredThis is readonly too
                if (declaredThis.Kind == StackValueKind.ByRef && (actualThis.Kind == StackValueKind.ByRef && actualThis.IsReadOnly))
                {
                    declaredThis.SetIsReadOnly();
                }
                CheckIsAssignable(actualThis, declaredThis);

                if (opcode == ILOpcode.call)
                {
                    // Rules for non-virtual call to a non-final virtual method (ECMA III.3.19: Verifiability of 'call'):

                    // Define:
                    // The "this" pointer is considered to be "possibly written" if
                    //   1. Its address have been taken (LDARGA 0) anywhere in the method.
                    //   (or)
                    //   2. It has been stored to (STARG.0) anywhere in the method.

                    // A non-virtual call to a non-final virtual method is only allowed if
                    //   1. The this pointer passed to the callee is an instance of a boxed value type.
                    //   (or)
                    //   2. The this pointer passed to the callee is the current method's this pointer.
                    //      (and) The current method's this pointer is not "possibly written".

                    // Thus the rule is that if you assign to this ANYWHERE you can't make "base" calls to
                    // virtual methods.  (Luckily this does not affect .ctors, since they are not virtual).
                    // This is stronger than is strictly needed, but implementing a laxer rule is significantly
                    // harder and more error prone.
                    if (method.IsVirtual && !method.IsFinal && !actualThis.IsBoxedValueType)
                    {
                        var methodTypeDef = methodType.GetTypeDefinition() as MetadataType; // Method is always considered final if owning type is sealed
                        if (methodTypeDef == null || !methodTypeDef.IsSealed)
                            Check(actualThis.IsThisPtr && !_modifiesThisPtr, VerifierError.ThisMismatch);
                    }
                }

                if (tailCall)
                {
                    // also check the special tailcall rule
                    Check(!IsByRefLike(declaredThis), VerifierError.TailByRef, declaredThis);

                    // Tail calls on constrained calls should be illegal too:
                    // when instantiated at a value type, a constrained call may pass the address of a stack allocated value
                    Check(constrained == null, VerifierError.TailByRef);
                }
            }

            // Check any constraints on the callee's class and type parameters
            if (!method.OwningType.CheckConstraints())
                VerificationError(VerifierError.UnsatisfiedMethodParentInst, method.OwningType);
            else if (!method.CheckConstraints())
                VerificationError(VerifierError.UnsatisfiedMethodInst, method);

            Check(_method.OwningType.CanAccess(method, instance), VerifierError.MethodAccess);

            TypeDesc returnType = sig.ReturnType;

            // special checks for tailcalls
            if (tailCall)
            {
                TypeDesc callerReturnType = _methodSignature.ReturnType;

                if (returnType.IsVoid || callerReturnType.IsVoid)
                {
                    Check(returnType.IsVoid && callerReturnType.IsVoid, VerifierError.TailRetVoid);
                }
                // else
                // if (returnType.IsValueType || callerReturnType.IsValueType)
                // {
                //      TODO: Check exact match
                // }
                else
                {
                    var retStackType = StackValue.CreateFromType(returnType);
                    var callerRetStackType = StackValue.CreateFromType(callerReturnType);
                    Check(IsAssignable(retStackType, callerRetStackType), VerifierError.TailRetType, retStackType, callerRetStackType);
                }

                // for tailcall, stack must be empty
                Check(_stackTop == 0, VerifierError.TailStackEmpty);

                // The instruction following a tail.call shall be a ret
                Check(_currentOffset < _ilBytes.Length && (ILOpcode)_ilBytes[_currentOffset] == ILOpcode.ret, VerifierError.TailRet);
            }

            // now push on the result
            if (opcode == ILOpcode.newobj)
            {
                Push(StackValue.CreateFromType(methodType));
            }
            else
            if (!returnType.IsVoid)
            {
                var returnValue = StackValue.CreateFromType(returnType);

                // "readonly." prefixed calls only allowed for the Address operation on arrays.
                // The methods supported by array types are under the control of the EE
                // so we can trust that only the Address operation returns a byref.
                if (HasPendingPrefix(Prefix.ReadOnly))
                {
                    if (method.OwningType.IsArray && sig.ReturnType.IsByRef)
                        returnValue.SetIsReadOnly();
                    else
                        VerificationError(VerifierError.ReadonlyUnexpectedCallee);

                    ClearPendingPrefix(Prefix.ReadOnly);
                }

                if (returnValue.Kind == StackValueKind.ByRef)
                    returnValue.SetIsPermanentHome();

                Push(returnValue);
            }
        }

        void ImportCalli(int token)
        {
            throw new NotImplementedException($"{nameof(ImportCalli)} not implemented");
        }

        void ImportLdFtn(int token, ILOpcode opCode)
        {
            MethodDesc method = ResolveMethodToken(token);
            Check(!method.IsConstructor, VerifierError.LdftnCtor);

#if false
            if (sig.hasTypeArg())
                NO_WAY("Currently do not support LDFTN of Parameterized functions");
#endif

            TypeDesc instance;

            if (opCode == ILOpcode.ldftn)
            {
                _delegateCreateStart = _currentInstructionOffset;

                instance = null;
            }
            else if (opCode == ILOpcode.ldvirtftn)
            {
                Check(!method.Signature.IsStatic, VerifierError.LdvirtftnOnStatic);

                StackValue declaredType;
                if (method.OwningType.IsValueType)
                {
                    // Box value type for comparison
                    declaredType = StackValue.CreateObjRef(method.OwningType);
                }
                else
                    declaredType = StackValue.CreateFromType(method.OwningType);

                var thisPtr = Pop();
                instance = thisPtr.Type;

                CheckIsObjRef(thisPtr);
                CheckIsAssignable(thisPtr, declaredType);
            }
            else
            {
                Debug.Fail("Unexpected ldftn opcode: " + opCode.ToString());
                return;
            }

            // Check any constraints on the callee's class and type parameters
            if (!method.OwningType.CheckConstraints())
                VerificationError(VerifierError.UnsatisfiedMethodParentInst, method.OwningType);
            else if (!method.CheckConstraints())
                VerificationError(VerifierError.UnsatisfiedMethodInst, method);

            Check(_method.OwningType.CanAccess(method, instance), VerifierError.MethodAccess);

            Push(StackValue.CreateMethod(method));
        }

        void ImportLoadInt(long value, StackValueKind kind)
        {
            Push(StackValue.CreatePrimitive(kind));
        }

        void ImportLoadFloat(double value)
        {
            Push(StackValue.CreatePrimitive(StackValueKind.Float));
        }

        void ImportLoadNull()
        {
            Push(StackValue.CreateObjRef(null));
        }

        void ImportReturn()
        {
            // 'this' must be init before return, unless System.Object
            if (_trackObjCtorState)
                Check(_isThisInitialized || _thisType.IsObject, VerifierError.ThisUninitReturn);

            // Check current region type
            Check(_currentBasicBlock.FilterIndex == null, VerifierError.ReturnFromFilter);
            Check(_currentBasicBlock.TryIndex == null, VerifierError.ReturnFromTry);
            Check(_currentBasicBlock.HandlerIndex == null, VerifierError.ReturnFromHandler);

            var declaredReturnType = _method.Signature.ReturnType;

            if (declaredReturnType.IsVoid)
            {
                Debug.Assert(_stackTop >= 0);

                if (_stackTop > 0)
                    VerificationError(VerifierError.ReturnVoid, _stack[_stackTop - 1]);
            }
            else
            {
                if (_stackTop <= 0)
                    VerificationError(VerifierError.ReturnMissing);
                else
                {
                    Check(_stackTop == 1, VerifierError.ReturnEmpty);

                    var actualReturnType = Pop();
                    CheckIsAssignable(actualReturnType, StackValue.CreateFromType(declaredReturnType));

                    Check((!declaredReturnType.IsByRef && !declaredReturnType.IsByRefLike) || actualReturnType.IsPermanentHome, VerifierError.ReturnPtrToStack);
                }
            }
        }

        void ImportFallthrough(BasicBlock next)
        {
            PropagateControlFlow(next, isFallthrough: true);
        }

        void PropagateThisState(BasicBlock current, BasicBlock next)
        {
            if (next.State == BasicBlock.ImportState.Unmarked)
                next.IsThisInitialized = _isThisInitialized;
            else
            {
                if (next.IsThisInitialized && !_isThisInitialized)
                {
                    // Next block has 'this' initialized, but current state has not
                    // therefore next block must be reverified with 'this' uninitialized
                    if (next.State == BasicBlock.ImportState.WasVerified && next.ErrorCount == 0)
                        next.State = BasicBlock.ImportState.Unmarked;
                }

                next.IsThisInitialized = next.IsThisInitialized && _isThisInitialized;
            }
        }

        void ImportSwitchJump(int jmpBase, int[] jmpDelta, BasicBlock fallthrough)
        {
            var value = Pop();
            CheckIsAssignable(value, StackValue.CreatePrimitive(StackValueKind.Int32));

            for (int i = 0; i < jmpDelta.Length; i++)
            {
                BasicBlock target = _basicBlocks[jmpBase + jmpDelta[i]];
                PropagateControlFlow(target, isFallthrough: false);
            }

            if (fallthrough != null)
                ImportFallthrough(fallthrough);
        }

        void ImportBranch(ILOpcode opcode, BasicBlock target, BasicBlock fallthrough)
        {
            switch (opcode)
            {
                case ILOpcode.br:
                    break;
                case ILOpcode.brfalse:
                case ILOpcode.brtrue:
                    {
                        StackValue value = Pop();
                        Check(value.Kind >= StackValueKind.Int32 && value.Kind <= StackValueKind.NativeInt || value.Kind == StackValueKind.ObjRef || value.Kind == StackValueKind.ByRef, VerifierError.StackUnexpected);
                    }
                    break;
                case ILOpcode.beq:
                case ILOpcode.bge:
                case ILOpcode.bgt:
                case ILOpcode.ble:
                case ILOpcode.blt:
                case ILOpcode.bne_un:
                case ILOpcode.bge_un:
                case ILOpcode.bgt_un:
                case ILOpcode.ble_un:
                case ILOpcode.blt_un:
                    {
                        StackValue value1 = Pop();
                        StackValue value2 = Pop();

                        CheckIsComparable(value1, value2, opcode);
                    }
                    break;
                default:
                    Debug.Fail("Unexpected branch opcode");
                    break;
            }

            PropagateControlFlow(target, isFallthrough: false);

            if (fallthrough != null)
                ImportFallthrough(fallthrough);
        }

        void PropagateControlFlow(BasicBlock next, bool isFallthrough)
        {
            if (!IsValidBranchTarget(_currentBasicBlock, next, isFallthrough) || _currentBasicBlock.ErrorCount > 0)
                return;

            PropagateThisState(_currentBasicBlock, next);

            // Propagate stack across block bounds
            StackValue[] entryStack = next.EntryStack;

            if (entryStack != null)
            {
                FatalCheck(entryStack.Length == _stackTop, VerifierError.PathStackDepth);

                for (int i = 0; i < entryStack.Length; i++)
                {
                    // TODO: Do we need to allow conversions?
                    FatalCheck(entryStack[i].Kind == _stack[i].Kind, VerifierError.PathStackUnexpected, entryStack[i], _stack[i]);

                    if (entryStack[i].Type != _stack[i].Type)
                    {
                        if (!IsAssignable(_stack[i], entryStack[i]))
                        {
                            StackValue mergedValue;
                            if (!TryMergeStackValues(entryStack[i], _stack[i], out mergedValue))
                                FatalCheck(false, VerifierError.PathStackUnexpected, entryStack[i], _stack[i]);

                            // If merge actually changed entry stack
                            if (mergedValue != entryStack[i])
                            {
                                entryStack[i] = mergedValue;

                                if (next.ErrorCount == 0 && next.State != BasicBlock.ImportState.IsPending)
                                    next.State = BasicBlock.ImportState.Unmarked; // Make sure block is reverified
                            }
                        }
                    }
                }
            }
            else
            {
                entryStack = (_stackTop != 0) ? new StackValue[_stackTop] : s_emptyStack;

                for (int i = 0; i < entryStack.Length; i++)
                    entryStack[i] = _stack[i];

                next.EntryStack = entryStack;
            }

            MarkBasicBlock(next);
        }

        void ImportBinaryOperation(ILOpcode opcode)
        {
            var op1 = Pop();
            var op2 = Pop();

            switch (opcode)
            {
                case ILOpcode.add:
                case ILOpcode.sub:
                case ILOpcode.mul:
                case ILOpcode.div:
                case ILOpcode.rem:
                    CheckIsNumeric(op1);
                    CheckIsNumeric(op2);
                    break;
                case ILOpcode.and:
                case ILOpcode.or:
                case ILOpcode.xor:
                case ILOpcode.div_un:
                case ILOpcode.rem_un:
                case ILOpcode.add_ovf:
                case ILOpcode.add_ovf_un:
                case ILOpcode.mul_ovf:
                case ILOpcode.mul_ovf_un:
                case ILOpcode.sub_ovf:
                case ILOpcode.sub_ovf_un:
                    CheckIsInteger(op1);
                    CheckIsInteger(op2);
                    break;
            }

            // StackValueKind is carefully ordered to make this work
            StackValue result = (op1.Kind > op2.Kind) ? op1 : op2;

            if ((op1.Kind != op2.Kind) && (result.Kind != StackValueKind.NativeInt))
            {
                VerificationError(VerifierError.StackUnexpected, op2, op1);
            }

            // The one exception from the above rule
            if ((result.Kind == StackValueKind.ByRef) &&
                    (opcode == ILOpcode.sub || opcode == ILOpcode.sub_ovf || opcode == ILOpcode.sub_ovf_un))
            {
                result = StackValue.CreatePrimitive(StackValueKind.NativeInt);
            }

            Push(result);
        }

        void ImportShiftOperation(ILOpcode opcode)
        {
            var shiftBy = Pop();
            var toBeShifted = Pop();

            Check(shiftBy.Kind == StackValueKind.Int32 || shiftBy.Kind == StackValueKind.NativeInt, VerifierError.StackUnexpected, shiftBy);
            CheckIsInteger(toBeShifted);

            Push(StackValue.CreatePrimitive(toBeShifted.Kind));
        }

        void ImportCompareOperation(ILOpcode opcode)
        {
            var value1 = Pop();
            var value2 = Pop();

            CheckIsComparable(value1, value2, opcode);

            Push(StackValue.CreatePrimitive(StackValueKind.Int32));
        }

        void ImportConvert(WellKnownType wellKnownType, bool checkOverflow, bool unsigned)
        {
            var value = Pop();

            CheckIsNumeric(value);

            Push(StackValue.CreateFromType(GetWellKnownType(wellKnownType)));
        }

        void ImportLoadField(int token, bool isStatic)
        {
            ClearPendingPrefix(Prefix.Unaligned);
            ClearPendingPrefix(Prefix.Volatile);

            var field = ResolveFieldToken(token);

            TypeDesc instance;
            if (isStatic)
            {
                Check(field.IsStatic, VerifierError.ExpectedStaticField);

                instance = null;
            }
            else
            {
                var owningType = field.OwningType;

                // Note that even if the field is static, we require that the this pointer
                // satisfy the same constraints as a non-static field  This happens to
                // be simpler and seems reasonable
                var actualThis = Pop(allowUninitThis: true);
                if (actualThis.Kind == StackValueKind.ValueType)
                    actualThis = StackValue.CreateByRef(actualThis.Type);

                var declaredThis = owningType.IsValueType ?
                    StackValue.CreateByRef(owningType, readOnly : true) : StackValue.CreateObjRef(owningType);

                CheckIsAssignable(actualThis, declaredThis);

                instance = actualThis.Type;
            }

            Check(_method.OwningType.CanAccess(field, instance), VerifierError.FieldAccess);

            Push(StackValue.CreateFromType(field.FieldType));
        }

        void ImportAddressOfField(int token, bool isStatic)
        {
            var field = ResolveFieldToken(token);
            bool isPermanentHome = false;

            TypeDesc instance;
            if (isStatic)
            {
                Check(field.IsStatic, VerifierError.ExpectedStaticField);

                isPermanentHome = true;
                instance = null;

                if (field.IsInitOnly)
                    Check(_method.IsStaticConstructor && field.OwningType == _method.OwningType, VerifierError.InitOnly);
            }
            else
            {
                var owningType = field.OwningType;

                // Note that even if the field is static, we require that the this pointer
                // satisfy the same constraints as a non-static field  This happens to
                // be simpler and seems reasonable
                var actualThis = Pop(allowUninitThis: true);
                if (actualThis.Kind == StackValueKind.ValueType)
                    actualThis = StackValue.CreateByRef(actualThis.Type);

                var declaredThis = owningType.IsValueType ?
                    StackValue.CreateByRef(owningType) : StackValue.CreateObjRef(owningType);

                CheckIsAssignable(actualThis, declaredThis);

                isPermanentHome = actualThis.Kind == StackValueKind.ObjRef || actualThis.IsPermanentHome;
                instance = actualThis.Type;

                // TODO: verification of readonly references https://github.com/dotnet/runtime/issues/57444
                // if (field.IsInitOnly)
                //    Check(_method.IsConstructor && field.OwningType == _method.OwningType && actualThis.IsThisPtr, VerifierError.InitOnly);
            }

            Check(_method.OwningType.CanAccess(field, instance), VerifierError.FieldAccess);

            Push(StackValue.CreateByRef(field.FieldType, false, isPermanentHome));
        }

        void ImportStoreField(int token, bool isStatic)
        {
            ClearPendingPrefix(Prefix.Unaligned);
            ClearPendingPrefix(Prefix.Volatile);

            var value = Pop();
            var field = ResolveFieldToken(token);
            TypeDesc instance;

            if (isStatic)
            {
                Check(field.IsStatic, VerifierError.ExpectedStaticField);

                instance = null;

                if (field.IsInitOnly)
                    Check(_method.IsStaticConstructor && field.OwningType == _method.OwningType, VerifierError.InitOnly);
            }
            else
            {
                var owningType = field.OwningType;

                // Note that even if the field is static, we require that the this pointer
                // satisfy the same constraints as a non-static field  This happens to
                // be simpler and seems reasonable
                var actualThis = Pop(allowUninitThis: true);
                if (actualThis.Kind == StackValueKind.ValueType)
                    actualThis = StackValue.CreateByRef(actualThis.Type);

                var declaredThis = owningType.IsValueType ?
                    StackValue.CreateByRef(owningType) : StackValue.CreateObjRef(owningType);

                CheckIsAssignable(actualThis, declaredThis);

                instance = actualThis.Type;

                if (field.IsInitOnly)
                    Check(field.OwningType == _method.OwningType && actualThis.IsThisPtr &&
                        (_method.IsConstructor || HasIsExternalInit(_method.Signature)), VerifierError.InitOnly);
            }

            // Check any constraints on the fields' class --- accessing the field might cause a class constructor to run.
            Check(field.OwningType.CheckConstraints(), VerifierError.UnsatisfiedFieldParentInst);

            Check(_method.OwningType.CanAccess(field, instance), VerifierError.FieldAccess);

            CheckIsAssignable(value, StackValue.CreateFromType(field.FieldType));
        }

        void ImportLoadIndirect(int token)
        {
            ImportLoadIndirect(ResolveTypeToken(token));
        }

        void ImportLoadIndirect(TypeDesc type)
        {
            ClearPendingPrefix(Prefix.Unaligned);
            ClearPendingPrefix(Prefix.Volatile);

            var address = Pop();
            CheckIsByRef(address);

            if (type == null)
            {
                CheckIsObjRef(address.Type);
                type = address.Type;
            }
            else
            {
                CheckIsAssignable(address.Type.GetVerificationType(), type.GetVerificationType());
            }
            Push(StackValue.CreateFromType(type));
        }

        void ImportStoreIndirect(int token)
        {
            ImportStoreIndirect(ResolveTypeToken(token));
        }

        void ImportStoreIndirect(TypeDesc type)
        {
            ClearPendingPrefix(Prefix.Unaligned);
            ClearPendingPrefix(Prefix.Volatile);

            var value = Pop();
            var address = Pop();

            Check(!address.IsReadOnly, VerifierError.ReadOnlyIllegalWrite);

            CheckIsByRef(address);

            if (type == null)
                type = address.Type;

            var typeVal = StackValue.CreateFromType(type);
            var addressVal = StackValue.CreateFromType(address.Type);

            CheckIsAssignable(typeVal, addressVal);
            CheckIsAssignable(value, typeVal);
        }

        void ImportThrow()
        {
            var value = Pop();

            CheckIsObjRef(value);

            if (SanityChecks && value != StackValue.CreateObjRef(GetWellKnownType(WellKnownType.Object)))
            {
                CheckIsAssignable(value, StackValue.CreateFromType(GetWellKnownType(WellKnownType.Exception)),
                    VerifierError.ThrowOrCatchOnlyExceptionType);
            }

            EmptyTheStack();
        }

        void ImportLoadString(int token)
        {
            object tokenObj = _methodIL.GetObject(token);
            Check(tokenObj is String, VerifierError.StringOperand);

            Push(StackValue.CreateObjRef(_typeSystemContext.GetWellKnownType(WellKnownType.String)));
        }

        void ImportInitObj(int token)
        {
            var type = ResolveTypeToken(token);

            var value = Pop();

            Check(value.Kind == StackValueKind.ByRef, VerifierError.StackByRef, value);

            CheckIsAssignable(value, StackValue.CreateByRef(type));
        }

        void ImportBox(int token)
        {
            var type = ResolveTypeToken(token);

            var value = Pop();

            var targetType = StackValue.CreateFromType(type);

            Check(!IsByRefLike(targetType), VerifierError.BoxByRef, targetType);

            Check(type.IsPrimitive || targetType.Kind == StackValueKind.ObjRef ||
                type.IsGenericParameter || type.IsValueType, VerifierError.ExpectedValClassObjRefVariable);

            Check(type.CheckConstraints(), VerifierError.UnsatisfiedBoxOperand);

            Check(_method.OwningType.CanAccess(type), VerifierError.TypeAccess);

            CheckIsAssignable(value, targetType);

            // for nullable<T> we push T
            var typeForBox = type.IsNullable ? type.Instantiation[0] : type;

            // even if type is a value type we want the ref
            Push(StackValue.CreateObjRef(typeForBox));
        }

        static bool IsOffsetContained(int offset, int start, int length)
        {
            return start <= offset && offset < start + length;
        }

        void ImportLeave(BasicBlock target)
        {
            EmptyTheStack();

            PropagateThisState(_currentBasicBlock, target);
            MarkBasicBlock(target);

            CheckIsValidLeaveTarget(_currentBasicBlock, target);
        }

        void ImportEndFinally()
        {
            Check(_currentBasicBlock.HandlerIndex.HasValue, VerifierError.Endfinally);
            Check(_exceptionRegions[_currentBasicBlock.HandlerIndex.Value].ILRegion.Kind == ILExceptionRegionKind.Finally ||
                _exceptionRegions[_currentBasicBlock.HandlerIndex.Value].ILRegion.Kind == ILExceptionRegionKind.Fault, VerifierError.Endfinally);

            EmptyTheStack();
        }

        void ImportNewArray(int token)
        {
            var elementType = ResolveTypeToken(token);

            var length = Pop();

            Check(!IsByRefLike(StackValue.CreateFromType(elementType)), VerifierError.ArrayByRef);

            Push(StackValue.CreateObjRef(elementType.Context.GetArrayType(elementType)));
        }

        void ImportLoadElement(int token)
        {
            ImportLoadElement(ResolveTypeToken(token));
        }

        void ImportLoadElement(TypeDesc elementType)
        {
            var index = Pop();
            var array = Pop();

            CheckIsIndex(index);
            CheckIsArray(array);

            if (array.Type != null)
            {
                var actualElementType = ((ArrayType)array.Type).ElementType;

                if (elementType != null)
                {
                    CheckIsArrayElementCompatibleWith(actualElementType.GetVerificationType(), elementType);
                }
                else
                {
                    elementType = actualElementType;
                    CheckIsObjRef(elementType);
                }
            }

            Push(StackValue.CreateFromType(elementType));
        }

        void ImportStoreElement(int token)
        {
            ImportStoreElement(ResolveTypeToken(token));
        }

        void ImportStoreElement(TypeDesc elementType)
        {
            var value = Pop();
            var index = Pop();
            var array = Pop();

            CheckIsIndex(index);
            CheckIsArray(array);

            if (array.Type != null)
            {
                var actualElementType = ((ArrayType)array.Type).ElementType;

                if (elementType != null)
                {
                    CheckIsArrayElementCompatibleWith(elementType, actualElementType.GetVerificationType());
                }
                else
                {
                    elementType = actualElementType;
                    CheckIsObjRef(elementType);
                }
            }

            if (elementType != null)
            {
                // TODO: Change to CheckIsArrayElementCompatibleWith for two intermediate types
                CheckIsAssignable(value, StackValue.CreateFromType(elementType));
            }
        }

        void ImportAddressOfElement(int token)
        {
            var elementType = ResolveTypeToken(token);

            var index = Pop();
            var array = Pop();

            CheckIsIndex(index);
            CheckIsArray(array);

            if (array.Type != null)
            {
                var actualElementType = ((ArrayType)array.Type).ElementType;

                CheckIsPointerElementCompatibleWith(actualElementType, elementType);
            }

            // an array interior pointer is always on the heap, hence permanentHome = true
            Push(StackValue.CreateByRef(elementType, HasPendingPrefix(Prefix.ReadOnly), true));
            ClearPendingPrefix(Prefix.ReadOnly);
        }

        void ImportLoadLength()
        {
            var array = Pop();

            CheckIsArray(array);

            Push(StackValue.CreatePrimitive(StackValueKind.NativeInt));
        }

        void ImportUnaryOperation(ILOpcode opCode)
        {
            var operand = Pop();

            switch (opCode)
            {
                case ILOpcode.neg:
                    CheckIsNumeric(operand);
                    break;
                case ILOpcode.not:
                    CheckIsInteger(operand);
                    break;
                default:
                    Debug.Fail("Unexpected branch opcode");
                    break;
            }

            Push(StackValue.CreatePrimitive(operand.Kind));
        }

        void ImportCpOpj(int token)
        {
            var type = ResolveTypeToken(token);

            var src = Pop();
            var dst = Pop();

            Check(src.Kind == StackValueKind.ByRef, VerifierError.StackByRef, src);
            Check(dst.Kind == StackValueKind.ByRef, VerifierError.StackByRef, dst);

            // TODO !!!
            // CheckIsAssignable(src.Type, type);
            // CheckIsAssignable(type, dst.Type);
        }

        void ImportUnbox(int token, ILOpcode opCode)
        {
            var type = ResolveTypeToken(token);

            CheckIsObjRef(Pop());

            if (opCode == ILOpcode.unbox_any)
            {
                Push(StackValue.CreateFromType(type));
            }
            else
            {
                Check(type.IsValueType, VerifierError.ValueTypeExpected);

                // We always come from an ObjRef, hence this is permanentHome
                Push(StackValue.CreateByRef(type, true, true));
            }
        }

        void ImportCkFinite()
        {
            var value = Pop();

            Check(value.Kind == StackValueKind.Float, VerifierError.ExpectedFloatType);

            Push(value);
        }

        void ImportLdToken(int token)
        {
            Object obj = ResolveToken(token);

            WellKnownType handleKind;

            if (obj is TypeDesc)
            {
                handleKind = WellKnownType.RuntimeTypeHandle;
            }
            else
            if (obj is MethodDesc)
            {
                handleKind = WellKnownType.RuntimeMethodHandle;
            }
            else
            if (obj is FieldDesc)
            {
                handleKind = WellKnownType.RuntimeFieldHandle;
            }
            else
            {
                throw new BadImageFormatException("Invalid token");
            }

            var handleType = _typeSystemContext.GetWellKnownType(handleKind);

            Push(StackValue.CreateValueType(handleType));
        }

        void ImportLocalAlloc()
        {
            Unverifiable();

            var size = Pop();

            Check(_stackTop == 0, VerifierError.LocallocStackNotEmpty);

            CheckIsInteger(size);

            Push(StackValue.CreatePrimitive(StackValueKind.NativeInt));
        }

        void ImportEndFilter()
        {
            Check(_currentBasicBlock.FilterIndex.HasValue, VerifierError.Endfilter);
            Check(_currentOffset == _exceptionRegions[_currentBasicBlock.FilterIndex.Value].ILRegion.HandlerOffset, VerifierError.Endfilter);

            var result = Pop(allowUninitThis: true);
            Check(result.Kind == StackValueKind.Int32, VerifierError.StackUnexpected, result);
            Check(_stackTop == 0, VerifierError.EndfilterStack);
        }

        void ImportCpBlk()
        {
            ClearPendingPrefix(Prefix.Unaligned);
            ClearPendingPrefix(Prefix.Volatile);

            Unverifiable();

            var size = Pop();
            var srcaddr = Pop();
            var dstaddr = Pop();

            CheckIsInteger(size);

            // TODO: Validate srcaddr, dstaddr
        }

        void ImportInitBlk()
        {
            ClearPendingPrefix(Prefix.Unaligned);
            ClearPendingPrefix(Prefix.Volatile);

            Unverifiable();

            var size = Pop();
            var value = Pop();
            var addr = Pop();

            CheckIsInteger(size);
            CheckIsInteger(value);

            // TODO: Validate addr
        }

        void ImportRethrow()
        {
            if (_currentBasicBlock.HandlerIndex.HasValue)
            {
                var eR = _exceptionRegions[_currentBasicBlock.HandlerIndex.Value].ILRegion;

                //in case a simple catch
                if (eR.Kind == ILExceptionRegionKind.Catch)
                {
                    return;
                }

                //in case a filter make sure rethrow is within the handler
                if (eR.Kind == ILExceptionRegionKind.Filter &&
                    _currentOffset >= eR.HandlerOffset &&
                    _currentOffset <= eR.HandlerOffset + eR.HandlerLength)
                {
                    return;
                }
            }

            VerificationError(VerifierError.Rethrow);
        }

        void ImportSizeOf(int token)
        {
            var type = ResolveTypeToken(token);

            Push(StackValue.CreatePrimitive(StackValueKind.Int32));
        }

        //
        // Prefix
        //

        void ImportUnalignedPrefix(byte alignment)
        {
            CheckPendingPrefix(_pendingPrefix & ~Prefix.Volatile);
            _pendingPrefix |= Prefix.Unaligned;
        }

        void ImportVolatilePrefix()
        {
            CheckPendingPrefix(_pendingPrefix & ~Prefix.Unaligned);
            _pendingPrefix |= Prefix.Volatile;
        }

        void ImportTailPrefix()
        {
            CheckPendingPrefix(_pendingPrefix);
            _pendingPrefix |= Prefix.Tail;

            Check(!_currentBasicBlock.TryIndex.HasValue && !_currentBasicBlock.FilterIndex.HasValue &&
                !_currentBasicBlock.HandlerIndex.HasValue, VerifierError.TailCallInsideER);
        }

        void ImportConstrainedPrefix(int token)
        {
            CheckPendingPrefix(_pendingPrefix);
            _pendingPrefix |= Prefix.Constrained;

            _constrained = ResolveTypeToken(token);
        }

        void ImportNoPrefix(byte mask)
        {
            Unverifiable();

            CheckPendingPrefix(_pendingPrefix);
            _pendingPrefix |= Prefix.No;
        }

        void ImportReadOnlyPrefix()
        {
            CheckPendingPrefix(_pendingPrefix);
            _pendingPrefix |= Prefix.ReadOnly;
        }

        void CheckPendingPrefix(Prefix mask)
        {
            if (mask == 0) return;

            //illegal to stack prefixes
            Check((mask & Prefix.Unaligned) == 0, VerifierError.Unaligned);
            Check((mask & Prefix.Tail) == 0, VerifierError.TailCall);
            Check((mask & Prefix.Volatile) == 0, VerifierError.Volatile);
            Check((mask & Prefix.ReadOnly) == 0, VerifierError.ReadOnly);
            Check((mask & Prefix.Constrained) == 0, VerifierError.Constrained);
        }

        static bool HasIsExternalInit(MethodSignature signature)
        {
            if (signature.HasEmbeddedSignatureData)
            {
                foreach (var data in signature.GetEmbeddedSignatureData())
                {
                    if (data.type is MetadataType mdType && mdType.Namespace == "System.Runtime.CompilerServices" && mdType.Name == "IsExternalInit" &&
                        data.index == MethodSignature.IndexOfCustomModifiersOnReturnType)
                        return true;
                }
            }

            return false;
        }

        bool HasPendingPrefix(Prefix prefix)
        {
            return (_pendingPrefix & prefix) != 0;
        }

        void ClearPendingPrefix(Prefix prefix)
        {
            _pendingPrefix &= ~prefix;
        }

        void ReportInvalidBranchTarget(int targetOffset)
        {
            VerificationError(VerifierError.BadBranch);
        }

        void ReportFallthroughAtEndOfMethod()
        {
            VerificationError(VerifierError.MethodFallthrough);
        }

        void ReportMethodEndInsideInstruction()
        {
            VerificationError(VerifierError.MethodEnd);
            AbortMethodVerification();
        }

        void ReportInvalidInstruction(ILOpcode opcode)
        {
            VerificationError(VerifierError.UnknownOpcode);
        }

        //
        // Deprecated
        //

        void ImportArgList()
        {
            throw new PlatformNotSupportedException("RuntimeArgumentHandle not supported in .NET Core");
        }

        void ImportRefAnyType()
        {
            throw new PlatformNotSupportedException("TypedReference not supported in .NET Core");
        }

        void ImportMkRefAny(int token)
        {
            throw new PlatformNotSupportedException("TypedReference not supported in .NET Core");
        }

        void ImportRefAnyVal(int token)
        {
            throw new PlatformNotSupportedException("TypedReference not supported in .NET Core");
        }
    }
}
