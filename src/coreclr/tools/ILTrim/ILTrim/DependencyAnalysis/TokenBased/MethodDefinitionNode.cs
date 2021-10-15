// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents a row in the MethodDef table.
    /// </summary>
    public sealed class MethodDefinitionNode : TokenBasedNode
    {
        public MethodDefinitionNode(EcmaModule module, MethodDefinitionHandle handle)
            : base(module, handle)
        {
        }

        private MethodDefinitionHandle Handle => (MethodDefinitionHandle)_handle;

        public bool IsInstanceMethodOnReferenceType {
            get {
                MethodDefinition methodDef = _module.MetadataReader.GetMethodDefinition(Handle);
                TypeDefinitionHandle declaringType = methodDef.GetDeclaringType();
                EcmaType ecmaType = (EcmaType)_module.GetObject(declaringType);
                return !ecmaType.IsValueType && !methodDef.Attributes.HasFlag(MethodAttributes.Static);

            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            MethodDefinition methodDef = _module.MetadataReader.GetMethodDefinition(Handle);
            TypeDefinitionHandle declaringType = methodDef.GetDeclaringType();

            DependencyList dependencies = new DependencyList();

            EcmaSignatureAnalyzer.AnalyzeMethodSignature(
                _module,
                _module.MetadataReader.GetBlobReader(methodDef.Signature),
                factory,
                dependencies);

            dependencies.Add(factory.TypeDefinition(_module, declaringType), "Method owning type");

            if (!IsInstanceMethodOnReferenceType || factory.IsModuleTrimmedInLibraryMode())
            {
                // Static methods and methods on value types are not subject to the unused method body optimization.
                dependencies.Add(factory.MethodBody(_module, Handle), "Method body");
            }

            foreach (CustomAttributeHandle customAttribute in methodDef.GetCustomAttributes())
            {
                dependencies.Add(factory.CustomAttribute(_module, customAttribute), "Custom attribute of a method");
            }

            foreach (ParameterHandle parameter in methodDef.GetParameters())
            {
                dependencies.Add(factory.Parameter(_module, parameter), "Parameter of method");
            }

            foreach (GenericParameterHandle parameter in methodDef.GetGenericParameters())
            {
                dependencies.Add(factory.GenericParameter(_module, parameter), "Generic Parameter of method");
            }

            if ((methodDef.Attributes & MethodAttributes.PinvokeImpl) != 0)
            {
                MethodImport import = methodDef.GetImport();
                dependencies.Add(factory.ModuleReference(_module, import.Module), "DllImport");
            }

            return dependencies;
        }

        // Instance methods on reference types conditionally depend on their bodies.
        public override bool HasConditionalStaticDependencies => IsInstanceMethodOnReferenceType;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            MethodDefinition methodDef = _module.MetadataReader.GetMethodDefinition(Handle);
            TypeDefinitionHandle declaringType = methodDef.GetDeclaringType();
            var ecmaType = (EcmaType)_module.GetObject(declaringType);

            // Conditionally depend on the method body if the declaring type was constructed.
            yield return new(
                factory.MethodBody(_module, Handle),
                factory.ConstructedType(ecmaType),
                "Method body on constructed type");
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;

            MethodDefinition methodDef = reader.GetMethodDefinition(Handle);

            var builder = writeContext.MetadataBuilder;

            EcmaType ecmaType = (EcmaType)_module.GetObject(methodDef.GetDeclaringType());
            MethodBodyNode bodyNode = writeContext.Factory.MethodBody(_module, Handle);
            int bodyOffset = bodyNode.Marked
                ? bodyNode.Write(writeContext)
                : writeContext.WriteUnreachableMethodBody(Handle, _module);

            BlobBuilder signatureBlob = writeContext.GetSharedBlobBuilder();
            EcmaSignatureRewriter.RewriteMethodSignature(
                reader.GetBlobReader(methodDef.Signature),
                writeContext.TokenMap,
                signatureBlob);

            MethodDefinitionHandle outputHandle = builder.AddMethodDefinition(
                methodDef.Attributes,
                methodDef.ImplAttributes,
                builder.GetOrAddString(reader.GetString(methodDef.Name)),
                builder.GetOrAddBlob(signatureBlob),
                bodyOffset,
                writeContext.TokenMap.MapMethodParamList(Handle));

            if ((methodDef.Attributes & MethodAttributes.PinvokeImpl) != 0)
            {
                MethodImport import = methodDef.GetImport();
                builder.AddMethodImport(outputHandle,
                    import.Attributes,
                    builder.GetOrAddString(reader.GetString(import.Name)),
                    (ModuleReferenceHandle)writeContext.TokenMap.MapToken(import.Module));
            }

            return outputHandle;
        }

        public override string ToString()
        {
            // TODO: would be nice to have a common formatter we can call into that also includes owning type
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetMethodDefinition(Handle).Name);
        }
    }
}
