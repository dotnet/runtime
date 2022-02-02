// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Debug = System.Diagnostics.Debug;

namespace ILCompiler.Metadata
{
    public struct MetadataTransformResult<T>
        where T : struct, IMetadataPolicy
    {
        private Transform<T> _transform;

        /// <summary>
        /// Gets a collection of records representing the top level transformed metadata scopes.
        /// </summary>
        public IReadOnlyCollection<ScopeDefinition> Scopes
        {
            get
            {
                return _transform._scopeDefs.Records;
            }
        }

        /// <summary>
        /// Gets an instance of <see cref="MetadataTransform"/> that allows generation of additional
        /// (non-definition) metadata records. This can be used to retrieve or create metadata records
        /// to be referenced from e.g. code.
        /// Note that the records may not be reachable from any of the top level <see cref="Scopes"/>.
        /// To make sure the records get emitted into the metadata blob by the <see cref="MetadataWriter"/>,
        /// add them to <see cref="MetadataWriter.AdditionalRootRecords"/>.
        /// </summary>
        public MetadataTransform Transform
        {
            get
            {
                return _transform;
            }
        }

        internal MetadataTransformResult(Transform<T> transform)
        {
            _transform = transform;
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="TypeDefinition"/> record corresponding to the specified
        /// <paramref name="type"/>. Returns null if not found.
        /// </summary>
        public TypeDefinition GetTransformedTypeDefinition(Cts.MetadataType type)
        {
            Debug.Assert(type.IsTypeDefinition);

            MetadataRecord rec;
            if (!_transform._types.TryGet(type, out rec))
            {
                return null;
            }

            return rec as TypeDefinition;
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="TypeReference"/> record corresponding to the specified
        /// <paramref name="type"/>. Returns null if not found.
        /// </summary>
        public TypeReference GetTransformedTypeReference(Cts.MetadataType type)
        {
            Debug.Assert(type.IsTypeDefinition);

            MetadataRecord rec;
            if (!_transform._types.TryGet(type, out rec))
            {
                return null;
            }

            return rec as TypeReference;
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="Method"/> record corresponding to the specified
        /// <paramref name="method"/>. Returns null if not found.
        /// </summary>
        public Method GetTransformedMethodDefinition(Cts.MethodDesc method)
        {
            Debug.Assert(method.IsTypicalMethodDefinition);

            MetadataRecord rec;
            if (!_transform._methods.TryGet(method, out rec))
            {
                return null;
            }

            return rec as Method;
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="Field"/> record corresponding to the specified
        /// <paramref name="field"/>. Returns null if not found.
        /// </summary>
        public Field GetTransformedFieldDefinition(Cts.FieldDesc field)
        {
            Debug.Assert(field.OwningType.IsTypeDefinition);

            MetadataRecord rec;
            if (!_transform._fields.TryGet(field, out rec))
            {
                return null;
            }

            return rec as Field;
        }
    }
}
