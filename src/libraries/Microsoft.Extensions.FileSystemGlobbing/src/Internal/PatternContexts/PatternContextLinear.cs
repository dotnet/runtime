// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts
{
    public abstract class PatternContextLinear
        : PatternContext<PatternContextLinear.FrameData>
    {
        public PatternContextLinear(ILinearPattern pattern)
        {
            ThrowHelper.ThrowIfNull(pattern);

            Pattern = pattern;
        }

        public override PatternTestResult Test(FileInfoBase file)
        {
            if (IsStackEmpty())
            {
                throw new InvalidOperationException(SR.CannotTestFile);
            }

            if (!Frame.IsNotApplicable && IsLastSegment() && TestMatchingSegment(file.Name))
            {
                return PatternTestResult.Success(CalculateStem(file));
            }

            return PatternTestResult.Failed;
        }

        public override void PushDirectory(DirectoryInfoBase directory)
        {
            // copy the current frame
            FrameData frame = Frame;

            if (IsStackEmpty() || Frame.IsNotApplicable)
            {
                // when the stack is being initialized
                // or no change is required.
            }
            else if (!TestMatchingSegment(directory.Name))
            {
                // nothing down this path is affected by this pattern
                frame.IsNotApplicable = true;
            }
            else
            {
                // Determine this frame's contribution to the stem (if any)
                IPathSegment segment = Pattern.Segments[Frame.SegmentIndex];
                if (frame.InStem || segment.CanProduceStem)
                {
                    frame.InStem = true;
                    frame.StemItems.Add(directory.Name);
                }

                // directory matches segment, advance position in pattern
                frame.SegmentIndex++;
            }

            PushDataFrame(frame);
        }

        public struct FrameData
        {
            public bool IsNotApplicable;
            public int SegmentIndex;
            public bool InStem;
            private IList<string>? _stemItems;

            public IList<string> StemItems => _stemItems ??= new List<string>();

            public string? Stem => _stemItems == null ? null : string.Join("/", _stemItems);
        }

        protected ILinearPattern Pattern { get; }

        protected bool IsLastSegment()
        {
            return Frame.SegmentIndex == Pattern.Segments.Count - 1;
        }

        protected bool TestMatchingSegment(string value)
        {
            if (Frame.SegmentIndex >= Pattern.Segments.Count)
            {
                return false;
            }

            return Pattern.Segments[Frame.SegmentIndex].Match(value);
        }

        protected string CalculateStem(FileInfoBase matchedFile)
        {
            return MatcherContext.CombinePath(Frame.Stem, matchedFile.Name);
        }
    }
}
