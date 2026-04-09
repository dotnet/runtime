// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    public abstract class TypeDescriptionProviderService
    {
        public abstract TypeDescriptionProvider GetProvider(object instance);
        public abstract TypeDescriptionProvider GetProvider(Type type);
    }
}
