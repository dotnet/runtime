// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace R2RTest
{
    public enum SerpCompositeScenario
    {
        /// <summary>
        /// Compiles all Serp, Asp.Net, and Framework assemblies in their own version bubble
        /// </summary>
        NoComposite,
        /// <summary>
        /// Compiles Serp Core, Asp.Net, and Framework into their own composite images. Compiles Serp packages assemblies individually.
        /// </summary>
        SerpAspNetSharedFramework,
        /// <summary>
        /// Composite image containing Serp Core, Asp.Net, Shared Framework, plus composite image containing package assemblies
        /// </summary>
        SerpAspNetSharedFrameworkCompositeAndPackagesComposite,
    }

    class CompileSerpCommand
    {
        private static readonly string BackupFolder  = "backup";
        private static readonly string CompileFolder  = "compile";
        private static readonly string FrameworkCompositeFilename = "framework-r2r.dll";
        private static readonly string PackagesCompositeFilename = "packages-r2r.dll";

        private List<string> _packageCompileAssemblies;
        private List<string> _packageReferenceAssemblies;
        private List<string> _coreCompileAssemblies = new List<string>();
        private List<string> _coreReferenceAssemblies = new List<string>();
        private List<string> _frameworkCompileAssemblies = new List<string>();
        private List<string> _frameworkReferenceAssemblies = new List<string>();
        private List<string> _aspCompileAssemblies = new List<string>();
        private List<string> _aspReferenceAssemblies = new List<string>();
        private string SerpDir { get; set; }
        private string BinDir { get;set; }
        private BuildOptions _options;

        public CompileSerpCommand(BuildOptions options)
        {
            // This command does not work in the context of an app, just a loose set of rsp files so don't execute anything we compile
            options.NoJit = true;
            options.NoEtw = true;
            options.Release = true;
            options.LargeBubble = true;

            _options = options;

            if (_options.InputDirectory == null)
            {
                throw new ArgumentException("Specify --response-file or --input-directory containing multiple response files.");
            }

            if (_options.CoreRootDirectory == null)
            {
                throw new ArgumentException("--core-root-directory (--cr) is a required argument.");
            }

            if (_options.AspNetPath == null || !File.Exists(Path.Combine(_options.AspNetPath.FullName, "Microsoft.AspNetCore.dll")))
            {
                throw new ArgumentException($"Error: Asp.NET Core path must contain Microsoft.AspNetCore.dll");
            }
            
            SerpDir = _options.InputDirectory.FullName;
            BinDir = Path.Combine(SerpDir, "bin");

            if (!File.Exists(Path.Combine(SerpDir, "runserp.cmd")))
            {
                throw new ArgumentException($"Error: InputDirectory must point at a SERP build. Could not find {Path.Combine(SerpDir, "runserp.cmd")}");
            }

            string allowListFilePath = Path.Combine(SerpDir, "App_Data", "AllowedDllList.txt");
            if (!File.Exists(allowListFilePath))
            {
                throw new ArgumentException($"File {allowListFilePath} was not found");
            }

            // Add all assemblies from the various SERP packages (filtered by ShouldInclude)
            _packageCompileAssemblies = Directory.GetFiles(Path.Combine(SerpDir, "App_Data\\Answers\\Services\\Packages"), "*.dll", SearchOption.AllDirectories)
                    .Where((string x) => ShouldInclude(x))
                    .ToList();
            _packageReferenceAssemblies = new List<string>();
            {
                HashSet<string> packageReferenceAssemblyDirectories = new HashSet<string>();
                foreach (var binFile in _packageCompileAssemblies)
                {
                    var directory = Path.GetDirectoryName(binFile);
                    if (!packageReferenceAssemblyDirectories.Contains(directory))
                        packageReferenceAssemblyDirectories.Add(directory);
                }

                foreach (string binFile in ResolveReferences(packageReferenceAssemblyDirectories))
                {
                    _packageReferenceAssemblies.Add(binFile);
                }
            }

            _coreCompileAssemblies = new List<string>();
            _coreReferenceAssemblies = new List<string>();
            {
                // Add an allow-list of assemblies from bin. This unified list includes binaries from /bin and /App_data so filter just the /bin assemblies
                foreach (string item in new HashSet<string>(File.ReadAllLines(allowListFilePath)))
                {
                    string binAssembly = Path.Combine(BinDir, item);
                    if (File.Exists(binAssembly) &&
                        !FrameworkExclusion.Exclude(Path.GetFileNameWithoutExtension(binAssembly), CompilerIndex.CPAOT, out string reason))
                    {
                        _coreCompileAssemblies.Add(binAssembly);
                    }
                }

                HashSet<string> coreReferenceAssemblyDirectories = new HashSet<string>();
                foreach (var binFile in _coreCompileAssemblies)
                {
                    var directory = Path.GetDirectoryName(binFile);
                    if (!coreReferenceAssemblyDirectories.Contains(directory))
                        coreReferenceAssemblyDirectories.Add(directory);
                }

                foreach (string binFile in ResolveReferences(coreReferenceAssemblyDirectories))
                {
                    _coreReferenceAssemblies.Add(binFile);
                }
            }

            _frameworkCompileAssemblies = new List<string>();
            _frameworkReferenceAssemblies = new List<string>();
            {
                foreach (string frameworkDll in ComputeManagedAssemblies.GetManagedAssembliesInFolder(options.CoreRootDirectory.FullName, "System.*.dll"))
                {
                    string simpleName = Path.GetFileNameWithoutExtension(frameworkDll);
                    if (!FrameworkExclusion.Exclude(simpleName, CompilerIndex.CPAOT, out string reason))
                    {
                        _frameworkCompileAssemblies.Add(frameworkDll);
                    }
                }
                foreach (string frameworkDll in ComputeManagedAssemblies.GetManagedAssembliesInFolder(options.CoreRootDirectory.FullName, "Microsoft.*.dll"))
                {
                    string simpleName = Path.GetFileNameWithoutExtension(frameworkDll);
                    if (!FrameworkExclusion.Exclude(simpleName, CompilerIndex.CPAOT, out string reason))
                    {
                        _frameworkCompileAssemblies.Add(frameworkDll);
                    }
                }
                _frameworkCompileAssemblies.Add(Path.Combine(options.CoreRootDirectory.FullName, "mscorlib.dll"));
                _frameworkCompileAssemblies.Add(Path.Combine(options.CoreRootDirectory.FullName, "netstandard.dll"));
                _frameworkReferenceAssemblies.AddRange(ComputeManagedAssemblies.GetManagedAssembliesInFolder(options.CoreRootDirectory.FullName, "System.*.dll"));
                _frameworkReferenceAssemblies.AddRange(ComputeManagedAssemblies.GetManagedAssembliesInFolder(options.CoreRootDirectory.FullName, "Microsoft.*.dll"));
                _frameworkReferenceAssemblies.Add(Path.Combine(options.CoreRootDirectory.FullName, "mscorlib.dll"));
                _frameworkReferenceAssemblies.Add(Path.Combine(options.CoreRootDirectory.FullName, "netstandard.dll"));
            }

            _aspCompileAssemblies = new List<string>();
            _aspReferenceAssemblies = new List<string>();
            {
                _aspCompileAssemblies.AddRange(ComputeManagedAssemblies.GetManagedAssembliesInFolder(options.AspNetPath.FullName, "Microsoft.AspNetCore.*.dll"));
                _aspCompileAssemblies.AddRange(ComputeManagedAssemblies.GetManagedAssembliesInFolder(options.AspNetPath.FullName, "Microsoft.Extensions.*.dll"));
                _aspCompileAssemblies.Add(Path.Combine(options.AspNetPath.FullName, "Microsoft.JSInterop.dll"));
                _aspCompileAssemblies.Add(Path.Combine(options.AspNetPath.FullName, "Microsoft.Net.Http.Headers.dll"));
                _aspCompileAssemblies.Add(Path.Combine(options.AspNetPath.FullName, "Microsoft.Win32.SystemEvents.dll"));
                _aspCompileAssemblies.Add(Path.Combine(options.AspNetPath.FullName, "System.Diagnostics.EventLog.dll"));
                _aspCompileAssemblies.Add(Path.Combine(options.AspNetPath.FullName, "System.Drawing.Common.dll"));
                _aspCompileAssemblies.Add(Path.Combine(options.AspNetPath.FullName, "System.IO.Pipelines.dll"));
                _aspCompileAssemblies.Add(Path.Combine(options.AspNetPath.FullName, "System.Security.Cryptography.Pkcs.dll"));
                _aspCompileAssemblies.Add(Path.Combine(options.AspNetPath.FullName, "System.Security.Cryptography.Xml.dll"));
                _aspCompileAssemblies.Add(Path.Combine(options.AspNetPath.FullName, "System.Security.Permissions.dll"));
                _aspCompileAssemblies.Add(Path.Combine(options.AspNetPath.FullName, "System.Windows.Extensions.dll"));

                _aspReferenceAssemblies = new List<string>(_aspCompileAssemblies);
            }

        }
        public int CompileSerpAssemblies()
        {
            Console.WriteLine($"Compiling serp in {_options.CompositeScenario} scenario");

            string serpRoot = Directory.GetParent(SerpDir).Parent.Parent.Parent.FullName;
            string compileOutRoot = Path.Combine(serpRoot, CompileFolder);
            if (Directory.Exists(compileOutRoot))
                Directory.Delete(compileOutRoot, true);
            
            // Composite FX, Composite ASP.NET, Composite Serp core, Individual package assemblies
            List<ProcessInfo> fileCompilations = new List<ProcessInfo>();

            bool combinedComposite = false;
            bool compositeFramework = false;
            bool compositeAspNet = false;
            bool compositeSerpCore = false;
            if (_options.CompositeScenario == SerpCompositeScenario.SerpAspNetSharedFramework)
            {
                compositeFramework = true;
                compositeAspNet = true;
                compositeSerpCore = true;
            }
            if (_options.CompositeScenario == SerpCompositeScenario.SerpAspNetSharedFrameworkCompositeAndPackagesComposite)
            {
                combinedComposite = true;
            }

            // Single composite image for Serp, Asp, Fx
            {
                if (combinedComposite)
                {
                    List<string> combinedCompileAssemblies = new List<string>();
                    HashSet<string> simpleNameList = new HashSet<string>();
                    combinedCompileAssemblies.AddRange(FilterAssembliesNoSimpleNameDuplicates(simpleNameList, _coreCompileAssemblies));
                    combinedCompileAssemblies.AddRange(FilterAssembliesNoSimpleNameDuplicates(simpleNameList, _aspCompileAssemblies));
                    combinedCompileAssemblies.AddRange(FilterAssembliesNoSimpleNameDuplicates(simpleNameList, _frameworkCompileAssemblies));

                    List<string> combinedCompileAssembliesBackup = BackupAndUseOriginalAssemblies(serpRoot, combinedCompileAssemblies);
                    string frameworkCompositeDll = Path.Combine(_options.CoreRootDirectory.FullName, FrameworkCompositeFilename);
                    if (File.Exists(frameworkCompositeDll))
                        File.Delete(frameworkCompositeDll);
                    
                    string frameworkCompositeDllCompile = GetCompileFile(serpRoot, frameworkCompositeDll);
                    Crossgen2RunnerOptions crossgen2Options = new Crossgen2RunnerOptions() { Composite = true, CompositeRoot = GetBackupFile(serpRoot, SerpDir) };
                    var runner = new Crossgen2Runner(_options, crossgen2Options, combinedCompileAssembliesBackup);
                    var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, frameworkCompositeDllCompile, combinedCompileAssembliesBackup));
                    fileCompilations.Add(compilationProcess);
                }
            }

            if (!combinedComposite)
            {
                // Composite FX
                {
                    List<string> frameworkCompileAssembliesBackup = BackupAndUseOriginalAssemblies(serpRoot, _frameworkCompileAssemblies);
                    string frameworkCompositeDll = Path.Combine(_options.CoreRootDirectory.FullName, FrameworkCompositeFilename);
                    if (File.Exists(frameworkCompositeDll))
                        File.Delete(frameworkCompositeDll);

                    // Always restore the framework from the backup if present first since we run CG2 on it
                    var backupFrameworkDir = Path.GetDirectoryName(GetBackupFile(serpRoot, frameworkCompositeDll));
                    var backedUpFiles = Directory.GetFiles(backupFrameworkDir, "*.dll", SearchOption.AllDirectories);
                    foreach (var file in backedUpFiles)
                    {
                        string destinationFile = GetOriginalFile(serpRoot, file);
                        File.Copy(file, destinationFile, true);
                    }

                    if (compositeFramework)
                    {
                        string frameworkCompositeDllCompile = GetCompileFile(serpRoot, frameworkCompositeDll);
                        Crossgen2RunnerOptions crossgen2Options = new Crossgen2RunnerOptions() { Composite = true };
                        var runner = new Crossgen2Runner(_options, crossgen2Options, new List<string>());
                        var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, frameworkCompositeDllCompile, frameworkCompileAssembliesBackup));
                        fileCompilations.Add(compilationProcess);
                    }
                    else
                    {
                        Crossgen2RunnerOptions crossgen2Options = new Crossgen2RunnerOptions() { Composite = false };
                        var runner = new Crossgen2Runner(_options, crossgen2Options, new List<string>());
                        foreach (string assembly in frameworkCompileAssembliesBackup)
                        {
                            string dllCompile = GetCompileFile(serpRoot, assembly);
                            var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, dllCompile, new string[] { assembly }));
                            fileCompilations.Add(compilationProcess);
                        }
                    }
                }

                // Composite Asp.Net
                {
                    List<string> aspCombinedReferences = new List<string>();
                    aspCombinedReferences.AddRange(_aspReferenceAssemblies);
                    aspCombinedReferences.AddRange(_frameworkCompileAssemblies);
                    List<string> aspCombinedReferencesBackup = BackupAndUseOriginalAssemblies(serpRoot, aspCombinedReferences);
                    List<string> aspCompileAssembliesBackup = BackupAndUseOriginalAssemblies(serpRoot, _aspCompileAssemblies);
                    string aspCompositeDll = Path.Combine(_options.AspNetPath.FullName, "asp-r2r.dll");
                    if (File.Exists(aspCompositeDll))
                        File.Delete(aspCompositeDll);

                    if (compositeAspNet)
                    {
                        string aspCompositeDllCompile = GetCompileFile(serpRoot, aspCompositeDll);
                        Crossgen2RunnerOptions crossgen2Options = new Crossgen2RunnerOptions() { Composite = true, PartialComposite = true };
                        var runner = new Crossgen2Runner(_options, crossgen2Options, aspCombinedReferencesBackup);
                        var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, aspCompositeDllCompile, aspCompileAssembliesBackup));
                        fileCompilations.Add(compilationProcess);
                    }
                    else
                    {
                        Crossgen2RunnerOptions crossgen2Options = new Crossgen2RunnerOptions() { Composite = false };
                        var runner = new Crossgen2Runner(_options, crossgen2Options, aspCombinedReferencesBackup);
                        foreach (string assembly in aspCompileAssembliesBackup)
                        {
                            string dllCompile = GetCompileFile(serpRoot, assembly);
                            var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, dllCompile, new string[] { assembly }));
                            fileCompilations.Add(compilationProcess);
                        }
                    }
                }

                // Composite Serp core
                {
                    List<string> coreCombinedReferences = new List<string>();
                    coreCombinedReferences.AddRange(_coreReferenceAssemblies);
                    coreCombinedReferences.AddRange(_aspReferenceAssemblies);
                    coreCombinedReferences.AddRange(_frameworkReferenceAssemblies);
                    List<string> coreCombinedReferencesBackup = BackupAndUseOriginalAssemblies(serpRoot, coreCombinedReferences);
                    List<string> coreCompileAssembliesBackup = BackupAndUseOriginalAssemblies(serpRoot, _coreCompileAssemblies);
                    string serpCompositeDll = Path.Combine(BinDir, "serp-r2r.dll");
                    if (File.Exists(serpCompositeDll))
                        File.Delete(serpCompositeDll);

                    if (compositeSerpCore)
                    {
                        string coreCompositeDllCompile = GetCompileFile(serpRoot, serpCompositeDll);
                        Crossgen2RunnerOptions crossgen2Options = new Crossgen2RunnerOptions() { Composite = true, PartialComposite = true };
                        var runner = new Crossgen2Runner(_options, crossgen2Options, coreCombinedReferencesBackup);
                        var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, coreCompositeDllCompile, coreCompileAssembliesBackup));
                        fileCompilations.Add(compilationProcess);
                    }
                    else
                    {
                        Crossgen2RunnerOptions crossgen2Options = new Crossgen2RunnerOptions() { Composite = false };
                        var runner = new Crossgen2Runner(_options, crossgen2Options, coreCombinedReferencesBackup);
                        foreach (string assembly in coreCompileAssembliesBackup)
                        {
                            string dllCompile = GetCompileFile(serpRoot, assembly);
                            var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, dllCompile, new string[] { assembly }));
                            fileCompilations.Add(compilationProcess);
                        }
                    }
                }
            }

            string packagesCompositeDirectory = null;
            // Individual Serp package assemblies
            {
                List<string> packageCombinedReferences = new List<string>();
                HashSet<string> simpleNameList = new HashSet<string>();
                packageCombinedReferences.AddRange(FilterAssembliesNoSimpleNameDuplicates(simpleNameList, _packageReferenceAssemblies));
                packageCombinedReferences.AddRange(FilterAssembliesNoSimpleNameDuplicates(simpleNameList, _coreReferenceAssemblies));
                packageCombinedReferences.AddRange(FilterAssembliesNoSimpleNameDuplicates(simpleNameList, _aspReferenceAssemblies));
                packageCombinedReferences.AddRange(FilterAssembliesNoSimpleNameDuplicates(simpleNameList, _frameworkReferenceAssemblies));
                List<string> packageCombinedReferencesBackup = BackupAndUseOriginalAssemblies(serpRoot, packageCombinedReferences);
                List<string> packageCompileAssembliesBackup = BackupAndUseOriginalAssemblies(serpRoot, _packageCompileAssemblies);

                string packagesCompositeDll = Path.Combine(_options.CoreRootDirectory.FullName, PackagesCompositeFilename);
                packagesCompositeDirectory = SerpDir;
                if (File.Exists(packagesCompositeDll))
                    File.Delete(packagesCompositeDll);
                
                if (combinedComposite)
                {
                    string packagesCompositeDllCompile = GetCompileFile(serpRoot, packagesCompositeDll);
                    Crossgen2RunnerOptions crossgen2Options = new Crossgen2RunnerOptions() { Composite = true, CompositeRoot = GetBackupFile(serpRoot, SerpDir), PartialComposite = true };
                    var runner = new Crossgen2Runner(_options, crossgen2Options, packageCombinedReferencesBackup);
                    var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, packagesCompositeDllCompile, packageCompileAssembliesBackup));
                    fileCompilations.Add(compilationProcess);
                }
                else
                {
                    Crossgen2RunnerOptions crossgen2Options = new Crossgen2RunnerOptions() { Composite = false };
                    var runner = new Crossgen2Runner(_options, crossgen2Options, packageCombinedReferencesBackup);
                    foreach (string assembly in packageCompileAssembliesBackup)
                    {
                        string dllCompile = GetCompileFile(serpRoot, assembly);
                        var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, dllCompile, new string[] { assembly }));
                        fileCompilations.Add(compilationProcess);
                    }
                }
            }

            ParallelRunner.Run(fileCompilations, _options.DegreeOfParallelism);
            
            bool success = true;
            foreach (var compilationProcess in fileCompilations)
            {
                if (!compilationProcess.Succeeded)
                {
                    success = false;
                    Console.WriteLine($"Failed compiling {compilationProcess.Parameters.OutputFileName}");
                }
            }

            if (!success)
                return 1;

            
            if (combinedComposite)
            {
                // For combined composite, move the component assemblies we added the R2R header from the composite out folder
                // to the correct compile tree destination folder so they get copied into the right place
                string compositeOutputRootDir = GetCompileFile(serpRoot, _options.CoreRootDirectory.FullName);
                string frameworkCompositeDll = Path.Combine(compositeOutputRootDir, FrameworkCompositeFilename);
                Debug.Assert(File.Exists(frameworkCompositeDll));
                var compiledCompositeFiles = Directory.GetFiles(compositeOutputRootDir, "*.dll", SearchOption.AllDirectories);
                foreach (var componentAssembly in compiledCompositeFiles)
                {
                    if (Path.GetFileName(componentAssembly).Equals(FrameworkCompositeFilename))
                        continue;

                    if (Path.GetFileName(componentAssembly).Equals(PackagesCompositeFilename))
                        continue;

                    string assemblyRelativePath = Path.GetRelativePath(compositeOutputRootDir, componentAssembly);
                    string destinationFile = Path.Combine(SerpDir, assemblyRelativePath);
                    Debug.Assert(File.Exists(GetBackupFile(serpRoot, destinationFile)));
                    File.Move(componentAssembly, destinationFile, true);
                }
            }
            
            // Move everything we compiled to the main directory structure
            var compiledFiles = Directory.GetFiles(Path.Combine(serpRoot, CompileFolder), "*.dll", SearchOption.AllDirectories);
            foreach (var file in compiledFiles)
            {
                string destinationFile = GetOriginalFile(serpRoot, file);
                File.Move(file, destinationFile, true);
            }

            return success ? 0 : 1;
        }

        private static bool ShouldInclude(string file)
        {
            if (!string.IsNullOrEmpty(file))
            {
                if (file.EndsWith("Shared.Exports.dll", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (file.EndsWith(".parallax.dll", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (!file.EndsWith("Exports.dll", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<string> ResolveReferences(IEnumerable<string> folders)
        {
            foreach (string referenceFolder in folders)
            {
                foreach (string reference in ComputeManagedAssemblies.GetManagedAssembliesInFolder(referenceFolder))
                {
                    yield return reference;
                }
            }
        }

        /// <summary>
        /// Backs up the assemblies to a separate folder tree and replaces each file with the original reference
        /// in the output list. This keeps the Serp folder clean of junk.
        /// </summary>
        private static List<string> BackupAndUseOriginalAssemblies(string rootFolder, List<string> assemblies)
        {
            List<string> rewrittenList = new List<string>();

            foreach (var assembly in assemblies)
            {
                rewrittenList.Add(BackupAndUseOriginalAssembly(rootFolder, assembly));
            }

            return rewrittenList;
        }

        private static string BackupAndUseOriginalAssembly(string rootFolder, string assembly)
        {
            string backupFile = GetBackupFile(rootFolder, assembly);
            string backupDir = Path.GetDirectoryName(backupFile);

            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            if (!File.Exists(backupFile))
            {
                File.Copy(assembly, backupFile);
            }

            return backupFile;
        }

        private static string GetBackupFile(string rootFolder, string assembly)
        {
            string relativePath = GetOriginalFileRelativePath(rootFolder, assembly);
            return Path.Combine(rootFolder, BackupFolder, relativePath);
        }

        private static string GetCompileFile(string rootFolder, string assembly)
        {
            string relativePath = GetOriginalFileRelativePath(rootFolder, assembly);
            return Path.Combine(rootFolder, CompileFolder, relativePath);
        }

        private static string GetOriginalFile(string rootFolder, string assembly)
        {
            string relativePath = GetOriginalFileRelativePath(rootFolder, assembly);
            return Path.Combine(rootFolder, relativePath);
        }

        private static string GetOriginalFileRelativePath(string rootFolder, string assembly)
        {
            string relativePath = Path.GetRelativePath(rootFolder, assembly);
            if (relativePath.StartsWith(CompileFolder))
            {
                relativePath = Path.GetRelativePath(Path.Combine(rootFolder, CompileFolder), assembly);
            }
            else if (relativePath.StartsWith(BackupFolder))
            {
                relativePath = Path.GetRelativePath(Path.Combine(rootFolder, BackupFolder), assembly);
            }
            return relativePath;
        }

        private static IEnumerable<string> FilterAssembliesNoSimpleNameDuplicates(HashSet<string> simpleNameSet, IEnumerable<string> assemblyFileList)
        {
            foreach (var x in assemblyFileList)
            {
                string simpleName = Path.GetFileNameWithoutExtension(x);
                if (!simpleNameSet.Contains(simpleName))
                {
                    simpleNameSet.Add(simpleName);
                    yield return x;
                }
            }
        }
    }
}
