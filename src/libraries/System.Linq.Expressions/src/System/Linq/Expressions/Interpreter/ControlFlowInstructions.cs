// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class OffsetInstruction : Instruction
    {
        internal const int Unknown = int.MinValue;
        internal const int CacheSize = 32;

        // the offset to jump to (relative to this instruction):
        protected int _offset = Unknown;

        public abstract Instruction[] Cache { get; }

        public Instruction Fixup(int offset)
        {
            Debug.Assert(_offset == Unknown && offset != Unknown);
            _offset = offset;

            Instruction[] cache = Cache;
            if (cache != null && offset >= 0 && offset < cache.Length)
            {
                return cache[offset] ?? (cache[offset] = this);
            }

            return this;
        }

        public override string ToDebugString(int instructionIndex, object? cookie, Func<int, int> labelIndexer, IReadOnlyList<object>? objects)
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
        private static Instruction[]? s_cache;

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

        public override string InstructionName => "BranchFalse";
        public override int ConsumedStack => 1;

        public override int Run(InterpretedFrame frame)
        {
            Debug.Assert(_offset != Unknown);

            if (!(bool)frame.Pop()!)
            {
                return _offset;
            }

            return 1;
        }
    }

    internal sealed class BranchTrueInstruction : OffsetInstruction
    {
        private static Instruction[]? s_cache;

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

        public override string InstructionName => "BranchTrue";
        public override int ConsumedStack => 1;

        public override int Run(InterpretedFrame frame)
        {
            Debug.Assert(_offset != Unknown);

            if ((bool)frame.Pop()!)
            {
                return _offset;
            }

            return 1;
        }
    }

    internal sealed class CoalescingBranchInstruction : OffsetInstruction
    {
        private static Instruction[]? s_cache;

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

        public override string InstructionName => "CoalescingBranch";
        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame)
        {
            Debug.Assert(_offset != Unknown);

            if (frame.Peek() != null)
            {
                return _offset;
            }

            return 1;
        }
    }

    internal sealed class BranchInstruction : OffsetInstruction
    {
        private static Instruction[][][]? s_caches;

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

        public override string InstructionName => "Branch";
        public override int ConsumedStack => _hasValue ? 1 : 0;
        public override int ProducedStack => _hasResult ? 1 : 0;

        public override int Run(InterpretedFrame frame)
        {
            Debug.Assert(_offset != Unknown);

            return _offset;
        }
    }

    internal abstract class IndexedBranchInstruction : Instruction
    {
        protected const int CacheSize = 32;
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

        public override string ToDebugString(int instructionIndex, object? cookie, Func<int, int> labelIndexer, IReadOnlyList<object>? objects)
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
    /// Goto can jump into an arbitrary child of a BlockExpression since the block doesn't accumulate values
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

        public override string InstructionName => "Goto";

        private readonly bool _hasResult;

        private readonly bool _hasValue;
        private readonly bool _labelTargetGetsValue;

        // Should technically return 1 for ConsumedContinuations and ProducedContinuations for gotos that target a label whose continuation depth
        // is different from the current continuation depth. This is because we will consume one continuation from the _continuations
        // and at meantime produce a new _pendingContinuation. However, in case of forward gotos, we don't not know that is the
        // case until the label is emitted. By then the consumed and produced stack information is useless.
        // The important thing here is that the stack balance is 0.

        public override int ConsumedStack => _hasValue ? 1 : 0;
        public override int ProducedStack => _hasResult ? 1 : 0;

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
                int index = Variants * labelIndex | (labelTargetGetsValue ? 4 : 0) | (hasResult ? 2 : 0) | (hasValue ? 1 : 0);
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
            object? value = _hasValue ? frame.Pop() : Interpreter.NoValue;
            return frame.Goto(_labelIndex, _labelTargetGetsValue ? value : Interpreter.NoValue, gotoExceptionHandler: false);
        }
    }

    internal sealed class EnterTryCatchFinallyInstruction : IndexedBranchInstruction
    {
        private readonly bool _hasFinally;
        private TryCatchFinallyHandler? _tryHandler;

        internal void SetTryHandler(TryCatchFinallyHandler tryHandler)
        {
            Debug.Assert(_tryHandler == null && tryHandler != null, "the tryHandler can be set only once");
            _tryHandler = tryHandler;
        }

        internal TryCatchFinallyHandler? Handler => _tryHandler;

        public override int ProducedContinuations => _hasFinally ? 1 : 0;

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
            Instruction[] instructions = frame.Interpreter.Instructions.Instructions;
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
                    Debug.Assert(instructions[index] is GotoInstruction, "should be the 'Goto' instruction that jumps out the try/catch/finally");
                    frame.InstructionIndex += instructions[index].Run(frame);
                }
            }
            catch (Exception exception) when (_tryHandler.HasHandler(frame, exception, out ExceptionHandler? exHandler, out object? unwrappedException))
            {
                Debug.Assert(!(unwrappedException is RethrowException));
                frame.InstructionIndex += frame.Goto(exHandler.LabelIndex, unwrappedException, gotoExceptionHandler: true);

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
                        Debug.Assert(instructions[index] is GotoInstruction, "should be the 'Goto' instruction that jumps out the try/catch/finally");
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

        public override string InstructionName => _hasFinally ? "EnterTryFinally" : "EnterTryCatch";

        public override string ToString() => _hasFinally ? "EnterTryFinally[" + _labelIndex + "]" : "EnterTryCatch";
    }

    internal sealed class EnterTryFaultInstruction : IndexedBranchInstruction
    {
        private TryFaultHandler? _tryHandler;

        internal EnterTryFaultInstruction(int targetIndex)
            : base(targetIndex)
        {
        }

        public override string InstructionName => "EnterTryFault";
        public override int ProducedContinuations => 1;

        internal TryFaultHandler? Handler => _tryHandler;

        internal void SetTryHandler(TryFaultHandler tryHandler)
        {
            Debug.Assert(tryHandler != null);
            Debug.Assert(_tryHandler == null, "the tryHandler can be set only once");
            _tryHandler = tryHandler;
        }

        public override int Run(InterpretedFrame frame)
        {
            Debug.Assert(_tryHandler != null, "the tryHandler must be set already");

            // Push fault.
            frame.PushContinuation(_labelIndex);

            int prevInstrIndex = frame.InstructionIndex;
            frame.InstructionIndex++;

            // Start to run the try/fault blocks
            Instruction[] instructions = frame.Interpreter.Instructions.Instructions;

            // C# 6 has no direct support for fault blocks, but they can be faked or coerced out of the compiler
            // in several ways. Catch-and-rethrow can work in specific cases, but not generally as the double-pass
            // will not work correctly with filters higher up the call stack. Iterators can be used to produce real
            // fault blocks, but it depends on an implementation detail rather than a guarantee, and is rather
            // indirect. This leaves using a finally block and not doing anything in it if the body ran to
            // completion, which is the approach used here.
            bool ranWithoutFault = false;
            try
            {
                // run the try block
                int index = frame.InstructionIndex;
                while (index >= _tryHandler.TryStartIndex && index < _tryHandler.TryEndIndex)
                {
                    index += instructions[index].Run(frame);
                    frame.InstructionIndex = index;
                }

                // run the 'Goto' that jumps out of the try/fault blocks
                Debug.Assert(instructions[index] is GotoInstruction, "should be the 'Goto' instruction that jumps out the try/fault");

                // if we've arrived here there was no exception thrown. As the fault block won't run, we need to
                // pop the continuation for it here, before Gotoing the end of the try/fault.
                ranWithoutFault = true;
                frame.RemoveContinuation();
                frame.InstructionIndex += instructions[index].Run(frame);
            }
            finally
            {
                if (!ranWithoutFault)
                {
                    // run the fault block
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
    }

    /// <summary>
    /// The first instruction of finally block.
    /// </summary>
    internal sealed class EnterFinallyInstruction : IndexedBranchInstruction
    {
        private static readonly EnterFinallyInstruction[] s_cache = new EnterFinallyInstruction[CacheSize];

        private EnterFinallyInstruction(int labelIndex)
            : base(labelIndex)
        {
        }

        public override string InstructionName => "EnterFinally";
        public override int ProducedStack => 2;
        public override int ConsumedContinuations => 1;

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
            // Else we were getting into this finally block from a 'Goto' jump, and the stack depth is already set properly
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

        private LeaveFinallyInstruction() { }

        public override int ConsumedStack => 2;
        public override string InstructionName => "LeaveFinally";

        public override int Run(InterpretedFrame frame)
        {
            frame.PopPendingContinuation();

            // If _pendingContinuation == -1 then we were getting into the finally block because an exception was thrown
            // In this case we just return 1, and the real instruction index will be calculated by GotoHandler later
            if (!frame.IsJumpHappened()) { return 1; }
            // jump to goto target or to the next finally:
            return frame.YieldToPendingContinuation();
        }
    }

    internal sealed class EnterFaultInstruction : IndexedBranchInstruction
    {
        private static readonly EnterFaultInstruction[] s_cache = new EnterFaultInstruction[CacheSize];

        private EnterFaultInstruction(int labelIndex)
            : base(labelIndex)
        {
        }

        public override string InstructionName => "EnterFault";
        public override int ProducedStack => 2;

        internal static EnterFaultInstruction Create(int labelIndex)
        {
            if (labelIndex < CacheSize)
            {
                return s_cache[labelIndex] ?? (s_cache[labelIndex] = new EnterFaultInstruction(labelIndex));
            }

            return new EnterFaultInstruction(labelIndex);
        }

        public override int Run(InterpretedFrame frame)
        {
            Debug.Assert(!frame.IsJumpHappened());

            frame.SetStackDepth(GetLabel(frame).StackDepth);
            frame.PushPendingContinuation();
            frame.RemoveContinuation();
            return 1;
        }
    }

    internal sealed class LeaveFaultInstruction : Instruction
    {
        internal static readonly Instruction Instance = new LeaveFaultInstruction();

        private LeaveFaultInstruction() { }

        public override int ConsumedStack => 2;
        public override int ConsumedContinuations => 1;
        public override string InstructionName => "LeaveFault";

        public override int Run(InterpretedFrame frame)
        {
            frame.PopPendingContinuation();

            Debug.Assert(!frame.IsJumpHappened());
            // Just return 1, and the real instruction index will be calculated by GotoHandler later
            return 1;
        }
    }

    // no-op: we need this just to balance the stack depth and aid debugging of the instruction list.
    internal sealed class EnterExceptionFilterInstruction : Instruction
    {
        internal static readonly EnterExceptionFilterInstruction Instance = new EnterExceptionFilterInstruction();

        private EnterExceptionFilterInstruction() { }

        public override string InstructionName => "EnterExceptionFilter";


        // The exception is pushed onto the stack in the filter runner.
        public override int ProducedStack => 1;

        [ExcludeFromCodeCoverage(Justification = "Known to be a no-op, this instruction is skipped on execution")]
        public override int Run(InterpretedFrame frame) => 1;
    }

    // no-op: we need this just to balance the stack depth and aid debugging of the instruction list.
    internal sealed class LeaveExceptionFilterInstruction : Instruction
    {
        internal static readonly LeaveExceptionFilterInstruction Instance = new LeaveExceptionFilterInstruction();

        private LeaveExceptionFilterInstruction() { }

        public override string InstructionName => "LeaveExceptionFilter";

        // The exception and the boolean result are popped from the stack in the filter runner.
        public override int ConsumedStack => 2;

        [ExcludeFromCodeCoverage(Justification = "Known to be a no-op, this instruction is skipped on execution")]
        public override int Run(InterpretedFrame frame) => 1;
    }

    // no-op: we need this just to balance the stack depth.
    internal sealed class EnterExceptionHandlerInstruction : Instruction
    {
        internal static readonly EnterExceptionHandlerInstruction Void = new EnterExceptionHandlerInstruction(false);
        internal static readonly EnterExceptionHandlerInstruction NonVoid = new EnterExceptionHandlerInstruction(true);

        // True if try-expression is non-void.
        private readonly bool _hasValue;

        private EnterExceptionHandlerInstruction(bool hasValue)
        {
            _hasValue = hasValue;
        }

        public override string InstructionName => "EnterExceptionHandler";

        // If an exception is throws in try-body the expression result of try-body is not evaluated and loaded to the stack.
        // So the stack doesn't contain the try-body's value when we start executing the handler.
        // However, while emitting instructions try block falls thru the catch block with a value on stack.
        // We need to declare it consumed so that the stack state upon entry to the handler corresponds to the real
        // stack depth after throw jumped to this catch block.
        public override int ConsumedStack => _hasValue ? 1 : 0;

        // A variable storing the current exception is pushed to the stack by exception handling.
        // Catch handlers: The value is immediately popped and stored into a local.
        public override int ProducedStack => 1;

        [ExcludeFromCodeCoverage(Justification = "Known to be a no-op, this instruction is skipped on execution")]
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
        private static readonly LeaveExceptionHandlerInstruction[] s_cache = new LeaveExceptionHandlerInstruction[2 * CacheSize];

        private readonly bool _hasValue;

        private LeaveExceptionHandlerInstruction(int labelIndex, bool hasValue)
            : base(labelIndex)
        {
            _hasValue = hasValue;
        }

        public override string InstructionName => "LeaveExceptionHandler";

        // The catch block yields a value if the body is non-void. This value is left on the stack.
        public override int ConsumedStack => _hasValue ? 1 : 0;
        public override int ProducedStack => _hasValue ? 1 : 0;

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

    internal sealed class ThrowInstruction : Instruction
    {
        internal static readonly ThrowInstruction Throw = new ThrowInstruction(true, false);
        internal static readonly ThrowInstruction VoidThrow = new ThrowInstruction(false, false);
        internal static readonly ThrowInstruction Rethrow = new ThrowInstruction(true, true);
        internal static readonly ThrowInstruction VoidRethrow = new ThrowInstruction(false, true);

        private readonly bool _hasResult, _rethrow;

        private ThrowInstruction(bool hasResult, bool isRethrow)
        {
            _hasResult = hasResult;
            _rethrow = isRethrow;
        }

        public override string InstructionName => "Throw";
        public override int ProducedStack => _hasResult ? 1 : 0;
        public override int ConsumedStack => 1;

        public override int Run(InterpretedFrame frame)
        {
            Exception? ex = WrapThrownObject(frame.Pop());
            if (_rethrow)
            {
                throw new RethrowException();
            }

            throw ex!;
        }

        private static Exception? WrapThrownObject(object? thrown) =>
            thrown == null ? null : (thrown as Exception ?? new RuntimeWrappedException(thrown));
    }

    internal sealed class IntSwitchInstruction<T> : Instruction where T : notnull
    {
        private readonly Dictionary<T, int> _cases;

        internal IntSwitchInstruction(Dictionary<T, int> cases)
        {
            Assert.NotNull(cases);
            _cases = cases;
        }

        public override string InstructionName => "IntSwitch";
        public override int ConsumedStack => 1;

        public override int Run(InterpretedFrame frame)
        {
            int target;
            return _cases.TryGetValue((T)frame.Pop()!, out target) ? target : 1;
        }
    }

    internal sealed class StringSwitchInstruction : Instruction
    {
        private readonly Dictionary<string, int> _cases;
        private readonly StrongBox<int> _nullCase;

        internal StringSwitchInstruction(Dictionary<string, int> cases, StrongBox<int> nullCase)
        {
            Assert.NotNull(cases);
            Assert.NotNull(nullCase);
            _cases = cases;
            _nullCase = nullCase;
        }

        public override string InstructionName => "StringSwitch";
        public override int ConsumedStack => 1;

        public override int Run(InterpretedFrame frame)
        {
            object? value = frame.Pop();

            if (value == null)
            {
                return _nullCase.Value;
            }

            int target;
            return _cases.TryGetValue((string)value, out target) ? target : 1;
        }
    }
}
