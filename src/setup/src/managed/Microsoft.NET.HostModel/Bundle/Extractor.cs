// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    /// Extractor: The functionality to extract the files embedded 
    /// within a bundle to separate files.
    /// </summary>
    public class Extractor
    {
        string OutputDir;
        string BundlePath;

        readonly Trace trace;

        public Extractor(string bundlePath, string outputDir,
                         bool diagnosticOutput = false)
        {
            BundlePath = bundlePath;
            OutputDir = Path.GetFullPath(string.IsNullOrEmpty(outputDir) ? Environment.CurrentDirectory : outputDir);
            trace = new Trace(diagnosticOutput);
        }

        /// <summary>
        /// Extract all files in the bundle to disk
        /// </summary>
        /// <exceptions>
        /// BundleException if the bundle is invalid or malformed.
        /// IOExceptions and ArgumentExceptions from callees flow to the caller.
        /// </exceptions>
        public void ExtractFiles()
        {
            try
            {
                trace.Log($"Bundler version {Bundler.Version}");
                trace.Log($"Extract from file: {BundlePath}");
                trace.Log($"Output Directory: {OutputDir}");

                using (BinaryReader reader = new BinaryReader(File.OpenRead(BundlePath)))
                {
                    Manifest manifest = Manifest.Read(reader);

                    foreach (FileEntry entry in manifest.Files)
                    {
                        trace.Log($"Extract: {entry}");

                        string fileRelativePath = entry.RelativePath.Replace(FileEntry.DirectorySeparatorChar, Path.DirectorySeparatorChar);
                        string filePath = Path.Combine(OutputDir, fileRelativePath);
                        string fileDir = Path.GetDirectoryName(filePath);

                        if ((fileDir != null) && !fileDir.Equals(String.Empty))
                        {
                            Directory.CreateDirectory(fileDir);
                        }

                        reader.BaseStream.Position = entry.Offset;
                        using (BinaryWriter file = new BinaryWriter(File.Create(filePath)))
                        {
                            long size = entry.Size;
                            do
                            {
                                int copySize = (int)(size <= int.MaxValue ? size : int.MaxValue);
                                file.Write(reader.ReadBytes(copySize));
                                size -= copySize;
                            } while (size > 0);
                        }
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // Trying to read non-existant bits in the bundle
                throw new BundleException("Malformed Bundle");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Trying to set file-stream position to an invalid value
                throw new BundleException("Malformed Bundle");
            }
        }
    }
}

