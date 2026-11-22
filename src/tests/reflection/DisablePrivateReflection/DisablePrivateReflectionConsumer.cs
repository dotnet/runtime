// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

// This assembly is not annotated with DisablePrivateReflectionAttribute, but it can see the private types of the library
// (InternalsVisibleTo). The attributes of its types that mention these types are available, and surface them as private types.
[GenericMarker<PrivateClass>]
[GenericMarker<PrivateStruct>]
[GenericMarker<List<PrivateClass>>]
[GenericMarker<PrivateClass[]>]
[TypeMarker(typeof(PrivateClass))]
[TypeMarker(typeof(PrivateStruct))]
[TypeMarker(typeof(List<PrivateClass>))]
[TypeMarker(typeof(PrivateClass[]))]
public class ConsumerWithPrivateArguments;

[GenericMarker<PrivateWithGenericNested.Box<int>>]
[GenericMarker<PrivateWithGenericNested.Box<string>>]
[GenericMarker<List<PrivateWithGenericNested.Box<int>>>]
[GenericMarker<PrivateWithGenericNested.Box<PrivateClass>[]>]
[TypeMarker(typeof(PrivateWithGenericNested.Box<int>))]
[TypeMarker(typeof(PrivateWithGenericNested.Box<string>))]
[TypeMarker(typeof(List<PrivateWithGenericNested.Box<int>>))]
[TypeMarker(typeof(PrivateWithGenericNested.Box<PrivateClass>[]))]
public class ConsumerWithPrivateGenericArguments;

// The attribute class is a type that is not available to reflection (it is nested in an internal type), so the attribute is not available either
[PrivateContainer.NestedMarker]
public class ConsumerWithHiddenAttribute
{
    [PrivateContainer.NestedMarker]
    [Marker]
    public void Method() { }
}

public class ConsumerDerived : PublicMembers;

public static class DisablePrivateReflectionConsumer
{
    // UnsafeAccessor does not use reflection: it reaches the members that are not available to it, also the ones of types that are
    // not available, whose names can be given as strings.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Secret")]
    private static extern int Secret([UnsafeAccessorType("PrivateWithSecrets, DisablePrivateReflectionLibrary")] object instance);

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    [return: UnsafeAccessorType("PrivateWithSecrets, DisablePrivateReflectionLibrary")]
    private static extern object CreateSecret(int secret);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "StaticSecret")]
    private static extern int StaticSecret([UnsafeAccessorType("PrivateWithSecrets, DisablePrivateReflectionLibrary")] object instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_secret")]
    private static extern ref int SecretField([UnsafeAccessorType("PrivateWithSecrets, DisablePrivateReflectionLibrary")] object instance);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Secret")]
    private static extern int DirectSecret(PrivateWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Secret")]
    private static extern int PublicSecret(PublicWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_secret")]
    private static extern ref int PublicSecretField(PublicWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "s_secret")]
    private static extern ref int PublicStaticSecretField(PublicWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GenericSecret")]
    private static extern T GenericSecret<T>(PublicWithSecrets instance, T value);

    public static int Run()
    {
        DisablePrivateReflectionTests.CheckAttributesWithPrivateArguments(typeof(ConsumerWithPrivateArguments), "type of another assembly");
        DisablePrivateReflectionTests.CheckAttributesWithPrivateGenericArguments(typeof(ConsumerWithPrivateGenericArguments), "type of another assembly, generic");

        // The attribute of a hidden type is not available to the types and members of other assemblies either (and getting them does not fail)
        Type attributed = typeof(ConsumerWithHiddenAttribute);
        DisablePrivateReflectionTests.Check(attributed.GetCustomAttributes(inherit: false).Length == 0 && !attributed.CustomAttributes.Any() &&
            !attributed.IsDefined(typeof(PrivateContainer.NestedMarkerAttribute), inherit: false) && attributed.GetCustomAttributes(typeof(PrivateContainer.NestedMarkerAttribute), inherit: false).Length == 0,
            "attribute of a hidden type on a type of another assembly");
        MethodInfo attributedMethod = attributed.GetMethod(nameof(ConsumerWithHiddenAttribute.Method));
        DisablePrivateReflectionTests.Check(attributedMethod.GetCustomAttributes(inherit: false).Length == 1 && attributedMethod.GetCustomAttributes(inherit: false)[0] is MarkerAttribute &&
            attributedMethod.CustomAttributes.Count() == 1 && attributedMethod.CustomAttributes.Single().AttributeType == typeof(MarkerAttribute) &&
            !attributedMethod.IsDefined(typeof(PrivateContainer.NestedMarkerAttribute), inherit: false),
            "attributes of a hidden type and of a visible type on a member of another assembly");

        // A type of another assembly that derives from a public type sees the public members only
        DisablePrivateReflectionTests.CheckMembers(typeof(ConsumerDerived), "type derived from a public type of another assembly");

        object hidden = CreateSecret(31);
        DisablePrivateReflectionTests.Check(Secret(hidden) == 31 && DirectSecret((PrivateWithSecrets)hidden) == 31 && StaticSecret(hidden) == 12, "UnsafeAccessor methods of a private type, from another assembly");
        SecretField(hidden) = 32;
        DisablePrivateReflectionTests.Check(Secret(hidden) == 32, "UnsafeAccessor field of a private type, from another assembly");

        PublicWithSecrets secrets = new();
        DisablePrivateReflectionTests.Check(PublicSecret(secrets) == 21 && PublicSecretField(secrets) == 21 && PublicStaticSecretField(secrets) == 22 &&
            GenericSecret(secrets, "a") == "a" && GenericSecret(secrets, 3) == 3, "UnsafeAccessor members of a public type, from another assembly");

        return 100;
    }
}
