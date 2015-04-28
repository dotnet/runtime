// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.IO
{
    public sealed class InvalidDataException : Exception
    {
        public InvalidDataException()
            : base(SR.GenericInvalidData)
        {
        }

        public InvalidDataException(string message)
            : base(message)
        {
        }

        public InvalidDataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
