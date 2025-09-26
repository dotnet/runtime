// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices;

/// <summary>
/// This attribute is emitted by Roslyn when a type is deleted during a
/// hot reload session. The intent is to use it as a filter in places we cannot
/// delete the type outright.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class MetadataUpdateDeletedAttribute : Attribute;
