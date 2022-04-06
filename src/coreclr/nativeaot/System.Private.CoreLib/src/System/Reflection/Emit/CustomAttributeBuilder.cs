// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public class CustomAttributeBuilder
    {
        public CustomAttributeBuilder(ConstructorInfo con, object[] constructorArgs)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
        }

        public CustomAttributeBuilder(ConstructorInfo con, object[] constructorArgs, FieldInfo[] namedFields, object[] fieldValues)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
        }

        public CustomAttributeBuilder(ConstructorInfo con, object[] constructorArgs, PropertyInfo[] namedProperties, object[] propertyValues)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
        }

        public CustomAttributeBuilder(ConstructorInfo con, object[] constructorArgs, PropertyInfo[] namedProperties, object[] propertyValues, FieldInfo[] namedFields, object[] fieldValues)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
        }
    }
}
