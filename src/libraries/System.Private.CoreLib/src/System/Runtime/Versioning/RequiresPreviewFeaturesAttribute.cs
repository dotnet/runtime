// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Versioning
{
    [AttributeUsage(AttributeTargets.Assembly |
                AttributeTargets.Module |
                AttributeTargets.Class |
                AttributeTargets.Interface |
                AttributeTargets.Delegate |
                AttributeTargets.Struct |
                AttributeTargets.Enum |
                AttributeTargets.Constructor |
                AttributeTargets.Method |
                AttributeTargets.Property |
                AttributeTargets.Field |
                AttributeTargets.Event, Inherited = false)]
    public sealed class RequiresPreviewFeaturesAttribute : Attribute
    {
        public RequiresPreviewFeaturesAttribute() { }
    }
}
