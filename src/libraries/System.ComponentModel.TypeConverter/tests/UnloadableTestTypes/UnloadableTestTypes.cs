// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace UnloadableTestTypes
{
    [SimpleType]
    public class SimpleType
    {
        public string P1 { get; set; }
        public int P2 { get; set; }
        public event Action ActionEvent;
        public void OnActionEvent()
        {
            ActionEvent?.Invoke();
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class SimpleTypeAttribute : Attribute { }

    public sealed class SimpleTypeDescriptionProvider : TypeDescriptionProvider
    {
        public override bool IsSupportedType(Type type) => type.AssemblyQualifiedName == typeof(SimpleType).AssemblyQualifiedName;

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance) => new SimpleTypeDescriptor();

        public sealed class SimpleTypeDescriptor : CustomTypeDescriptor { }
    }
}
