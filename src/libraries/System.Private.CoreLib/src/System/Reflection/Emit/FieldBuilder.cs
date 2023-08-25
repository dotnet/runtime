// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public abstract class FieldBuilder : FieldInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FieldBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is invoked by derived classes.
        /// </remarks>
        protected FieldBuilder()
        {
        }

        public void SetConstant(object? defaultValue)
            => SetConstantCore(defaultValue);

        /// <summary>
        /// When overridden in a derived class, sets the default value of this field.
        /// </summary>
        /// <param name="defaultValue">The new default value for this field.</param>
        protected abstract void SetConstantCore(object? defaultValue);

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            SetCustomAttributeCore(con, binaryAttribute);
        }

        /// <summary>
        /// When overridden in a derived class, sets a custom attribute on this assembly.
        /// </summary>
        /// <param name="con">The constructor for the custom attribute.</param>
        /// <param name="binaryAttribute">A <see cref="ReadOnlySpan{T}"/> of bytes representing the attribute.</param>
        protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
        }

        public void SetOffset(int iOffset)
            => SetOffsetCore(iOffset);

        /// <summary>
        /// When overridden in a derived class, specifies the field layout.
        /// </summary>
        /// <param name="iOffset">The offset of the field within the type containing this field.</param>
        protected abstract void SetOffsetCore(int iOffset);
    }
}
