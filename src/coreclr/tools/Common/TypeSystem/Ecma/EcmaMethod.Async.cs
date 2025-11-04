// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace Internal.TypeSystem.Ecma
{
    public sealed partial class EcmaMethod
    {
        public override AsyncMethodKind AsyncMethodKind
        {
            get
            {
                // EcmaMethod always represents the Task-returning variant for Task-returning methods
                // in order to match the signature, even though the IL in metadata may not match.
                // Other MethodDescs represent the async variant.
                bool returnsTask = Signature.ReturnsTaskOrValueTask();
                if (returnsTask)
                {
                    return IsAsync
                        ? AsyncMethodKind.RuntimeAsync
                        : AsyncMethodKind.TaskReturning;
                }
                else
                {
                    return IsAsync
                        ? AsyncMethodKind.AsyncExplicitImpl
                        : AsyncMethodKind.NotAsync;
                }
            }
        }
    }
}
