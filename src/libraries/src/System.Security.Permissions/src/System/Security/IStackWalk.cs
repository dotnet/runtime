// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public partial interface IStackWalk
    {
        void Assert();
        void Demand();
        void Deny();
        void PermitOnly();
    }
}
