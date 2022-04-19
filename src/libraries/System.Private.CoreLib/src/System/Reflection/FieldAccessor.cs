// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal sealed partial class FieldAccessor
    {
        private readonly RtFieldInfo _fieldInfo;
        public InvocationFlags _invocationFlags;

        public FieldAccessor(RtFieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public object? GetValue(object? obj)
        {
            // Todo: add strategy for calling IL Emit-based version
            return _fieldInfo.GetValueNonEmit(obj);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public void SetValue(object? obj, object? value)
        {
            // Todo: add strategy for calling IL Emit-based version
            _fieldInfo.SetValueNonEmit(obj, value);
        }
    }
}
