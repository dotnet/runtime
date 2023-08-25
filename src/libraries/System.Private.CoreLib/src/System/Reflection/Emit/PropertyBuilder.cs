// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public abstract class PropertyBuilder : PropertyInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PropertyBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is invoked by derived classes.
        /// </remarks>
        protected PropertyBuilder()
        {
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
            => AddOtherMethodCore(mdBuilder);

        /// <summary>
        /// When overridden in a derived class, adds one of the other methods associated with this property.
        /// </summary>
        /// <param name="mdBuilder">A <see cref="MethodBuilder"/> object that represents the other method.</param>
        protected abstract void AddOtherMethodCore(MethodBuilder mdBuilder);

        public void SetConstant(object? defaultValue)
            => SetConstantCore(defaultValue);

        /// <summary>
        /// When overridden in a derived class, sets the default value of this property.
        /// </summary>
        /// <param name="defaultValue">The default value of this property.</param>
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

        public void SetGetMethod(MethodBuilder mdBuilder)
            => SetGetMethodCore(mdBuilder);

        /// <summary>
        /// When overridden in a derived class, sets the method that gets the property value.
        /// </summary>
        /// <param name="mdBuilder">A <see cref="MethodBuilder"/> object that represents the method that gets the property value.</param>
        protected abstract void SetGetMethodCore(MethodBuilder mdBuilder);

        public void SetSetMethod(MethodBuilder mdBuilder)
            => SetSetMethodCore(mdBuilder);

        /// <summary>
        /// When overridden in a derived class, sets the method that sets the property value.
        /// </summary>
        /// <param name="mdBuilder">A <see cref="MethodBuilder"/> object that represents the method that sets the property value.</param>
        protected abstract void SetSetMethodCore(MethodBuilder mdBuilder);
    }
}
