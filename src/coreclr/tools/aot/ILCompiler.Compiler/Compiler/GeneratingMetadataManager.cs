// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Internal.TypeSystem;
using Internal.Metadata.NativeFormat.Writer;

using ILCompiler.Metadata;
using ILCompiler.DependencyAnalysis;

namespace ILCompiler
{
    /// <summary>
    /// Base class for metadata managers that generate metadata blobs.
    /// </summary>
    public abstract class GeneratingMetadataManager : MetadataManager
    {
        protected readonly string _metadataLogFile;
        protected readonly StackTraceEmissionPolicy _stackTraceEmissionPolicy;
        private readonly ModuleDesc _generatedAssembly;

        public GeneratingMetadataManager(CompilerTypeSystemContext typeSystemContext, MetadataBlockingPolicy blockingPolicy,
            ManifestResourceBlockingPolicy resourceBlockingPolicy, string logFile, StackTraceEmissionPolicy stackTracePolicy,
            DynamicInvokeThunkGenerationPolicy invokeThunkGenerationPolicy, MetadataManagerOptions options)
            : base(typeSystemContext, blockingPolicy, resourceBlockingPolicy, invokeThunkGenerationPolicy, options)
        {
            _metadataLogFile = logFile;
            _stackTraceEmissionPolicy = stackTracePolicy;
            _generatedAssembly = typeSystemContext.GeneratedAssembly;
        }

        public sealed override bool WillUseMetadataTokenToReferenceMethod(MethodDesc method)
        {
            return (GetMetadataCategory(method) & MetadataCategory.Description) != 0;
        }

        public sealed override bool WillUseMetadataTokenToReferenceField(FieldDesc field)
        {
            return (GetMetadataCategory(field) & MetadataCategory.Description) != 0;
        }

