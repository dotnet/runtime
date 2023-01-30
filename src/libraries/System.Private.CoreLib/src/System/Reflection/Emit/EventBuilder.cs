// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public abstract class EventBuilder
    {
        protected EventBuilder()
        {
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
            => AddOtherMethodCore(mdBuilder);

        protected abstract void AddOtherMethodCore(MethodBuilder mdBuilder);

        public void SetAddOnMethod(MethodBuilder mdBuilder)
            => SetAddOnMethodCore(mdBuilder);

        protected abstract void SetAddOnMethodCore(MethodBuilder mdBuilder);

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

        public void SetRaiseMethod(MethodBuilder mdBuilder)
            => SetRaiseMethodCore(mdBuilder);

        protected abstract void SetRaiseMethodCore(MethodBuilder mdBuilder);

        public void SetRemoveOnMethod(MethodBuilder mdBuilder)
            => SetRemoveOnMethodCore(mdBuilder);

        protected abstract void SetRemoveOnMethodCore(MethodBuilder mdBuilder);
    }
}
