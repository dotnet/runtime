// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.DotNet.Build.Bundle
{
    /// <summary>
    /// Extractor: The functionality to extract the files embedded 
    /// within a bundle to sepearte files.
    /// </summary>
    public class Extractor
    {
        string OutputDir;
        string BundlePath;

        public Extractor(string bundlePath, string outputDir)
        {
            BundlePath = bundlePath;
            OutputDir = outputDir;
        }

        public void Spill()
        {
            try
            {
                if (!File.Exists(BundlePath))
                {
                    throw new BundleException("File not found: " + BundlePath);
                }

                using (BinaryReader reader = new BinaryReader(File.OpenRead(BundlePath)))
                {
                    Manifest manifest = Manifest.Read(reader);

                    foreach (FileEntry entry in manifest.Files)
                    {
                        Program.Log($"Spill: {entry}");

                        string fileRelativePath = entry.RelativePath.Replace(Manifest.DirectorySeparatorChar, Path.DirectorySeparatorChar);
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
                                int copySize = (int)(size % int.MaxValue);
                                file.Write(reader.ReadBytes(copySize));
                                size -= copySize;
                            } while (size > 0);
                        }
                    }
                }
            }
            catch (IOException)
            {
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

