// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Internal
{
    internal struct StreamConstraints
    {
        public readonly object GuardOpt;
        public readonly long ImageStart;
        public readonly int ImageSize;

        public StreamConstraints(object guardOpt, long startPosition, int imageSize)
        {
            GuardOpt = guardOpt;
            ImageStart = startPosition;
            ImageSize = imageSize;
        }
    }
}
