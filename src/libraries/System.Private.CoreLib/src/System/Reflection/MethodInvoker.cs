// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed partial class MethodInvoker
    {
        internal InvocationFlags _invocationFlags;
        private readonly RuntimeMethodInfo _methodInfo;
        private readonly bool _hasRefs;

        public MethodInvoker(RuntimeMethodInfo methodInfo)
        {
            _methodInfo = methodInfo;

            RuntimeType[] sigTypes = methodInfo.Signature.Arguments;
            for (int i = 0; i < sigTypes.Length; i++)
            {
                if (sigTypes[i].IsByRef)
                {
                    _hasRefs = true;
                    break;
                }
            }
        }

        public bool HasRefs => _hasRefs;
    }
}
