// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILLink.Shared.DataFlow
{
	// Adds ability to deep copy a value
	public interface IDeepCopyValue<TSingleValue>
	{
		public TSingleValue DeepCopy ();
	}
}