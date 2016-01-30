// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
