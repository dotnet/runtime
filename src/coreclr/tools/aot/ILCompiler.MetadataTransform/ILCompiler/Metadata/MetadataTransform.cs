// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;

namespace ILCompiler.Metadata
{
    /// <summary>
    /// Transforms type system object model into metadata writer object model that is suitable
    /// for binary serialization into a metadata blob using the <see cref="MetadataWriter"/> class.
    /// </summary>
    public abstract class MetadataTransform
    {
        /// <summary>
        /// Transforms the specified modules using the specified metadata generation
        /// policy. Policy is required to be a struct for performance reasons.
        /// </summary>
        /// <remarks>
        /// The list of <paramref name="modules"/> is required to be transitively complete with respect to
        /// <paramref name="policy"/>: whenever there's a reference from the object graph to a type or member defined in a
        /// module that was not included in the <paramref name="modules"/> enumeration, the
        /// <see cref="IMetadataPolicy.GeneratesMetadata(Cts.MetadataType)"/>
        /// and <see cref="IMetadataPolicy.GeneratesMetadata(Cts.MethodDesc)"/> are required to return false.
        /// </remarks>
        public static MetadataTransformResult<TPolicy> Run<TPolicy>(TPolicy policy, IEnumerable<Cts.ModuleDesc> modules)
            where TPolicy : struct, IMetadataPolicy
        {
            // TODO: Make this multithreaded. The high level plan is:
            // - make EntityMap thread safe
            // - change the way TypeDefs are hooked up into namespaces and scopes
            // - queue up calls to the various Initialize* methods on a threadpool

            var transform = new Transform<TPolicy>(policy);

            foreach (var module in modules)
            {
                foreach (var type in module.GetAllTypes())
                {
                    if (policy.GeneratesMetadata(type) && !policy.IsBlocked(type))
                    {
                        transform.HandleType(type);
                    }
                }
            }

            return new MetadataTransformResult<TPolicy>(transform);
        }

        /// <summary>
        /// Retrieves an existing <see cref="TypeDefinition"/>, <see cref="TypeReference"/>,
        /// or <see cref="TypeSpecification"/> record representing specified type in the metadata writer object
        /// model, or creates a new one.
        /// </summary>
        public abstract MetadataRecord HandleType(Cts.TypeDesc type);

        /// <summary>
        /// Retrieves an existing <see cref="QualifiedMethod"/>, <see cref="MemberReference"/>, or <see cref="MethodInstantiation"/>
        /// record representing specified method in the metadata writer object model, or creates a new one.
        /// </summary>
        public abstract MetadataRecord HandleQualifiedMethod(Cts.MethodDesc method);

        /// <summary>
        /// Retrieves an existing <see cref="QualifiedField"> or a <see cref="MemberReference"/> record
        /// representing specified field in the metadata writer object model, or creates a new one.
        /// </summary>
        public abstract MetadataRecord HandleQualifiedField(Cts.FieldDesc field);

        /// <summary>
        /// Retrieves an existing <see cref="MethodSignature"/> record representing the specified signature
        /// in the metadata writer object model, or creates a new one.
        /// </summary>
        public abstract MethodSignature HandleMethodSignature(Cts.MethodSignature signature);
    }
}
