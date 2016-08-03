// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace System.Diagnostics.Debug.SymbolReader
{

    public class SymbolReader
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DebugInfo
        {
            public int lineNumber;
            public int ilOffset;
            public string fileName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MethodDebugInfo
        {
            public IntPtr points;
            public int size;
        }


        /// <summary>
        /// Checks availability of debugging information for given assembly.
        /// </summary>
        /// <param name="assemblyFileName">file name of the assembly</param>
        /// <returns>true if debugging information is available</returns>
        public static bool LoadSymbolsForModule(string assemblyFileName)
        {
            MetadataReader peReader, pdbReader;
            bool found = GetReaders(assemblyFileName, out peReader, out pdbReader);
            peReader = null;
            pdbReader = null;
            return found;
        }
    
        /// <summary>
        /// Returns method token and IL offset for given source line number.
        /// </summary>
        /// <param name="assemblyFileName">file name of the assembly</param>
        /// <param name="fileName">source file name</param>
        /// <param name="lineNumber">source line number</param>
        /// <param name="methToken">method token return</param>
        /// <param name="ilOffset">IL offset return</param>
        /// <returns> true if information is available</returns>
        public static bool ResolveSequencePoint(string assemblyFileName, string fileName, int lineNumber, out int methToken, out int ilOffset)
        {
            MetadataReader peReader, pdbReader;
            methToken = 0;
            ilOffset = 0;
            
            try {
                if (!GetReaders(assemblyFileName, out peReader, out pdbReader))
                    return false;
                
                foreach (MethodDefinitionHandle methodDefHandle in peReader.MethodDefinitions)
                {
                    MethodDebugInformation methodDebugInfo = pdbReader.GetMethodDebugInformation(methodDefHandle);
                    SequencePointCollection sequencePoints = methodDebugInfo.GetSequencePoints();
                    foreach (SequencePoint point in sequencePoints)
                    {
                        string sourceName = pdbReader.GetString(pdbReader.GetDocument(point.Document).Name);
                        if (Path.GetFileName(sourceName) == Path.GetFileName(fileName) && point.StartLine == lineNumber)
                        {
                            methToken = MetadataTokens.GetToken(peReader, methodDefHandle);
                            ilOffset = point.Offset;
                            return true;
                        }
                    }
                }
            }
            finally
            {
                peReader = null;
                pdbReader = null;
            }
            return false;
        }


        /// <summary>
        /// Returns source name, line numbers and IL offsets for given method token.
        /// </summary>
        /// <param name="assemblyFileName">file name of the assembly</param>
        /// <param name="methToken">method token</param>
        /// <param name="debugInfo">structure with debug information return</param>
        /// <returns> true if information is available</returns>
        public static bool GetInfoForMethod(string assemblyFileName, int methodToken, ref MethodDebugInfo debugInfo)
        {
            List<DebugInfo> points = null;
            
            if (!GetDebugInfoForMethod(assemblyFileName, methodToken, out points))
            {
                return false;
            }

            var structSize = Marshal.SizeOf<DebugInfo>();

            debugInfo.size = points.Count;
            var ptr = debugInfo.points;

            foreach (var info in points)
            {
                Marshal.StructureToPtr(info, ptr, false);
                ptr = (IntPtr)(ptr.ToInt64() + structSize);
            }

            return true;
        }


        /// <summary>
        /// Helper method to return source name, line numbers and IL offsets for given method token.
        /// </summary>
        /// <param name="assemblyFileName">file name of the assembly</param>
        /// <param name="methToken">method token</param>
        /// <param name="pints">List of debug information for each sequence point return</param>
        /// <returns> true if information is available</returns>
        private static bool GetDebugInfoForMethod(string assemblyFileName, int methodToken, out List<DebugInfo> points)
        {
            MetadataReader peReader, pdbReader;
            
            points = null;
            try
            {
                if (!GetReaders(assemblyFileName, out peReader, out pdbReader))
                    return false;
                Handle handle = MetadataTokens.Handle(methodToken);
                if (handle.Kind != HandleKind.MethodDefinition)
                    return false;

                points = new List<DebugInfo>();
                MethodDebugInformationHandle methodDebugHandle =
                    ((MethodDefinitionHandle)handle).ToDebugInformationHandle();
                MethodDebugInformation methodDebugInfo = pdbReader.GetMethodDebugInformation(methodDebugHandle);
                SequencePointCollection sequencePoints = methodDebugInfo.GetSequencePoints();

                foreach (SequencePoint point in sequencePoints)
                {
                    if (point.StartLine == 0 || point.StartLine == SequencePoint.HiddenLine)
                        continue;
                    DebugInfo debugInfo = new DebugInfo();

                    debugInfo.lineNumber = point.StartLine;
                    debugInfo.fileName = pdbReader.GetString(pdbReader.GetDocument(point.Document).Name);
                    debugInfo.ilOffset = point.Offset;
                    points.Add(debugInfo);
                }
                return true;
            }
            finally
            {
                peReader = null;
                pdbReader = null;
            }
        }

        /// <summary>
        /// Returns source line number and source file name for given IL offset and method token.
        /// </summary>
        /// <param name="assemblyFileName">file name of the assembly</param>
        /// <param name="methToken">method token</param>
        /// <param name="ilOffset">IL offset</param>
        /// <param name="lineNumber">source line number return</param>
        /// <param name="fileName">source file name return</param>
        /// <returns> true if information is available</returns>
        public static bool GetLineByILOffset(string assemblyFileName, int methodToken, long ilOffset, out int lineNumber, out IntPtr fileName)
        {
            lineNumber = 0;
            fileName = IntPtr.Zero;

            string sourceFileName = null;
            
            if (!GetSourceLineByILOffset(assemblyFileName, methodToken, ilOffset, out lineNumber, out sourceFileName))
                return false;
            
            fileName = Marshal.StringToBSTR(sourceFileName);
            sourceFileName = null;
            return true;
        }
        
        /// <summary>
        /// Helper method to return source line number and source file name for given IL offset and method token.
        /// </summary>
        /// <param name="assemblyFileName">file name of the assembly</param>
        /// <param name="methToken">method token</param>
        /// <param name="ilOffset">IL offset</param>
        /// <param name="lineNumber">source line number return</param>
        /// <param name="fileName">source file name return</param>
        /// <returns> true if information is available</returns>
        private static bool GetSourceLineByILOffset(string assemblyFileName, int methodToken, long ilOffset, out int lineNumber, out string fileName)
        {
            MetadataReader peReader, pdbReader;
            lineNumber = 0;
            fileName = null;

            try
            {
                if (!GetReaders(assemblyFileName, out peReader, out pdbReader))
                    return false;
                Handle handle = MetadataTokens.Handle(methodToken);
                if (handle.Kind != HandleKind.MethodDefinition)
                    return false;

                MethodDebugInformationHandle methodDebugHandle =
                    ((MethodDefinitionHandle)handle).ToDebugInformationHandle();
                MethodDebugInformation methodDebugInfo = pdbReader.GetMethodDebugInformation(methodDebugHandle);
                SequencePointCollection sequencePoints = methodDebugInfo.GetSequencePoints();

                SequencePoint nearestPoint = sequencePoints.GetEnumerator().Current;
                foreach (SequencePoint point in sequencePoints)
                {
                    if (point.Offset < ilOffset)
                        nearestPoint = point;
                    else
                    {
                        if (point.Offset == ilOffset)
                            nearestPoint = point;
                        if (nearestPoint.StartLine == 0 || nearestPoint.StartLine == SequencePoint.HiddenLine)
                            return false;
                        lineNumber = nearestPoint.StartLine;
                        fileName = pdbReader.GetString(pdbReader.GetDocument(nearestPoint.Document).Name);
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                peReader = null;
                pdbReader = null;
            }
        }


        /// <summary>
        /// Returns local variable name for given local index and IL offset.
        /// </summary>
        /// <param name="assemblyFileName">file name of the assembly</param>
        /// <param name="methodToken">method token</param>
        /// <param name="localIndex">local variable index</param>
        /// <param name="localVarName">local variable name return</param>
        /// <returns>true if name has been found</returns>
        public static bool GetLocalVariableName(string assemblyFileName, int methodToken, int localIndex, out IntPtr localVarName)
        {
            localVarName = IntPtr.Zero;

            string localVar = null;
            if (!GetLocalVariableByIndex(assemblyFileName, methodToken, localIndex, out localVar))
                return false;
            
            localVarName = Marshal.StringToBSTR(localVar);
            localVar = null;
            return true;
        }
    
        /// <summary>
        /// Helper method to return local variable name for given local index and IL offset.
        /// </summary>
        /// <param name="assemblyFileName">file name of the assembly</param>
        /// <param name="methodToken">method token</param>
        /// <param name="localIndex">local variable index</param>
        /// <param name="localVarName">local variable name return</param>
        /// <returns>true if name has been found</returns>
        public static bool GetLocalVariableByIndex(string assemblyFileName, int methodToken, int localIndex, out string localVarName)
        {
            MetadataReader peReader, pdbReader;
            localVarName = null;

            try
            {
                if (!GetReaders(assemblyFileName, out peReader, out pdbReader))
                    return false;
                
                Handle handle = MetadataTokens.Handle(methodToken);
                if (handle.Kind != HandleKind.MethodDefinition)
                    return false;

                MethodDebugInformationHandle methodDebugHandle = ((MethodDefinitionHandle)handle).ToDebugInformationHandle();
                LocalScopeHandleCollection localScopes = pdbReader.GetLocalScopes(methodDebugHandle);
                foreach (LocalScopeHandle scopeHandle in localScopes)
                {
                    LocalScope scope = pdbReader.GetLocalScope(scopeHandle);
                    LocalVariableHandleCollection localVars = scope.GetLocalVariables();
                    foreach (LocalVariableHandle varHandle in localVars)
                    {
                        LocalVariable localVar = pdbReader.GetLocalVariable(varHandle);
                        if (localVar.Index == localIndex)
                        {
                            if (localVar.Attributes == LocalVariableAttributes.DebuggerHidden)
                                return false;
                            localVarName = pdbReader.GetString(localVar.Name);
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                peReader = null;
                pdbReader = null;
            }
        }
    
        /// <summary>
        /// Returns metadata readers for assembly PE file and portable PDB.
        /// </summary>
        /// <param name="assemblyFileName">file name of the assembly</param>
        /// <param name="peReader">PE metadata reader return</param>
        /// <param name="pdbReader">PDB metadata reader return</param>
        /// <returns>true if debugging information is available</returns>
        private static bool GetReaders(string assemblyFileName, out MetadataReader peReader, out MetadataReader pdbReader)
        {
            peReader = null;
            pdbReader = null;
            
            if (!File.Exists(assemblyFileName))
            {
                return false;
            }
            Stream peStream = File.OpenRead(assemblyFileName);
            PEReader reader = new PEReader(peStream);
            string pdbPath = null;
            
            foreach (DebugDirectoryEntry entry in reader.ReadDebugDirectory())
            {
                if (entry.Type == DebugDirectoryEntryType.CodeView)
                {
                    CodeViewDebugDirectoryData codeViewData = reader.ReadCodeViewDebugDirectoryData(entry);
                    pdbPath = codeViewData.Path;
                    break;
                }
            }
            if (pdbPath == null)
            {
                return false;
            }
            if (!File.Exists(pdbPath))
            {
                pdbPath = Path.GetFileName(pdbPath);
                if (!File.Exists(pdbPath))
                {
                    return false;
                }
            }
            
            peReader = reader.GetMetadataReader();
            Stream pdbStream = File.OpenRead(pdbPath);
            MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            pdbReader = provider.GetMetadataReader();
            
            return true;
            
        }
    }

}
