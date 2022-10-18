// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using LibObjectFile.Elf;
using NUnit.Framework;

namespace LibObjectFile.Tests.Elf
{
    public abstract class ElfTestBase
    {
        protected static void AssertReadElf(ElfObjectFile elf, string fileName)
        {
            AssertReadElfInternal(elf, fileName);
            AssertReadBack(elf, fileName, readAsReadOnly: false);
            AssertReadBack(elf, fileName, readAsReadOnly: true);
            AssertLsbMsb(elf, fileName);
        }
        
        private static void AssertReadElfInternal(ElfObjectFile elf, string fileName, bool writeFile = true, string context = null)
        {
            if (writeFile)
            {
                using (var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, fileName), FileMode.Create))
                {
                    elf.Write(stream);
                    stream.Flush(); 
                    Assert.AreEqual(stream.Length, (long)elf.Layout.TotalSize);
                }
            }

            var stringWriter = new StringWriter();
            elf.Print(stringWriter);

            var result = stringWriter.ToString().Replace("\r\n", "\n").TrimEnd();
            Console.WriteLine(result);
            var readelf = LinuxUtil.ReadElf(fileName).TrimEnd();
            if (readelf != result)
            {
                Console.WriteLine("=== Expected:");
                Console.WriteLine(readelf);
                Console.WriteLine("=== Result:");
                Console.WriteLine(result);
                if (context != null)
                {
                    Assert.AreEqual(readelf, result, context);
                }
                else
                {
                    Assert.AreEqual(readelf, result);
                }
            }
        }
        
        private static void AssertReadBack(ElfObjectFile elf, string fileName, bool readAsReadOnly)
        {
            ElfObjectFile newObjectFile;

            var filePath = Path.Combine(Environment.CurrentDirectory, fileName);
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                newObjectFile = ElfObjectFile.Read(stream, new ElfReaderOptions() {ReadOnly = readAsReadOnly});

                Console.WriteLine();
                Console.WriteLine("=============================================================================");
                Console.WriteLine($"readback {(readAsReadOnly ? "as readonly" : "as readwrite")}");
                Console.WriteLine("=============================================================================");
                Console.WriteLine();

                AssertReadElfInternal(newObjectFile, fileName, false, $"Unexpected error while reading back {fileName}");

                var originalBuffer = File.ReadAllBytes(filePath);
                var memoryStream = new MemoryStream();
                newObjectFile.Write(memoryStream);
                var newBuffer = memoryStream.ToArray();

                Assert.AreEqual(originalBuffer, newBuffer, "Invalid binary diff between write -> (original) -> read -> write -> (new)");
            }
        }

        private static void AssertLsbMsb(ElfObjectFile elf, string fileName)
        {
            Console.WriteLine();
            Console.WriteLine("*****************************************************************************");
            Console.WriteLine("LSB to MSB");
            Console.WriteLine("*****************************************************************************");
            Console.WriteLine();

            elf.Encoding = ElfEncoding.Msb;
            var newFileName = Path.GetFileNameWithoutExtension(fileName) + "_msb.elf";
            AssertReadElfInternal(elf, newFileName);
            AssertReadBack(elf, newFileName, readAsReadOnly: false);
            AssertReadBack(elf, newFileName, readAsReadOnly: true);
        }
    }
}