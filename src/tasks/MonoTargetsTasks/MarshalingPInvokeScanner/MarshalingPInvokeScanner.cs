// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace MonoTargetsTasks
{
    public class MarshalingPInvokeScanner : Task
    {
        [Required]
        public string[] Assemblies { get; set; } = Array.Empty<string>();

        [Output]
        public string[]? IncompatibleAssemblies { get; private set; }

        public override bool Execute()
        {
            if (Assemblies is null || Assemblies!.Length == 0)
            {
                Log.LogError($"{nameof(MarshalingPInvokeScanner)}.{nameof(Assemblies)} cannot be empty");
                return false;
            }

            try
            {
                ExecuteInternal();
                return !Log.HasLoggedErrors;
            }
            catch (LogAsErrorException e)
            {
                Log.LogError(e.Message);
                return false;
            }
        }

        private void ExecuteInternal()
        {
            IncompatibleAssemblies = ScanAssemblies(Assemblies);
        }

        private string[] ScanAssemblies(string[] assemblies)
        {
            HashSet<string> incompatible = new HashSet<string>();
            MinimalMarshalingTypeCompatibilityProvider mmtcp = new(Log);
            foreach (string aname in assemblies)
            {
                if (IsAssemblyIncompatible(aname, mmtcp))
                    incompatible.Add(aname);
            }

            if (mmtcp.IsSecondPassNeeded)
            {
                foreach (string aname in assemblies)
                    ResolveInconclusiveTypes(incompatible, aname, mmtcp);
            }

            return incompatible.ToArray();
        }

        private static string GetMethodName(MetadataReader mr, MethodDefinition md) => mr.GetString(md.Name);

        private void ResolveInconclusiveTypes(HashSet<string> incompatible, string assyPath, MinimalMarshalingTypeCompatibilityProvider mmtcp)
        {
            using FileStream file = new FileStream(assyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using PEReader peReader = new PEReader(file);
            if (!peReader.HasMetadata)
                return; // Just return. There are no metadata in the DLL to help us with remaining types.

            MetadataReader mdtReader = peReader.GetMetadataReader();
            string assyName = mdtReader.GetString(mdtReader.GetAssemblyDefinition().Name);
            HashSet<string> inconclusiveTypes = mmtcp.GetInconclusiveTypesForAssembly(assyName);
            if(inconclusiveTypes.Count == 0)
                return;

            SignatureDecoder<Compatibility, object> decoder = new(mmtcp, mdtReader, null!);

            foreach (TypeDefinitionHandle typeDefHandle in mdtReader.TypeDefinitions)
            {
                TypeDefinition typeDef = mdtReader.GetTypeDefinition(typeDefHandle);
                string fullTypeName = string.Join(":", mdtReader.GetString(typeDef.Namespace), mdtReader.GetString(typeDef.Name));

                // This is not perfect, but should work right for enums defined in other assemblies,
                // which is the only case where we use Compatibility.Inconclusive.
                if (inconclusiveTypes.Contains(fullTypeName) &&
                    mmtcp.GetTypeFromDefinition(mdtReader, typeDefHandle, 0) != Compatibility.Compatible)
                {
                    Log.LogMessage(MessageImportance.Low, string.Format("Type {0} is marshaled and requires marshal-ilgen.", fullTypeName));

                    incompatible.Add("(unknown assembly)");
                }
            }
        }

        private bool IsAssemblyIncompatible(string assyPath, MinimalMarshalingTypeCompatibilityProvider mmtcp)
        {
            using FileStream file = new FileStream(assyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using PEReader peReader = new PEReader(file);
            if (!peReader.HasMetadata)
                return false; // No types in this DLL means no incompatible marshaling constructs.

            MetadataReader mdtReader = peReader.GetMetadataReader();

            foreach(CustomAttributeHandle attrHandle in mdtReader.CustomAttributes)
            {
                CustomAttribute attr = mdtReader.GetCustomAttribute(attrHandle);

                if(attr.Constructor.Kind == HandleKind.MethodDefinition)
                {
                    MethodDefinitionHandle mdh = (MethodDefinitionHandle)attr.Constructor;
                    MethodDefinition md = mdtReader.GetMethodDefinition(mdh);
                    TypeDefinitionHandle tdh = md.GetDeclaringType();
                    TypeDefinition td = mdtReader.GetTypeDefinition(tdh);

                    if(mdtReader.GetString(td.Namespace) == "System.Runtime.CompilerServices" &&
                        mdtReader.GetString(td.Name) == "DisableRuntimeMarshallingAttribute")
                        return false;
                }
            }

            foreach (TypeDefinitionHandle typeDefHandle in mdtReader.TypeDefinitions)
            {
                TypeDefinition typeDef = mdtReader.GetTypeDefinition(typeDefHandle);
                string ns = mdtReader.GetString(typeDef.Namespace);
                string name = mdtReader.GetString(typeDef.Name);

                foreach(MethodDefinitionHandle mthDefHandle in typeDef.GetMethods())
                {
                    MethodDefinition mthDef = mdtReader.GetMethodDefinition(mthDefHandle);
                    if(!mthDef.Attributes.HasFlag(MethodAttributes.PinvokeImpl))
                        continue;

                    BlobReader sgnBlobReader = mdtReader.GetBlobReader(mthDef.Signature);
                    SignatureDecoder<Compatibility, object> decoder = new(mmtcp, mdtReader, null!);

                    MethodSignature<Compatibility> sgn = decoder.DecodeMethodSignature(ref sgnBlobReader);
                    if(sgn.ReturnType == Compatibility.Incompatible || sgn.ParameterTypes.Any(p => p == Compatibility.Incompatible))
                    {
                        Log.LogMessage(MessageImportance.Low, string.Format("Assembly {0} requires marshal-ilgen for method {1}.{2}:{3} (first pass).",
                            assyPath, ns, name, mdtReader.GetString(mthDef.Name)));

                        return true;
                    }
                }
            }

            return false;
        }
    }
}
