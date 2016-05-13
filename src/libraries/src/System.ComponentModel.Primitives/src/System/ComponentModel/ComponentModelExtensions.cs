// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    public interface IIsDefaultAttribute
    {
        bool IsDefaultAttribute();
    }

    public interface ITypeId
    {
        object TypeId { get; }
    }

    /// <summary>
    /// These extension methods are used to mimic behavior from the .NET Framework where System.Attribute
    /// had extra methods. For compatibility reasons, they are implemented with interfaces on the attributes
    /// that need them, and then exposed via extension methods off of System.Attribute. The only difference
    /// is that Attribute.TypeId is now accessed as Attribute.GetTypeId() as extension properties don't exist.
    /// </summary>
    public static class ComponentModelExtensions
    {
        public static bool IsDefaultAttribute(this Attribute attribute)
        {
            return (attribute as IIsDefaultAttribute)?.IsDefaultAttribute() ?? false;
        }

        public static object GetTypeId(this Attribute attribute)
        {
            return (attribute as ITypeId)?.TypeId ?? attribute.GetType();
        }

        public static bool Match(this Attribute attribute, object obj)
        {
            return attribute.Equals(obj);
        }
    }
}
