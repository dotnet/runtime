// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.DataAnnotations.Schema
{
    /// <summary>
    ///     Denotes that the class is a complex type.
    ///     Complex types are non-scalar properties of entity types that enable scalar properties to be organized within
    ///     entities.
    ///     Complex types do not have keys and cannot be managed by the Entity Framework apart from the parent object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ComplexTypeAttribute : Attribute
    {
    }
}
