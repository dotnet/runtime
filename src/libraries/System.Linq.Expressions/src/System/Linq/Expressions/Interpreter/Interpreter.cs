// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq.Expressions.Interpreter
{
    /// <summary>
    /// A simple forth-style stack machine for executing Expression trees
    /// without the need to compile to IL and then invoke the JIT.  This trades
    /// off much faster compilation time for a slower execution performance.
    /// For code that is only run a small number of times this can be a 
    /// sweet spot.
    /// 
    /// The core loop in the interpreter is the RunInstructions method.
    /// </summary>
    internal sealed class Interpreter
    {
        internal static readonly object NoValue = new object();
        internal const int RethrowOnReturn = Int32.MaxValue;

        private readonly int _localCount;
        private readonly HybridReferenceDictionary<LabelTarget, BranchLabel> _labelMapping;
        private readonly Dictionary<ParameterExpression, LocalVariable> _closureVariables;

        private readonly InstructionArray _instructions;
        internal readonly object[] _objects;
        internal readonly RuntimeLabel[] _labels;

        internal readonly string _name;
        internal readonly DebugInfo[] _debugInfos;

        internal Interpreter(string name, LocalVariables locals, HybridReferenceDictionary<LabelTarget, BranchLabel> labelMapping,
            InstructionArray instructions, DebugInfo[] debugInfos)
        {
            _name = name;
            _localCount = locals.LocalCount;
            _closureVariables = locals.ClosureVariables;

            _instructions = instructions;
            _objects = instructions.Objects;
            _labels = instructions.Labels;
            _labelMapping = labelMapping;

            _debugInfos = debugInfos;
        }

        internal int ClosureSize
        {
            get
            {
                if (_closureVariables == null)
                {
                    return 0;
                }
                return _closureVariables.Count;
            }
        }

        internal int LocalCount
        {
            get
            {
                return _localCount;
            }
        }

        internal InstructionArray Instructions
        {
            get { return _instructions; }
        }

        internal Dictionary<ParameterExpression, LocalVariable> ClosureVariables
        {
            get { return _closureVariables; }
        }

        internal HybridReferenceDictionary<LabelTarget, BranchLabel> LabelMapping
        {
            get { return _labelMapping; }
        }

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
            var instructions = _instructions.Instructions;
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
