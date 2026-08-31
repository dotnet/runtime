// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    internal sealed partial class EcmaModule
    {

        /// <summary>
        /// Helper routine for the more general Module.GetType() family of apis. Also used in typeRef resolution.
        ///
        /// Resolves top-level named types only. No nested types. No constructed types. The input name must not be escaped.
        ///
        /// If a type is not contained or forwarded from the assembly, this method returns null (does not throw.)
        /// This supports the "throwOnError: false" behavior of Module.GetType(string, bool).
        /// </summary>
        [UnconditionalSuppressMessage("SingleFile", "IL3002:RequiresAssemblyFiles on FullyQualifiedName",
            Justification = "FullyQualifiedName is only used for exception message")]
        protected sealed override RoDefinitionType? GetTypeCoreNoCache(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name, out Exception? e)
        {
            MetadataReader reader = Reader;

            // Look through types declared in the manifest module.
            foreach (TypeDefinitionHandle h in reader.TypeDefinitions)
            {
                TypeDefinition td = h.GetTypeDefinition(reader);
                if (td.IsNested)
                    continue;  // GetTypeCore() is never asked to look for nested types.
                if (!(td.Name.Equals(name, reader)))
                    continue;
                if (!(td.Namespace.Equals(ns, reader)))
                    continue;

                e = null;
                return h.ResolveTypeDef(this);
            }

            // Look for forwarded types.
            foreach (ExportedTypeHandle h in reader.ExportedTypes)
            {
                ExportedType et = h.GetExportedType(reader);
                if (!et.IsForwarder)
                    continue;

                EntityHandle implementation = et.Implementation;
                if (implementation.Kind != HandleKind.AssemblyReference) // This check also weeds out nested types. This is intentional.
                    continue;

                if (!(et.Name.Equals(name, reader)))
                    continue;

                if (!(et.Namespace.Equals(ns, reader)))
                    continue;

                RoAssembly? assembly = ((AssemblyReferenceHandle)implementation).TryResolveAssembly(this, out e);
                return assembly?.GetTypeCore(ns, name, ignoreCase: false, out e);
            }

            e = new TypeLoadException(SR.Format(SR.TypeNotFound, ns.ToUtf16().AppendTypeName(name.ToUtf16()), FullyQualifiedName));
            return null;
        }
    }
}
