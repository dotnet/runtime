// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Build.Framework;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests;

[TestCategory("no-workload")]
public sealed class JsonManifestReaderTests
{
	private const string JsonWithAllOutputs = """
		{
		  "properties": {
		    "UnusedProperty": "still validated"
		  },
		  "items": {
		    "_MonoRuntimeComponentSharedLibExt": [
		      { "identity": ".dll", "RuntimeIdentifier": "win-x64" },
		    ],
		    "_MonoRuntimeComponentStaticLibExt": [
		      { "identity": ".lib", "RuntimeIdentifier": "win-x64" },
		    ],
		    "_MonoRuntimeComponentLinking": [
		      { "identity": "static", "RuntimeIdentifier": "win-x64" },
		    ],
		    "_MonoRuntimeAvailableComponents": [
		      { "identity": "diagnostics_tracing", "RuntimeIdentifier": "win-x64" },
		    ],
		    "EmccProperties": [
		      { "identity": "RuntimeEmccVersion", "value": "4.0.1" },
		    ],
		    "WasmOptConfigurationFlags": [
		      "--enable-simd",
		      { "identity": "--enable-threads", "source": "runtime-pack" },
		    ],
		    "EmccDefaultExportedRuntimeMethods": [],
		    "PropertiesThatTriggerRelinking": [
		      { "identity": "InvariantGlobalization", "defaultValueInRuntimePack": "false" },
		    ],
		  },
		}
		""";

	[Fact]
	public void ReadWasmPropsPreservesItemsMetadataAndMissingGroups()
	{
		using var directory = new TempDirectory();
		string jsonPath = directory.WriteFile("input.json", JsonWithAllOutputs);
		var task = new ReadWasmProps
		{
			BuildEngine = new TestBuildEngine(),
			JsonFilePath = jsonPath,
		};

		Assert.True(task.Execute());
		ITaskItem emccProperty = Assert.Single(task.EmccProperties!);
		Assert.Equal("RuntimeEmccVersion", emccProperty.ItemSpec);
		Assert.Equal("4.0.1", emccProperty.GetMetadata("Value"));
		Assert.Collection(
			task.WasmOptConfigurationFlags!,
			item => Assert.Equal("--enable-simd", item.ItemSpec),
			item =>
			{
				Assert.Equal("--enable-threads", item.ItemSpec);
				Assert.Equal("runtime-pack", item.GetMetadata("Source"));
			});
		Assert.Null(task.EmccDefaultExportedFunctions);
		Assert.Empty(task.EmccDefaultExportedRuntimeMethods!);
		ITaskItem relinkingProperty = Assert.Single(task.PropertiesThatTriggerRelinking!);
		Assert.Equal("InvariantGlobalization", relinkingProperty.ItemSpec);
		Assert.Equal("false", relinkingProperty.GetMetadata("defaultValueInRuntimePack"));
	}

	[Fact]
	public void ComponentManifestReaderPreservesItemsAndRuntimeIdentifiers()
	{
		using var directory = new TempDirectory();
		string jsonPath = directory.WriteFile("input.json", JsonWithAllOutputs);
		var task = new MonoRuntimeComponentManifestReadTask
		{
			BuildEngine = new TestBuildEngine(),
			JsonFilePath = jsonPath,
		};

		Assert.True(task.Execute());
		AssertItem(task._MonoRuntimeComponentSharedLibExt, ".dll");
		AssertItem(task._MonoRuntimeComponentStaticLibExt, ".lib");
		AssertItem(task._MonoRuntimeComponentLinking, "static");
		AssertItem(task._MonoRuntimeAvailableComponents, "diagnostics_tracing");

		static void AssertItem(ITaskItem[]? items, string expectedIdentity)
		{
			ITaskItem item = Assert.Single(items!);
			Assert.Equal(expectedIdentity, item.ItemSpec);
			Assert.Equal("win-x64", item.GetMetadata("RuntimeIdentifier"));
		}
	}

