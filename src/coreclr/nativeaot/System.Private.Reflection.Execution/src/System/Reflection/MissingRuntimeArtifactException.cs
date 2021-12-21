// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
  Type:  MissingRuntimeArtifactException
**
==============================================================*/

using global::System;

namespace System.Reflection
{
    internal sealed class MissingRuntimeArtifactException : MemberAccessException
    {
        public MissingRuntimeArtifactException()
        {
        }

        public MissingRuntimeArtifactException(string message)
            : base(message)
        {
        }
    }
}
