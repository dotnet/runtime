#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.Linq;
using System.Reflection;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when code unexpectedly fails to raise an event.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class RaisesException : XunitException
	{
#if XUNIT_NULLABLE
		readonly string? stackTrace = null;
#else
		readonly string stackTrace = null;
#endif

		/// <summary>
		/// Creates a new instance of the <see cref="RaisesException" /> class. Call this constructor
		/// when no event was raised.
		/// </summary>
		/// <param name="expected">The type of the event args that was expected</param>
		public RaisesException(Type expected)
			: base("(No event was raised)")
		{
			Expected = ConvertToSimpleTypeName(expected.GetTypeInfo());
			Actual = "(No event was raised)";
		}

		/// <summary>
		/// Creates a new instance of the <see cref="RaisesException" /> class. Call this constructor
		/// when an
		/// </summary>
		/// <param name="expected"></param>
		/// <param name="actual"></param>
		public RaisesException(Type expected, Type actual)
			: base("(Raised event did not match expected event)")
		{
			Expected = ConvertToSimpleTypeName(expected.GetTypeInfo());
			Actual = ConvertToSimpleTypeName(actual.GetTypeInfo());
		}

		/// <summary>
		/// Gets the actual value.
		/// </summary>
		public string Actual { get; }

		/// <summary>
		/// Gets the expected value.
		/// </summary>
		public string Expected { get; }

		/// <summary>
		/// Gets a message that describes the current exception. Includes the expected and actual values.
		/// </summary>
		/// <returns>The error message that explains the reason for the exception, or an empty string("").</returns>
		/// <filterpriority>1</filterpriority>
		public override string Message =>
			$"{base.Message}{Environment.NewLine}{Expected ?? "(null)"}{Environment.NewLine}{Actual ?? "(null)"}";

		/// <summary>
		/// Gets a string representation of the frames on the call stack at the time the current exception was thrown.
		/// </summary>
		/// <returns>A string that describes the contents of the call stack, with the most recent method call appearing first.</returns>
#if XUNIT_NULLABLE
		public override string? StackTrace => stackTrace ?? base.StackTrace;
#else
		public override string StackTrace => stackTrace ?? base.StackTrace;
#endif

		static string ConvertToSimpleTypeName(TypeInfo typeInfo)
		{
			if (!typeInfo.IsGenericType)
				return typeInfo.Name;

			var simpleNames = typeInfo.GenericTypeArguments.Select(type => ConvertToSimpleTypeName(type.GetTypeInfo()));
			var backTickIdx = typeInfo.Name.IndexOf('`');
			if (backTickIdx < 0)
				backTickIdx = typeInfo.Name.Length;  // F# doesn't use backticks for generic type names

			return $"{typeInfo.Name.Substring(0, backTickIdx)}<{string.Join(", ", simpleNames)}>";
		}
	}
}
