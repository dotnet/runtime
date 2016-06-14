// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace System.Diagnostics.Debug.SymbolReader
{

    public class SymbolReader
    {
    
        /// <summary>
        /// Checks availability of debugging information for given assembly.
        /// </summary>
        /// <param name="assemblyFileName">file name of the assembly</param>
        /// <returns>true if debugging information is available</returns>
        public static bool LoadSymbolsForModule(string assemblyFileName)
        {
            MetadataReader peReader, pdbReader;
            
            return GetReaders(assemblyFileName, out peReader, out pdbReader);
        }
    
        /// <summary>
        /// Returns method token and IL offset for given source line number.
        /// </summary>
        /// <param name="assemblyFileName">file name of the assembly</param>
        /// <param name="fileName">source file name</param>
        /// <param name="lineNumber">source line number</param>
        /// <param name="methToken">method token return</param>
        /// <param name="ilOffset">IL offset return</param>
        public static void ResolveSequencePoint(string assemblyFileName, string fileName, int lineNumber, out int methToken, out int ilOffset)
        {
            MetadataReader peReader, pdbReader;
            methToken = 0;
            ilOffset = 0;
            
            if (!GetReaders(assemblyFileName, out peReader, out pdbReader))
                return;
            
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
                        return;
                    }
                }
                
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
