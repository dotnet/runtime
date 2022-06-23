// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Context.Virtual
{
    internal abstract partial class VirtualPropertyBase
    {
        protected abstract class PropertyGetterBase : FuncPropertyAccessorBase
        {
            protected PropertyGetterBase(VirtualPropertyBase property)
                : base(property)
            {
            }

            public override sealed string Name
            {
                get { return "get_" + DeclaringProperty.Name; }
            }

            public override sealed Type ReturnType
            {
                get { return DeclaringProperty.PropertyType; }
            }

            protected override Type[] GetParameterTypes()
            {
                return CollectionServices.Empty<Type>();
            }
        }
    }
}
