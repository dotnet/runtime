// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Policy;

namespace System.Security
{
    static internal class SecurityManager
    {
        // Get a sandbox permission set that the CLR considers safe to grant an application with the given
        // evidence.  Note that this API is not a policy API, but rather a host helper API so that a host can
        // determine if an application's requested permission set is reasonable.  This is esentially just a
        // hard coded mapping of Zone -> Sandbox and is not configurable in any way.
        public static PermissionSet GetStandardSandbox(Evidence evidence)
        {
            return null;
        }
    }
}
