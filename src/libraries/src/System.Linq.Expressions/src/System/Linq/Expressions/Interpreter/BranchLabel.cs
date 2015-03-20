// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;

namespace System.Linq.Expressions.Interpreter
{
    internal struct RuntimeLabel
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

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "->{0} C({1}) S({2})", Index, ContinuationStackDepth, StackDepth);
        }
    }

    internal sealed class BranchLabel
    {
        internal const int UnknownIndex = Int32.MinValue;
        internal const int UnknownDepth = Int32.MinValue;

        internal int _labelIndex = UnknownIndex;
        internal int _targetIndex = UnknownIndex;
        internal int _stackDepth = UnknownDepth;
        internal int _continuationStackDepth = UnknownDepth;

        // Offsets of forward branching instructions targetting this label 
        // that need to be updated after we emit the label.
        private List<int> _forwardBranchFixups;

        public BranchLabel()
        {
        }

        internal int LabelIndex
        {
            get { return _labelIndex; }
            set { _labelIndex = value; }
        }

        internal bool HasRuntimeLabel
        {
            get { return _labelIndex != UnknownIndex; }
        }

        internal int TargetIndex
        {
            get { return _targetIndex; }
        }

        internal int StackDepth
        {
            get { return _stackDepth; }
        }

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
                foreach (var branchIndex in _forwardBranchFixups)
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
                if (_forwardBranchFixups == null)
                {
                    _forwardBranchFixups = new List<int>();
                }
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
