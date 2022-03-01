namespace System;

/// <summary>Defines a number type which can represent both positive and negative values.</summary>
/// <remarks>
/// This weak Notion of 'Orientation' becomes more important in higher Dimensions R^n,
/// where the Orientation/Sign has (n-1) Dimensions with the Magnitude/Length forming the last Dimension.
/// 
/// In Fact the simple Sign Bit becomes the Orientation/Arg of the Polar Representation.
/// </remarks>
public interface ISignedNumber<TSelf, That> : INumber<TSelf, That> where TSelf : ISignedNumber<TSelf, That>
{
    /// <summary>Gets the value <c>-1</c> for the type.</summary>
    static abstract TSelf NegativeOne { get; }

}
