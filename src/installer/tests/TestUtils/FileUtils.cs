// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class FileUtils
    {
        public static void EnsureFileDirectoryExists(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        public static void CreateEmptyFile(string filePath)
        {
            EnsureFileDirectoryExists(filePath);
            File.WriteAllText(filePath, string.Empty);
        }

        public static void DeleteFileIfPossible(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (System.IO.IOException)
            {
            }
        }

        /// <summary>
        /// Writes JSON content to a file with optional UTF8 BOM.
        /// </summary>
        /// <param name="filePath">The path to the file to write</param>
        /// <param name="jsonContent">The JSON content to write</param>
        /// <param name="withUtf8Bom">Whether to include UTF8 BOM (0xEF, 0xBB, 0xBF) at the beginning</param>
        public static void WriteJsonWithOptionalUtf8Bom(string filePath, string jsonContent, bool withUtf8Bom)
        {
            EnsureFileDirectoryExists(filePath);
            
            if (withUtf8Bom)
            {
                // Write with UTF8 BOM
                byte[] utf8Bom = new byte[] { 0xEF, 0xBB, 0xBF };
                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
                byte[] fileBytes = new byte[utf8Bom.Length + jsonBytes.Length];
                
                Array.Copy(utf8Bom, 0, fileBytes, 0, utf8Bom.Length);
                Array.Copy(jsonBytes, 0, fileBytes, utf8Bom.Length, jsonBytes.Length);
                
                File.WriteAllBytes(filePath, fileBytes);
            }
            else
            {
                // Write without UTF8 BOM (default behavior)
                File.WriteAllText(filePath, jsonContent);
            }
        }
    }
}
