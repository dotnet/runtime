using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JitBench
{
    public static class FileTasks
    {
        public async static Task DownloadAndUnzip(string remotePath, string localExpandedDirPath, ITestOutputHelper output, bool deleteTempFiles=true)
        {
            string tempDownloadPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(remotePath));
            Download(remotePath, tempDownloadPath, output);
            await Unzip(tempDownloadPath, localExpandedDirPath, output, true);
        }

        public static void Download(string remotePath, string localPath, ITestOutputHelper output)
        {
            output.WriteLine("Downloading: " + remotePath + " -> " + localPath);
            Directory.CreateDirectory(Path.GetDirectoryName(localPath));
            using (var client = new HttpClient())
            {
                using (FileStream localStream = File.Create(localPath))
                {
                    using (Stream stream = client.GetStreamAsync(remotePath).Result)
                        stream.CopyTo(localStream);
                    localStream.Flush();
                }
            }
        }

        public static async Task Unzip(string zipPath, string expandedDirPath, ITestOutputHelper output, bool deleteZippedFiles=true, string tempTarPath=null)
        {
            if (zipPath.EndsWith(".zip"))
            {
                await FileTasks.UnWinZip(zipPath, expandedDirPath, output);
                if (deleteZippedFiles)
                {
                    File.Delete(zipPath);
                }
            }
            else if (zipPath.EndsWith(".tar.gz"))
            {
                bool deleteTar = deleteZippedFiles;
                if(tempTarPath == null)
                {
                    tempTarPath = Path.Combine(Path.GetTempPath(), zipPath.Substring(0, zipPath.Length - ".gz".Length));
                    deleteTar = true;
                }
                await UnGZip(zipPath, tempTarPath, output);
                await UnTar(tempTarPath, expandedDirPath, output);
                if(deleteZippedFiles)
                {
                    File.Delete(zipPath);
                }
                if(deleteTar)
                {
                    File.Delete(tempTarPath);
                }
            }
            else
            {
                output.WriteLine("Unsupported compression format: " + zipPath);
                throw new NotSupportedException("Unsupported compression format: " + zipPath);
            }
        }

        public static async Task UnWinZip(string zipPath, string expandedDirPath, ITestOutputHelper output)
        {
            output.WriteLine("Unziping: " + zipPath + " -> " + expandedDirPath);
            using (FileStream zipStream = File.OpenRead(zipPath))
            {
                ZipArchive zip = new ZipArchive(zipStream);
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    if(entry.CompressedLength == 0)
                    {
                        continue;
                    }
                    string extractedFilePath = Path.Combine(expandedDirPath, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(extractedFilePath));
                    using (Stream zipFileStream = entry.Open())
                    {
                        using (FileStream extractedFileStream = File.OpenWrite(extractedFilePath))
                        {
                            await zipFileStream.CopyToAsync(extractedFileStream);
                        }
                    }
                }
            }
        }

        public async static Task UnGZip(string gzipPath, string expandedFilePath, ITestOutputHelper output)
        {
            output.WriteLine("Unziping: " + gzipPath + " -> " + expandedFilePath);
            using (FileStream gzipStream = File.OpenRead(gzipPath))
            {
                using (GZipStream expandedStream = new GZipStream(gzipStream, CompressionMode.Decompress))
                {
                    using (FileStream targetFileStream = File.OpenWrite(expandedFilePath))
                    {
                        await expandedStream.CopyToAsync(targetFileStream);
                    }
                }
            }
        }

        public async static Task UnTar(string tarPath, string expandedDirPath, ITestOutputHelper output)
        {
            Directory.CreateDirectory(expandedDirPath);
            string tarToolPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                tarToolPath = "/bin/tar";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                tarToolPath = "/usr/bin/tar";
            }
            else
            {
                throw new NotSupportedException("Unknown where this OS stores the tar executable");
            }

            await new ProcessRunner(tarToolPath, "-xf " + tarPath).
                   WithWorkingDirectory(expandedDirPath).
                   WithLog(output).
                   WithExpectedExitCode(0).
                   Run();
        }

        public static void DirectoryCopy(string sourceDir, string destDir, ITestOutputHelper output = null, bool overwrite = true)
        {
            if(output != null)
            {
                output.WriteLine("Copying " + sourceDir + " -> " + destDir);
            }
            
            DirectoryInfo dir = new DirectoryInfo(sourceDir);

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDir, file.Name);
                file.CopyTo(temppath, overwrite);
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDir, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, null, overwrite);
            }
        }

        public static void DeleteDirectory(string path, ITestOutputHelper output)
        {
            output.WriteLine("Deleting " + path);
            int retries = 10;
            for(int i = 0; i < retries; i++)
            {
                if(!Directory.Exists(path))
                {
                    return;
                }
                try
                {
                    // On some systems, directories/files created programmatically are created with attributes
                    // that prevent them from being deleted. Set those attributes to be normal
                    SetAttributesNormal(path);
                    Directory.Delete(path, true);
                    return;
                }
                catch(IOException e) when (i < retries-1)
                {
                    output.WriteLine($"    Attempt #{i+1} failed: {e.Message}");
                }
                catch(UnauthorizedAccessException e) when (i < retries - 1)
                {
                    output.WriteLine($"    Attempt #{i + 1} failed: {e.Message}");
                }
                // if something has a transient lock on the file waiting may resolve the issue
                Thread.Sleep((i+1) * 10);
            }
        }

        public static void SetAttributesNormal(string path)
        {
            foreach (var subDir in Directory.GetDirectories(path))
            {
                SetAttributesNormal(subDir);
            }
            foreach (var file in Directory.GetFiles(path))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
        }

        public static void MoveDirectory(string sourceDirName, string destDirName, ITestOutputHelper output)
        {
            if (output != null)
            {
                output.WriteLine("Moving " + sourceDirName + " -> " + destDirName);
            }
            int retries = 10;
            for (int i = 0; i < retries; i++)
            {
                if (!Directory.Exists(sourceDirName) && Directory.Exists(destDirName))
                {
                    return;
                }
                try
                {
                    Directory.Move(sourceDirName, destDirName);
                    return;
                }
                catch (IOException e) when (i < retries - 1)
                {
                    output.WriteLine($"    Attempt #{i + 1} failed: {e.Message}");
                }
                catch (UnauthorizedAccessException e) when (i < retries - 1)
                {
                    output.WriteLine($"    Attempt #{i + 1} failed: {e.Message}");
                }
                // if something has a transient lock on the file waiting may resolve the issue
                Thread.Sleep((i + 1) * 10);
            }
        }

        public static void MoveFile(string sourceFileName, string destFileName, ITestOutputHelper output)
        {
            if (output != null)
            {
                output.WriteLine("Moving " + sourceFileName + " -> " + destFileName);
            }
            int retries = 10;
            for (int i = 0; i < retries; i++)
            {
                if (!File.Exists(sourceFileName) && File.Exists(destFileName))
                {
                    return;
                }
                try
                {
                    File.Move(sourceFileName, destFileName);
                    return;
                }
                catch (IOException e) when (i < retries - 1)
                {
                    output.WriteLine($"    Attempt #{i + 1} failed: {e.Message}");
                }
                catch (UnauthorizedAccessException e) when (i < retries - 1)
                {
                    output.WriteLine($"    Attempt #{i + 1} failed: {e.Message}");
                }
                // if something has a transient lock on the file waiting may resolve the issue
                Thread.Sleep((i + 1) * 10);
            }
        }

        public static void CreateDirectory(string path, ITestOutputHelper output)
        {
            output.WriteLine("Creating " + path);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
