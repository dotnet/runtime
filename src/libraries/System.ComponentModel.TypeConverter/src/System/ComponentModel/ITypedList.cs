// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel
{
    public interface ITypedList
    {
        string GetListName(PropertyDescriptor[] listAccessors);

        PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors);
    }
}
