// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.Serialization
{
    internal sealed class SerializationEvents
    {
        private readonly List<MethodInfo>? _onSerializingMethods;
        private readonly List<MethodInfo>? _onSerializedMethods;
        private readonly List<MethodInfo>? _onDeserializingMethods;
        private readonly List<MethodInfo>? _onDeserializedMethods;

        internal SerializationEvents([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? t)
        {
            _onSerializingMethods = GetMethodsWithAttribute(typeof(OnSerializingAttribute), t);
            _onSerializedMethods = GetMethodsWithAttribute(typeof(OnSerializedAttribute), t);
            _onDeserializingMethods = GetMethodsWithAttribute(typeof(OnDeserializingAttribute), t);
            _onDeserializedMethods = GetMethodsWithAttribute(typeof(OnDeserializedAttribute), t);
        }

        private List<MethodInfo>? GetMethodsWithAttribute(
            Type attribute,
            // currently the only way to preserve base, non-public methods is to use All
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? t)
        {
            List<MethodInfo>? mi = null;

            // Traverse the hierarchy to find all methods with the particular attribute
            Type? baseType = t;
            while (baseType != null && baseType != typeof(object))
            {
                // Get all methods which are declared on this type, instance and public or nonpublic
                MethodInfo[] mis = baseType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (MethodInfo m in mis)
                {
                    // For each method find if attribute is present, the return type is void and the method is not virtual
                    if (m.IsDefined(attribute, false))
                    {
                        mi ??= new List<MethodInfo>();
                        mi.Add(m);
                    }
                }
                baseType = baseType.BaseType;
            }
            mi?.Reverse(); // We should invoke the methods starting from base

            return mi;
        }

        internal bool HasOnSerializingEvents =>
            _onSerializingMethods != null || _onSerializedMethods != null;

        internal void InvokeOnSerializing(object obj, StreamingContext context) =>
            InvokeOnDelegate(obj, context, _onSerializingMethods);

        internal void InvokeOnDeserializing(object obj, StreamingContext context) =>
            InvokeOnDelegate(obj, context, _onDeserializingMethods);

        internal void InvokeOnDeserialized(object obj, StreamingContext context) =>
            InvokeOnDelegate(obj, context, _onDeserializedMethods);

        internal SerializationEventHandler? AddOnSerialized(object obj, SerializationEventHandler? handler) =>
            AddOnDelegate(obj, handler, _onSerializedMethods);

        internal SerializationEventHandler? AddOnDeserialized(object obj, SerializationEventHandler? handler) =>
            AddOnDelegate(obj, handler, _onDeserializedMethods);

        /// <summary>Invoke all methods.</summary>
        private static void InvokeOnDelegate(object obj, StreamingContext context, List<MethodInfo>? methods)
        {
            Debug.Assert(obj != null, "object should have been initialized");
            AddOnDelegate(obj, null, methods)?.Invoke(context);
        }

        /// <summary>Add all methods to a delegate.</summary>
        private static SerializationEventHandler? AddOnDelegate(object obj, SerializationEventHandler? handler, List<MethodInfo>? methods)
        {
            if (methods != null)
            {
                foreach (MethodInfo m in methods)
                {
                    SerializationEventHandler onDeserialized = m.CreateDelegate<SerializationEventHandler>(obj);
                    handler = (SerializationEventHandler)Delegate.Combine(handler, onDeserialized);
                }
            }
            return handler;
        }
    }

    internal static class SerializationEventsCache
    {
        private static readonly ConcurrentDictionary<Type, SerializationEvents> s_cache = new ConcurrentDictionary<Type, SerializationEvents>();

        internal static SerializationEvents GetSerializationEventsForType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) =>
            s_cache.GetOrAdd(t, CreateSerializationEvents);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "The Type is annotated correctly, it just can't pass through the lambda method.")]
        private static SerializationEvents CreateSerializationEvents(Type t) => new SerializationEvents(t);
    }
}
