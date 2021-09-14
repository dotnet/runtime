// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using CilStrip.Mono.Cecil;
using CilStrip.Mono.Cecil.Binary;
using CilStrip.Mono.Cecil.Cil;
using CilStrip.Mono.Cecil.Metadata;

public class ILStrip : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Assemblies to be stripped.
    /// The assemblies will be modified in place if OutputPath metadata is not set.
    /// </summary>
    [Required]
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Disable parallel stripping
    /// </summary>
    public bool DisableParallelStripping { get; set; }

    public override bool Execute()
    {
        if (Assemblies.Length == 0)
        {
            throw new ArgumentException($"'{nameof(Assemblies)}' is required.", nameof(Assemblies));
        }

        if (DisableParallelStripping)
        {
            foreach (var assemblyItem in Assemblies)
            {
                if (!StripAssembly(assemblyItem))
                    return !Log.HasLoggedErrors;
            }
        }
        else
        {
            Parallel.ForEach(Assemblies,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                assemblyItem => StripAssembly(assemblyItem));
        }

        return !Log.HasLoggedErrors;
    }

    private bool StripAssembly(ITaskItem assemblyItem)
    {
        string assemblyFile = assemblyItem.ItemSpec;
        var outputPath = assemblyItem.GetMetadata("OutputPath");
        if (String.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = assemblyFile;
        	Log.LogMessage(MessageImportance.Low, $"[ILStrip] {assemblyFile}");
        }
		else
		{
        	Log.LogMessage(MessageImportance.Low, $"[ILStrip] {assemblyFile} to {outputPath}");
		}

        try
        {
            AssemblyDefinition assembly = AssemblyFactory.GetAssembly(assemblyFile);
            AssemblyStripper.StripAssembly(assembly, outputPath);
        }
        catch (Exception ex)
        {
            Log.LogMessage(MessageImportance.Low, ex.ToString());
            Log.LogError($"ILStrip failed for {assemblyFile}: {ex.Message}");
            return false;
        }

        return true;
    }

    private class AssemblyStripper
    {
        AssemblyDefinition assembly;
        BinaryWriter writer;

        Image original;
        Image stripped;

        ReflectionWriter reflection_writer;
        MetadataWriter metadata_writer;

        TablesHeap original_tables;
        TablesHeap stripped_tables;

        AssemblyStripper(AssemblyDefinition assembly, BinaryWriter writer)
        {
            this.assembly = assembly;
            this.writer = writer;
        }

        void Strip()
        {
            FullLoad();
            ClearMethodBodies();
            CopyOriginalImage();
            PatchMethods();
            PatchFields();
            PatchResources();
            Write();
        }

        void FullLoad()
        {
            assembly.MainModule.FullLoad();
        }

        void ClearMethodBodies()
        {
            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                ClearMethodBodies(type.Constructors);
                ClearMethodBodies(type.Methods);
            }
        }

        static void ClearMethodBodies(ICollection methods)
        {
            foreach (MethodDefinition method in methods)
            {
                if (!method.HasBody)
                    continue;

                MethodBody body = new MethodBody(method);
                body.CilWorker.Emit(OpCodes.Ret);

                method.Body = body;
            }
        }

        void CopyOriginalImage()
        {
            original = assembly.MainModule.Image;
            stripped = Image.CreateImage();

            stripped.Accept(new CopyImageVisitor(original));

            assembly.MainModule.Image = stripped;

            original_tables = original.MetadataRoot.Streams.TablesHeap;
            stripped_tables = stripped.MetadataRoot.Streams.TablesHeap;

            TableCollection tables = original_tables.Tables;
            foreach (IMetadataTable table in tables)
                stripped_tables.Tables.Add(table);

            stripped_tables.Valid = original_tables.Valid;
            stripped_tables.Sorted = original_tables.Sorted;

            reflection_writer = new ReflectionWriter(assembly.MainModule);
            reflection_writer.StructureWriter = new StructureWriter(assembly, writer);
            reflection_writer.CodeWriter.Stripped = true;

            metadata_writer = reflection_writer.MetadataWriter;

            PatchHeap(metadata_writer.StringWriter, original.MetadataRoot.Streams.StringsHeap);
            PatchHeap(metadata_writer.GuidWriter, original.MetadataRoot.Streams.GuidHeap);
            PatchHeap(metadata_writer.UserStringWriter, original.MetadataRoot.Streams.UserStringsHeap);
            PatchHeap(metadata_writer.BlobWriter, original.MetadataRoot.Streams.BlobHeap);

            if (assembly.EntryPoint != null)
                metadata_writer.EntryPointToken = assembly.EntryPoint.MetadataToken.ToUInt();
        }

        static void PatchHeap(MemoryBinaryWriter heap_writer, MetadataHeap heap)
        {
            if (heap == null)
                return;

            heap_writer.BaseStream.Position = 0;
            heap_writer.Write(heap.Data);
        }

        void PatchMethods()
        {
            MethodTable methodTable = (MethodTable)stripped_tables[MethodTable.RId];
            if (methodTable == null)
                return;

            RVA method_rva = RVA.Zero;

            for (int i = 0; i < methodTable.Rows.Count; i++)
            {
                MethodRow methodRow = methodTable[i];

                methodRow.ImplFlags |= MethodImplAttributes.NoInlining;

                MetadataToken methodToken = MetadataToken.FromMetadataRow(TokenType.Method, i);

                MethodDefinition method = (MethodDefinition)assembly.MainModule.LookupByToken(methodToken);

                if (method.HasBody)
                {
                    method_rva = method_rva != RVA.Zero
                        ? method_rva
                        : reflection_writer.CodeWriter.WriteMethodBody(method);

                    methodRow.RVA = method_rva;
                }
                else
                    methodRow.RVA = RVA.Zero;
            }
        }

        void PatchFields()
        {
            FieldRVATable fieldRvaTable = (FieldRVATable)stripped_tables[FieldRVATable.RId];
            if (fieldRvaTable == null)
                return;

            for (int i = 0; i < fieldRvaTable.Rows.Count; i++)
            {
                FieldRVARow fieldRvaRow = fieldRvaTable[i];

                MetadataToken fieldToken = new MetadataToken(TokenType.Field, fieldRvaRow.Field);

                FieldDefinition field = (FieldDefinition)assembly.MainModule.LookupByToken(fieldToken);

                fieldRvaRow.RVA = metadata_writer.GetDataCursor();
                metadata_writer.AddData(field.InitialValue.Length + 3 & (~3));
                metadata_writer.AddFieldInitData(field.InitialValue);
            }
        }

        void PatchResources()
        {
            ManifestResourceTable resourceTable = (ManifestResourceTable)stripped_tables[ManifestResourceTable.RId];
            if (resourceTable == null)
                return;

            for (int i = 0; i < resourceTable.Rows.Count; i++)
            {
                ManifestResourceRow resourceRow = resourceTable[i];

                if (resourceRow.Implementation.RID != 0)
                    continue;

                foreach (Resource resource in assembly.MainModule.Resources)
                {
                    EmbeddedResource er = resource as EmbeddedResource;
                    if (er == null)
                        continue;

                    if (resource.Name != original.MetadataRoot.Streams.StringsHeap[resourceRow.Name])
                        continue;

                    resourceRow.Offset = metadata_writer.AddResource(er.Data);
                }
            }
        }

        void Write()
        {
            stripped.MetadataRoot.Accept(metadata_writer);
        }

        public static void StripAssembly(AssemblyDefinition assembly, string file)
        {
            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new AssemblyStripper(assembly, new BinaryWriter(fs)).Strip();
            }
        }
    }
}
