// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal abstract class FileStreamBase : Stream
    {
        public abstract bool IsAsync { get; }
        public abstract string Name { get; }
        public abstract SafeFileHandle SafeFileHandle { get; }

        public abstract void Flush(bool flushToDisk);
    }
}
