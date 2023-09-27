// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Numerics.Hashing;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceGenerators;

/// <summary>
/// Descriptor for diagnostic instances using structural equality comparison.
/// Provides a work-around for https://github.com/dotnet/roslyn/issues/68291.
/// </summary>
internal readonly struct DiagnosticInfo : IEquatable<DiagnosticInfo>
{
    private static readonly Lazy<CSharpSyntaxTree?> s_dummySyntaxTree = new Lazy<CSharpSyntaxTree?>(GetDummySyntaxTree);
    private static readonly Lazy<FieldInfo?> s_sourceTreeBackingFieldInfo = new Lazy<FieldInfo?>(GetSourceTreeBackingFieldInfo);

    public DiagnosticDescriptor Descriptor { get; private init; }
    public object?[] MessageArgs { get; private init; }
    public Location? Location { get; private init; }

    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, object?[]? messageArgs)
    {
        Location? trimmedLocation = location is null ? null : GetTrimmedLocation(location);

        return new DiagnosticInfo
        {
            Descriptor = descriptor,
            Location = trimmedLocation,
            MessageArgs = messageArgs ?? Array.Empty<object?>()
        };

        // Creates a copy of the Location instance that does not capture a reference to Compilation.
        static Location? GetTrimmedLocation(Location? sourceLocation)
        {
            if (sourceLocation is null)
            {
                return null;
            }

            Location trimmedLocation = Location.Create(sourceLocation.SourceTree?.FilePath ?? "", sourceLocation.SourceSpan, sourceLocation.GetLineSpan().Span);

            if (sourceLocation.IsInSource &&
                !trimmedLocation.IsInSource &&
                s_sourceTreeBackingFieldInfo.Value is FieldInfo sourceTreeField &&
                s_dummySyntaxTree.Value is CSharpSyntaxTree syntaxTree)
            {
                // Attempt to mark this as a source location, so that it is suppressible with #pragma.
                try
                {
                    sourceTreeField.SetValue(trimmedLocation, syntaxTree);

                    if (!trimmedLocation.IsInSource)
                    {
                        sourceTreeField.SetValue(trimmedLocation, null);
                    }
                }
                catch { }
            }

            return trimmedLocation;
        }
    }

    public Diagnostic CreateDiagnostic()
        => Diagnostic.Create(Descriptor, Location, MessageArgs);

    public override readonly bool Equals(object? obj) => obj is DiagnosticInfo info && Equals(info);

    public readonly bool Equals(DiagnosticInfo other)
    {
        return Descriptor.Equals(other.Descriptor) &&
            MessageArgs.SequenceEqual(other.MessageArgs) &&
            Location == other.Location;
    }

    public override readonly int GetHashCode()
    {
        int hashCode = Descriptor.GetHashCode();
        foreach (object? messageArg in MessageArgs)
        {
            hashCode = HashHelpers.Combine(hashCode, messageArg?.GetHashCode() ?? 0);
        }

        hashCode = HashHelpers.Combine(hashCode, Location?.GetHashCode() ?? 0);
        return hashCode;
    }

    private static FieldInfo? GetSourceTreeBackingFieldInfo()
    {
        FieldInfo? info;

        try
        {
            FieldInfo[] fields = typeof(Location).GetFields(BindingFlags.NonPublic);
            info = typeof(Location).GetField("<SourceTree>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        }
        catch
        {
            info = null;
        }

        return info;
    }

    private static CSharpSyntaxTree? GetDummySyntaxTree()
    {
        try
        {
            Type? dummySyntaxTree = typeof(CSharpSyntaxTree).Assembly.GetType("Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.DummySyntaxTree", throwOnError: false);
            return dummySyntaxTree is null ? null : (CSharpSyntaxTree?)Activator.CreateInstance(dummySyntaxTree);
        }
        catch
        {
            return null;
        }
    }
}
