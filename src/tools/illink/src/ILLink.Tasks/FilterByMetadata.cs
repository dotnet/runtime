using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	public class FilterByMetadata : Task
	{
		/// <summary>
		///   Items to filter.
		/// </summary>
		[Required]
		public ITaskItem[] Items { get; set; }

		/// <summary>
		///   Name of metadata to filter on.
		/// </summary>
		[Required]
		public String MetadataName { get; set; }

		/// <summary>
		///   The set of metadata values to include.
		/// </summary>
		[Required]
		public ITaskItem[] MetadataValues { get; set; }

		/// <summary>
		///   Filtered items: the input items for which the
		///   specified metadata was one of the allowed
		///   values.
		/// </summary>
		[Output]
		public ITaskItem[] FilteredItems { get; set; }

		public override bool Execute()
		{
			var metadataValues = new HashSet<string>(MetadataValues.Select(v => v.ItemSpec));
			FilteredItems = Items
				.Where(i => metadataValues.Contains(i.GetMetadata(MetadataName)))
				.ToArray();
			return true;
		}
	}
}
