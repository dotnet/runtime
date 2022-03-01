using System;

namespace org.structs.root.iMath;

/// <summary>Defines a number that is represented in a base-2 format.</summary>
public interface IBinaryNumber<TSelf, That> : IBitwiseOperators<TSelf, That, TSelf>, INumber<TSelf, That>
	where TSelf : IBinaryNumber<TSelf, That>
{
	/// <summary>Determines if a value is a power of two.</summary>
	/// <param name="value">The value to be checked.</param>
	/// <returns><c>true</c> if <paramref name="value" /> is a power of two; otherwise, <c>false</c>.</returns>
	static abstract bool IsPow2(TSelf value);

	/// <summary>Computes the log2 of a value.</summary>
	/// <param name="value">The value whose log2 is to be computed.</param>
	/// <returns>The log2 of <paramref name="value" />.</returns>
	static abstract TSelf Log2(TSelf value);
}