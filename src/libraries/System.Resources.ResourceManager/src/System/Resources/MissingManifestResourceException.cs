// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Resources
{
    public class MissingManifestResourceException : Exception
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
    }
}
