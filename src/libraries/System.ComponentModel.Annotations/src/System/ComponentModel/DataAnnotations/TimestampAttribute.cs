// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     This attribute is used to mark a Timestamp member of a Type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TimestampAttribute : Attribute
    {
    }
}
