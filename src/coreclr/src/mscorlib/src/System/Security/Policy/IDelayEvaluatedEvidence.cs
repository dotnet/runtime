// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace System.Security.Policy {
    /// <summary>
    ///     Interface for evidence objects that support being "unverified".  For instance, StrongName
    ///     evidence for a strong name signature which was not yet verified.  This interface is used to
    ///     keep track of weather or not the evidence object was needed to compute a grant set.  If it was,
    ///     then we can force verificaiton of the evidence object -- if not we can save time by not doing
    ///     any verification on it.  (Since we didn't use it for policy resolution, it wouldn't have
    ///     mattered if the evidence was not present in the first place).
    /// </summary>
    internal interface IDelayEvaluatedEvidence {
        /// <summary>
        ///     Is this evidence object verified yet?
        /// </summary>
        bool IsVerified 
        { 
            [System.Security.SecurityCritical]
            get; 
        }

        /// <summary>
        ///     Was this evidence object used during the course of policy evaluation?
        /// </summary>
        bool WasUsed { get; }

        /// <summary>
        ///     Mark the object as used
        /// </summary>
        void MarkUsed();
    }
}