#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if XUNIT_NULLABLE
using System.Diagnostics.CodeAnalysis;
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Default implementation of <see cref="IEqualityComparer{T}"/> used by the xUnit.net equality assertions.
	/// </summary>
	/// <typeparam name="T">The type that is being compared.</typeparam>
	class AssertEqualityComparer<T> : IEqualityComparer<T>
	{
		static readonly IEqualityComparer DefaultInnerComparer = new AssertEqualityComparerAdapter<object>(new AssertEqualityComparer<object>());
		static readonly TypeInfo NullableTypeInfo = typeof(Nullable<>).GetTypeInfo();

		readonly Func<IEqualityComparer> innerComparerFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="AssertEqualityComparer{T}" /> class.
		/// </summary>
		/// <param name="innerComparer">The inner comparer to be used when the compared objects are enumerable.</param>
#if XUNIT_NULLABLE
		public AssertEqualityComparer(IEqualityComparer? innerComparer = null)
#else
		public AssertEqualityComparer(IEqualityComparer innerComparer = null)
#endif
		{
			// Use a thunk to delay evaluation of DefaultInnerComparer
			innerComparerFactory = () => innerComparer ?? DefaultInnerComparer;
		}

		/// <inheritdoc/>
		public bool Equals(
#if XUNIT_NULLABLE
			[AllowNull] T x,
			[AllowNull] T y)
#else
			T x,
			T y)