        protected void ComputeMetadata<TPolicy>(
            TPolicy policy,
            NodeFactory factory,
            out byte[] metadataBlob,
            out List<MetadataMapping<MetadataType>> typeMappings,
            out List<MetadataMapping<MethodDesc>> methodMappings,
            out List<MetadataMapping<FieldDesc>> fieldMappings,
            out List<MetadataMapping<MethodDesc>> stackTraceMapping) where TPolicy : struct, IMetadataPolicy
        {
            var transformed = MetadataTransform.Run(policy, GetCompilationModulesWithMetadata());
            MetadataTransform transform = transformed.Transform;

            // TODO: DeveloperExperienceMode: Use transformed.Transform.HandleType() to generate
            //       TypeReference records for _typeDefinitionsGenerated that don't have metadata.
            //       (To be used in MissingMetadataException messages)

            // Generate metadata blob
            var writer = new MetadataWriter();
            writer.ScopeDefinitions.AddRange(transformed.Scopes);

            // Generate entries in the blob for methods that will be necessary for stack trace purposes.
            var stackTraceRecords = new List<KeyValuePair<MethodDesc, MetadataRecord>>();
            foreach (var methodBody in GetCompiledMethodBodies())
            {
                MethodDesc method = methodBody.Method;

                MethodDesc typicalMethod = method.GetTypicalMethodDefinition();

                // Methods that will end up in the reflection invoke table should not have an entry in stack trace table
                // We'll try looking them up in reflection data at runtime.
                if (transformed.GetTransformedMethodDefinition(typicalMethod) != null &&
                    ShouldMethodBeInInvokeMap(method) &&
                    (GetMetadataCategory(method) & MetadataCategory.RuntimeMapping) != 0)
                    continue;

                if (!_stackTraceEmissionPolicy.ShouldIncludeMethod(method))
                    continue;

                MetadataRecord record = CreateStackTraceRecord(transform, method);

                stackTraceRecords.Add(new KeyValuePair<MethodDesc, MetadataRecord>(
                    method,
                    record));

                writer.AdditionalRootRecords.Add(record);
            }

            var ms = new MemoryStream();

            // .NET metadata is UTF-16 and UTF-16 contains code points that don't translate to UTF-8.
            var noThrowUtf8Encoding = new UTF8Encoding(false, false);

            using (var logWriter = _metadataLogFile != null ? new StreamWriter(File.Open(_metadataLogFile, FileMode.Create, FileAccess.Write, FileShare.Read), noThrowUtf8Encoding) : null)
            {
                writer.LogWriter = logWriter;
                writer.Write(ms);
            }

            metadataBlob = ms.ToArray();

            const int MaxAllowedMetadataOffset = 0xFFFFFF;
            if (metadataBlob.Length > MaxAllowedMetadataOffset)
            {
                // Offset portion of metadata handles is limited to 16 MB.
                throw new InvalidOperationException($"Metadata blob exceeded the addressing range (allowed: {MaxAllowedMetadataOffset}, actual: {metadataBlob.Length})");
            }

            typeMappings = new List<MetadataMapping<MetadataType>>();
            methodMappings = new List<MetadataMapping<MethodDesc>>();
            fieldMappings = new List<MetadataMapping<FieldDesc>>();
            stackTraceMapping = new List<MetadataMapping<MethodDesc>>();

            // Generate type definition mappings
            foreach (var type in factory.MetadataManager.GetTypesWithEETypes())
            {
                MetadataType definition = type.IsTypeDefinition ? type as MetadataType : null;
                if (definition == null)
                    continue;

                MetadataRecord record = transformed.GetTransformedTypeDefinition(definition);

                // Reflection requires that we maintain type identity. Even if we only generated a TypeReference record,
                // if there is an MethodTable for it, we also need a mapping table entry for it.
                record ??= transformed.GetTransformedTypeReference(definition);

                if (record != null)
                    typeMappings.Add(new MetadataMapping<MetadataType>(definition, writer.GetRecordHandle(record)));
            }

            foreach (var method in GetReflectableMethods())
            {
                if (method.IsGenericMethodDefinition || method.OwningType.IsGenericDefinition)
                {
                    // Generic definitions don't have runtime artifacts we would need to map to.
                    continue;
                }

                if (method.GetCanonMethodTarget(CanonicalFormKind.Specific) != method)
                {
                    // Methods that are not in their canonical form are not interesting
                    continue;
                }

                if (IsReflectionBlocked(method.Instantiation) || IsReflectionBlocked(method.OwningType.Instantiation))
                    continue;

                if ((GetMetadataCategory(method) & MetadataCategory.RuntimeMapping) == 0)
                    continue;

                MetadataRecord record = transformed.GetTransformedMethodDefinition(method.GetTypicalMethodDefinition());

                if (record != null)
                    methodMappings.Add(new MetadataMapping<MethodDesc>(method, writer.GetRecordHandle(record)));
            }

            HashSet<FieldDesc> canonicalFields = new HashSet<FieldDesc>();
            foreach (var field in GetFieldsWithRuntimeMapping())
            {
                FieldDesc fieldToAdd = field;
                TypeDesc canonOwningType = field.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific);
                if (canonOwningType.IsCanonicalSubtype(CanonicalFormKind.Any))
                {
                    FieldDesc canonField = _typeSystemContext.GetFieldForInstantiatedType(field.GetTypicalFieldDefinition(), (InstantiatedType)canonOwningType);

                    // If we already added a canonically equivalent field, skip this one.
                    if (!canonicalFields.Add(canonField))
                        continue;

                    fieldToAdd = canonField;
                }

                Field record = transformed.GetTransformedFieldDefinition(fieldToAdd.GetTypicalFieldDefinition());
                if (record != null)
                    fieldMappings.Add(new MetadataMapping<FieldDesc>(fieldToAdd, writer.GetRecordHandle(record)));
            }

            // Generate stack trace metadata mapping
            foreach (var stackTraceRecord in stackTraceRecords)
            {
                stackTraceMapping.Add(new MetadataMapping<MethodDesc>(stackTraceRecord.Key, writer.GetRecordHandle(stackTraceRecord.Value)));
            }
        }

        /// <summary>
        /// Gets a list of fields that got "compiled" and are eligible for a runtime mapping.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<FieldDesc> GetFieldsWithRuntimeMapping();

        /// <summary>
        /// Gets a stub that can be used to reflection-invoke a method with a given signature.
        /// </summary>
        public sealed override MethodDesc GetReflectionInvokeStub(MethodDesc method)
        {
            return _typeSystemContext.GetDynamicInvokeThunk(method.Signature,
                !method.Signature.IsStatic && method.OwningType.IsValueType);
        }
    }
}
