// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts
{
    public class PatternContextRaggedExclude : PatternContextRagged
    {
        public PatternContextRaggedExclude(IRaggedPattern pattern)
            : base(pattern)
        {
        }

        public override bool Test(DirectoryInfoBase directory)
        {
            if (IsStackEmpty())
            {
                throw new InvalidOperationException(SR.CannotTestDirectory);
            }

            if (Frame.IsNotApplicable)
            {
                return false;
            }

            if (IsEndingGroup() && TestMatchingGroup(directory))
            {
                // directory excluded with file-like pattern
                return true;
            }

            if (Pattern.EndsWith.Count == 0 &&
                Frame.SegmentGroupIndex == Pattern.Contains.Count - 1 &&
                TestMatchingGroup(directory))
            {
                // directory excluded by matching up to final '/**'
                return true;
            }

            return false;
        }
    }
}
