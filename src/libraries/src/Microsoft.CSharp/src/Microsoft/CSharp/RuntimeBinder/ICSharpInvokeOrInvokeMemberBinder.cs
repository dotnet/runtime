// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal interface ICSharpInvokeOrInvokeMemberBinder
    {
        // Helper methods.
        bool StaticCall { get; }
        bool ResultDiscarded { get; }

        // Members.
        Type CallingContext { get; }
        CSharpCallFlags Flags { get; }
        string Name { get; }
        IList<Type> TypeArguments { get; }
        IList<CSharpArgumentInfo> ArgumentInfo { get; }
    }
}
