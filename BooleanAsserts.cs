#if XUNIT_NULLABLE
#nullable enable
#endif

using Xunit.Sdk;

#if XUNIT_NULLABLE
using System.Diagnostics.CodeAnalysis;
#endif

namespace Xunit
{
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	partial class Assert
	{
		/// <summary>
		/// Verifies that the condition is false.
		/// </summary>
		/// <param name="condition">The condition to be tested</param>
		/// <exception cref="FalseException">Thrown if the condition is not false</exception>
#if XUNIT_NULLABLE
		public static void False([DoesNotReturnIf(parameterValue: true)] bool condition)
#else
		public static void False(bool condition)
#endif
		{
			False((bool?)condition, null);
		}

		/// <summary>
		/// Verifies that the condition is false.
		/// </summary>
		/// <param name="condition">The condition to be tested</param>
		/// <exception cref="FalseException">Thrown if the condition is not false</exception>
#if XUNIT_NULLABLE
		public static void False([DoesNotReturnIf(parameterValue: true)] bool? condition)
#else
		public static void False(bool? condition)
#endif
		{
			False(condition, null);
		}

		/// <summary>
		/// Verifies that the condition is false.
		/// </summary>
		/// <param name="condition">The condition to be tested</param>
		/// <param name="userMessage">The message to show when the condition is not false</param>
		/// <exception cref="FalseException">Thrown if the condition is not false</exception>
		public static void False(
#if XUNIT_NULLABLE
			[DoesNotReturnIf(parameterValue: true)] bool condition,
			string? userMessage) =>
#else
			bool condition,
			string userMessage) =>
#endif
				False((bool?)condition, userMessage);

		/// <summary>
		/// Verifies that the condition is false.
		/// </summary>
		/// <param name="condition">The condition to be tested</param>
		/// <param name="userMessage">The message to show when the condition is not false</param>
		/// <exception cref="FalseException">Thrown if the condition is not false</exception>
		public static void False(
#if XUNIT_NULLABLE
			[DoesNotReturnIf(parameterValue: true)] bool? condition,
			string? userMessage)
#else
			bool? condition,
			string userMessage)
#endif
		{
			if (!condition.HasValue || condition.GetValueOrDefault())
				throw new FalseException(userMessage, condition);
		}

		/// <summary>
		/// Verifies that an expression is true.
		/// </summary>
		/// <param name="condition">The condition to be inspected</param>
		/// <exception cref="TrueException">Thrown when the condition is false</exception>
#if XUNIT_NULLABLE
		public static void True([DoesNotReturnIf(parameterValue: false)] bool condition)
#else
		public static void True(bool condition)
#endif
		{
			True((bool?)condition, null);
		}

		/// <summary>
		/// Verifies that an expression is true.
		/// </summary>
		/// <param name="condition">The condition to be inspected</param>
		/// <exception cref="TrueException">Thrown when the condition is false</exception>
#if XUNIT_NULLABLE
		public static void True([DoesNotReturnIf(parameterValue: false)] bool? condition)
#else
		public static void True(bool? condition)
#endif
		{
			True(condition, null);
		}

		/// <summary>
		/// Verifies that an expression is true.
		/// </summary>
		/// <param name="condition">The condition to be inspected</param>
		/// <param name="userMessage">The message to be shown when the condition is false</param>
		/// <exception cref="TrueException">Thrown when the condition is false</exception>
		public static void True(
#if XUNIT_NULLABLE
			[DoesNotReturnIf(parameterValue: false)] bool condition,
			string? userMessage) =>
#else
			bool condition,
			string userMessage) =>
#endif
				True((bool?)condition, userMessage);

		/// <summary>
		/// Verifies that an expression is true.
		/// </summary>
		/// <param name="condition">The condition to be inspected</param>
		/// <param name="userMessage">The message to be shown when the condition is false</param>
		/// <exception cref="TrueException">Thrown when the condition is false</exception>
		public static void True(
#if XUNIT_NULLABLE
			[DoesNotReturnIf(parameterValue: false)] bool? condition,
			string? userMessage)
#else
			bool? condition,
			string userMessage)
#endif
		{
			if (!condition.HasValue || !condition.GetValueOrDefault())
				throw new TrueException(userMessage, condition);
		}
	}
}
