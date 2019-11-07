// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

using Internal.JitInterface;
using Internal.NativeFormat;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// DebugInfoTableNode emits the data structures required to support managed code debugging
    /// for ready to run code. See https://github.com/dotnet/coreclr/blob/master/src/inc/cordebuginfo.h
    /// for the types this information is based on. We store two tables of information in an 
    /// image:
    /// 
    /// Location Information
    ///
    ///     In order to provide source line debugging, generated ready-to-run code is mapped 
    ///     back to the original IL instruction. This maps to OffsetMapping in cordebuginfo.h.
    ///     
    ///     A row in the table looks like this:
    ///     
    ///     Native offset | IL Offset | Flags
    ///     
    ///     Native offset starts at 0 for the beginning of a method. The rows are stored in 
    ///     ascending order of offset and use a delta encoding. That is, each successive row
    ///     stores the difference between the previous native offset and the new one to save
    ///     encoding space.
    ///     
    ///     Flags stores values useful to the debugger. For example, if the offset is a 
    ///     call site, or the stack is empty at that point. These are used for edit and 
    ///     continue. It maps to SourceTypes in cordebuginfo.h.
    ///     
    /// Variable Information
    /// 
    ///     The debugger needs to know, for a given native code range, where the memory 
    ///     backing each variable is stored. That can be in a register, on the stack, or maybe
    ///     even split between a combination of registers and the stack. This maps to 
    ///     NativeVarInfo in cordebuginfo.h.
    ///     
    ///     A row in the table looks like this:
    ///     
    ///     Start Offset | End Offset | Variable Number | Location
    ///     
    ///     Start Offset and End Offset are offsets within the generated native code and are
    ///     used to capture the range of instructions during which the variable is stored
    ///     in the same location.
    ///     
    ///     Variable Number is the index of each variable in the original IL code.
    ///     
    ///     Location is a variant data structure specifying what memory is backing the variable
    ///     for a given native code range. This is a specific register, or a stack offset. This
    ///     maps to VarLoc in cordebuginfo.h.
    ///     
    /// </summary>
    public class DebugInfoTableNode : HeaderTableNode
    {
        public DebugInfoTableNode(TargetDetails target) : base(target) { }

        public override int ClassCode => 1000735112;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunDebugInfoTable");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            NativeWriter writer = new NativeWriter();
            Section section = writer.NewSection();
            VertexArray vertexArray = new VertexArray(section);
            section.Place(vertexArray);
            ReadyToRunCodegenNodeFactory r2rFactory = (ReadyToRunCodegenNodeFactory)factory;

            foreach (MethodWithGCInfo method in r2rFactory.EnumerateCompiledMethods())
            {
                MemoryStream methodDebugBlob = new MemoryStream();
                
                byte[] bounds = CreateBoundsBlobForMethod(method);
                byte[] vars = CreateVarBlobForMethod(method);

                NibbleWriter nibbleWriter = new NibbleWriter();
                nibbleWriter.WriteUInt((uint)(bounds?.Length ?? 0));
                nibbleWriter.WriteUInt((uint)(vars?.Length ?? 0));

                byte[] header = nibbleWriter.ToArray();
                methodDebugBlob.Write(header, 0, header.Length);

                if (bounds?.Length > 0)
                {
                    methodDebugBlob.Write(bounds, 0, bounds.Length);
                }

                if (vars?.Length > 0)
                {
                    methodDebugBlob.Write(vars, 0, vars.Length);
                }

                BlobVertex debugBlob = new BlobVertex(methodDebugBlob.ToArray());

                vertexArray.Set(r2rFactory.RuntimeFunctionsTable.GetIndex(method), new DebugInfoVertex(debugBlob));
            }

            vertexArray.ExpandLayout();

            MemoryStream writerContent = new MemoryStream();
            writer.Save(writerContent);

            return new ObjectData(
                data: writerContent.ToArray(),
                relocs: null,
                alignment: 8,
                definedSymbols: new ISymbolDefinitionNode[] { this });
        }

        private byte[] CreateBoundsBlobForMethod(MethodWithGCInfo method)
        {
            if (method.DebugLocInfos == null || method.DebugLocInfos.Length == 0)
                return null;

            NibbleWriter writer = new NibbleWriter();
            writer.WriteUInt((uint)method.DebugLocInfos.Length);

            uint previousNativeOffset = 0;
            foreach (var locInfo in method.DebugLocInfos)
            {
                writer.WriteUInt(locInfo.nativeOffset - previousNativeOffset);
                writer.WriteUInt(locInfo.ilOffset + 3); // Count of items in Internal.JitInterface.MappingTypes to adjust the IL offset by
                writer.WriteUInt((uint)locInfo.source);

                previousNativeOffset = locInfo.nativeOffset;
            }

            return writer.ToArray();
        }

        private byte[] CreateVarBlobForMethod(MethodWithGCInfo method)
        {
            if (method.DebugVarInfos == null || method.DebugVarInfos.Length == 0)
                return null;

            NibbleWriter writer = new NibbleWriter();
            writer.WriteUInt((uint)method.DebugVarInfos.Length);

            foreach (var nativeVarInfo in method.DebugVarInfos)
            {
                writer.WriteUInt(nativeVarInfo.startOffset);
                writer.WriteUInt(nativeVarInfo.endOffset - nativeVarInfo.startOffset);
                writer.WriteUInt((uint)(nativeVarInfo.varNumber - (int)ILNum.MAX_ILNUM));

                VarLocType varLocType = nativeVarInfo.varLoc.LocationType;
                
                writer.WriteUInt((uint)varLocType);

                switch (varLocType)
                {
                    case VarLocType.VLT_REG:
                    case VarLocType.VLT_REG_FP:
                    case VarLocType.VLT_REG_BYREF:
                        writer.WriteUInt((uint)nativeVarInfo.varLoc.B);
                        break;
                    case VarLocType.VLT_STK:
                    case VarLocType.VLT_STK_BYREF:
                        writer.WriteUInt((uint)nativeVarInfo.varLoc.B);
                        writer.WriteInt(nativeVarInfo.varLoc.C);
                        break;
                    case VarLocType.VLT_REG_REG:
                        writer.WriteUInt((uint)nativeVarInfo.varLoc.B);
                        writer.WriteUInt((uint)nativeVarInfo.varLoc.C);
                        break;
                    case VarLocType.VLT_REG_STK:
                        writer.WriteUInt((uint)nativeVarInfo.varLoc.B);
                        writer.WriteUInt((uint)nativeVarInfo.varLoc.C);
                        writer.WriteInt(nativeVarInfo.varLoc.D);
                        break;
                    case VarLocType.VLT_STK_REG:
                        writer.WriteInt(nativeVarInfo.varLoc.B);
                        writer.WriteUInt((uint)nativeVarInfo.varLoc.C);
                        writer.WriteUInt((uint)nativeVarInfo.varLoc.D);
                        break;
                    case VarLocType.VLT_STK2:
                        writer.WriteUInt((uint)nativeVarInfo.varLoc.B);
                        writer.WriteInt(nativeVarInfo.varLoc.C);
                        break;
                    case VarLocType.VLT_FPSTK:
                        writer.WriteUInt((uint)nativeVarInfo.varLoc.B);
                        break;
                    case VarLocType.VLT_FIXED_VA:
                        writer.WriteUInt((uint)nativeVarInfo.varLoc.B);
                        break;
                    default:
                        throw new BadImageFormatException("Unexpected var loc type");
                }
            }

            return writer.ToArray();
        }
    }
}
