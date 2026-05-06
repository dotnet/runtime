// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.TypeLoading
{
    internal sealed partial class RoLocalVariableInfo : LocalVariableInfo
    {
        private readonly int _localIndex;
        private readonly bool _isPinned;
        private readonly Type _localType;

        internal RoLocalVariableInfo(int localIndex, bool isPinned, Type localType)
        {
            _localIndex = localIndex;
            _isPinned = isPinned;
            _localType = localType;
        }

        public sealed override int LocalIndex => _localIndex;
        public sealed override bool IsPinned => _isPinned;
        public sealed override Type LocalType => _localType;
    }
}
