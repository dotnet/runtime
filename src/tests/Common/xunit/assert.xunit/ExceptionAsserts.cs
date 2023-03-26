#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Sdk;

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
		/// Verifies that the exact exception is thrown (and not a derived exception type).
		/// </summary>
		/// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
		/// <param name="testCode">A delegate to the code to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static T Throws<T>(Action testCode)
			where T : Exception =>
				(T)Throws(typeof(T), RecordException(testCode));

		/// <summary>
		/// Verifies that the exact exception is thrown (and not a derived exception type).
		/// Generally used to test property accessors.
		/// </summary>
		/// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
		/// <param name="testCode">A delegate to the code to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
#if XUNIT_NULLABLE
		public static T Throws<T>(Func<object?> testCode)
#else
		public static T Throws<T>(Func<object> testCode)
#endif
			where T : Exception =>
				(T)Throws(typeof(T), RecordException(testCode));

		/// <summary/>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("You must call Assert.ThrowsAsync<T> (and await the result) when testing async code.", true)]
		public static T Throws<T>(Func<Task> testCode)
			where T : Exception
		{
			throw new NotImplementedException();
		}

#if XUNIT_VALUETASK
		/// <summary/>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("You must call Assert.ThrowsAsync<T> (and await the result) when testing async code.", true)]
		public static T Throws<T>(Func<ValueTask> testCode)
			where T : Exception
		{
			throw new NotImplementedException();
		}
#endif

		/// <summary>
		/// Verifies that the exact exception is thrown (and not a derived exception type).
		/// </summary>
		/// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
		/// <param name="testCode">A delegate to the task to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static async Task<T> ThrowsAsync<T>(Func<Task> testCode)
			where T : Exception =>
				(T)Throws(typeof(T), await RecordExceptionAsync(testCode));

#if XUNIT_VALUETASK
		/// <summary>
		/// Verifies that the exact exception is thrown (and not a derived exception type).
		/// </summary>
		/// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
		/// <param name="testCode">A delegate to the task to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static async ValueTask<T> ThrowsAsync<T>(Func<ValueTask> testCode)
			where T : Exception =>
				(T)Throws(typeof(T), await RecordExceptionAsync(testCode));
#endif

		/// <summary>
		/// Verifies that the exact exception or a derived exception type is thrown.
		/// </summary>
		/// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
		/// <param name="testCode">A delegate to the code to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static T ThrowsAny<T>(Action testCode)
			where T : Exception =>
				(T)ThrowsAny(typeof(T), RecordException(testCode));

		/// <summary>
		/// Verifies that the exact exception or a derived exception type is thrown.
		/// Generally used to test property accessors.
		/// </summary>
		/// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
		/// <param name="testCode">A delegate to the code to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
#if XUNIT_NULLABLE
		public static T ThrowsAny<T>(Func<object?> testCode)
#else
		public static T ThrowsAny<T>(Func<object> testCode)
#endif
			where T : Exception =>
				(T)ThrowsAny(typeof(T), RecordException(testCode));

		/// <summary>
		/// Verifies that the exact exception or a derived exception type is thrown.
		/// </summary>
		/// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
		/// <param name="testCode">A delegate to the task to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static async Task<T> ThrowsAnyAsync<T>(Func<Task> testCode)
			where T : Exception =>
				(T)ThrowsAny(typeof(T), await RecordExceptionAsync(testCode));

#if XUNIT_VALUETASK
		/// <summary>
		/// Verifies that the exact exception or a derived exception type is thrown.
		/// </summary>
		/// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
		/// <param name="testCode">A delegate to the task to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static async ValueTask<T> ThrowsAnyAsync<T>(Func<ValueTask> testCode)
			where T : Exception =>
				(T)ThrowsAny(typeof(T), await RecordExceptionAsync(testCode));
#endif

		/// <summary>
		/// Verifies that the exact exception is thrown (and not a derived exception type).
		/// </summary>
		/// <param name="exceptionType">The type of the exception expected to be thrown</param>
		/// <param name="testCode">A delegate to the code to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static Exception Throws(
			Type exceptionType,
			Action testCode) =>
				Throws(exceptionType, RecordException(testCode));

		/// <summary>
		/// Verifies that the exact exception is thrown (and not a derived exception type).
		/// Generally used to test property accessors.
		/// </summary>
		/// <param name="exceptionType">The type of the exception expected to be thrown</param>
		/// <param name="testCode">A delegate to the code to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static Exception Throws(
			Type exceptionType,
#if XUNIT_NULLABLE
			Func<object?> testCode) =>
#else
			Func<object> testCode) =>
#endif
				Throws(exceptionType, RecordException(testCode));

		/// <summary/>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("You must call Assert.ThrowsAsync (and await the result) when testing async code.", true)]
		public static Exception Throws(
			string paramName,
			Func<Task> testCode)
		{
			throw new NotImplementedException();
		}

#if XUNIT_VALUETASK
		/// <summary/>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("You must call Assert.ThrowsAsync (and await the result) when testing async code.", true)]
		public static Exception Throws(
			string paramName,
			Func<ValueTask> testCode)
		{
			throw new NotImplementedException();
		}
