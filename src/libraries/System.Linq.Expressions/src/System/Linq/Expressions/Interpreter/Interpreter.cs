// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter
{
    /// <summary>
    /// A simple forth-style stack machine for executing Expression trees
    /// without the need to compile to IL and then invoke the JIT.  This trades
    /// off much faster compilation time for a slower execution performance.
    /// For code that is only run a small number of times this can be a
    /// sweet spot.
    /// 
    /// The core loop in the interpreter is the <see cref="Run(InterpretedFrame)"/>  method.
    /// </summary>
    internal sealed class Interpreter
    {
        internal static readonly object NoValue = new object();
        internal const int RethrowOnReturn = int.MaxValue;

        private readonly InstructionArray _instructions;
        internal readonly object[] _objects;
        internal readonly RuntimeLabel[] _labels;
        internal readonly DebugInfo[] _debugInfos;

        internal Interpreter(string name, LocalVariables locals, HybridReferenceDictionary<LabelTarget, BranchLabel> labelMapping,
            InstructionArray instructions, DebugInfo[] debugInfos)
        {
            Name = name;
            LocalCount = locals.LocalCount;
            LabelMapping = labelMapping;
            ClosureVariables = locals.ClosureVariables;

            _instructions = instructions;
            _objects = instructions.Objects;
            _labels = instructions.Labels;
            _debugInfos = debugInfos;
        }

        internal string Name { get; }
        internal int LocalCount { get; }
        internal int ClosureSize => ClosureVariables?.Count ?? 0;
        internal InstructionArray Instructions => _instructions;
        internal Dictionary<ParameterExpression, LocalVariable> ClosureVariables { get; }
        internal HybridReferenceDictionary<LabelTarget, BranchLabel> LabelMapping { get; }

        /// <summary>
        /// Runs instructions within the given frame.
        /// </summary>
        /// <remarks>
        /// Interpreted stack frames are linked via Parent reference so that each CLR frame of this method corresponds
        /// to an interpreted stack frame in the chain. It is therefore possible to combine CLR stack traces with
        /// interpreted stack traces by aligning interpreted frames to the frames of this method.
        /// Each group of subsequent frames of Run method corresponds to a single interpreted frame.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Run(InterpretedFrame frame)
        {
            Instruction[] instructions = _instructions.Instructions;
            int index = frame.InstructionIndex;
            while (index < instructions.Length)
            {
                index += instructions[index].Run(frame);
                frame.InstructionIndex = index;
            }
        }

        internal int ReturnAndRethrowLabelIndex
        {
            get
            {
                // the last label is "return and rethrow" label:
                Debug.Assert(_labels[_labels.Length - 1].Index == RethrowOnReturn);
                return _labels.Length - 1;
            }
        }
    }
}
