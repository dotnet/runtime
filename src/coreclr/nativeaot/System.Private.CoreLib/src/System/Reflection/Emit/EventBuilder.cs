// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public sealed class EventBuilder
    {
        internal EventBuilder()
        {
            // Prevent generating a default constructor
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
        {
        }

        public void SetAddOnMethod(MethodBuilder mdBuilder)
        {
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
        }

        public void SetRaiseMethod(MethodBuilder mdBuilder)
        {
        }

        public void SetRemoveOnMethod(MethodBuilder mdBuilder)
        {
        }
    }
}
