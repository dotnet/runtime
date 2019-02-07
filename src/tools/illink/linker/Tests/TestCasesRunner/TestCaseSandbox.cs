﻿using System;
using System.Collections.Generic;
using System.IO;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestCaseSandbox {
		protected readonly TestCase _testCase;
		protected readonly NPath _directory;

		public TestCaseSandbox (TestCase testCase)
			: this (testCase, NPath.SystemTemp)
		{
		}

		public TestCaseSandbox (TestCase testCase, NPath rootTemporaryDirectory)
			: this (testCase, rootTemporaryDirectory, string.Empty)
		{
		}

		public TestCaseSandbox (TestCase testCase, string rootTemporaryDirectory, string namePrefix)
			: this (testCase, rootTemporaryDirectory.ToNPath (), namePrefix)
		{
		}

		public TestCaseSandbox (TestCase testCase, NPath rootTemporaryDirectory, string namePrefix)
		{
			_testCase = testCase;
			var name = string.IsNullOrEmpty (namePrefix) ? "linker_tests" : $"{namePrefix}_linker_tests";
			_directory = rootTemporaryDirectory.Combine (name);

			_directory.DeleteContents ();

			InputDirectory = _directory.Combine ("input").EnsureDirectoryExists ();
			OutputDirectory = _directory.Combine ("output").EnsureDirectoryExists ();
			ExpectationsDirectory = _directory.Combine ("expectations").EnsureDirectoryExists ();
			ResourcesDirectory = _directory.Combine ("resources").EnsureDirectoryExists ();
		}

		public NPath InputDirectory { get; }

		public NPath OutputDirectory { get; }

		public NPath ExpectationsDirectory { get; }

		public NPath ResourcesDirectory { get; }

		public IEnumerable<NPath> SourceFiles {
			get { return _directory.Files ("*.cs"); }
		}

		public IEnumerable<NPath> LinkXmlFiles {
			get { return InputDirectory.Files ("*.xml"); }
		}

		public IEnumerable<NPath> ResponseFiles {
			get { return InputDirectory.Files ("*.rsp"); }
		}

		public IEnumerable<NPath> ResourceFiles => ResourcesDirectory.Files ();

		public virtual void Populate (TestCaseMetadaProvider metadataProvider)
		{
			_testCase.SourceFile.Copy (_directory);

			if (_testCase.HasLinkXmlFile)
				_testCase.LinkXmlFile.Copy (InputDirectory);

			CopyToInputAndExpectations (GetExpectationsAssemblyPath ());

			foreach (var dep in metadataProvider.AdditionalFilesToSandbox ()) {
				var destination = _directory.Combine (dep.DestinationFileName);
				dep.Source.FileMustExist ().Copy (destination);

				// In a few niche tests we need to copy pre-built assemblies directly into the input directory.
				// When this is done, we also need to copy them into the expectations directory so that if they are used
				// as references we can still compile the expectations version of the assemblies
				if (destination.Parent == InputDirectory)
					dep.Source.Copy (ExpectationsDirectory.Combine (destination.RelativeTo (InputDirectory)));
			}
			
			// Copy non class library dependencies to the sandbox
			foreach (var fileName in metadataProvider.GetReferenceValues ()) {
				if (!fileName.StartsWith ("System.", StringComparison.Ordinal) && !fileName.StartsWith ("Mono.", StringComparison.Ordinal) && !fileName.StartsWith ("Microsoft.", StringComparison.Ordinal))
					CopyToInputAndExpectations (_testCase.SourceFile.Parent.Combine (fileName.ToNPath ()));
			}
			
			foreach (var referenceDependency in metadataProvider.GetReferenceDependencies ())
				CopyToInputAndExpectations (_testCase.SourceFile.Parent.Combine (referenceDependency.ToNPath()));

			foreach (var res in metadataProvider.GetResources ()) {
				res.Source.FileMustExist ().Copy (ResourcesDirectory.Combine (res.DestinationFileName));
			}

			foreach (var res in metadataProvider.GetResponseFiles()) {
				res.Source.FileMustExist ().Copy (InputDirectory.Combine (res.DestinationFileName));
			}

			foreach (var compileRefInfo in metadataProvider.GetSetupCompileAssembliesBefore ())
			{
				var destination = BeforeReferenceSourceDirectoryFor (compileRefInfo.OutputName).EnsureDirectoryExists ();
				compileRefInfo.SourceFiles.Copy (destination);
				
				destination = BeforeReferenceResourceDirectoryFor (compileRefInfo.OutputName).EnsureDirectoryExists ();
				compileRefInfo.Resources?.Copy (destination);
			}

			foreach (var compileRefInfo in metadataProvider.GetSetupCompileAssembliesAfter ())
			{
				var destination = AfterReferenceSourceDirectoryFor (compileRefInfo.OutputName).EnsureDirectoryExists ();
				compileRefInfo.SourceFiles.Copy (destination);
				
				destination = AfterReferenceResourceDirectoryFor (compileRefInfo.OutputName).EnsureDirectoryExists ();
				compileRefInfo.Resources?.Copy (destination);
			}
		}

		private static NPath GetExpectationsAssemblyPath ()
		{
			return new Uri (typeof (KeptAttribute).Assembly.CodeBase).LocalPath.ToNPath ();
		}

		protected void CopyToInputAndExpectations (NPath source)
		{
			source.Copy (InputDirectory);
			source.Copy (ExpectationsDirectory);
		}

		public NPath BeforeReferenceSourceDirectoryFor (string outputName)
		{
			return _directory.Combine ($"ref_source_before_{Path.GetFileNameWithoutExtension (outputName)}");
		}

		public NPath AfterReferenceSourceDirectoryFor (string outputName)
		{
			return _directory.Combine ($"ref_source_after_{Path.GetFileNameWithoutExtension (outputName)}");
		}
		
		public NPath BeforeReferenceResourceDirectoryFor (string outputName)
		{
			return _directory.Combine ($"ref_resource_before_{Path.GetFileNameWithoutExtension (outputName)}");
		}

		public NPath AfterReferenceResourceDirectoryFor (string outputName)
		{
			return _directory.Combine ($"ref_resource_after_{Path.GetFileNameWithoutExtension (outputName)}");
		}
	}
}