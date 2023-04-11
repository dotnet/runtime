// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public abstract class FieldBuilder : FieldInfo
    {
        protected FieldBuilder()
        {
        }

        public void SetConstant(object? defaultValue)
            => SetConstantCore(defaultValue);

        protected abstract void SetConstantCore(object? defaultValue);

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            SetCustomAttributeCore(con, binaryAttribute);
        }

        protected abstract void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            SetCustomAttributeCore(customBuilder);
        }

        protected abstract void SetCustomAttributeCore(CustomAttributeBuilder customBuilder);

        public void SetOffset(int iOffset)
            => SetOffsetCore(iOffset);

        protected abstract void SetOffsetCore(int iOffset);
    }
}
