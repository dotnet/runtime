// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Resources
{
    [System.Runtime.InteropServices.ComVisible(true)]
    public class MissingManifestResourceException : Exception
    {
        public MissingManifestResourceException()
            : base(SR.Arg_MissingManifestResourceException)
        {
            HResult = HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }

        public MissingManifestResourceException(String message)
            : base(message)
        {
            HResult = HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }

        public MissingManifestResourceException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }
    }
}
