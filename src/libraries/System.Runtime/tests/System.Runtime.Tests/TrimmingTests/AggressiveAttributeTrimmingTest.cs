// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Reflection;

/// <summary>
/// Ensures setting _AggressiveAttributeTrimming = true causes various attributes to be trimmed
/// </summary>
class Program
{
    [UnconditionalSuppressMessage ("ReflectionAnalysis", "IL2111", Justification = "Expected trim warning for reflection over annotated members.")]
    [UnconditionalSuppressMessage ("ReflectionAnalysis", "IL2026", Justification = "Expected trim warning for reflection over annotated members.")]
    static int Main(string[] args)
    {
        // Reference to IsDynamicCodeSupported (which has FeatureGuard(typeof(RequiresDynamicCodeAttribute)))
        // should not produce a warning because both RequiresDynamicCodeAttribute and FeatureGuardAttribute are removed.
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            UseDynamicCode();
        }

        // Check that a few attribute instances are indeed removed
        CheckRemovedAttributes(typeof(MembersWithRemovedAttributes));

        return 100;
    }

    [RequiresDynamicCode(nameof(UseDynamicCode))]
    static void UseDynamicCode() { }

    class MembersWithRemovedAttributes
    {
        static void DynamicallyAccessedMembers([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) { }

        [FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
        static bool FeatureGuard => throw null!;

        [FeatureSwitchDefinition("Program.MembersWithRemovedAttributes.FeatureSwitchDefinition")]
        static bool FeatureSwitchDefinition => throw null!;

        [RequiresDynamicCode(nameof(RequiresDynamicCode))]
        static void RequiresDynamicCode() { }

        [RequiresUnreferencedCode(nameof(RequiresUnreferencedCode))]
        static void RequiresUnreferencedCode() { }
    }

    static void CheckRemovedAttributes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        Console.WriteLine($"Validating {type}");
        foreach (var member in type.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            CheckRemovedAttributes(member);
            
            if (member is MethodInfo method)
            {
                foreach (var parameter in method.GetParameters())
                {
                    CheckRemovedAttributes(parameter);
                }
            }
        }
    }

    static void CheckRemovedAttributes(ICustomAttributeProvider provider)
    {
        foreach (var attribute in provider.GetCustomAttributes(false))
        {
            if (attribute is NullableContextAttribute)
                continue;

            throw new Exception($"Unexpected attribute {attribute.GetType()} on {provider}");
        }
    }
}
