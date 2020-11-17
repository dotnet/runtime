// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts
{
    public class PatternContextRaggedInclude : PatternContextRagged
    {
        public PatternContextRaggedInclude(IRaggedPattern pattern)
            : base(pattern)
        {
        }

        public override void Declare(Action<IPathSegment, bool> onDeclare)
        {
            if (IsStackEmpty())
            {
                throw new InvalidOperationException("Can't declare path segment before entering a directory.");
            }

            if (Frame.IsNotApplicable)
            {
                return;
            }

            if (IsStartingGroup() && Frame.SegmentIndex < Frame.SegmentGroup.Count)
            {
                onDeclare(Frame.SegmentGroup[Frame.SegmentIndex], false);
            }
            else
            {
                onDeclare(WildcardPathSegment.MatchAll, false);
            }
        }

        public override bool Test(DirectoryInfoBase directory)
        {
            if (IsStackEmpty())
            {
                throw new InvalidOperationException("Can't test directory before entering a directory.");
            }

            if (Frame.IsNotApplicable)
            {
                return false;
            }

            if (IsStartingGroup() && !TestMatchingSegment(directory.Name))
            {
                // deterministic not-included
                return false;
            }

            return true;
        }
    }
}
