// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CdacUsageGraph.Model;

/// <summary>
/// One typed descriptor field dependency declared on an <c>IData&lt;T&gt;</c> member.
/// </summary>
internal sealed record DataDescriptorFieldDependency(
    string FieldName,
    string NativeType,
    string? TypeName = null);

/// <summary>
/// Descriptor dependencies declared on an <c>IData&lt;T&gt;</c> property or method.
/// </summary>
internal sealed record DataDescriptorDependencies(
    IReadOnlyList<DataDescriptorFieldDependency> Fields,
    IReadOnlyList<string?> TypeSizeTypeNames);
