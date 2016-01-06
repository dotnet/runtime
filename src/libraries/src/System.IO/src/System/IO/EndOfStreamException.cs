// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    public class EndOfStreamException : IOException
    {
        public EndOfStreamException()
            : base(SR.Arg_EndOfStreamException)
        {
            HResult = HResults.COR_E_ENDOFSTREAM;
        }

        public EndOfStreamException(string message)
            : base(message)
        {
            HResult = HResults.COR_E_ENDOFSTREAM;
        }

        public EndOfStreamException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_ENDOFSTREAM;
        }
    }
}
