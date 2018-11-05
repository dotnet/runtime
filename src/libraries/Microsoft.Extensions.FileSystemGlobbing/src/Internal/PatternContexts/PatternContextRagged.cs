// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts
{
    public abstract class PatternContextRagged : PatternContext<PatternContextRagged.FrameData>
    {
        public PatternContextRagged(IRaggedPattern pattern)
        {
            Pattern = pattern;
        }

        public override PatternTestResult Test(FileInfoBase file)
        {
            if (IsStackEmpty())
            {
                throw new InvalidOperationException("Can't test file before entering a directory.");
            }

            if(!Frame.IsNotApplicable && IsEndingGroup() && TestMatchingGroup(file))
            {
                return PatternTestResult.Success(CalculateStem(file));
            }
            return PatternTestResult.Failed;
        }

        public sealed override void PushDirectory(DirectoryInfoBase directory)
        {
            // copy the current frame
            var frame = Frame;

            if (IsStackEmpty())
            {
                // initializing
                frame.SegmentGroupIndex = -1;
                frame.SegmentGroup = Pattern.StartsWith;
            }
            else if (Frame.IsNotApplicable)
            {
                // no change
            }
            else if (IsStartingGroup())
            {
                if (!TestMatchingSegment(directory.Name))
                {
                    // nothing down this path is affected by this pattern
                    frame.IsNotApplicable = true;
                }
                else
                {
                    // starting path incrementally satisfied
                    frame.SegmentIndex += 1;
                }
            }
            else if (!IsStartingGroup() && directory.Name == "..")
            {
                // any parent path segment is not applicable in **
                frame.IsNotApplicable = true;
            }
            else if (!IsStartingGroup() && !IsEndingGroup() && TestMatchingGroup(directory))
            {
                frame.SegmentIndex = Frame.SegmentGroup.Count;
                frame.BacktrackAvailable = 0;
            }
            else
            {
                // increase directory backtrack length
                frame.BacktrackAvailable += 1;
            }

            if (frame.InStem)
            {
                frame.StemItems.Add(directory.Name);
            }

            while (
                frame.SegmentIndex == frame.SegmentGroup.Count &&
                frame.SegmentGroupIndex != Pattern.Contains.Count)
            {
                frame.SegmentGroupIndex += 1;
                frame.SegmentIndex = 0;
                if (frame.SegmentGroupIndex < Pattern.Contains.Count)
                {
                    frame.SegmentGroup = Pattern.Contains[frame.SegmentGroupIndex];
                }
                else
                {
                    frame.SegmentGroup = Pattern.EndsWith;
                }

                // We now care about the stem
                frame.InStem = true;
            }

            PushDataFrame(frame);
        }

        public override void PopDirectory()
        {
            base.PopDirectory();
            if (Frame.StemItems.Count > 0)
            {
                Frame.StemItems.RemoveAt(Frame.StemItems.Count - 1);
            }
        }

        public struct FrameData
        {
            public bool IsNotApplicable;

            public int SegmentGroupIndex;

            public IList<IPathSegment> SegmentGroup;

            public int BacktrackAvailable;

            public int SegmentIndex;

            public bool InStem;

            private IList<string> _stemItems;

            public IList<string> StemItems
            {
                get { return _stemItems ?? (_stemItems = new List<string>()); }
            }

            public string Stem
            {
                get { return _stemItems == null ? null : string.Join("/", _stemItems); }
            }
        }

        protected IRaggedPattern Pattern { get; }

        protected bool IsStartingGroup()
        {
            return Frame.SegmentGroupIndex == -1;
        }

        protected bool IsEndingGroup()
        {
            return Frame.SegmentGroupIndex == Pattern.Contains.Count;
        }

        protected bool TestMatchingSegment(string value)
        {
            if (Frame.SegmentIndex >= Frame.SegmentGroup.Count)
            {
                return false;
            }
            return Frame.SegmentGroup[Frame.SegmentIndex].Match(value);
        }

        protected bool TestMatchingGroup(FileSystemInfoBase value)
        {
            var groupLength = Frame.SegmentGroup.Count;
            var backtrackLength = Frame.BacktrackAvailable + 1;
            if (backtrackLength < groupLength)
            {
                return false;
            }

            var scan = value;
            for (int index = 0; index != groupLength; ++index)
            {
                var segment = Frame.SegmentGroup[groupLength - index - 1];
                if (!segment.Match(scan.Name))
                {
                    return false;
                }
                scan = scan.ParentDirectory;
            }
            return true;
        }

        protected string CalculateStem(FileInfoBase matchedFile)
        {
            return MatcherContext.CombinePath(Frame.Stem, matchedFile.Name);
        }
    }
}
