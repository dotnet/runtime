// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    internal static unsafe class PerfMapSymbolReader
    {
        private const int HiddenLineNumber = unchecked((int)0x00feefee);

        [StructLayout(LayoutKind.Sequential)]
        private struct SequencePointInfoNative
        {
            public int lineNumber;
            public int ilOffset;
            public char* fileName;
        }

        /// <summary>
        /// Populate a native buffer with sequence points for the specified method token using the assembly's associated or embedded PDB.
        /// </summary>
        /// <returns>true if at least one sequence point was written; otherwise false.</returns>
        internal static bool GetInfoForMethod([MarshalAs(UnmanagedType.LPUTF8Str)] string assemblyPath, int methodToken, IntPtr points, int size)
        {
            if (string.IsNullOrEmpty(assemblyPath) || points == IntPtr.Zero || size <= 0)
            {
                return false;
            }

            MetadataReaderProvider? provider = TryOpenReaderFromAssemblyFile(assemblyPath);
            if (provider is null)
            {
                return false;
            }

            using (provider)
            {
                MetadataReader reader = provider.GetMetadataReader();
                Handle handle = MetadataTokens.Handle(methodToken);
                if (handle.IsNil || handle.Kind != HandleKind.MethodDefinition)
                {
                    return false;
                }

                MethodDebugInformationHandle methodDebugHandle = ((MethodDefinitionHandle)handle).ToDebugInformationHandle();
                MethodDebugInformation methodInfo = reader.GetMethodDebugInformation(methodDebugHandle);
                if (methodInfo.SequencePointsBlob.IsNil)
                {
                    return false;
                }

                int count = 0;
                foreach (SequencePoint _ in methodInfo.GetSequencePoints())
                {
                    count++;
                }

                if (count == 0)
                {
                    return false;
                }

                SequencePointInfoNative* nativePoints = (SequencePointInfoNative*)points;
                int writeIndex = 0;
                foreach (SequencePoint point in methodInfo.GetSequencePoints())
                {
                    if (writeIndex >= size)
                    {
                        break;
                    }

                    string? documentName = null;
                    if (!point.Document.IsNil)
                    {
                        documentName = reader.GetString(reader.GetDocument(point.Document).Name);
                    }

                    nativePoints[writeIndex].ilOffset = point.Offset;
                    nativePoints[writeIndex].lineNumber = point.StartLine == SequencePoint.HiddenLine ? HiddenLineNumber : point.StartLine;
                    nativePoints[writeIndex].fileName = documentName is null ? null : (char*)Marshal.StringToCoTaskMemUni(documentName);
                    writeIndex++;
                }

                return writeIndex > 0;
            }
        }

        private static MetadataReaderProvider? TryOpenReaderFromAssemblyFile(string assemblyPath)
        {
            using PEReader? peReader = TryGetPEReader(assemblyPath);
            if (peReader is null)
            {
                return null;
            }

            if (peReader.TryOpenAssociatedPortablePdb(assemblyPath, TryOpenFile, out MetadataReaderProvider? provider, out _))
            {
                return provider;
            }

            foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
            {
                if (entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
                {
                    try
                    {
                        return peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
                    }
                    catch (Exception e) when (e is BadImageFormatException || e is IOException)
                    {
                        break;
                    }
                }
            }

            return null;
        }

        private static PEReader? TryGetPEReader(string assemblyPath)
        {
            Stream? peStream = TryOpenFile(assemblyPath);
            if (peStream is null)
            {
                return null;
            }

            return new PEReader(peStream);
        }

        private static Stream? TryOpenFile(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            }
            catch
            {
                return null;
            }
        }
    }
}
