// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    public interface IIsDefaultAttribute
    {
        bool IsDefaultAttribute();
    }

    public static class ComponentModelExtensions
    {
        public static bool IsDefaultAttribute(this Attribute attribute)
        {
            return (attribute as IIsDefaultAttribute)?.IsDefaultAttribute() ?? false;
        }

        public static object GetTypeId(this Attribute attribute)
        {
            return attribute.GetType();
        }

        public static bool Match(this Attribute attribute, object obj)
        {
            return attribute.Equals(obj);
        }
    }
}
