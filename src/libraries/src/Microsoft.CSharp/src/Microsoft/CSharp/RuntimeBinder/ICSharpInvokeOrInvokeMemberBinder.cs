// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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