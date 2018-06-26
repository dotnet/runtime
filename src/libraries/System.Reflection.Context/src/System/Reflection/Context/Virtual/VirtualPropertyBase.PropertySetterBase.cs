// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Context.Virtual
{
    partial class VirtualPropertyBase
    {
        protected abstract class PropertySetterBase : FuncPropertyAccessorBase
        {
            private Type[] _parameterTypes;

            protected PropertySetterBase(VirtualPropertyBase property)
                : base(property)
            {
            }

            public override sealed string Name
            {
                get { return "set_" + DeclaringProperty.Name; }
            }

            public override sealed Type ReturnType
            {
                get { return DeclaringProperty.ReflectionContext.MapType(IntrospectionExtensions.GetTypeInfo(typeof(void))); }
            }

            protected override Type[] GetParameterTypes()
            {
                return (_parameterTypes != null) ?
                       _parameterTypes :
                       _parameterTypes = new Type[1] { DeclaringProperty.PropertyType };
            }
        }
    }
}
