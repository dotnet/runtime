// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class OffsetInstruction : Instruction
    {
        internal const int Unknown = Int32.MinValue;
        internal const int CacheSize = 32;

        // the offset to jump to (relative to this instruction):
        protected int _offset = Unknown;

        public int Offset { get { return _offset; } }
        public abstract Instruction[] Cache { get; }

        public override string InstructionName
        {
            get { return "Offset"; }
        }

        public Instruction Fixup(int offset)
        {
            Debug.Assert(_offset == Unknown && offset != Unknown);
            _offset = offset;

            var cache = Cache;
            if (cache != null && offset >= 0 && offset < cache.Length)
            {
                return cache[offset] ?? (cache[offset] = this);
            }

            return this;
        }

        public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects)
        {
            return ToString() + (_offset != Unknown ? " -> " + (instructionIndex + _offset) : "");
        }

        public override string ToString()
        {
            return InstructionName + (_offset == Unknown ? "(?)" : "(" + _offset + ")");
        }
    }

    internal sealed class BranchFalseInstruction : OffsetInstruction
    {
        private static Instruction[] s_cache;
        public override string InstructionName
        {
            get { return "BranchFalse"; }
        }

        public override Instruction[] Cache
        {
            get
            {
                if (s_cache == null)
                {
                    s_cache = new Instruction[CacheSize];
                }
                return s_cache;
            }
        }

        internal BranchFalseInstruction()
        {
        }

        public override int ConsumedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            Debug.Assert(_offset != Unknown);

            if (!(bool)frame.Pop())
            {
                return _offset;
            }

            return +1;
        }
    }

    internal sealed class BranchTrueInstruction : OffsetInstruction
    {
        private static Instruction[] s_cache;
        public override string InstructionName
        {
            get { return "BranchTrue"; }
        }

        public override Instruction[] Cache
        {
            get
            {
                if (s_cache == null)
                {
                    s_cache = new Instruction[CacheSize];
                }
                return s_cache;
            }
        }

        internal BranchTrueInstruction()
        {
        }

        public override int ConsumedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            Debug.Assert(_offset != Unknown);

            if ((bool)frame.Pop())
            {
                return _offset;
            }

            return +1;
        }
    }

    internal sealed class CoalescingBranchInstruction : OffsetInstruction
    {
        private static Instruction[] s_cache;
        public override string InstructionName
        {
            get { return "CoalescingBranch"; }
        }

        public override Instruction[] Cache
        {
            get
            {
                if (s_cache == null)
                {
                    s_cache = new Instruction[CacheSize];
                }
                return s_cache;
            }
        }

        internal CoalescingBranchInstruction()
        {
        }

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            Debug.Assert(_offset != Unknown);

            if (frame.Peek() != null)
            {
                return _offset;
            }

            return +1;
        }
    }

    internal class BranchInstruction : OffsetInstruction
    {
        private static Instruction[][][] s_caches;
        public override string InstructionName
        {
            get { return "Branch"; }
        }

        public override Instruction[] Cache
        {
            get
            {
                if (s_caches == null)
                {
                    s_caches = new Instruction[2][][] { new Instruction[2][], new Instruction[2][] };
                }
                return s_caches[ConsumedStack][ProducedStack] ?? (s_caches[ConsumedStack][ProducedStack] = new Instruction[CacheSize]);
            }
        }

        internal readonly bool _hasResult;
        internal readonly bool _hasValue;

        internal BranchInstruction()
            : this(false, false)
        {
        }

        public BranchInstruction(bool hasResult, bool hasValue)
        {
            _hasResult = hasResult;
            _hasValue = hasValue;
        }

        public override int ConsumedStack
        {
            get { return _hasValue ? 1 : 0; }
        }

        public override int ProducedStack
        {
            get { return _hasResult ? 1 : 0; }
        }

        public override int Run(InterpretedFrame frame)
        {
            Debug.Assert(_offset != Unknown);

            return _offset;
        }
    }

    internal abstract class IndexedBranchInstruction : Instruction
    {
        protected const int CacheSize = 32;
        public override string InstructionName
        {
            get { return "IndexedBranch"; }
        }
        internal readonly int _labelIndex;

        public IndexedBranchInstruction(int labelIndex)
        {
            _labelIndex = labelIndex;
        }

        public RuntimeLabel GetLabel(InterpretedFrame frame)
        {
            Debug.Assert(_labelIndex != UnknownInstrIndex);
            return frame.Interpreter._labels[_labelIndex];
        }

        public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects)
        {
            Debug.Assert(_labelIndex != UnknownInstrIndex);
            int targetIndex = labelIndexer(_labelIndex);
            return ToString() + (targetIndex != BranchLabel.UnknownIndex ? " -> " + targetIndex : "");
        }

        public override string ToString()
        {
            Debug.Assert(_labelIndex != UnknownInstrIndex);
            return InstructionName + "[" + _labelIndex + "]";
        }
    }

    /// <summary>
    /// This instruction implements a goto expression that can jump out of any expression. 
    /// It pops values (arguments) from the evaluation stack that the expression tree nodes in between 
    /// the goto expression and the target label node pushed and not consumed yet. 
    /// A goto expression can jump into a node that evaluates arguments only if it carries 
    /// a value and jumps right after the first argument (the carried value will be used as the first argument). 
    /// Goto can jump into an arbitrary child of a BlockExpression since the block doesn�t accumulate values 
    /// on evaluation stack as its child expressions are being evaluated.
    /// 
    /// Goto needs to execute any finally blocks on the way to the target label.
    /// <example>
    /// { 
    ///     f(1, 2, try { g(3, 4, try { goto L } finally { ... }, 6) } finally { ... }, 7, 8)
    ///     L: ... 
    /// }
    /// </example>
    /// The goto expression here jumps to label L while having 4 items on evaluation stack (1, 2, 3 and 4). 
    /// The jump needs to execute both finally blocks, the first one on stack level 4 the 
    /// second one on stack level 2. So, it needs to jump the first finally block, pop 2 items from the stack, 
    /// run second finally block and pop another 2 items from the stack and set instruction pointer to label L.
    /// 
    /// Goto also needs to rethrow ThreadAbortException iff it jumps out of a catch handler and 
    /// the current thread is in "abort requested" state.
    /// </summary>
    internal sealed class GotoInstruction : IndexedBranchInstruction
    {
        private const int Variants = 8;
        private static readonly GotoInstruction[] s_cache = new GotoInstruction[Variants * CacheSize];

        public override string InstructionName
        {
            get { return "Goto"; }
        }
        private readonly bool _hasResult;

        private readonly bool _hasValue;
        private readonly bool _labelTargetGetsValue;

        // The values should technically be Consumed = 1, Produced = 1 for gotos that target a label whose continuation depth 
        // is different from the current continuation depth. This is because we will consume one continuation from the _continuations
        // and at meantime produce a new _pendingContinuation. However, in case of forward gotos, we don't not know that is the 
        // case until the label is emitted. By then the consumed and produced stack information is useless.
        // The important thing here is that the stack balance is 0.
        public override int ConsumedContinuations { get { return 0; } }
        public override int ProducedContinuations { get { return 0; } }

        public override int ConsumedStack
        {
            get { return _hasValue ? 1 : 0; }
        }

        public override int ProducedStack
        {
            get { return _hasResult ? 1 : 0; }
        }

        private GotoInstruction(int targetIndex, bool hasResult, bool hasValue, bool labelTargetGetsValue)
            : base(targetIndex)
        {
            _hasResult = hasResult;
            _hasValue = hasValue;
            _labelTargetGetsValue = labelTargetGetsValue;
        }

        internal static GotoInstruction Create(int labelIndex, bool hasResult, bool hasValue, bool labelTargetGetsValue)
        {
            if (labelIndex < CacheSize)
            {
                var index = Variants * labelIndex | (labelTargetGetsValue ? 4 : 0) | (hasResult ? 2 : 0) | (hasValue ? 1 : 0);
                return s_cache[index] ?? (s_cache[index] = new GotoInstruction(labelIndex, hasResult, hasValue, labelTargetGetsValue));
            }
            return new GotoInstruction(labelIndex, hasResult, hasValue, labelTargetGetsValue);
        }

        public override int Run(InterpretedFrame frame)
        {
            // Are we jumping out of catch/finally while aborting the current thread?
#if FEATURE_THREAD_ABORT
            Interpreter.AbortThreadIfRequested(frame, _labelIndex);
#endif

            // goto the target label or the current finally continuation:
            object value = _hasValue ? frame.Pop() : Interpreter.NoValue;
            return frame.Goto(_labelIndex, _labelTargetGetsValue ? value : Interpreter.NoValue, gotoExceptionHandler: false);
        }
    }

    internal sealed class EnterTryCatchFinallyInstruction : IndexedBranchInstruction
    {
        private readonly bool _hasFinally = false;
        private TryCatchFinallyHandler _tryHandler;

        internal void SetTryHandler(TryCatchFinallyHandler tryHandler)
        {
            Debug.Assert(_tryHandler == null && tryHandler != null, "the tryHandler can be set only once");
            _tryHandler = tryHandler;
        }

        public override int ProducedContinuations { get { return _hasFinally ? 1 : 0; } }

        private EnterTryCatchFinallyInstruction(int targetIndex, bool hasFinally)
            : base(targetIndex)
        {
            _hasFinally = hasFinally;
        }

        internal static EnterTryCatchFinallyInstruction CreateTryFinally(int labelIndex)
        {
            return new EnterTryCatchFinallyInstruction(labelIndex, true);
        }
        internal static EnterTryCatchFinallyInstruction CreateTryCatch()
        {
            return new EnterTryCatchFinallyInstruction(UnknownInstrIndex, false);
        }

        public override int Run(InterpretedFrame frame)
        {
            Debug.Assert(_tryHandler != null, "the tryHandler must be set already");

            if (_hasFinally)
            {
                // Push finally. 
                frame.PushContinuation(_labelIndex);
            }
            int prevInstrIndex = frame.InstructionIndex;
            frame.InstructionIndex++;

            // Start to run the try/catch/finally blocks
            var instructions = frame.Interpreter.Instructions.Instructions;
            try
            {
                // run the try block
                int index = frame.InstructionIndex;
                while (index >= _tryHandler.TryStartIndex && index < _tryHandler.TryEndIndex)
                {
                    index += instructions[index].Run(frame);
                    frame.InstructionIndex = index;
                }

                // we finish the try block and is about to jump out of the try/catch blocks
                if (index == _tryHandler.GotoEndTargetIndex)
                {
                    // run the 'Goto' that jumps out of the try/catch/finally blocks
                    Debug.Assert(instructions[index] is GotoInstruction, "should be the 'Goto' instruction that jumpes out the try/catch/finally");
                    frame.InstructionIndex += instructions[index].Run(frame);
                }
            }
            catch (RethrowException)
            {
                // a rethrow instruction in the try handler gets to run
                throw;
            }
            catch (Exception exception)
            {
                frame.SaveTraceToException(exception);
                // rethrow if there is no catch blocks defined for this try block
                if (!_tryHandler.IsCatchBlockExist) { throw; }

                // Search for the best handler in the TryCatchFianlly block. If no suitable handler is found, rethrow
                ExceptionHandler exHandler;
                frame.InstructionIndex += _tryHandler.GotoHandler(frame, exception, out exHandler);
                if (exHandler == null) { throw; }

#if FEATURE_THREAD_ABORT
                // stay in the current catch so that ThreadAbortException is not rethrown by CLR:
                var abort = exception as ThreadAbortException;
                if (abort != null)
                {
                    Interpreter.AnyAbortException = abort;
                    frame.CurrentAbortHandler = exHandler;
                }
#endif

                bool rethrow = false;
                try
                {
                    // run the catch block
                    int index = frame.InstructionIndex;
                    while (index >= exHandler.HandlerStartIndex && index < exHandler.HandlerEndIndex)
                    {
                        index += instructions[index].Run(frame);
                        frame.InstructionIndex = index;
                    }

                    // we finish the catch block and is about to jump out of the try/catch blocks
                    if (index == _tryHandler.GotoEndTargetIndex)
                    {
                        // run the 'Goto' that jumps out of the try/catch/finally blocks
                        Debug.Assert(instructions[index] is GotoInstruction, "should be the 'Goto' instruction that jumpes out the try/catch/finally");
                        frame.InstructionIndex += instructions[index].Run(frame);
                    }
                }
                catch (RethrowException)
                {
                    // a rethrow instruction in a catch block gets to run
                    rethrow = true;
                }

                if (rethrow) { throw; }
            }
            finally
            {
                if (_tryHandler.IsFinallyBlockExist)
                {
                    // We get to the finally block in two paths:
                    //  1. Jump from the try/catch blocks. This includes two sub-routes:
                    //        a. 'Goto' instruction in the middle of try/catch block
                    //        b. try/catch block runs to its end. Then the 'Goto(end)' will be trigger to jump out of the try/catch block
                    //  2. Exception thrown from the try/catch blocks
                    // In the first path, the continuation mechanism works and frame.InstructionIndex will be updated to point to the first instruction of the finally block
                    // In the second path, the continuation mechanism is not involved and frame.InstructionIndex is not updated
#if DEBUG
                    bool isFromJump = frame.IsJumpHappened();
                    Debug.Assert(!isFromJump || (isFromJump && _tryHandler.FinallyStartIndex == frame.InstructionIndex), "we should already jump to the first instruction of the finally");
#endif
                    // run the finally block
                    // we cannot jump out of the finally block, and we cannot have an immediate rethrow in it
                    int index = frame.InstructionIndex = _tryHandler.FinallyStartIndex;
                    while (index >= _tryHandler.FinallyStartIndex && index < _tryHandler.FinallyEndIndex)
                    {
                        index += instructions[index].Run(frame);
                        frame.InstructionIndex = index;
                    }
                }
            }

            return frame.InstructionIndex - prevInstrIndex;
        }

        public override string InstructionName
        {
            get { return _hasFinally ? "EnterTryFinally" : "EnterTryCatch"; }
        }

        public override string ToString()
        {
            return _hasFinally ? "EnterTryFinally[" + _labelIndex + "]" : "EnterTryCatch";
        }
    }

    /// <summary>
    /// The first instruction of finally block.
    /// </summary>
    internal sealed class EnterFinallyInstruction : IndexedBranchInstruction
    {
        private readonly static EnterFinallyInstruction[] s_cache = new EnterFinallyInstruction[CacheSize];

        public override string InstructionName
        {
            get { return "EnterFinally"; }
        }
        public override int ProducedStack { get { return 2; } }
        public override int ConsumedContinuations { get { return 1; } }

        private EnterFinallyInstruction(int labelIndex)
            : base(labelIndex)
        {
        }

        internal static EnterFinallyInstruction Create(int labelIndex)
        {
            if (labelIndex < CacheSize)
            {
                return s_cache[labelIndex] ?? (s_cache[labelIndex] = new EnterFinallyInstruction(labelIndex));
            }
            return new EnterFinallyInstruction(labelIndex);
        }

        public override int Run(InterpretedFrame frame)
        {
            // If _pendingContinuation == -1 then we were getting into the finally block because an exception was thrown
            //      in this case we need to set the stack depth
            // Else we were getting into this finnaly block from a 'Goto' jump, and the stack depth is alreayd set properly
            if (!frame.IsJumpHappened())
            {
                frame.SetStackDepth(GetLabel(frame).StackDepth);
            }

            frame.PushPendingContinuation();
            frame.RemoveContinuation();
            return 1;
        }
    }

    /// <summary>
    /// The last instruction of finally block.
    /// </summary>
    internal sealed class LeaveFinallyInstruction : Instruction
    {
        internal static readonly Instruction Instance = new LeaveFinallyInstruction();

        public override int ConsumedStack { get { return 2; } }
        public override string InstructionName
        {
            get { return "LeaveFinally"; }
        }
        private LeaveFinallyInstruction()
        {
        }

        public override int Run(InterpretedFrame frame)
        {
            frame.PopPendingContinuation();

            // If _pendingContinuation == -1 then we were getting into the finally block because an exception was thrown
            // In this case we just return 1, and the the real instruction index will be calculated by GotoHandler later
            if (!frame.IsJumpHappened()) { return 1; }
            // jump to goto target or to the next finally:
            return frame.YieldToPendingContinuation();
        }
    }

    // no-op: we need this just to balance the stack depth.
    internal sealed class EnterExceptionHandlerInstruction : Instruction
    {
        internal static readonly EnterExceptionHandlerInstruction Void = new EnterExceptionHandlerInstruction(false);
        internal static readonly EnterExceptionHandlerInstruction NonVoid = new EnterExceptionHandlerInstruction(true);

        // True if try-expression is non-void.
        private readonly bool _hasValue;
        public override string InstructionName
        {
            get { return "EnterExceptionHandler"; }
        }
        private EnterExceptionHandlerInstruction(bool hasValue)
        {
            _hasValue = hasValue;
        }

        // If an exception is throws in try-body the expression result of try-body is not evaluated and loaded to the stack. 
        // So the stack doesn't contain the try-body's value when we start executing the handler.
        // However, while emitting instructions try block falls thru the catch block with a value on stack. 
        // We need to declare it consumed so that the stack state upon entry to the handler corresponds to the real 
        // stack depth after throw jumped to this catch block.
        public override int ConsumedStack { get { return _hasValue ? 1 : 0; } }

        // A variable storing the current exception is pushed to the stack by exception handling.
        // Catch handlers: The value is immediately popped and stored into a local.
        // Fault handlers: The value is kept on stack during fault handler evaluation.
        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            // nop (the exception value is pushed by the interpreter in HandleCatch)
            return 1;
        }
    }

    /// <summary>
    /// The last instruction of a catch exception handler.
    /// </summary>
    internal sealed class LeaveExceptionHandlerInstruction : IndexedBranchInstruction
    {
        private static LeaveExceptionHandlerInstruction[] s_cache = new LeaveExceptionHandlerInstruction[2 * CacheSize];

        private readonly bool _hasValue;
        public override string InstructionName
        {
            get { return "LeaveExceptionHandler"; }
        }
        // The catch block yields a value if the body is non-void. This value is left on the stack. 
        public override int ConsumedStack
        {
            get { return _hasValue ? 1 : 0; }
        }

        public override int ProducedStack
        {
            get { return _hasValue ? 1 : 0; }
        }

        private LeaveExceptionHandlerInstruction(int labelIndex, bool hasValue)
            : base(labelIndex)
        {
            _hasValue = hasValue;
        }

        internal static LeaveExceptionHandlerInstruction Create(int labelIndex, bool hasValue)
        {
            if (labelIndex < CacheSize)
            {
                int index = (2 * labelIndex) | (hasValue ? 1 : 0);
                return s_cache[index] ?? (s_cache[index] = new LeaveExceptionHandlerInstruction(labelIndex, hasValue));
            }
            return new LeaveExceptionHandlerInstruction(labelIndex, hasValue);
        }

        public override int Run(InterpretedFrame frame)
        {
            // CLR rethrows ThreadAbortException when leaving catch handler if abort is requested on the current thread.
#if FEATURE_THREAD_ABORT
            Interpreter.AbortThreadIfRequested(frame, _labelIndex);
#endif
            return GetLabel(frame).Index - frame.InstructionIndex;
        }
    }

    /// <summary>
    /// The last instruction of a fault exception handler.
    /// </summary>
    internal sealed class LeaveFaultInstruction : Instruction
    {
        internal static readonly Instruction NonVoid = new LeaveFaultInstruction(true);
        internal static readonly Instruction Void = new LeaveFaultInstruction(false);

        private readonly bool _hasValue;

        public override string InstructionName
        {
            get { return "LeaveFault"; }
        }

        // The fault block has a value if the body is non-void, but the value is never used.
        // We compile the body of a fault block as void.
        // However, we keep the exception object that was pushed upon entering the fault block on the stack during execution of the block
        // and pop it at the end.
        public override int ConsumedStack
        {
            get { return 1; }
        }

        // While emitting instructions a non-void try-fault expression is expected to produce a value. 
        public override int ProducedStack
        {
            get { return _hasValue ? 1 : 0; }
        }

        private LeaveFaultInstruction(bool hasValue)
        {
            _hasValue = hasValue;
        }

        public override int Run(InterpretedFrame frame)
        {
            object exception = frame.Pop();
            throw new RethrowException();
        }
    }


    internal sealed class ThrowInstruction : Instruction
    {
        internal static readonly ThrowInstruction Throw = new ThrowInstruction(true, false);
        internal static readonly ThrowInstruction VoidThrow = new ThrowInstruction(false, false);
        internal static readonly ThrowInstruction Rethrow = new ThrowInstruction(true, true);
        internal static readonly ThrowInstruction VoidRethrow = new ThrowInstruction(false, true);

        private readonly bool _hasResult, _rethrow;
        public override string InstructionName
        {
            get { return "Throw"; }
        }
        private ThrowInstruction(bool hasResult, bool isRethrow)
        {
            _hasResult = hasResult;
            _rethrow = isRethrow;
        }

        public override int ProducedStack
        {
            get { return _hasResult ? 1 : 0; }
        }

        public override int ConsumedStack
        {
            get
            {
                return 1;
            }
        }

        public override int Run(InterpretedFrame frame)
        {
            var ex = (Exception)frame.Pop();
            if (_rethrow)
            {
                throw new RethrowException();
            }
            throw ex;
        }
    }

    internal sealed class SwitchInstruction : Instruction
    {
        private readonly Dictionary<int, int> _cases;

        public override string InstructionName
        {
            get { return "Switch"; }
        }
        internal SwitchInstruction(Dictionary<int, int> cases)
        {
            Assert.NotNull(cases);
            _cases = cases;
        }

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 0; } }

        public override int Run(InterpretedFrame frame)
        {
            int target;
            return _cases.TryGetValue((int)frame.Pop(), out target) ? target : 1;
        }
    }

    internal sealed class EnterLoopInstruction : Instruction
    {
        private readonly int _instructionIndex;
        private Dictionary<ParameterExpression, LocalVariable> _variables;
        private Dictionary<ParameterExpression, LocalVariable> _closureVariables;
        private LoopExpression _loop;
        private int _loopEnd;

        public override string InstructionName
        {
            get { return "EnterLoop"; }
        }

        internal EnterLoopInstruction(LoopExpression loop, LocalVariables locals, int instructionIndex)
        {
            _loop = loop;
            _variables = locals.CopyLocals();
            _closureVariables = locals.ClosureVariables;
            _instructionIndex = instructionIndex;
        }

        internal void FinishLoop(int loopEnd)
        {
            _loopEnd = loopEnd;
        }

        public override int Run(InterpretedFrame frame)
        {
            return 1;
        }

        private bool Compiled
        {
            get { return _loop == null; }
        }

        private void Compile(object frameObj)
        {
        }
    }
}
