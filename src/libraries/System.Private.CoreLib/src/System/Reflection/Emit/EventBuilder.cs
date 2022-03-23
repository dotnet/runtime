// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public class EventBuilder
    {
        protected EventBuilder()
        {
        }

        public virtual void AddOtherMethod(MethodBuilder mdBuilder)
            => AddOtherMethod(mdBuilder);

        public virtual void SetAddOnMethod(MethodBuilder mdBuilder)
            => SetAddOnMethod(mdBuilder);

        public virtual void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
            => SetCustomAttribute(con, binaryAttribute);

        public virtual void SetCustomAttribute(CustomAttributeBuilder customBuilder)
            => SetCustomAttribute(customBuilder);

        public virtual void SetRaiseMethod(MethodBuilder mdBuilder)
            => SetRaiseMethod(mdBuilder);

        public virtual void SetRemoveOnMethod(MethodBuilder mdBuilder)
            => SetRemoveOnMethod(mdBuilder);
    }
}
