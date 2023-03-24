// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

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
