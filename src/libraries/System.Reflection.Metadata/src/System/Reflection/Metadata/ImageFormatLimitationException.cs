// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection.Metadata.Ecma335;

#if SRM
namespace System.Reflection.Metadata
#else
namespace Roslyn.Reflection.Metadata
#endif
{
#if SRM
    public
#endif
    class ImageFormatLimitationException : Exception
    {
        public ImageFormatLimitationException()
            : base()
        {
        }

        public ImageFormatLimitationException(string message)
            : base(message)
        {
        }

        public ImageFormatLimitationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal static void ThrowHeapSizeLimitExceeded(HeapIndex heapIndex)
        {
            // TODO: localize
            throw new ImageFormatLimitationException(string.Format(SR.HeapSizeLimitExceeded, heapIndex));
        }
    }
}
