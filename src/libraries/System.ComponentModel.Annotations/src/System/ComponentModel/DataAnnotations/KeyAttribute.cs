// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Used to mark one or more entity properties that provide the entity's unique identity
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class KeyAttribute : Attribute
    {
    }
}
