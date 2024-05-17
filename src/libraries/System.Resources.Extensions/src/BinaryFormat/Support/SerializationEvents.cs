// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace System.Resources.Extensions.BinaryFormat;

internal sealed class SerializationEvents
{
    private static readonly ConcurrentDictionary<Type, SerializationEvents> s_cache = new();

    private static readonly SerializationEvents s_noEvents = new();

    private readonly List<MethodInfo>? _onDeserializingMethods;
    private readonly List<MethodInfo>? _onDeserializedMethods;

    private SerializationEvents() { }

    private SerializationEvents(
        List<MethodInfo>? onDeserializingMethods,
        List<MethodInfo>? onDeserializedMethods)
    {
        _onDeserializingMethods = onDeserializingMethods;
        _onDeserializedMethods = onDeserializedMethods;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111:UnrecognizedReflectionPattern",
        Justification = "The Type is annotated correctly, it just can't pass through the lambda method.")]
    private static SerializationEvents GetSerializationEventsForType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) =>
        s_cache.GetOrAdd(t, CreateSerializationEvents);

    private static SerializationEvents CreateSerializationEvents([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        List<MethodInfo>? onDeserializingMethods = GetMethodsWithAttribute(typeof(OnDeserializingAttribute), type);
        List<MethodInfo>? onDeserializedMethods = GetMethodsWithAttribute(typeof(OnDeserializedAttribute), type);

        return onDeserializingMethods is null && onDeserializedMethods is null
            ? s_noEvents
            : new SerializationEvents(onDeserializingMethods, onDeserializedMethods);
    }

    private static List<MethodInfo>? GetMethodsWithAttribute(
        Type attribute,
        // Currently the only way to preserve base, non-public methods is to use All
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? type)
    {
        List<MethodInfo>? attributedMethods = null;

        // Traverse the hierarchy to find all methods with the specified attribute.
        Type? baseType = type;
        while (baseType is not null && baseType != typeof(object))
        {
            MethodInfo[] methods = baseType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (MethodInfo method in methods)
            {
                if (method.IsDefined(attribute, inherit: false))
                {
                    attributedMethods ??= [];
                    attributedMethods.Add(method);
                }
            }

            baseType = baseType.BaseType;
        }

        // We should invoke the methods starting from base.
        attributedMethods?.Reverse();

        return attributedMethods;
    }

    internal static Action<StreamingContext>? GetOnDeserializingForType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
        object obj) =>
        GetSerializationEventsForType(type).GetOnDeserializing(obj);

    internal static Action<StreamingContext>? GetOnDeserializedForType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
        object obj) =>
        GetSerializationEventsForType(type).GetOnDeserialized(obj);

    private Action<StreamingContext>? GetOnDeserialized(object obj) =>
        AddOnDelegate(obj, _onDeserializedMethods);

    private Action<StreamingContext>? GetOnDeserializing(object obj) =>
        AddOnDelegate(obj, _onDeserializingMethods);

    /// <summary>Add all methods to a delegate.</summary>
    private static Action<StreamingContext>? AddOnDelegate(object obj, List<MethodInfo>? methods)
    {
        Action<StreamingContext>? handler = null;

        if (methods is not null)
        {
            foreach (MethodInfo method in methods)
            {
                Action<StreamingContext> onDeserialized =
#if NETCOREAPP
                    method.CreateDelegate<Action<StreamingContext>>(obj);
#else
                    (Action<StreamingContext>)method.CreateDelegate(typeof(Action<StreamingContext>), obj);
#endif
                handler += onDeserialized;
            }
        }

        return handler;
    }
}