#endif
		{
			int? _;

			return Equals(x, y, out _);
		}

		/// <inheritdoc/>
		public bool Equals(
#if XUNIT_NULLABLE
			[AllowNull] T x,
			[AllowNull] T y,
#else
			T x,
			T y,
#endif
			out int? mismatchIndex)
		{
			mismatchIndex = null;
			var typeInfo = typeof(T).GetTypeInfo();

			// Null?
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;

			// Implements IEquatable<T>?
			var equatable = x as IEquatable<T>;
			if (equatable != null)
				return equatable.Equals(y);

			// Implements IComparable<T>?
			var comparableGeneric = x as IComparable<T>;
			if (comparableGeneric != null)
			{
				try
				{
					return comparableGeneric.CompareTo(y) == 0;
				}
				catch
				{
					// Some implementations of IComparable<T>.CompareTo throw exceptions in
					// certain situations, such as if x can't compare against y.
					// If this happens, just swallow up the exception and continue comparing.
				}
			}

			// Implements IComparable?
			var comparable = x as IComparable;
			if (comparable != null)
			{
				try
				{
					return comparable.CompareTo(y) == 0;
				}
				catch
				{
					// Some implementations of IComparable.CompareTo throw exceptions in
					// certain situations, such as if x can't compare against y.
					// If this happens, just swallow up the exception and continue comparing.
				}
			}

			// Dictionaries?
			var dictionariesEqual = CheckIfDictionariesAreEqual(x, y);
			if (dictionariesEqual.HasValue)
				return dictionariesEqual.GetValueOrDefault();

			// Sets?
			var setsEqual = CheckIfSetsAreEqual(x, y, typeInfo);
			if (setsEqual.HasValue)
				return setsEqual.GetValueOrDefault();

			// Enumerable?
			var enumerablesEqual = CheckIfEnumerablesAreEqual(x, y, out mismatchIndex);
			if (enumerablesEqual.HasValue)
			{
				if (!enumerablesEqual.GetValueOrDefault())
				{
					return false;
				}

				// Array.GetEnumerator() flattens out the array, ignoring array ranks and lengths
				var xArray = x as Array;
				var yArray = y as Array;
				if (xArray != null && yArray != null)
				{
					// new object[2,1] != new object[2]
					if (xArray.Rank != yArray.Rank)
						return false;

					// new object[2,1] != new object[1,2]
					for (var i = 0; i < xArray.Rank; i++)
						if (xArray.GetLength(i) != yArray.GetLength(i))
							return false;
				}

				return true;
			}

			// Implements IStructuralEquatable?
			var structuralEquatable = x as IStructuralEquatable;
			if (structuralEquatable != null && structuralEquatable.Equals(y, new TypeErasedEqualityComparer(innerComparerFactory())))
				return true;

			// Implements IEquatable<typeof(y)>?
			var iequatableY = typeof(IEquatable<>).MakeGenericType(y.GetType()).GetTypeInfo();
			if (iequatableY.IsAssignableFrom(x.GetType().GetTypeInfo()))
			{
				var equalsMethod = iequatableY.GetDeclaredMethod(nameof(IEquatable<T>.Equals));
				if (equalsMethod == null)
					return false;

#if XUNIT_NULLABLE
				return equalsMethod.Invoke(x, new object[] { y }) is true;
#else
				return (bool)equalsMethod.Invoke(x, new object[] { y });
#endif
			}

			// Implements IComparable<typeof(y)>?
			var icomparableY = typeof(IComparable<>).MakeGenericType(y.GetType()).GetTypeInfo();
			if (icomparableY.IsAssignableFrom(x.GetType().GetTypeInfo()))
			{
				var compareToMethod = icomparableY.GetDeclaredMethod(nameof(IComparable<T>.CompareTo));
				if (compareToMethod == null)
					return false;

				try
				{
#if XUNIT_NULLABLE
					return compareToMethod.Invoke(x, new object[] { y }) is 0;
#else
					return (int)compareToMethod.Invoke(x, new object[] { y }) == 0;
#endif
				}
				catch
				{
					// Some implementations of IComparable.CompareTo throw exceptions in
					// certain situations, such as if x can't compare against y.
					// If this happens, just swallow up the exception and continue comparing.
				}
			}

			// Last case, rely on object.Equals
			return object.Equals(x, y);
		}

		bool? CheckIfEnumerablesAreEqual(
#if XUNIT_NULLABLE
			[AllowNull] T x,
			[AllowNull] T y,
#else
			T x,
			T y,
#endif
			out int? mismatchIndex)
		{
			mismatchIndex = null;

			var enumerableX = x as IEnumerable;
			var enumerableY = y as IEnumerable;

			if (enumerableX == null || enumerableY == null)
				return null;

			var enumeratorX = default(IEnumerator);
			var enumeratorY = default(IEnumerator);

			try
			{
				enumeratorX = enumerableX.GetEnumerator();
				enumeratorY = enumerableY.GetEnumerator();
				var equalityComparer = innerComparerFactory();

				mismatchIndex = 0;

				while (true)
				{
					var hasNextX = enumeratorX.MoveNext();
					var hasNextY = enumeratorY.MoveNext();

					if (!hasNextX || !hasNextY)
					{
						if (hasNextX == hasNextY)
						{
							mismatchIndex = null;
							return true;
						}

						return false;
					}

					if (!equalityComparer.Equals(enumeratorX.Current, enumeratorY.Current))
						return false;

					mismatchIndex++;
				}
			}
			finally
			{
				var asDisposable = enumeratorX as IDisposable;
				if (asDisposable != null)
					asDisposable.Dispose();
				asDisposable = enumeratorY as IDisposable;
				if (asDisposable != null)
					asDisposable.Dispose();
			}
		}

		bool? CheckIfDictionariesAreEqual(
#if XUNIT_NULLABLE
			[AllowNull] T x,
			[AllowNull] T y)
#else
			T x,
			T y)
#endif
		{
			var dictionaryX = x as IDictionary;
			var dictionaryY = y as IDictionary;

			if (dictionaryX == null || dictionaryY == null)
				return null;

			if (dictionaryX.Count != dictionaryY.Count)
				return false;

			var equalityComparer = innerComparerFactory();
			var dictionaryYKeys = new HashSet<object>(dictionaryY.Keys.Cast<object>());

			foreach (var key in dictionaryX.Keys.Cast<object>())
			{
				if (!dictionaryYKeys.Contains(key))
					return false;

				var valueX = dictionaryX[key];
				var valueY = dictionaryY[key];

				if (!equalityComparer.Equals(valueX, valueY))
					return false;

				dictionaryYKeys.Remove(key);
			}

			return dictionaryYKeys.Count == 0;
		}

