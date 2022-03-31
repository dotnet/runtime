// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ILLink.Shared.DataFlow
{
	// Adds ability to deep copy a value
	public interface IDeepCopyValue<TSingleValue>
	{
		public TSingleValue DeepCopy ();
	}
}
