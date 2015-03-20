// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Xml.Serialization.LegacyNetCF
{
    internal class XmlNode
    {
    }
}

namespace System.Security
{
    [AttributeUsage(AttributeTargets.Delegate |
        AttributeTargets.Interface |
        AttributeTargets.Event |
        AttributeTargets.Property |
        AttributeTargets.Method |
        AttributeTargets.Constructor |
        AttributeTargets.Struct |
        AttributeTargets.Enum |
        AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class FrameworkVisibilityCompactFrameworkInternalAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Delegate |
        AttributeTargets.Interface |
        AttributeTargets.Event |
        AttributeTargets.Property |
        AttributeTargets.Method |
        AttributeTargets.Constructor |
        AttributeTargets.Struct |
        AttributeTargets.Enum |
        AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class FrameworkVisibilitySilverlightInternalAttribute : Attribute
    {
    }
}
