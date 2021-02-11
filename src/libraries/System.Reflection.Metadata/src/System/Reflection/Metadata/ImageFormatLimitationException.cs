// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public partial class ImageFormatLimitationException : Exception
    {
        public ImageFormatLimitationException()
            : base()
        {
        }

        public ImageFormatLimitationException(string? message)
            : base(message)
        {
        }

        public ImageFormatLimitationException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
