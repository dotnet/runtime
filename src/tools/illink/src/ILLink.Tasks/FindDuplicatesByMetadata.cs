using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	public class FindDuplicatesByMetadata : Task
	{
		/// <summary>
		///   Items to scan.
		/// </summary>
		[Required]
		public ITaskItem [] Items { get; set; }

		/// <summary>
		///   Name of metadata to scan for.
		/// </summary>
		[Required]
		public String MetadataName { get; set; }

		/// <summary>
		///   Duplicate items: the input items for which the
		///   specified metadata was shared by multiple input
		///   items.
		/// </summary>
		[Output]
		public ITaskItem [] DuplicateItems { get; set; }

		/// <summary>
		///   Duplicate representatives: includes one input
		///   item from each set of duplicates.
		/// </summary>
		[Output]
		public ITaskItem [] DuplicateRepresentatives { get; set; }

		public override bool Execute ()
		{
			var duplicateGroups = Items.GroupBy (i => i.GetMetadata (MetadataName))
				.Where (g => g.Count () > 1);
			DuplicateItems = duplicateGroups.SelectMany (g => g).ToArray ();
			DuplicateRepresentatives = duplicateGroups.Select (g => g.First ()).ToArray ();
			return true;
		}
	}
}
