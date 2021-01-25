// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal sealed class RuntimeLocalVariableInfo : LocalVariableInfo
    {
        private RuntimeType? _type;
        private int _localIndex;
        private bool _isPinned;

        private RuntimeLocalVariableInfo() { }

        public override Type LocalType { get { Debug.Assert(_type != null, "type must be set!"); return _type; } }
        public override int LocalIndex => _localIndex;
        public override bool IsPinned => _isPinned;
    }
}
