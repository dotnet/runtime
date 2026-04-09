// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    public class LocalVariableInfo
    {
        public virtual Type LocalType { get { Debug.Fail("type must be set!"); return null!; } }
        public virtual int LocalIndex => 0;
        public virtual bool IsPinned => false;
        protected LocalVariableInfo() { }
        public override string ToString() => IsPinned ?
            $"{LocalType} ({LocalIndex}) (pinned)" :
            $"{LocalType} ({LocalIndex})";
    }
}
