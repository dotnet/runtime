// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILLink.Shared;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	public class ValueNodeList : List<MultiValue>
	{
		public ValueNodeList ()
		{
		}

		public ValueNodeList (int capacity)
			: base (capacity)
		{
		}

		public ValueNodeList (List<MultiValue> other)
			: base (other)
		{
		}

		public override int GetHashCode ()
		{
			HashCode hashCode = default;
			foreach (var item in this)
				hashCode.Add (item.GetHashCode ());
			return hashCode.ToHashCode ();
		}

		public override bool Equals (object? other)
		{
			if (!(other is ValueNodeList otherList))
				return false;

			if (otherList.Count != Count)
				return false;

			for (int i = 0; i < Count; i++) {
				if (!otherList[i].Equals (this[i]))
					return false;
			}
			return true;
		}
	}

	public struct ValueBasicBlockPair : IEquatable<ValueBasicBlockPair>
	{
		public ValueBasicBlockPair (MultiValue value, int basicBlockIndex)
		{
			Value = value;
			BasicBlockIndex = basicBlockIndex;
		}

		public MultiValue Value { get; }
		public int BasicBlockIndex { get; }

		public bool Equals (ValueBasicBlockPair other) => Value.Equals (other.Value) && BasicBlockIndex.Equals (other.BasicBlockIndex);

		public override bool Equals (object? obj) => obj is ValueBasicBlockPair other && Equals (other);

		public override int GetHashCode () => HashUtils.Combine (Value.GetHashCode (), BasicBlockIndex);

		public static bool operator == (ValueBasicBlockPair left, ValueBasicBlockPair right) => left.Equals (right);
		public static bool operator != (ValueBasicBlockPair left, ValueBasicBlockPair right) => !(left == right);
	}
}
