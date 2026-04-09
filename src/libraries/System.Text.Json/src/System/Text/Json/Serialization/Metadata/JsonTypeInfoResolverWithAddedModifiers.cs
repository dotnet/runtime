// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Metadata
{
    internal sealed class JsonTypeInfoResolverWithAddedModifiers : IJsonTypeInfoResolver
    {
        private readonly IJsonTypeInfoResolver _source;
        private readonly Action<JsonTypeInfo>[] _modifiers;

        public JsonTypeInfoResolverWithAddedModifiers(IJsonTypeInfoResolver source, Action<JsonTypeInfo>[] modifiers)
        {
            Debug.Assert(modifiers.Length > 0);
            _source = source;
            _modifiers = modifiers;
        }

        public JsonTypeInfoResolverWithAddedModifiers WithAddedModifier(Action<JsonTypeInfo> modifier)
        {
            var newModifiers = new Action<JsonTypeInfo>[_modifiers.Length + 1];
            _modifiers.CopyTo(newModifiers, 0);
            newModifiers[_modifiers.Length] = modifier;

            return new JsonTypeInfoResolverWithAddedModifiers(_source, newModifiers);
        }

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo? typeInfo = _source.GetTypeInfo(type, options);

            if (typeInfo != null)
            {
                foreach (Action<JsonTypeInfo> modifier in _modifiers)
                {
                    modifier(typeInfo);
                }
            }

            return typeInfo;
        }
    }
}
