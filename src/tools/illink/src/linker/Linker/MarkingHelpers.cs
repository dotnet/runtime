using System;
using Mono.Cecil;

namespace Mono.Linker
{
	public class MarkingHelpers
	{
		protected readonly LinkContext _context;

		public MarkingHelpers (LinkContext context)
		{
			_context = context;
		}

		public void MarkExportedType (ExportedType type, ModuleDefinition module, in DependencyInfo reason)
		{
			_context.Annotations.Mark (type, reason);
			if (_context.KeepTypeForwarderOnlyAssemblies)
				_context.Annotations.Mark (module, new DependencyInfo (DependencyKind.ModuleOfExportedType, type));
		}
	}
}
