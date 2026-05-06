// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Linq.Expressions.Interpreter
{
    internal readonly struct RuntimeLabel
    {
        public readonly int Index;
        public readonly int StackDepth;
        public readonly int ContinuationStackDepth;

        public RuntimeLabel(int index, int continuationStackDepth, int stackDepth)
        {
            Index = index;
            ContinuationStackDepth = continuationStackDepth;
            StackDepth = stackDepth;
        }

        public override string ToString() =>
            string.Create(CultureInfo.InvariantCulture, $"->{Index} C({ContinuationStackDepth}) S({StackDepth})");
    }

    internal sealed class BranchLabel
    {
        internal const int UnknownIndex = int.MinValue;
        internal const int UnknownDepth = int.MinValue;

        private int _targetIndex = UnknownIndex;
        private int _stackDepth = UnknownDepth;
        private int _continuationStackDepth = UnknownDepth;

        // Offsets of forward branching instructions targeting this label
        // that need to be updated after we emit the label.
        private List<int>? _forwardBranchFixups;

        internal int LabelIndex { get; set; } = UnknownIndex;
        internal bool HasRuntimeLabel => LabelIndex != UnknownIndex;
        internal int TargetIndex => _targetIndex;

        internal RuntimeLabel ToRuntimeLabel()
        {
            Debug.Assert(_targetIndex != UnknownIndex && _stackDepth != UnknownDepth && _continuationStackDepth != UnknownDepth);
            return new RuntimeLabel(_targetIndex, _continuationStackDepth, _stackDepth);
        }

        internal void Mark(InstructionList instructions)
        {
            //ContractUtils.Requires(_targetIndex == UnknownIndex && _stackDepth == UnknownDepth && _continuationStackDepth == UnknownDepth);

            _stackDepth = instructions.CurrentStackDepth;
            _continuationStackDepth = instructions.CurrentContinuationsDepth;
            _targetIndex = instructions.Count;

            if (_forwardBranchFixups != null)
            {
                foreach (int branchIndex in _forwardBranchFixups)
                {
                    FixupBranch(instructions, branchIndex);
                }
                _forwardBranchFixups = null;
            }
        }

        internal void AddBranch(InstructionList instructions, int branchIndex)
        {
            Debug.Assert(((_targetIndex == UnknownIndex) == (_stackDepth == UnknownDepth)));
            Debug.Assert(((_targetIndex == UnknownIndex) == (_continuationStackDepth == UnknownDepth)));

            if (_targetIndex == UnknownIndex)
            {
                _forwardBranchFixups ??= new List<int>();
                _forwardBranchFixups.Add(branchIndex);
            }
            else
            {
                FixupBranch(instructions, branchIndex);
            }
        }

        internal void FixupBranch(InstructionList instructions, int branchIndex)
        {
            Debug.Assert(_targetIndex != UnknownIndex);
            instructions.FixupBranch(branchIndex, _targetIndex - branchIndex);
        }
    }
}
