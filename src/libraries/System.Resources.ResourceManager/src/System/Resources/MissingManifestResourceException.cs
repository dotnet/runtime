// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Resources
{
    [Serializable]
    public class MissingManifestResourceException : SystemException
    {
        public MissingManifestResourceException()
            : base(SR.Arg_MissingManifestResourceException)
        {
            HResult = HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }

        public MissingManifestResourceException(string message)
            : base(message)
        {
            HResult = HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }

        public MissingManifestResourceException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }

        protected MissingManifestResourceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
