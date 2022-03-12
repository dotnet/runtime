// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed partial class ConstructorInvoker
    {
        private readonly bool _hasRefs;
        private readonly RuntimeConstructorInfo _constructorInfo;
        public InvocationFlags _invocationFlags;

        public ConstructorInvoker(RuntimeConstructorInfo constructorInfo)
        {
            _constructorInfo = constructorInfo;

            RuntimeType[] argTypes = constructorInfo.ArgumentTypes;
            for (int i = 0; i < argTypes.Length; i++)
            {
                if (argTypes[i].IsByRef)
                {
                    _hasRefs = true;
                    break;
                }
            }
        }

        public bool HasRefs => _hasRefs;
    }
}
