// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace System.IO.Tests.Performance
{
    /// <summary>
    /// Benchmarks to validate FileStream buffering optimizations when used with StreamReader/StreamWriter.
    /// These tests demonstrate the performance improvements from disabling FileStream internal buffering
    /// when it's wrapped by StreamReader/StreamWriter (which provide their own buffering).
    /// </summary>
    [MemoryDiagnoser]
    public class FileStreamBufferingBenchmark
    {
        private string _tempDir = null!;
        private string _testFile = null!;
        private string[] _testLines = null!;
        private string _testContent = null!;
        private const int LineCount = 1000;
        private const int ContentSize = 50000; // ~50KB

        [GlobalSetup]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "FileStreamBenchmark_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _testFile = Path.Combine(_tempDir, "test.txt");

            // Generate test data
            _testLines = new string[LineCount];
            StringBuilder contentBuilder = new StringBuilder();
            for (int i = 0; i < LineCount; i++)
            {
                string line = $"This is test line {i} with some content to make it more realistic";
                _testLines[i] = line;
                contentBuilder.AppendLine(line);
            }
            _testContent = contentBuilder.ToString();

            // Create initial file for read tests
            File.WriteAllText(_testFile, _testContent);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch { }
        }

        // ==================== File.WriteAllLinesAsync ====================

        [Benchmark]
        public async Task File_WriteAllLinesAsync()
        {
            string path = Path.Combine(_tempDir, "write_lines.txt");
            await File.WriteAllLinesAsync(path, _testLines);
            File.Delete(path);
        }

        // ==================== File.AppendAllLinesAsync ====================

        [Benchmark]
        public async Task File_AppendAllLinesAsync()
        {
            string path = Path.Combine(_tempDir, "append_lines.txt");
            File.WriteAllText(path, "Initial content\n");
            await File.AppendAllLinesAsync(path, _testLines);
            File.Delete(path);
        }

        // ==================== File.ReadAllTextAsync ====================

        [Benchmark]
        public async Task<string> File_ReadAllTextAsync()
        {
            return await File.ReadAllTextAsync(_testFile);
        }

        // ==================== File.ReadAllLinesAsync ====================

        [Benchmark]
        public async Task<string[]> File_ReadAllLinesAsync()
        {
            return await File.ReadAllLinesAsync(_testFile);
        }

        // ==================== StreamReader(path) ====================

        [Benchmark]
        public async Task<string> StreamReader_PathConstructor()
        {
            using (var reader = new StreamReader(_testFile))
            {
                return await reader.ReadToEndAsync();
            }
        }

        // ==================== StreamWriter(path) ====================

        [Benchmark]
        public async Task StreamWriter_PathConstructor()
        {
            string path = Path.Combine(_tempDir, "streamwriter.txt");
            using (var writer = new StreamWriter(path))
            {
                await writer.WriteAsync(_testContent);
            }
            File.Delete(path);
        }

        // ==================== FileInfo.OpenText ====================

        [Benchmark]
        public async Task<string> FileInfo_OpenText()
        {
            var fileInfo = new FileInfo(_testFile);
            using (var reader = fileInfo.OpenText())
            {
                return await reader.ReadToEndAsync();
            }
        }

        // ==================== FileInfo.CreateText ====================

        [Benchmark]
        public async Task FileInfo_CreateText()
        {
            string path = Path.Combine(_tempDir, "fileinfo_create.txt");
            var fileInfo = new FileInfo(path);
            using (var writer = fileInfo.CreateText())
            {
                await writer.WriteAsync(_testContent);
            }
            File.Delete(path);
        }

        // ==================== FileInfo.AppendText ====================

        [Benchmark]
        public async Task FileInfo_AppendText()
        {
            string path = Path.Combine(_tempDir, "fileinfo_append.txt");
            File.WriteAllText(path, "Initial content\n");
            var fileInfo = new FileInfo(path);
            using (var writer = fileInfo.AppendText())
            {
                await writer.WriteAsync(_testContent);
            }
            File.Delete(path);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<FileStreamBufferingBenchmark>();
        }
    }
}
