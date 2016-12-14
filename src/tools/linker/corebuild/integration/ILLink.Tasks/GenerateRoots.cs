using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Build.Utilities; // Task
using Microsoft.Build.Framework; // MessageImportance
using Microsoft.NET.Build.Tasks; // LockFileCache
using NuGet.ProjectModel; // LockFileTargetLibrary
using NuGet.Frameworks; // NuGetFramework.Parse(targetframework)

namespace ILLink.Tasks
{
	public class GenerateRoots : Task
	{

		[Required]
		public string AssetsFilePath { get; set; }

		[Required]
		public string TargetFramework { get; set; }

		[Required]
		public string RuntimeIdentifier { get; set; }

		[Required]
		public string PublishDir { get; set; }

		[Required]
		public ITaskItem SingleRootXmlFilePath { get; set; }

		[Required]
		public string MainAssemblyName { get; set; }

		[Output]
		public ITaskItem[] RootAssemblies { get; private set; }

		[Output]
		public ITaskItem[] FrameworkAssemblies { get; private set; }

		[Output]
		public ITaskItem[] PublishAssemblies { get; private set; }

		[Output]
		public ITaskItem[] UnmanagedFileAssets { get; private set; }


		private List<string> publishLibs;
		private List<string> unmanagedFileAssets;
		private List<string> rootLibs;
		private List<string> frameworkLibs;

		private void WriteSingleRootXmlFile()
		{
			var xdoc = new XDocument(new XElement("linker",
							new XElement("assembly",
								new XAttribute("fullname", MainAssemblyName),
								new XElement("type",
									new XAttribute("fullname", "*"),
									new XAttribute("required", "true")))));

			XmlWriterSettings xws = new XmlWriterSettings();
			xws.Indent = true;
			xws.OmitXmlDeclaration = true;

			using (XmlWriter xw = XmlWriter.Create(SingleRootXmlFilePath.ItemSpec, xws))
			{
				xdoc.Save(xw);
			}

		}

		private void GetAssembliesAndFiles()
		{
			unmanagedFileAssets = new List<string>();
			publishLibs = new List<string>();
			foreach (var f in Directory.GetFiles(PublishDir))
			{
				try
				{
					AssemblyName.GetAssemblyName(f);
					publishLibs.Add(f);
				}
				catch (BadImageFormatException)
				{
					unmanagedFileAssets.Add(f);
				}
			}
		}


		private void PopulateOutputItems()
		{
			FrameworkAssemblies = frameworkLibs.Select(l => new TaskItem(l)).ToArray();

			RootAssemblies = rootLibs.Select(l => Path.GetFileNameWithoutExtension(l))
									 .Select(l => new TaskItem(l)).ToArray();

			UnmanagedFileAssets = unmanagedFileAssets.Select(f => Path.GetFileName(f))
				.Select(f => new TaskItem(f)).ToArray();

			PublishAssemblies = publishLibs.Select(l => Path.GetFileName(l))
				.Select(l => new TaskItem(l)).ToArray();

		}

		public override bool Execute()
		{
			if (!Directory.Exists(PublishDir))
			{
				Log.LogMessageFromText($"Publish directory {PublishDir} does not exist. Run dotnet publish before dotnet link.", MessageImportance.High);				return false;
			}

			// TODO: make this a separate msbuild task
			WriteSingleRootXmlFile();

                        // TODO: make this a separate msbuild task
                        GetAssembliesAndFiles();

			// TODO: make this a separate msbuild task
			GetFrameworkLibraries();

			rootLibs = publishLibs.Select(l => Path.GetFileName(l)).Except(frameworkLibs).ToList();

			PopulateOutputItems();

			return true;
		}

		private void GetFrameworkLibraries()
		{
			var lockFile = new LockFileCache(BuildEngine4).GetLockFile(AssetsFilePath);
			var lockFileTarget = lockFile.GetTarget(NuGetFramework.Parse(TargetFramework), RuntimeIdentifier);

			if (lockFileTarget == null)
			{
				var targetString = string.IsNullOrEmpty(RuntimeIdentifier) ? TargetFramework : $"{TargetFramework}/{RuntimeIdentifier}";

				throw new Exception($"Missing target section {targetString} from assets file {AssetsFilePath}. Ensure you have restored this project previously.");
			}

			var netCoreAppPackage = lockFileTarget.Libraries.Single(l => l.Name == "Microsoft.NETCore.App");

			Dictionary<string, LockFileTargetLibrary> packages = new Dictionary<string, LockFileTargetLibrary>(lockFileTarget.Libraries.Count, StringComparer.OrdinalIgnoreCase);

			foreach (var lib in lockFileTarget.Libraries)
			{
				packages.Add(lib.Name, lib);
			}

			var packageQueue = new Queue<LockFileTargetLibrary>();
			packageQueue.Enqueue(netCoreAppPackage);


			var libraries = new List<string>();
			while (packageQueue.Count > 0)
			{
				var package = packageQueue.Dequeue();
				foreach (var lib in package.RuntimeAssemblies)
				{
					libraries.Add(lib.ToString());
				}

				foreach (var dep in package.Dependencies.Select(d => d.Id))
				{
					packageQueue.Enqueue(packages[dep]);
				}
			}

			frameworkLibs = libraries.Select(l => Path.GetFileName(l)).ToList();
		}
	}
}
