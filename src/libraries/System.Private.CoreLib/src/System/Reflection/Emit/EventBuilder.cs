// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public abstract class EventBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EventBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is invoked by derived classes.
        /// </remarks>
        protected EventBuilder()
        {
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
            => AddOtherMethodCore(mdBuilder);

        /// <summary>
        /// When overridden in a derived class, adds one of the "other" methods associated with this event.
        /// </summary>
        /// <param name="mdBuilder">A <see cref="MethodBuilder"/> object that represents the other method.</param>
        /// <remarks>
        /// "Other" methods are methods other than the "on" and "raise" methods associated with an event.
        /// This function can be called many times to add as many "other" methods.
        /// </remarks>
        protected abstract void AddOtherMethodCore(MethodBuilder mdBuilder);

        public void SetAddOnMethod(MethodBuilder mdBuilder)
            => SetAddOnMethodCore(mdBuilder);

        /// <summary>
        /// When overridden in a derived class, sets the method used to subscribe to this event.
        /// </summary>
        /// <param name="mdBuilder">A <see cref="MethodBuilder"/> object that represents the method used to subscribe to this event.</param>
        protected abstract void SetAddOnMethodCore(MethodBuilder mdBuilder);

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

        public void SetRaiseMethod(MethodBuilder mdBuilder)
            => SetRaiseMethodCore(mdBuilder);

        /// <summary>
        /// When overridden in a derived class, sets the method used to raise this event.
        /// </summary>
        /// <param name="mdBuilder">A <see cref="MethodBuilder"/> object that represents the method used to raise this event.</param>
        protected abstract void SetRaiseMethodCore(MethodBuilder mdBuilder);

        public void SetRemoveOnMethod(MethodBuilder mdBuilder)
            => SetRemoveOnMethodCore(mdBuilder);

        /// <summary>
        /// When overridden in a derived class, sets the method used to unsubscribe to this event.
        /// </summary>
        /// <param name="mdBuilder">A <see cref="MethodBuilder"/> object that represents the method used to unsubscribe to this event.</param>
        protected abstract void SetRemoveOnMethodCore(MethodBuilder mdBuilder);
    }
}
