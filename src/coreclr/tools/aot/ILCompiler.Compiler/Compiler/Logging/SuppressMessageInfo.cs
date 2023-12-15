// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.Logging
{
    public struct SuppressMessageInfo
    {
        public int Id;
        public string Scope;
        public string Target;
        public string MessageId;
    }
}
