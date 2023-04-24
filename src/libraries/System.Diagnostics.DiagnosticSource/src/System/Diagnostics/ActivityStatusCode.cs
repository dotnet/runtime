// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Define the status code of the Activity which indicate the status of the instrumented operation.
    /// </summary>
    public enum ActivityStatusCode
    {
        /// <summary>
        /// Unset status code is the default value indicating the status code is not initialized.
        /// </summary>
        Unset = 0,

        /// <summary>
        /// Status code indicating the operation has been validated and completed successfully.
        /// </summary>
        Ok = 1,

        /// <summary>
        /// Status code indicating an error is encountered during the operation.
        /// </summary>
        Error = 2
    }
}
