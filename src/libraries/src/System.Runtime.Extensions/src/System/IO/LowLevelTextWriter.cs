// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    internal abstract class LowLevelTextWriter
    {
        public abstract void Write(char c);
        public abstract void Write(String s);
    }
}