#if XUNIT_NULLABLE
		static MethodInfo? s_compareTypedSetsMethod;
#else
		static MethodInfo s_compareTypedSetsMethod;
#endif

		bool? CheckIfSetsAreEqual(
#if XUNIT_NULLABLE
			[AllowNull] T x,
			[AllowNull] T y,
#else
			T x,
			T y,
#endif
			TypeInfo typeInfo)
		{
			if (!IsSet(typeInfo))
				return null;

			var enumX = x as IEnumerable;
			var enumY = y as IEnumerable;
			if (enumX == null || enumY == null)
				return null;

			Type elementType;
			if (typeof(T).GenericTypeArguments.Length != 1)
				elementType = typeof(object);
			else
				elementType = typeof(T).GenericTypeArguments[0];

			if (s_compareTypedSetsMethod == null)
			{
				s_compareTypedSetsMethod = GetType().GetTypeInfo().GetDeclaredMethod(nameof(CompareTypedSets));
				if (s_compareTypedSetsMethod == null)
					return false;
			}

			var method = s_compareTypedSetsMethod.MakeGenericMethod(new Type[] { elementType });
#if XUNIT_NULLABLE
			return method.Invoke(this, new object[] { enumX, enumY }) is true;
#else
			return (bool)method.Invoke(this, new object[] { enumX, enumY });
#endif
		}

		bool CompareTypedSets<R>(
			IEnumerable enumX,
			IEnumerable enumY)
		{
			var setX = new HashSet<R>(enumX.Cast<R>());
			var setY = new HashSet<R>(enumY.Cast<R>());

			return setX.SetEquals(setY);
		}

		bool IsSet(TypeInfo typeInfo) =>
			typeInfo
				.ImplementedInterfaces
				.Select(i => i.GetTypeInfo())
				.Where(ti => ti.IsGenericType)
				.Select(ti => ti.GetGenericTypeDefinition())
				.Contains(typeof(ISet<>).GetGenericTypeDefinition());

		/// <inheritdoc/>
		public int GetHashCode(T obj)
		{
			throw new NotImplementedException();
		}

		private class TypeErasedEqualityComparer : IEqualityComparer
		{
			readonly IEqualityComparer innerComparer;

			public TypeErasedEqualityComparer(IEqualityComparer innerComparer)
			{
				this.innerComparer = innerComparer;
			}

#if XUNIT_NULLABLE
			static MethodInfo? s_equalsMethod;
#else
			static MethodInfo s_equalsMethod;
#endif

			public new bool Equals(
#if XUNIT_NULLABLE
				object? x,
				object? y)
#else
				object x,
				object y)
#endif
			{
				if (x == null)
					return y == null;
				if (y == null)
					return false;

				// Delegate checking of whether two objects are equal to AssertEqualityComparer.
				// To get the best result out of AssertEqualityComparer, we attempt to specialize the
				// comparer for the objects that we are checking.
				// If the objects are the same, great! If not, assume they are objects.
				// This is more naive than the C# compiler which tries to see if they share any interfaces
				// etc. but that's likely overkill here as AssertEqualityComparer<object> is smart enough.
				Type objectType = x.GetType() == y.GetType() ? x.GetType() : typeof(object);

				// Lazily initialize and cache the EqualsGeneric<U> method.
				if (s_equalsMethod == null)
				{
					s_equalsMethod = typeof(TypeErasedEqualityComparer).GetTypeInfo().GetDeclaredMethod(nameof(EqualsGeneric));
					if (s_equalsMethod == null)
						return false;
				}

#if XUNIT_NULLABLE
				return s_equalsMethod.MakeGenericMethod(objectType).Invoke(this, new object[] { x, y }) is true;
#else
				return (bool)s_equalsMethod.MakeGenericMethod(objectType).Invoke(this, new object[] { x, y });
#endif
			}

			bool EqualsGeneric<U>(
				U x,
				U y) =>
					new AssertEqualityComparer<U>(innerComparer: innerComparer).Equals(x, y);

			public int GetHashCode(object obj)
			{
				throw new NotImplementedException();
			}
		}
	}
}
