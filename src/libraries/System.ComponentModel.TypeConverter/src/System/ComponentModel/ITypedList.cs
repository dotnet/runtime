// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    public interface ITypedList
    {
        string GetListName(PropertyDescriptor[] listAccessors);

        [RequiresUnreferencedCode("Members of property types might be trimmed if not referenced directly")]
        PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors);
    }
}
