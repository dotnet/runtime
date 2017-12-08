using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	struct AssemblySizes
	{
		public long unlinkedSize;
		public long linkedSize;
	}
	public class CompareAssemblySizes : Task
	{
		/// <summary>
		///   Paths to managed assemblies before linking.
		/// </summary>
		[Required]
		public ITaskItem[] UnlinkedAssemblies { get; set; }

		/// <summary>
		///   Paths to managed assemblies after linking. These
		///   assembly names should be a subset of the
		///   assembly names in UnlinkedAssemblies.
		/// </summary>
		[Required]
		public ITaskItem[] LinkedAssemblies { get; set; }

		public override bool Execute()
		{
			string[] unlinkedFiles = UnlinkedAssemblies.Select (i => i.ItemSpec).ToArray();
			string[] linkedFiles = LinkedAssemblies.Select (i => i.ItemSpec).ToArray();

			Dictionary<string, AssemblySizes> sizes = new Dictionary<string, AssemblySizes> ();

			long totalUnlinked = 0;
			foreach (string unlinkedFile in unlinkedFiles) {
				if (!Utils.IsManagedAssembly (unlinkedFile)) {
					continue;
				}
				string fileName = Path.GetFileName (unlinkedFile);
				AssemblySizes assemblySizes = new AssemblySizes ();
				assemblySizes.unlinkedSize = new System.IO.FileInfo (unlinkedFile).Length;
				totalUnlinked += assemblySizes.unlinkedSize;
				sizes[fileName] = assemblySizes;
			}

			long totalLinked = 0;
			foreach (string linkedFile in linkedFiles) {
				if (!Utils.IsManagedAssembly (linkedFile)) {
					continue;
				}
				string fileName = Path.GetFileName (linkedFile);
				if (!sizes.ContainsKey(fileName)) {
					Console.WriteLine ($"{linkedFile} was specified as an assembly kept by the linker, but {fileName} was not specified as a managed publish assembly.");
					continue;
				}
				AssemblySizes assemblySizes = sizes[fileName];
				assemblySizes.linkedSize = new System.IO.FileInfo (linkedFile).Length;
				totalLinked += assemblySizes.linkedSize;
				sizes[fileName] = assemblySizes;
			}

			Console.WriteLine ("{0, -60} {1,-20:N0} {2, -20:N0} {3, -10:P}",
				"",
				"Before linking (B)",
				"After linking (B)",
				"Size decrease");
			Console.WriteLine ("{0, -60} {1,-20:N0} {2, -20:N0} {3, -10:P}",
				"-----------",
				"-----------",
				"-----------",
				"-----------"
			);

			Console.WriteLine ("{0, -60} {1,-20:N0} {2, -20:N0} {3, -10:P}",
				"Total size of assemblies",
				totalUnlinked,
				totalLinked,
				((double)totalUnlinked - (double)totalLinked) / (double)totalUnlinked);

			Console.WriteLine ("{0, -60} {1,-20:N0} {2, -20:N0} {3, -10:P}",
				"-----------",
				"-----------",
				"-----------",
				"-----------"
			);

			foreach (string assembly in sizes.Keys) {
				Console.WriteLine ("{0, -60} {1,-20:N0} {2, -20:N0} {3, -10:P}",
					assembly,
					sizes[assembly].unlinkedSize,
					sizes[assembly].linkedSize,
					(double)(sizes[assembly].unlinkedSize - sizes[assembly].linkedSize)/(double)sizes[assembly].unlinkedSize);
			}
			return true;
		}

		public static long DirSize(DirectoryInfo d)
		{
			long size = 0;
			// Add file sizes.
			FileInfo[] fis = d.GetFiles ();
			foreach (FileInfo fi in fis) {
				size += fi.Length;
			}
			// Add subdirectory sizes.
			DirectoryInfo[] dis = d.GetDirectories ();
			foreach (DirectoryInfo di in dis) {
				size += DirSize (di);
			}
			return size;
		}
	}
}
