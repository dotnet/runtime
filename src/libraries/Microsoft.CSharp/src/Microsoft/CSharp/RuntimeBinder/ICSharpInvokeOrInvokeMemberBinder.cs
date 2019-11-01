// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal interface ICSharpInvokeOrInvokeMemberBinder : ICSharpBinder
    {
        // Helper methods.
        bool StaticCall { get; }
        bool ResultDiscarded { get; }

        // Members.
        CSharpCallFlags Flags { get; }
        Type[] TypeArguments { get; }
    }
}
