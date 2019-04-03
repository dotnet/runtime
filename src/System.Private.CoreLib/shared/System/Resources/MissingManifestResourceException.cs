// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System.Resources
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MissingManifestResourceException : SystemException
    {
        public MissingManifestResourceException()
            : base(SR.Arg_MissingManifestResourceException)
        {
            HResult = System.HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }

        public MissingManifestResourceException(string? message)
            : base(message)
        {
            HResult = System.HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }

        public MissingManifestResourceException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = System.HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }

        protected MissingManifestResourceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
