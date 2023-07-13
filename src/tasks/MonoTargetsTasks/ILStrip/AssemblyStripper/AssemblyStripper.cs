// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using CilStrip.Mono.Cecil;
using CilStrip.Mono.Cecil.Binary;
using CilStrip.Mono.Cecil.Cil;
using CilStrip.Mono.Cecil.Metadata;

namespace AssemblyStripper
{
    class CustomAttrRowComparer : IComparer
    {
        public int Compare(object left, object right)
        {
            CustomAttributeRow row_left = (CustomAttributeRow)left;
            CustomAttributeRow row_right = (CustomAttributeRow)right;
            var leftParentCodedIdx = Utilities.CompressMetadataToken(CodedIndex.HasCustomAttribute, row_left.Parent);
            var rightParentCodedIdx = Utilities.CompressMetadataToken(CodedIndex.HasCustomAttribute, row_right.Parent);
            return leftParentCodedIdx.CompareTo(rightParentCodedIdx);
        }
    }

    public class AssemblyStripper
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
            SortCustomAttributes();
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

        // Types that are trimmed away also have their respective rows removed from the 
        // custom attribute table. This introduces holes in their places, causing the table 
        // to no longer be sorted by Parent, corrupting the assembly. Runtimes assume ordering
        // and may fail to locate the attributes set for a particular type. This step sorts 
        // the custom attribute table again.
        void SortCustomAttributes()
        {
            CustomAttributeTable table = (CustomAttributeTable)stripped_tables[CustomAttributeTable.RId];
            if (table == null)
                return;

            table.Rows.Sort(new CustomAttrRowComparer());
        }

        void Write()
        {
            stripped.MetadataRoot.Accept(metadata_writer);
        }

        internal static void StripAssembly(AssemblyDefinition assembly, string file)
        {
            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new AssemblyStripper(assembly, new BinaryWriter(fs)).Strip();
            }
        }

        public static void StripAssembly(string assemblyFile, string outputPath)
        {
            AssemblyDefinition assembly = AssemblyFactory.GetAssembly(assemblyFile);
            AssemblyStripper.StripAssembly(assembly, outputPath);
        }
    }
}
