// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices;

/// <summary>
/// This attribute is emitted by the compiler when a metadata entity is deleted during a
/// Hot Reload session.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class MetadataUpdateDeletedAttribute : Attribute;
