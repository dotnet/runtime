﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestCaseCompiler {
		public virtual NPath CompileTestIn (NPath outputDirectory, string outputName, IEnumerable<string> sourceFiles, IEnumerable<string> references, IEnumerable<string> defines)
		{
			var compilerOptions = CreateCompilerOptions (outputDirectory, outputName, references, defines);
			var provider = CodeDomProvider.CreateProvider ("C#");
			var result = provider.CompileAssemblyFromFile (compilerOptions, sourceFiles.ToArray ());
			if (!result.Errors.HasErrors)
				return compilerOptions.OutputAssembly.ToNPath ();

			var errors = new StringBuilder ();
			foreach (var error in result.Errors)
				errors.AppendLine (error.ToString ());
			throw new Exception ("Compilation errors: " + errors);
		}

		protected virtual CompilerParameters CreateCompilerOptions (NPath outputDirectory, string outputName, IEnumerable<string> references, IEnumerable<string> defines)
		{
			var outputPath = outputDirectory.Combine (outputName);

			var compilerParameters = new CompilerParameters
			{
				OutputAssembly = outputPath.ToString (),
				GenerateExecutable = outputName.EndsWith(".exe")
			};

			compilerParameters.CompilerOptions = defines?.Aggregate (string.Empty, (buff, arg) => $"{buff} /define:{arg}");

			compilerParameters.ReferencedAssemblies.AddRange (references.ToArray ());

			return compilerParameters;
		}
	}
}