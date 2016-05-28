// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
extern alias SRE; 

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using Path = SRE::System.IO.Path;  
using BitConverter = SRE::System.BitConverter;  

namespace System.Diagnostics
{
    public class StackTraceSymbols : IDisposable
    {
        private readonly Dictionary<IntPtr, Tuple<MetadataReaderProvider, MetadataReader>> _readerCache;

        /// <summary>
        /// Create an instance of this class.
        /// </summary>
        public StackTraceSymbols()
        {
            _readerCache = new Dictionary<IntPtr, Tuple<MetadataReaderProvider, MetadataReader>>();
        }

        /// <summary>
        /// Clean up any cached providers.
        /// </summary>
        void IDisposable.Dispose()
        {
            foreach (Tuple<MetadataReaderProvider, MetadataReader> tuple in _readerCache.Values)
            {
                tuple.Item1.Dispose();
            }
            _readerCache.Clear();
        }

        /// <summary>
        /// Returns the source file and line number information for the method.
        /// </summary>
        /// <param name="assemblyPath">file path of the assembly or null</param>
        /// <param name="loadedPeAddress">loaded PE image address or zero</param>
        /// <param name="loadedPeSize">loaded PE image size</param>
        /// <param name="inMemoryPdbAddress">in memory PDB address or zero</param>
        /// <param name="inMemoryPdbSize">in memory PDB size</param>
        /// <param name="methodToken">method token</param>
        /// <param name="ilOffset">il offset of the stack frame</param>
        /// <param name="sourceFile">source file return</param>
        /// <param name="sourceLine">line number return</param>
        /// <param name="sourceColumn">column return</param>
        public void GetSourceLineInfo(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize, 
            IntPtr inMemoryPdbAddress, int inMemoryPdbSize, int methodToken, int ilOffset, 
            out string sourceFile, out int sourceLine, out int sourceColumn)
        {
            sourceFile = null;
            sourceLine = 0;
            sourceColumn = 0;

            MetadataReader reader = GetReader(assemblyPath, loadedPeAddress, loadedPeSize, inMemoryPdbAddress, inMemoryPdbSize);
            if (reader != null)
            {
                Handle handle = MetadataTokens.Handle(methodToken);

                if (handle.Kind == HandleKind.MethodDefinition)
                {
                    MethodDebugInformationHandle methodDebugHandle = ((MethodDefinitionHandle)handle).ToDebugInformationHandle();
                    MethodDebugInformation methodInfo = reader.GetMethodDebugInformation(methodDebugHandle);

                    if (!methodInfo.SequencePointsBlob.IsNil)
                    {
                        SequencePointCollection sequencePoints = methodInfo.GetSequencePoints();

                        SequencePoint? bestPointSoFar = null;
                        foreach (SequencePoint point in sequencePoints)
                        {
                            if (point.Offset > ilOffset)
                                break;

                            if (point.StartLine != SequencePoint.HiddenLine)
                                bestPointSoFar = point;
                        }

                        if (bestPointSoFar.HasValue)
                        {
                            sourceLine = bestPointSoFar.Value.StartLine;
                            sourceColumn = bestPointSoFar.Value.StartColumn;
                            sourceFile = reader.GetString(reader.GetDocument(bestPointSoFar.Value.Document).Name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the portable PDB reader for the assembly path
        /// </summary>
        /// <param name="assemblyPath">file path of the assembly or null</param>
        /// <param name="loadedPeAddress">loaded PE image address or zero</param>
        /// <param name="loadedPeSize">loaded PE image size</param>
        /// <param name="inMemoryPdbAddress">in memory PDB address or zero</param>
        /// <param name="inMemoryPdbSize">in memory PDB size</param>
        /// <param name="reader">returns the reader</param>
        /// <returns>reader</returns>
        private MetadataReader GetReader(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize, IntPtr inMemoryPdbAddress, int inMemoryPdbSize)
        {
            if (loadedPeAddress != IntPtr.Zero)
            {
                Tuple<MetadataReaderProvider, MetadataReader> tuple;
                if (_readerCache.TryGetValue(loadedPeAddress, out tuple))
                {
                    return tuple.Item2;
                }
            }

            MetadataReaderProvider provider = null;
            MetadataReader reader = null;

            if (assemblyPath != null)
            {
                uint stamp;
                int age;
                Guid guid;

                string pdbName = GetPdbPathFromPeStream(assemblyPath, loadedPeAddress, loadedPeSize, out age, out guid, out stamp);
                if (pdbName != null && File.Exists(pdbName))
                {
                    Stream pdbStream = File.OpenRead(pdbName);
                    try
                    {
                        provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
                        MetadataReader rdr = provider.GetMetadataReader();

                        // Validate that the PDB matches the assembly version
                        if (age == 1 && IdEquals(rdr.DebugMetadataHeader.Id, guid, stamp))
                        {
                            reader = rdr;
                        }
                    }
                    catch (BadImageFormatException)
                    {
                    }
                }
            }
            else if (inMemoryPdbAddress != IntPtr.Zero && inMemoryPdbSize > 0)
            {
                unsafe
                {
                    try
                    {
                        provider = MetadataReaderProvider.FromPortablePdbImage((byte*)inMemoryPdbAddress.ToPointer(), inMemoryPdbSize);
                        reader = provider.GetMetadataReader();
                    }
                    catch (BadImageFormatException)
                    {
                    }
                }
            }

            if (reader != null)
            {
                if (loadedPeAddress != IntPtr.Zero)
                {
                    _readerCache.Add(loadedPeAddress, Tuple.Create(provider, reader));
                }
            }
            // if there wasn't a reader created, there was an error or no PDB match so dispose of the provider
            else if (provider != null)
            {
                provider.Dispose();
            }

            return reader;
        }

        /// <summary>
        /// Read the pdb file name and assembly version information from the PE file.
        /// </summary>
        /// <param name="assemblyPath">PE file path</param>
        /// <param name="loadedPeAddress">loaded PE image address or zero</param>
        /// <param name="loadedPeSize">loaded PE image size</param>
        /// <param name="age">age</param>
        /// <param name="guid">assembly guid</param>
        /// <param name="stamp">time stamp</param>
        /// <returns>pdb name or null</returns>
        /// <remarks>
        /// loadedPeAddress and loadedPeSize will be used here when/if the PEReader
        /// support runtime loaded images instead of just file images.
        /// </remarks>
        private static unsafe string GetPdbPathFromPeStream(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize, out int age, out Guid guid, out uint stamp)
        {
            if (File.Exists(assemblyPath))
            {
                Stream peStream = File.OpenRead(assemblyPath);

                using (PEReader peReader = new PEReader(peStream))
                {
                    foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
                    {
                        if (entry.Type == DebugDirectoryEntryType.CodeView)
                        {
                            CodeViewDebugDirectoryData codeViewData = peReader.ReadCodeViewDebugDirectoryData(entry);

                            stamp = entry.Stamp;
                            age = codeViewData.Age;
                            guid = codeViewData.Guid;

                            string peDirectory = Path.GetDirectoryName(assemblyPath);
                            return Path.Combine(peDirectory, Path.GetFileName(codeViewData.Path));
                        }
                    }
                }
            }

            stamp = 0;
            age = 0;
            guid = new Guid();
            return null;
        }

        /// <summary>
        /// Returns true if the portable pdb id matches the guid and stamp.
        /// </summary>
        private static bool IdEquals(ImmutableArray<byte> left, Guid rightGuid, uint rightStamp)
        {
            if (left.Length != 20)
            {
                // invalid id
                return false;
            }

            byte[] guidBytes = rightGuid.ToByteArray();
            for (int i = 0; i < guidBytes.Length; i++)
            {
                if (guidBytes[i] != left[i])
                {
                    return false;
                }
            }

            byte[] stampBytes = BitConverter.GetBytes(rightStamp);
            for (int i = 0; i < stampBytes.Length; i++)
            {
                if (stampBytes[i] != left[guidBytes.Length + i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
