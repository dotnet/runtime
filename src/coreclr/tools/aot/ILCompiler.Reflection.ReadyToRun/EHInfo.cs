// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// If COR_ILMETHOD_SECT_HEADER::Kind() = CorILMethod_Sect_EHTable then the attribute
    /// is a list of exception handling clauses.  There are two formats, fat or small
    /// </summary>
    [Flags]
    public enum CorExceptionFlag
    {
        COR_ILEXCEPTION_CLAUSE_NONE,                    // This is a typed handler
        COR_ILEXCEPTION_CLAUSE_OFFSETLEN = 0x0000,      // Deprecated
        COR_ILEXCEPTION_CLAUSE_DEPRECATED = 0x0000,     // Deprecated
        COR_ILEXCEPTION_CLAUSE_FILTER = 0x0001,         // If this bit is on, then this EH entry is for a filter
        COR_ILEXCEPTION_CLAUSE_FINALLY = 0x0002,        // This clause is a finally clause
        COR_ILEXCEPTION_CLAUSE_FAULT = 0x0004,          // Fault clause (finally that is called on exception only)
        COR_ILEXCEPTION_CLAUSE_DUPLICATED = 0x0008,     // duplicated clause. This clause was duplicated to a funclet which was pulled out of line
        COR_ILEXCEPTION_CLAUSE_SAMETRY = 0x0010,        // This clause covers same try block as the previous one

        COR_ILEXCEPTION_CLAUSE_KIND_MASK = COR_ILEXCEPTION_CLAUSE_FILTER | COR_ILEXCEPTION_CLAUSE_FINALLY | COR_ILEXCEPTION_CLAUSE_FAULT,
    }

    /// <summary>
    /// This class represents a single exception handling clause. It basically corresponds
    /// to IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT in
    /// <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/cordebuginfo.h">src\inc\corhdr.h</a>.
    /// </summary>
    public class EHClause
    {
        /// <summary>
        /// Length of the serialized EH clause in the PE image.
        /// </summary>
        internal const int Length = 6 * sizeof(uint);

        /// <summary>
        /// Flags describing the exception handler.
        /// </summary>
        private CorExceptionFlag Flags;

        /// <summary>
        /// Starting offset of the try block
        /// </summary>
        public uint TryOffset;

        /// <summary>
        /// End offset of the try block
        /// </summary>
        public uint TryEnd;

        /// <summary>
        /// Offset of the exception handler for the try block
        /// </summary>
        public uint HandlerOffset;

        /// <summary>
        /// End offset of the exception handler
        /// </summary>
        public uint HandlerEnd;

        /// <summary>
        /// For type-based exception handlers, this is the type token.
        /// For filter-based exception handlers, this is the filter offset.
        /// </summary>
        public uint ClassTokenOrFilterOffset;

        /// <summary>
        /// Textual representation of the class represented by the class token.
        /// </summary>
        public string ClassName;

        /// <summary>
        /// Read the EH clause from a given file offset in the PE image.
        /// </summary>
        /// <param name="reader">R2R image reader<param>
        /// <param name="offset">Offset of the EH clause in the image</param>
        public EHClause(ReadyToRunReader reader, int offset)
        {
            Flags = (CorExceptionFlag)BitConverter.ToUInt32(reader.Image, offset + 0 * sizeof(uint));
            TryOffset = BitConverter.ToUInt32(reader.Image, offset + 1 * sizeof(uint));
            TryEnd = BitConverter.ToUInt32(reader.Image, offset + 2 * sizeof(uint));
            HandlerOffset = BitConverter.ToUInt32(reader.Image, offset + 3 * sizeof(uint));
            HandlerEnd = BitConverter.ToUInt32(reader.Image, offset + 4 * sizeof(uint));
            ClassTokenOrFilterOffset = BitConverter.ToUInt32(reader.Image, offset + 5 * sizeof(uint));

            if ((Flags & CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_KIND_MASK) == CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_NONE)
            {
                if (reader.Composite)
                {
                    // TODO: EH clauses in composite mode
                    ClassName = "TODO-composite module in EH clause";
                }
                else
                {
                    ClassName = MetadataNameFormatter.FormatHandle(reader.GetGlobalMetadata()?.MetadataReader, MetadataTokens.Handle((int)ClassTokenOrFilterOffset));
                }
            }
        }

        /// <summary>
        /// Emit a textual representation of the EH info to a given text writer.
        /// </summary>
        /// <param name="writer">Output writer for the textual representation</param>
        /// <param name="methodRva">Starting RVA of the runtime function is used to display the try / handler info as RVA intervals</param>
        public void WriteTo(TextWriter writer, int methodRva, bool dumpRva)
        {
            writer.Write($"Flags {(uint)Flags:X2} ");
            writer.Write($"TryOff {TryOffset:X4} ");
            if (dumpRva)
                writer.Write($"(RVA {(TryOffset + methodRva):X4}) ");
            writer.Write($"TryEnd {TryEnd:X4} ");
            if (dumpRva)
                writer.Write($"(RVA {(TryEnd + methodRva):X4}) ");
            writer.Write($"HndOff {HandlerOffset:X4} ");
            if (dumpRva)
                writer.Write($"(RVA {(HandlerOffset + methodRva):X4}) ");
            writer.Write($"HndEnd {HandlerEnd:X4} ");
            if (dumpRva)
                writer.Write($"(RVA {(HandlerEnd + methodRva):X4}) ");
            writer.Write($"ClsFlt {ClassTokenOrFilterOffset:X4}");

            switch (Flags & CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_KIND_MASK)
            {
                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_NONE:
                    writer.Write($" CATCH: {ClassName ?? "null"}");
                    break;

                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FILTER:
                    writer.Write($" FILTER (RVA {(ClassTokenOrFilterOffset + methodRva):X4})");
                    break;

                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FINALLY:
                    writer.Write(" FINALLY");
                    break;

                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FAULT:
                    writer.Write(" FAULT");
                    break;

                default:
                    throw new NotImplementedException(Flags.ToString());
            }

            if ((Flags & CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_DUPLICATED) != (CorExceptionFlag)0)
            {
                writer.Write(" DUPLICATED");
            }

            if ((Flags & CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_SAMETRY) != (CorExceptionFlag)0)
            {
                writer.Write(" SAMETRY");
            }
        }
    }

    /// <summary>
    /// This class represents EH info for a single runtime function. EH info
    /// is located using the map from runtime functions to EH clause lists in
    /// the READYTORUN_SECTION_EXCEPTION_INFO header table.
    /// </summary>
    public class EHInfo
    {
        private readonly ReadyToRunReader _readyToRunReader;
        private readonly int _offset;
        private readonly int _clauseCount;

        /// <summary>
        /// RVA of the EH info in the PE image.
        /// </summary>
        public int RelativeVirtualAddress { get; }

        /// <summary>
        /// Starting RVA of the corresponding runtime function.
        /// </summary>
        internal int MethodRelativeVirtualAddress { get; }

        private List<EHClause> _clauses;

        /// <summary>
        /// List of EH clauses for the runtime function.
        /// </summary>
        public IReadOnlyList<EHClause> EHClauses
        {
            get
            {
                EnsureClauses();
                return _clauses;
            }
        }

        /// <summary>
        /// Construct the EH info for a given runtime method by reading it from a given offset
        /// in the R2R PE executable. The offset is located by looking up the starting
        /// IP address of the runtime function in the READYTORUN_SECTION_EXCEPTION_INFO table.
        /// The length of the
        /// </summary>
        /// <param name="reader">R2R PE image reader</param>
        /// <param name="ehInfoRva">RVA of the EH info</param>
        /// <param name="methodRva">Starting RVA of the runtime function</param>
        /// <param name="offset">File offset of the EH info</param>
        /// <param name="clauseCount">Number of EH info clauses</param>
        public EHInfo(ReadyToRunReader reader, int ehInfoRva, int methodRva, int offset, int clauseCount)
        {
            _readyToRunReader = reader;
            RelativeVirtualAddress = ehInfoRva;
            MethodRelativeVirtualAddress = methodRva;
            _offset = offset;
            _clauseCount = clauseCount;
        }

        private void EnsureClauses()
        {
            if (_clauses != null)
            {
                return;
            }
            _clauses = new List<EHClause>();
            for (int clauseIndex = 0; clauseIndex < _clauseCount; clauseIndex++)
            {
                _clauses.Add(new EHClause(_readyToRunReader, _offset + clauseIndex * EHClause.Length));
            }
        }

        /// <summary>
        /// Emit the textual representation of the EH info into a given writer.
        /// </summary>
        public void WriteTo(TextWriter writer, bool dumpRva)
        {
            foreach (EHClause ehClause in EHClauses)
            {
                ehClause.WriteTo(writer, MethodRelativeVirtualAddress, dumpRva: dumpRva);
                writer.WriteLine();
            }
        }
    }
}
