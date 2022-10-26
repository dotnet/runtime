// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Mono.Linker;
using Mono.Linker.Steps;

namespace ILLink.Tasks.Tests
{
	public class MockTask : ILLink
	{

		public List<(MessageImportance Importance, string Line)> Messages { get; } = new List<(MessageImportance Importance, string Line)> ();

		public MockTask ()
		{
			// Ensure that [Required] members are non-null
			AssemblyPaths = Array.Empty<ITaskItem> ();
			RootAssemblyNames = Array.Empty<ITaskItem> ();
			ILLinkPath = Path.Combine (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location), "illink.dll");
		}

		public MockDriver CreateDriver ()
		{
			using (var responseFileText = new StringReader (GenerateResponseFileCommands ())) {
				var arguments = new Queue<string> ();
				Driver.ParseResponseFile (responseFileText, arguments);
				return new MockDriver (arguments);
			}
		}

		public static string[] OptimizationNames {
			get {
				var field = typeof (ILLink).GetField ("_optimizationNames", BindingFlags.NonPublic | BindingFlags.Static);
				return (string[]) field.GetValue (null);
			}
		}

		public void SetOptimization (string optimization, bool enabled)
		{
			var property = typeof (ILLink).GetProperty (optimization);
			property.GetSetMethod ().Invoke (this, new object[] { enabled });
		}

		static readonly string[] nonOptimizationBooleanProperties = new string[] {
			"DumpDependencies",
			"RemoveSymbols",
			"TreatWarningsAsErrors",
			"SingleWarn"
		};

		public static IEnumerable<string> GetOptimizationPropertyNames ()
		{
			foreach (var property in typeof (ILLink).GetProperties (BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)) {
				if (property.PropertyType != typeof (bool))
					continue;
				if (nonOptimizationBooleanProperties.Contains (property.Name))
					continue;
				yield return property.Name;
			}
		}

		protected override void LogEventsFromTextOutput (string singleLine, MessageImportance messageImportance) => Messages.Add ((messageImportance, singleLine));
	}

	public class MockBuildEngine : IBuildEngine
	{
		public void LogErrorEvent (BuildErrorEventArgs e) { }
		public void LogWarningEvent (BuildWarningEventArgs e) { }
		public void LogMessageEvent (BuildMessageEventArgs e) { }
		public void LogCustomEvent (CustomBuildEventArgs e) { }
		public bool BuildProjectFile (string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => false;
		public bool ContinueOnError => false;
		public int LineNumberOfTaskNode => 0;
		public int ColumnNumberOfTaskNode => 0;
		public string ProjectFileOfTaskNode => null;
	}

	public class MockDriver : Driver
	{
		public class CustomLogger : Mono.Linker.ILogger
		{
			public List<MessageContainer> Messages = new List<MessageContainer> ();

			public void LogMessage (MessageContainer message)
			{
				Messages.Add (message);
			}
		}

		public MockDriver (Queue<string> arguments) : base (arguments)
		{
			// Always set up the context early on.
			Logger = new CustomLogger ();
			SetupContext (Logger);
		}

		public new LinkContext Context => base.Context;

		public CustomLogger Logger { get; private set; }

		public IEnumerable<string> GetRootAssemblies ()
		{
			foreach (var step in Context.Pipeline.GetSteps ()) {
				if (!(step is RootAssemblyInput))
					continue;

				var assemblyName = (string) typeof (RootAssemblyInput).GetField ("fileName", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (step);
				if (assemblyName == null)
					continue;

				yield return assemblyName;
			}
		}

		public IEnumerable<string> GetRootDescriptors ()
		{
			foreach (var step in Context.Pipeline.GetSteps ()) {
				if (!(step is ResolveFromXmlStep))
					continue;

				var descriptor = (string) typeof (ResolveFromXmlStep).GetField ("_xmlDocumentLocation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (step);

				yield return descriptor;
			}
		}

		public IEnumerable<string> GetReferenceAssemblies ()
		{
			return (IEnumerable<string>) typeof (AssemblyResolver).GetField ("_references", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (Context.Resolver);
		}

		protected override void AddResolveFromXmlStep (Pipeline pipeline, string file)
		{
			// Don't try to load an xml file - just pretend it exists.
			pipeline.PrependStep (new ResolveFromXmlStep (documentStream: null, file));
		}

		protected override void AddXmlDependencyRecorder (LinkContext context, string file)
		{
			// Don't try to open the output file for writing - just pretend it exists.
			Context.Tracer.AddRecorder (MockXmlDependencyRecorder.Singleton);
		}

		protected override void AddDgmlDependencyRecorder (LinkContext context, string file)
		{
			// Don't try to open the output file for writing - just pretend it exists.
			Context.Tracer.AddRecorder (MockDgmlDependencyRecorder.Singleton);
		}

		public IEnumerable<IDependencyRecorder> GetDependencyRecorders ()
		{
			return (IEnumerable<IDependencyRecorder>) typeof (Tracer).GetField ("recorders", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (Context.Tracer);
		}

		public new bool GetOptimizationName (string optimization, out CodeOptimizations codeOptimizations)
		{
			return base.GetOptimizationName (optimization, out codeOptimizations);
		}

		public CodeOptimizations GetDefaultOptimizations ()
		{
			return Context.Optimizations.Global;
		}

		public Dictionary<string, string> GetCustomData ()
		{
			var field = typeof (LinkContext).GetField ("_parameters", BindingFlags.NonPublic | BindingFlags.Instance);
			return (Dictionary<string, string>) field.GetValue (Context);
		}

		protected override List<BaseStep> CreateDefaultResolvers ()
		{
			return new List<BaseStep> () {
				new RootAssemblyInput (null, AssemblyRootMode.Default)
			};
		}
	}

	public class MockXmlDependencyRecorder : IDependencyRecorder
	{
		public static MockXmlDependencyRecorder Singleton { get; } = new MockXmlDependencyRecorder ();
		public void RecordDependency (object source, object arget, bool marked) { }
		public void RecordDependency (object target, in DependencyInfo reason, bool marked) { }
		public void FinishRecording () { }
	}

	public class MockDgmlDependencyRecorder : IDependencyRecorder
	{
		public static MockXmlDependencyRecorder Singleton { get; } = new MockXmlDependencyRecorder ();
		public void RecordDependency (object source, object arget, bool marked) { }
		public void RecordDependency (object target, in DependencyInfo reason, bool marked) { }
		public void FinishRecording () { }
	}

	public class MockCustomStep : IStep
	{
		public void Process (LinkContext context) { }
	}

	public class MockCustomStep2 : MockCustomStep { }

	public class MockCustomStep3 : MockCustomStep { }

	public class MockCustomStep4 : MockCustomStep { }

	public class MockCustomStep5 : MockCustomStep { }

	public class MockCustomStep6 : MockCustomStep { }

	public class MockMarkHandler : IMarkHandler
	{
		public void Initialize (LinkContext context, MarkContext markContext) { }
	}

	public class MockMarkHandler2 : MockMarkHandler { }

	public class MockMarkHandler3 : MockMarkHandler { }

	public class MockMarkHandler4 : MockMarkHandler { }

}