#endif

		/// <summary>
		/// Verifies that the exact exception is thrown (and not a derived exception type).
		/// </summary>
		/// <param name="exceptionType">The type of the exception expected to be thrown</param>
		/// <param name="testCode">A delegate to the task to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static async Task<Exception> ThrowsAsync(
			Type exceptionType,
			Func<Task> testCode) =>
				Throws(exceptionType, await RecordExceptionAsync(testCode));

#if XUNIT_VALUETASK
		/// <summary>
		/// Verifies that the exact exception is thrown (and not a derived exception type).
		/// </summary>
		/// <param name="exceptionType">The type of the exception expected to be thrown</param>
		/// <param name="testCode">A delegate to the task to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static async ValueTask<Exception> ThrowsAsync(
			Type exceptionType,
			Func<ValueTask> testCode) =>
				Throws(exceptionType, await RecordExceptionAsync(testCode));
#endif

		static Exception Throws(
			Type exceptionType,
#if XUNIT_NULLABLE
			Exception? exception)
#else
			Exception exception)
#endif
		{
			GuardArgumentNotNull(nameof(exceptionType), exceptionType);

			if (exception == null)
				throw new ThrowsException(exceptionType);

			if (!exceptionType.Equals(exception.GetType()))
				throw new ThrowsException(exceptionType, exception);

			return exception;
		}

		static Exception ThrowsAny(
			Type exceptionType,
#if XUNIT_NULLABLE
			Exception? exception)
#else
			Exception exception)
#endif
		{
			GuardArgumentNotNull(nameof(exceptionType), exceptionType);

			if (exception == null)
				throw new ThrowsException(exceptionType);

			if (!exceptionType.GetTypeInfo().IsAssignableFrom(exception.GetType().GetTypeInfo()))
				throw new ThrowsException(exceptionType, exception);

			return exception;
		}

		/// <summary>
		/// Verifies that the exact exception is thrown (and not a derived exception type), where the exception
		/// derives from <see cref="ArgumentException"/> and has the given parameter name.
		/// </summary>
		/// <param name="paramName">The parameter name that is expected to be in the exception</param>
		/// <param name="testCode">A delegate to the code to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static T Throws<T>(
#if XUNIT_NULLABLE
			string? paramName,
#else
			string paramName,
#endif
			Action testCode)
				where T : ArgumentException
		{
			var ex = Throws<T>(testCode);
			Equal(paramName, ex.ParamName);
			return ex;
		}

		/// <summary>
		/// Verifies that the exact exception is thrown (and not a derived exception type), where the exception
		/// derives from <see cref="ArgumentException"/> and has the given parameter name.
		/// </summary>
		/// <param name="paramName">The parameter name that is expected to be in the exception</param>
		/// <param name="testCode">A delegate to the code to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static T Throws<T>(
#if XUNIT_NULLABLE
			string? paramName,
			Func<object?> testCode)
#else
			string paramName,
			Func<object> testCode)
#endif
				where T : ArgumentException
		{
			var ex = Throws<T>(testCode);
			Equal(paramName, ex.ParamName);
			return ex;
		}

		/// <summary/>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("You must call Assert.ThrowsAsync<T> (and await the result) when testing async code.", true)]
		public static T Throws<T>(
#if XUNIT_NULLABLE
			string? paramName,
#else
			string paramName,
#endif
			Func<Task> testCode)
				where T : ArgumentException
		{
			throw new NotImplementedException();
		}

#if XUNIT_VALUETASK
		/// <summary/>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("You must call Assert.ThrowsAsync<T> (and await the result) when testing async code.", true)]
		public static T Throws<T>(
#if XUNIT_NULLABLE
			string? paramName,
#else
			string paramName,
#endif
			Func<ValueTask> testCode)
				where T : ArgumentException
		{
			throw new NotImplementedException();
		}
#endif

		/// <summary>
		/// Verifies that the exact exception is thrown (and not a derived exception type), where the exception
		/// derives from <see cref="ArgumentException"/> and has the given parameter name.
		/// </summary>
		/// <param name="paramName">The parameter name that is expected to be in the exception</param>
		/// <param name="testCode">A delegate to the task to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static async Task<T> ThrowsAsync<T>(
#if XUNIT_NULLABLE
			string? paramName,
#else
			string paramName,
#endif
			Func<Task> testCode)
				where T : ArgumentException
		{
			var ex = await ThrowsAsync<T>(testCode);
			Equal(paramName, ex.ParamName);
			return ex;
		}

#if XUNIT_VALUETASK
		/// <summary>
		/// Verifies that the exact exception is thrown (and not a derived exception type), where the exception
		/// derives from <see cref="ArgumentException"/> and has the given parameter name.
		/// </summary>
		/// <param name="paramName">The parameter name that is expected to be in the exception</param>
		/// <param name="testCode">A delegate to the task to be tested</param>
		/// <returns>The exception that was thrown, when successful</returns>
		/// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
		public static async ValueTask<T> ThrowsAsync<T>(
#if XUNIT_NULLABLE
			string? paramName,
#else
			string paramName,
#endif
			Func<ValueTask> testCode)
				where T : ArgumentException
		{
			var ex = await ThrowsAsync<T>(testCode);
			Equal(paramName, ex.ParamName);
			return ex;
		}
#endif
	}
}
