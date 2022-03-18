// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILLink.Shared.DataFlow
{
	// This is a sum type over the various kinds of values we track:
	// - dynamicallyaccessedmembertypes-annotated locations (types or strings)
	// - known typeof values and similar
	// - known strings
	// - known integers

	public abstract record SingleValue : IDeepCopyValue<SingleValue>
	{
		// All values must explicitely declare their ability to deep copy itself.
		// If the value is immutable, it can "return this" as an optimization.
		// Note: Since immutability is relatively tricky to determine, we require all values
		//       to explicitly implement the DeepCopy, even though the expectation is that
		//       most values will just "return this".
		public abstract SingleValue DeepCopy ();
	}
}