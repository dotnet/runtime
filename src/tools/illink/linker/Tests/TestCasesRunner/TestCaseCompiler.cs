﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestCaseCompiler {
		protected readonly TestCaseMetadaProvider _metadataProvider;
		protected readonly TestCaseSandbox _sandbox;
		protected readonly ILCompiler _ilCompiler;

		public TestCaseCompiler (TestCaseSandbox sandbox, TestCaseMetadaProvider metadataProvider)
			: this(sandbox, metadataProvider, new ILCompiler ())
		{
		}

		public TestCaseCompiler (TestCaseSandbox sandbox, TestCaseMetadaProvider metadataProvider, ILCompiler ilCompiler)
		{
			_ilCompiler = ilCompiler;
			_sandbox = sandbox;
			_metadataProvider = metadataProvider;
		}

		public NPath CompileTestIn (NPath outputDirectory, string outputName, IEnumerable<string> sourceFiles, IEnumerable<string> references, IEnumerable<string> defines, NPath[] resources)
		{
			var originalReferences = references.Select (r => r.ToNPath ()).ToArray ();
			var originalDefines = defines?.ToArray () ?? new string [0];

			Prepare (outputDirectory);

			var compiledReferences = CompileBeforeTestCaseAssemblies (outputDirectory, originalReferences, originalDefines).ToArray ();
			var allTestCaseReferences = originalReferences.Concat (compiledReferences).ToArray ();

			var options = CreateOptionsForTestCase (
				outputDirectory.Combine (outputName),
				sourceFiles.Select (s => s.ToNPath ()).ToArray (),
				allTestCaseReferences,
				originalDefines,
				resources);
			var testAssembly = CompileAssembly (options);
				

			// The compile after step is used by tests to mess around with the input to the linker.  Generally speaking, it doesn't seem like we would ever want to mess with the
			// expectations assemblies because this would undermine our ability to inspect them for expected results during ResultChecking.  The UnityLinker UnresolvedHandling tests depend on this
			// behavior of skipping the after test compile
			if (outputDirectory != _sandbox.ExpectationsDirectory)
				CompileAfterTestCaseAssemblies (outputDirectory, originalReferences, originalDefines);

			return testAssembly;
		}

		protected virtual void Prepare (NPath outputDirectory)
		{
		}

		protected virtual CompilerOptions CreateOptionsForTestCase (NPath outputPath, NPath[] sourceFiles, NPath[] references, string[] defines, NPath[] resources)
		{
			return new CompilerOptions
			{
				OutputPath = outputPath,
				SourceFiles = sourceFiles,
				References = references,
				Defines = defines.Concat (_metadataProvider.GetDefines ()).ToArray (),
				Resources = resources
			};
		}

		protected virtual CompilerOptions CreateOptionsForSupportingAssembly (SetupCompileInfo setupCompileInfo, NPath outputDirectory, NPath[] sourceFiles, NPath[] references, string[] defines)
		{
			var allDefines = defines.Concat (setupCompileInfo.Defines ?? new string [0]).ToArray ();
			var allReferences = references.Concat (setupCompileInfo.References?.Select (p => MakeSupportingAssemblyReferencePathAbsolute (outputDirectory, p)) ?? new NPath [0]).ToArray ();
			return new CompilerOptions
			{
				OutputPath = outputDirectory.Combine (setupCompileInfo.OutputName),
				SourceFiles = sourceFiles,
				References = allReferences,
				Defines = allDefines
			};
		}

		private IEnumerable<NPath> CompileBeforeTestCaseAssemblies (NPath outputDirectory, NPath[] references, string[] defines)
		{
			foreach (var setupCompileInfo in _metadataProvider.GetSetupCompileAssembliesBefore ())
			{
				var options = CreateOptionsForSupportingAssembly (setupCompileInfo, outputDirectory, CollectSetupBeforeSourcesFiles (setupCompileInfo), references, defines);
				var output = CompileAssembly (options);
				if (setupCompileInfo.AddAsReference)
					yield return output;
			}
		}

		private void CompileAfterTestCaseAssemblies (NPath outputDirectory, NPath[] references, string[] defines)
		{
			foreach (var setupCompileInfo in _metadataProvider.GetSetupCompileAssembliesAfter ())
			{
				var options = CreateOptionsForSupportingAssembly (setupCompileInfo, outputDirectory, CollectSetupAfterSourcesFiles (setupCompileInfo), references, defines);
				CompileAssembly (options);
			}
		}

		private NPath[] CollectSetupBeforeSourcesFiles (SetupCompileInfo info)
		{
			return CollectSourceFilesFrom (_sandbox.BeforeReferenceSourceDirectoryFor (info.OutputName));
		}

		private NPath[] CollectSetupAfterSourcesFiles (SetupCompileInfo info)
		{
			return CollectSourceFilesFrom (_sandbox.AfterReferenceSourceDirectoryFor (info.OutputName));
		}

		private static NPath[] CollectSourceFilesFrom (NPath directory)
		{
			var sourceFiles = directory.Files ("*.cs").ToArray ();
			if (sourceFiles.Length > 0)
				return sourceFiles;

			sourceFiles = directory.Files ("*.il").ToArray ();
			if (sourceFiles.Length > 0)
				return sourceFiles;

			throw new FileNotFoundException ($"Didn't find any sources files in {directory}");
		}

		protected static NPath MakeSupportingAssemblyReferencePathAbsolute (NPath outputDirectory, string referenceFileName)
		{
			// Not a good idea to use a full path in a test, but maybe someone is trying to quickly test something locally
			if (Path.IsPathRooted (referenceFileName))
				return referenceFileName.ToNPath ();

			var possiblePath = outputDirectory.Combine (referenceFileName);
			if (possiblePath.FileExists ())
				return possiblePath;

			return referenceFileName.ToNPath();
		}

		protected NPath CompileAssembly (CompilerOptions options)
		{
			if (options.SourceFiles.Any (path => path.ExtensionWithDot == ".cs"))
				return CompileCSharpAssembly (options);

			if (options.SourceFiles.Any (path => path.ExtensionWithDot == ".il"))
				return CompileIlAssembly (options);

			throw new NotSupportedException ($"Unable to compile sources files with extension `{options.SourceFiles.First ().ExtensionWithDot}`");
		}

		protected virtual NPath CompileCSharpAssembly (CompilerOptions options)
		{
			var compilerOptions = CreateCodeDomCompilerOptions (options);
			var provider = CodeDomProvider.CreateProvider ("C#");
			var result = provider.CompileAssemblyFromFile (compilerOptions, options.SourceFiles.Select (p => p.ToString ()).ToArray ());
			if (!result.Errors.HasErrors)
				return compilerOptions.OutputAssembly.ToNPath ();

			var errors = new StringBuilder ();
			foreach (var error in result.Errors)
				errors.AppendLine (error.ToString ());
			throw new Exception ("Compilation errors: " + errors);
		}

		protected NPath CompileIlAssembly (CompilerOptions options)
		{
			return _ilCompiler.Compile (options);
		}

		private CompilerParameters CreateCodeDomCompilerOptions (CompilerOptions options)
		{
			var compilerParameters = new CompilerParameters
			{
				OutputAssembly = options.OutputPath.ToString (),
				GenerateExecutable = options.OutputPath.FileName.EndsWith (".exe")
			};

			compilerParameters.CompilerOptions = options.Defines?.Aggregate (string.Empty, (buff, arg) => $"{buff} /define:{arg}");

			compilerParameters.ReferencedAssemblies.AddRange (options.References.Select (r => r.ToString ()).ToArray ());

			if (options.Resources != null)
				compilerParameters.EmbeddedResources.AddRange (options.Resources.Select (r => r.ToString ()).ToArray ());

			return compilerParameters;
		}
	}
}