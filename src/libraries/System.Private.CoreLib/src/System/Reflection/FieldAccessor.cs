// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
