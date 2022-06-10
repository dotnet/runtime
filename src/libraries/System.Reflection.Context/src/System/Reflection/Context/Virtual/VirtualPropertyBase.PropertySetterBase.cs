// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Context.Virtual
{
    internal partial class VirtualPropertyBase
    {
        protected abstract class PropertySetterBase : FuncPropertyAccessorBase
        {
            private Type[]? _parameterTypes;

            protected PropertySetterBase(VirtualPropertyBase property)
                : base(property)
            {
            }

            public sealed override string Name
            {
                get { return "set_" + DeclaringProperty.Name; }
            }

            public sealed override Type ReturnType
            {
                get { return DeclaringProperty.ReflectionContext.MapType(IntrospectionExtensions.GetTypeInfo(typeof(void))); }
            }

            protected override Type[] GetParameterTypes()
            {
                return _parameterTypes ??= new Type[1] { DeclaringProperty.PropertyType };
            }
        }
    }
}
