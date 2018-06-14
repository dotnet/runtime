// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Context.Virtual
{
    partial class VirtualPropertyBase
    {
        protected abstract class FuncPropertyAccessorBase : VirtualMethodBase
        {
            protected FuncPropertyAccessorBase(VirtualPropertyBase declaringProperty)
            {
                Debug.Assert(null != declaringProperty);

                DeclaringProperty = declaringProperty;
            }

            public CustomReflectionContext ReflectionContext
            {
                get { return DeclaringProperty.ReflectionContext; }
            }

            public override sealed MethodAttributes Attributes
            {
                get { return base.Attributes | MethodAttributes.SpecialName; }
            }

            public override sealed Type DeclaringType
            {
                get { return DeclaringProperty.DeclaringType; }
            }

            public VirtualPropertyBase DeclaringProperty { get; }
        }
    }
}
