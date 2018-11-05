// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts
{
    public class PatternContextLinearInclude : PatternContextLinear
    {
        public PatternContextLinearInclude(ILinearPattern pattern)
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

            if (Frame.SegmentIndex < Pattern.Segments.Count)
            {
                onDeclare(Pattern.Segments[Frame.SegmentIndex], IsLastSegment());
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

            return !IsLastSegment() && TestMatchingSegment(directory.Name);
        }
    }
}
