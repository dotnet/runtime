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
			if (!_context.Annotations.MarkProcessed (type, reason))
				return;
			if (_context.KeepTypeForwarderOnlyAssemblies)
				_context.Annotations.Mark (module, new DependencyInfo (DependencyKind.ModuleOfExportedType, type));
		}
	}
}
