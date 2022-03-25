// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public abstract class PropertyBuilder : PropertyInfo
    {
        protected PropertyBuilder()
        {
        }

        public virtual void AddOtherMethod(MethodBuilder mdBuilder)
            => AddOtherMethod(mdBuilder);

        public virtual void SetConstant(object defaultValue)
            => SetConstant(defaultValue);

        public virtual void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
            => SetCustomAttribute(con, binaryAttribute);

        public virtual void SetCustomAttribute(CustomAttributeBuilder customBuilder)
            => SetCustomAttribute(customBuilder);

        public virtual void SetGetMethod(MethodBuilder mdBuilder)
            => SetGetMethod(mdBuilder);

        public virtual void SetSetMethod(MethodBuilder mdBuilder)
            => SetSetMethod(mdBuilder);
    }
}