	[Theory]
	[InlineData("0")]
	[InlineData("1")]
	public async Task CompiledTasksRunInLegacyAndMultithreadedMsbuild(string forceMultithreaded)
	{
		using var directory = new TempDirectory();
		directory.WriteFile("input.json", JsonWithAllOutputs);
		string assemblyAttribute = new XAttribute("AssemblyFile", typeof(ReadWasmProps).Assembly.Location).ToString();
		string projectPath = directory.WriteFile(
			"reader.proj",
			$$"""
			<Project>
			  <UsingTask TaskName="MonoRuntimeComponentManifestReadTask" {{assemblyAttribute}} />
			  <UsingTask TaskName="ReadWasmProps" {{assemblyAttribute}} />
			  <Target Name="Run">
			    <MonoRuntimeComponentManifestReadTask JsonFilePath="$(MSBuildThisFileDirectory)input.json">
			      <Output TaskParameter="_MonoRuntimeComponentLinking" ItemName="_Linking" />
			      <Output TaskParameter="_MonoRuntimeAvailableComponents" ItemName="_Components" />
			    </MonoRuntimeComponentManifestReadTask>
			    <ReadWasmProps JsonFilePath="$(MSBuildThisFileDirectory)input.json">
			      <Output TaskParameter="WasmOptConfigurationFlags" ItemName="_Flags" />
			      <Output TaskParameter="PropertiesThatTriggerRelinking" ItemName="_Relinking" />
			    </ReadWasmProps>
			    <Message Importance="High" Text="linking=@(_Linking)|rid=%(_Linking.RuntimeIdentifier)" />
			    <Message Importance="High" Text="components=@(_Components)|rid=%(_Components.RuntimeIdentifier)" />
			    <Message Importance="High" Text="flags=@(_Flags, ',')" />
			    <Message Importance="High" Text="relinking=@(_Relinking)|default=%(_Relinking.defaultValueInRuntimePack)" />
			  </Target>
			</Project>
			""");

		string dotnetPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")
			?? Environment.ProcessPath
			?? throw new InvalidOperationException("The dotnet host path is unavailable.");
		var startInfo = new ProcessStartInfo
		{
			FileName = dotnetPath,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
			UseShellExecute = false,
			WorkingDirectory = directory.Path,
		};
		startInfo.ArgumentList.Add("msbuild");
		startInfo.ArgumentList.Add(projectPath);
		startInfo.ArgumentList.Add("-target:Run");
		startInfo.ArgumentList.Add($"-multithreaded:{(forceMultithreaded == "1" ? "true" : "false")}");
		startInfo.ArgumentList.Add("-nodeReuse:false");
		startInfo.ArgumentList.Add("-verbosity:minimal");
		startInfo.Environment["MSBUILDFORCEMULTITHREADED"] = forceMultithreaded;
		startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

		using Process process = Process.Start(startInfo)
			?? throw new InvalidOperationException("Failed to start MSBuild.");
		Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
		Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();
		string output = await standardOutputTask + await standardErrorTask;

		Assert.True(process.ExitCode == 0, output);
		Assert.Contains("linking=static|rid=win-x64", output);
		Assert.Contains("components=diagnostics_tracing|rid=win-x64", output);
		Assert.Contains("flags=--enable-simd,--enable-threads", output);
		Assert.Contains("relinking=InvariantGlobalization|default=false", output);
		Assert.DoesNotContain("Custom TaskFactory", output);
	}

	[Fact]
	public void MissingFileReturnsFalseAndLogsItsPath()
	{
		var buildEngine = new TestBuildEngine();
		var task = new ReadWasmProps
		{
			BuildEngine = buildEngine,
			JsonFilePath = "missing-wasm-props.json",
		};

		Assert.False(task.Execute());
		Assert.Equal("Could not find JsonFilePath=missing-wasm-props.json", Assert.Single(buildEngine.Errors));
	}

	[Theory]
	[InlineData("""{ "items": { "WasmOptConfigurationFlags": [ "" ] } }""")]
	[InlineData("""{ "items": { "WasmOptConfigurationFlags": [ { "value": "missing identity" } ] } }""")]
	public void InvalidJsonReturnsFalseAndLogsError(string json)
	{
		using var directory = new TempDirectory();
		var buildEngine = new TestBuildEngine();
		var task = new ReadWasmProps
		{
			BuildEngine = buildEngine,
			JsonFilePath = directory.WriteFile("input.json", json),
		};

		Assert.False(task.Execute());
		Assert.NotEmpty(buildEngine.Errors);
	}

	[Fact]
	public void CaseInsensitiveDuplicatePropertyThrows()
	{
		using var directory = new TempDirectory();
		var task = new ReadWasmProps
		{
			BuildEngine = new TestBuildEngine(),
			JsonFilePath = directory.WriteFile(
				"input.json",
				"""{ "properties": { "Value": "one", "value": "two" } }"""),
		};

		AggregateException exception = Assert.Throws<AggregateException>(() => task.Execute());
		Assert.IsType<ArgumentException>(exception.InnerException);
	}

	private sealed class TempDirectory : IDisposable
	{
		public TempDirectory()
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
			Directory.CreateDirectory(Path);
		}

		public string Path { get; }

		public string WriteFile(string fileName, string contents)
		{
			string path = System.IO.Path.Combine(Path, fileName);
			File.WriteAllText(path, contents);
			return path;
		}

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}

	private sealed class TestBuildEngine : IBuildEngine
	{
		public List<string> Errors { get; } = new();

		public bool ContinueOnError => false;

		public int LineNumberOfTaskNode => 0;

		public int ColumnNumberOfTaskNode => 0;

		public string ProjectFileOfTaskNode => string.Empty;

		public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) =>
			throw new NotSupportedException();

		public void LogCustomEvent(CustomBuildEventArgs e)
		{
		}

		public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e.Message ?? string.Empty);

		public void LogMessageEvent(BuildMessageEventArgs e)
		{
		}

		public void LogWarningEvent(BuildWarningEventArgs e)
		{
		}
	}
}
