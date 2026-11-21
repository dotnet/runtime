// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

using ILLink.Shared;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// Metadata blocking policy that blocks, in addition to whatever the wrapped policy blocks, the types that are annotated
    /// with System.Runtime.CompilerServices.ReflectionBlockedAttribute (and their members and nested types).
    /// The attribute is matched by name, so any assembly can define it.
    /// </summary>
    /// <remarks>
    /// Types annotated this way are "stripped": they are blocked, but they don't make the types that merely mention them blocked
    /// (see <see cref="MetadataBlockingPolicy.IsStripped(MetadataType)"/>).
    /// The attribute is not supported on generic types, enums and delegates (nor on types nested in a stripped type that are enums or delegates).
    /// Generic types that the compiler nests in a stripped type (closures, iterators) are stripped as well.
    /// </remarks>
    public sealed class ReflectionBlockedAttributeMetadataBlockingPolicy : MetadataBlockingPolicy
    {
        private const string AttributeNamespace = "System.Runtime.CompilerServices";
        private const string AttributeName = "ReflectionBlockedAttribute";

        private readonly MetadataBlockingPolicy _inner;
        private readonly Logger _logger;
        private readonly ConcurrentDictionary<MetadataType, bool> _strippedTypes = new();

        public ReflectionBlockedAttributeMetadataBlockingPolicy(MetadataBlockingPolicy inner, Logger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public override bool IsStripped(MetadataType type)
        {
            type = (MetadataType)type.GetTypeDefinition();

            if (type is not EcmaType)
                return false;

            if (!_strippedTypes.TryGetValue(type, out bool isStripped))
                isStripped = _strippedTypes.GetOrAdd(type, ComputeIsStripped);

            return isStripped;
        }

        private bool ComputeIsStripped(MetadataType type)
        {
            bool hasAttribute = type.HasCustomAttribute(AttributeNamespace, AttributeName);
            MetadataType containingType = type.ContainingType;

            if (!hasAttribute && (containingType == null || !IsStripped(containingType)))
                return false;

            string reason = null;
            if (type.IsEnum || type.IsDelegate)
                reason = "enums and delegates need their metadata at runtime";
            else if (hasAttribute && type.HasInstantiation)
                reason = "generic types are not supported";

            if (reason is not null)
            {
                _logger.LogError(type, DiagnosticId.ReflectionBlockedNotSupported, type.ToString(), reason);
                return false;
            }

            return true;
        }

        public override bool IsBlocked(MetadataType type) => _inner.IsBlocked(type) || IsStripped(type);

        public override bool IsHardBlocked(MetadataType type) => _inner.IsHardBlocked(type);

        public override bool IsBlocked(MethodDesc method)
            => _inner.IsBlocked(method) || (method.OwningType is MetadataType owningType && IsStripped(owningType));

        public override bool IsBlocked(FieldDesc field)
            => _inner.IsBlocked(field) || (field.OwningType is MetadataType owningType && IsStripped(owningType));
    }
}
