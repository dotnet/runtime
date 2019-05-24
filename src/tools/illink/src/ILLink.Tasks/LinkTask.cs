using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	public class ILLink : ToolTask
	{
		/// <summary>
		///   Paths to the assembly files that should be considered as
		///   input to the linker.
		///   Each path can also have an "action" metadata,
		///   which will set the illink action to take for
		///   that assembly.
		/// </summary>
		[Required]
		public ITaskItem [] AssemblyPaths { get; set; }

		/// <summary>
		///    Paths to assembly files that are reference assemblies,
		///    representing the surface area for compilation.
		/// </summary>
		public ITaskItem [] ReferenceAssemblyPaths { get; set; }

		/// <summary>
		///   The names of the assemblies to root. This should contain
		///   assembly names without an extension, not file names or
		///   paths. Exactly which parts of the assemblies get rooted
		///   is subject to change. Currently these get passed to
		///   illink with "-a", which roots the entry point for
		///   executables, and everything for libraries. To control
		///   the linker more explicitly, either pass descriptor
		///   files, or pass extra arguments for illink.
		/// </summary>
		[Required]
		public ITaskItem [] RootAssemblyNames { get; set; }

		/// <summary>
		///   The directory in which to place linked assemblies.
		/// </summary>
		[Required]
		public ITaskItem OutputDirectory { get; set; }

		/// <summary>
		///   A list of XML root descriptor files specifying linker
		///   roots at a granular level. See the mono/linker
		///   documentation for details about the format.
		/// </summary>
		public ITaskItem [] RootDescriptorFiles { get; set; }

		/// <summary>
		///   Boolean specifying whether to clear initlocals flag on methods.
		/// </summary>
		public bool ClearInitLocals { get; set; }

		/// <summary>
		///   A comma-separated list of assemblies whose methods
		///   should have initlocals flag cleared if ClearInitLocals is true.
		/// </summary>
		public string ClearInitLocalsAssemblies { get; set; }

		/// <summary>
		///   Extra arguments to pass to illink, delimited by spaces.
		/// </summary>
		public string ExtraArgs { get; set; }

		/// <summary>
		///   Make illink dump dependencies file for linker analyzer tool.
		/// </summary>
		public bool DumpDependencies { get; set; }


		private static string DotNetHostPathEnvironmentName = "DOTNET_HOST_PATH";

		private string _dotnetPath;

		private string DotNetPath
		{
			get
			{
				if (!String.IsNullOrEmpty (_dotnetPath))
					return _dotnetPath;

				_dotnetPath = Environment.GetEnvironmentVariable (DotNetHostPathEnvironmentName);
				if (String.IsNullOrEmpty (_dotnetPath))
					throw new InvalidOperationException ($"{DotNetHostPathEnvironmentName} is not set");

				return _dotnetPath;
			}
		}


		/// ToolTask implementation

		protected override string ToolName => Path.GetFileName (DotNetPath);

		protected override string GenerateFullPathToTool () => DotNetPath;

		private string _illinkPath = "";

		public string ILLinkPath {
			get {
				if (!String.IsNullOrEmpty (_illinkPath))
					return _illinkPath;

				var taskDirectory = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);
                                // The linker always runs on .NET Core, even when using desktop MSBuild to host ILLink.Tasks.
				_illinkPath = Path.Combine (Path.GetDirectoryName (taskDirectory), "netcoreapp2.0", "illink.dll");
				return _illinkPath;
			}
			set => _illinkPath = value;
		}

		private static string Quote (string path)
		{
			return $"\"{path.TrimEnd('\\')}\"";
		}

		protected override string GenerateCommandLineCommands ()
		{
			var args = new StringBuilder ();
			args.Append (Quote (ILLinkPath));
			return args.ToString ();
		}

		protected override string GenerateResponseFileCommands ()
		{
			var args = new StringBuilder ();

			if (RootDescriptorFiles != null) {
				foreach (var rootFile in RootDescriptorFiles)
					args.Append ("-x ").AppendLine (Quote (rootFile.ItemSpec));
			}

			foreach (var assemblyItem in RootAssemblyNames)
				args.Append ("-a ").AppendLine (Quote (assemblyItem.ItemSpec));

			HashSet<string> assemblyNames = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
			foreach (var assembly in AssemblyPaths) {
				var assemblyPath = assembly.ItemSpec;
				var assemblyName = Path.GetFileNameWithoutExtension (assemblyPath);

				// If there are multiple paths with the same assembly name, only use the first one.
				if (!assemblyNames.Add (assemblyName))
					continue;

				args.Append ("-reference ").AppendLine (Quote (assemblyPath));

				string action = assembly.GetMetadata ("action");
				if ((action != null) && (action.Length > 0)) {
					args.Append ("-p ");
					args.Append (action);
					args.Append (" ").AppendLine (Quote (assemblyName));
				}
			}

			if (ReferenceAssemblyPaths != null) {
				foreach (var assembly in ReferenceAssemblyPaths) {
					var assemblyPath = assembly.ItemSpec;
					var assemblyName = Path.GetFileNameWithoutExtension (assemblyPath);

					// Don't process references for which we already have
					// implementation assemblies.
					if (assemblyNames.Contains (assemblyName))
						continue;

					args.Append ("-reference ").AppendLine (Quote (assemblyPath));

					// Treat reference assemblies as "skip". Ideally we
					// would not even look at the IL, but only use them to
					// resolve surface area.
					args.Append ("-p skip ").AppendLine (Quote (assemblyName));
				}
			}

			if (OutputDirectory != null)
				args.Append ("-out ").AppendLine (Quote (OutputDirectory.ItemSpec));

			if (ClearInitLocals) {
				args.Append ("-s ");
				// Version of ILLink.CustomSteps is passed as a workaround for msbuild issue #3016
				args.AppendLine ("ILLink.CustomSteps.ClearInitLocalsStep,ILLink.CustomSteps,Version=0.0.0.0:OutputStep");
				if ((ClearInitLocalsAssemblies != null) && (ClearInitLocalsAssemblies.Length > 0)) {
					args.Append ("-m ClearInitLocalsAssemblies ");
					args.AppendLine (ClearInitLocalsAssemblies);
				}
			}

			if (ExtraArgs != null)
				args.AppendLine (ExtraArgs);

			if (DumpDependencies)
				args.AppendLine ("--dump-dependencies");

			return args.ToString ();
		}
	}
}
