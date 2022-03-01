// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System;

/// <summary> <see cref="Create{TOther}"/> converts from TOther to <typeparamref name="TSelf"/> </summary>
[RequiresPreviewFeatures(Number.PreviewFeatureMessage, Url = Number.PreviewFeatureUrl)]
public interface ICreate<TSelf> //: IEntity<TSelf, That> where TSelf : IEqualityOperators<TSelf, That>
{
    /// <summary> Static Factory Method to create Instances from any other <see cref="INumber{TOther}"/> </summary>
    /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
    /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
    /// <returns>An instance of <typeparamref name="TSelf" /> created from <paramref name="value" />.</returns>
    /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
    /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <typeparamref name="TSelf" />.</exception>
    /// <remarks>
    /// These static Factories make Member Factory Method largely obsolete and,
    /// together with generic Interface-Methods, allow for largely Client-driven Instancing.
    /// </remarks>
    static abstract TSelf Create<TOther>(TOther value);//where TOther : INumber<TOther, That>;

    /// <summary>Tries to create an instance of the current type from a value.</summary>
    /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
    /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
    /// <param name="result">On return, contains the result of successfully creating an instance of <typeparamref name="TSelf" /> from <paramref name="value" /> or an undefined value on failure.</param>
    /// <returns><c>true</c> if <paramref name="value" /> an instance of the current type was successfully created from <paramref name="value" />; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
    static abstract bool TryCreate<TOther>(TOther value, out TSelf result);// where TOther : INumber<TOther, That>;
}

[RequiresPreviewFeatures(Number.PreviewFeatureMessage, Url = Number.PreviewFeatureUrl)]
public interface ISemiAddGroup<TSelf, That> : ICreate<TSelf>, IAdditionOperators<TSelf, That, That>,
    IAdditiveIdentity<TSelf, That>,
    IUnaryPlusOperators<TSelf, TSelf>
    where TSelf : ISemiAddGroup<TSelf, That>
{
    /// <summary>Gets the value <c>0</c> for the type.</summary>
    static abstract TSelf Zero { get; }
}

[RequiresPreviewFeatures(Number.PreviewFeatureMessage, Url = Number.PreviewFeatureUrl)]
public interface IAddGroup<TSelf, That> : ISemiAddGroup<TSelf, That>, ISubtractionOperators<TSelf, That, That>,
    IUnaryNegationOperators<TSelf, TSelf>,
    IEqualityOperators<TSelf, That>
    where TSelf : IEqualityOperators<TSelf, That>, IAddGroup<TSelf, That>
{ }

[RequiresPreviewFeatures(Number.PreviewFeatureMessage, Url = Number.PreviewFeatureUrl)]
public interface ISemiGroup<TSelf, That> : ICreate<TSelf>, IMultiplyOperators<TSelf, That, That>,
    IMultiplicativeIdentity<TSelf, That>
    where TSelf : ISemiGroup<TSelf, That>
{
    /// <summary>Gets the value <c>1</c> for the type.</summary>
    static abstract TSelf One { get; }
}

[RequiresPreviewFeatures(Number.PreviewFeatureMessage, Url = Number.PreviewFeatureUrl)]
public interface IGroup<TSelf, That> : ISemiGroup<TSelf, That>, IDivisionOperators<TSelf, That, That>,
    IEqualityOperators<TSelf, That>
    where TSelf : IEqualityOperators<TSelf, That>, IGroup<TSelf, That> { }

[RequiresPreviewFeatures(Number.PreviewFeatureMessage, Url = Number.PreviewFeatureUrl)]
public interface IRing<TSelf, That> : IAddGroup<TSelf, That>, ISemiGroup<TSelf, That>, IMultiplyOperators<TSelf, That, That> where TSelf : IRing<TSelf, That> { }

[RequiresPreviewFeatures(Number.PreviewFeatureMessage, Url = Number.PreviewFeatureUrl)]
public interface IRing1<TSelf, That> : IRing<TSelf, That>, IMultiplicativeIdentity<TSelf, That>, //IDivisionOperators<TSelf, That, That>
    IDecrementOperators<TSelf>,
    IIncrementOperators<TSelf>,
    IModulusOperators<TSelf, That, That>
    where TSelf : IRing1<TSelf, That> { }

[RequiresPreviewFeatures(Number.PreviewFeatureMessage, Url = Number.PreviewFeatureUrl)]
public interface IField<TSelf, That> : IRing1<TSelf, That>, IDivisionOperators<TSelf, That, That>
    where TSelf : IField<TSelf, That>
{
    /// <summary>Computes the quotient and remainder of two values.</summary>
    /// <param name="left">The value which <paramref name="right" /> divides.</param>
    /// <param name="right">The value which divides <paramref name="left" />.</param>
    /// <returns>The quotient and remainder of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
    static abstract (That Quotient, That Remainder) DivRem(TSelf left, That right);
}

/// <summary> General 'Number' without total Order, including see cref="System.Numerics.Complex"/>
/// , <see cref="System.Numerics.Quaternion"/>n <see cref="System.Numerics.Vector2"/> etc. </summary>
[RequiresPreviewFeatures(Number.PreviewFeatureMessage, Url = Number.PreviewFeatureUrl)]
public interface INumber<TSelf, That> : IField<TSelf, That>, ISpanParseable<TSelf>, ISpanFormattable
    where TSelf : INumber<TSelf, That>//, ISpanParseable<TSelf>
{
    double Norm();
}
