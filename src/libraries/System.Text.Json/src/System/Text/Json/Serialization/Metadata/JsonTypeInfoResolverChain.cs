// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Metadata
{
    internal class JsonTypeInfoResolverChain : ConfigurationList<IJsonTypeInfoResolver>, IJsonTypeInfoResolver, IBuiltInJsonTypeInfoResolver
    {
        public JsonTypeInfoResolverChain() : base(null) { }
        public override bool IsReadOnly => true;
        protected override void OnCollectionModifying()
            => ThrowHelper.ThrowInvalidOperationException_TypeInfoResolverChainImmutable();

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            foreach (IJsonTypeInfoResolver resolver in _list)
            {
                JsonTypeInfo? typeInfo = resolver.GetTypeInfo(type, options);
                if (typeInfo != null)
                {
                    return typeInfo;
                }
            }

            return null;
        }

        internal void AddFlattened(IJsonTypeInfoResolver? resolver)
        {
            switch (resolver)
            {
                case null:
                    break;

                case JsonTypeInfoResolverChain otherChain:
                    _list.AddRange(otherChain);
                    break;

                default:
                    _list.Add(resolver);
                    break;
            }
        }

        bool IBuiltInJsonTypeInfoResolver.IsCompatibleWithOptions(JsonSerializerOptions options)
        {
            foreach (IJsonTypeInfoResolver component in _list)
            {
                if (!component.IsCompatibleWithOptions(options))
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("[");
            foreach (IJsonTypeInfoResolver resolver in _list)
            {
                sb.Append(resolver);
                sb.Append(", ");
            }

            if (_list.Count > 0)
                sb.Length -= 2;

            sb.Append(']');
            return sb.ToString();
        }
    }
}
