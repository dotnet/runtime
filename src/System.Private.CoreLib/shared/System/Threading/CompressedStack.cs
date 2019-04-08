// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System.Threading
{
    public sealed class CompressedStack : ISerializable
    {
        private CompressedStack()
        {
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public static CompressedStack Capture()
        {
            return GetCompressedStack();
        }

        public CompressedStack CreateCopy()
        {
            return this;
        }

        public static CompressedStack GetCompressedStack()
        {
            return new CompressedStack();
        }

        public static void Run(CompressedStack compressedStack, ContextCallback callback, object? state)
        {
            if (compressedStack == null)
            {
                throw new ArgumentNullException(nameof(compressedStack));
            }

            // The original code was not checking for a null callback and would throw NullReferenceException
            callback(state);
        }
    }
}
