using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.Linker.Tests.Cases.LinkAttributes.Dependencies
{
	public class AttributeFromCopyAssemblyAttribute : Attribute
	{
		// Add all kinds of members - as they cause the type to be marked again
		// and we don't want these to generate more warnings.

		private object _field;

		private class NestedType
		{
		}

		private int TestProperty { get; }

		private event EventHandler _event;
	}
}
