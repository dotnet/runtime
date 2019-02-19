using System;

namespace Mono.Linker {
	public static class TypeNameParser {
		public static bool TryParseTypeAssemblyQualifiedName (string value, out string typeName, out string assemblyName) {
			if (string.IsNullOrEmpty (value)) {
				typeName = null;
				assemblyName = null;
				return false;
			}

			//Filter the assembly qualified name down to the basic type by removing pointer, reference, and array markers on the type
			//We must also convert nested types from + to / to match cecil's formatting
			value = value
				.Replace ('+', '/')
				.Replace ("*", string.Empty)
				.Replace ("&", string.Empty);

			while (value.IndexOf ('[') > 0) {
				var openidx = value.IndexOf ('[');
				var closeidx = value.IndexOf (']');
				
				// No matching close ] or out of order
				if (closeidx < 0 || closeidx < openidx) {
					typeName = null;
					assemblyName = null;
					return false;
				}

				value = value.Remove (openidx, closeidx + 1 - openidx);
			}

			var tokens = value.Split (',');
			typeName = tokens [0].Trim ();
			assemblyName = null;
			if (tokens.Length > 1)
				assemblyName = tokens [1].Trim ();

			if (string.IsNullOrWhiteSpace (typeName)) {
				typeName = null;
				assemblyName = null;
				return false;
			}

			return true;
		}
	}
}