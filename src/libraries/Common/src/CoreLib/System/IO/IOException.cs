// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace System.IO
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class IOException : SystemException
    {
        public IOException()
            : base(SR.Arg_IOException)
        {
            HResult = HResults.COR_E_IO;
        }

        public IOException(String message)
            : base(message)
        {
            HResult = HResults.COR_E_IO;
        }

        public IOException(String message, int hresult)
            : base(message)
        {
            HResult = hresult;
        }

        public IOException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_IO;
        }

        protected IOException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
