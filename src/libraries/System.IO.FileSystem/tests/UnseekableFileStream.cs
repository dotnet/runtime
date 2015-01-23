// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.IO.FileSystem.Tests
{
    internal class UnseekableFileStream : FileStream
    {
        public UnseekableFileStream(string path, FileMode mode)
            : base(path, mode)
        { }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }
    }
}
