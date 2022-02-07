// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public readonly struct NativeSequencePoint
    {
        public readonly int NativeOffset;
        public readonly string FileName;
        public readonly int LineNumber;
        public readonly int ColNumber;

        public NativeSequencePoint(int nativeOffset, string fileName, int lineNumber, int colNumber = 0)
        {
            NativeOffset = nativeOffset;
            FileName = fileName;
            LineNumber = lineNumber;
            ColNumber = colNumber;
        }
    }
    
    public interface INodeWithDebugInfo
    {
        bool IsStateMachineMoveNextMethod { get; }

        IEnumerable<NativeSequencePoint> GetNativeSequencePoints();

        public IEnumerable<DebugVarInfoMetadata> GetDebugVars();
    }

    public readonly struct DebugVarInfoMetadata
    {
        public readonly string Name;
        public readonly TypeDesc Type;
        public readonly bool IsParameter;
        public readonly DebugVarInfo DebugVarInfo;

        public DebugVarInfoMetadata(string name, TypeDesc type, bool isParameter, DebugVarInfo info)
            => (Name, Type, IsParameter, DebugVarInfo) = (name, type, isParameter, info);
    }

    public readonly struct DebugVarInfo
    {
        public readonly uint VarNumber;
        public readonly DebugVarRangeInfo[] Ranges;

        public DebugVarInfo(uint varNumber, DebugVarRangeInfo[] ranges)
            => (VarNumber, Ranges) = (varNumber, ranges);
    }

    public readonly struct DebugVarRangeInfo
    {
        public readonly uint StartOffset;
        public readonly uint EndOffset;
        public readonly VarLoc VarLoc;

        public DebugVarRangeInfo(uint startOffset, uint endOffset, VarLoc varLoc)
            => (StartOffset, EndOffset, VarLoc) = (startOffset, endOffset, varLoc);
    }

    public static class WellKnownLineNumber
    {
        /// <summary>
        /// Informs the debugger that it should step through the annotated sequence point.
        /// </summary>
        public const int DebuggerStepThrough = 0xF00F00;

        /// <summary>
        /// Informs the debugger that it should step into the annotated sequence point.
        /// </summary>
        public const int DebuggerStepIn = 0xFEEFEE;
    }
}
