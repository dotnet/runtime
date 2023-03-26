#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.ComponentModel;
using System.Threading.Tasks;

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
		/// Records any exception which is thrown by the given code.
		/// </summary>
		/// <param name="testCode">The code which may thrown an exception.</param>
		/// <returns>Returns the exception that was thrown by the code; null, otherwise.</returns>
#if XUNIT_NULLABLE
		protected static Exception? RecordException(Action testCode)
#else
		protected static Exception RecordException(Action testCode)
#endif
		{
			GuardArgumentNotNull(nameof(testCode), testCode);

			try
			{
				testCode();
				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}

		/// <summary>
		/// Records any exception which is thrown by the given code that has
		/// a return value. Generally used for testing property accessors.
		/// </summary>
		/// <param name="testCode">The code which may thrown an exception.</param>
		/// <returns>Returns the exception that was thrown by the code; null, otherwise.</returns>
#if XUNIT_NULLABLE
		protected static Exception? RecordException(Func<object?> testCode)
#else
		protected static Exception RecordException(Func<object> testCode)
#endif
		{
			GuardArgumentNotNull(nameof(testCode), testCode);
			var task = default(Task);

			try
			{
				task = testCode() as Task;
			}
			catch (Exception ex)
			{
				return ex;
			}

			if (task != null)
				throw new InvalidOperationException("You must call Assert.ThrowsAsync, Assert.DoesNotThrowAsync, or Record.ExceptionAsync when testing async code.");

			return null;
		}

		/// <summary/>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("You must call Assert.RecordExceptionAsync (and await the result) when testing async code.", true)]
		protected static Exception RecordException(Func<Task> testCode)
		{
			throw new NotImplementedException();
		}

#if XUNIT_VALUETASK
		/// <summary/>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("You must call Assert.RecordExceptionAsync (and await the result) when testing async code.", true)]
		protected static Exception RecordException(Func<ValueTask> testCode)
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("You must call Assert.RecordExceptionAsync (and await the result) when testing async code.", true)]
		protected static Exception RecordException<T>(Func<ValueTask<T>> testCode)
		{
			throw new NotImplementedException();
		}
#endif

		/// <summary>
		/// Records any exception which is thrown by the given task.
		/// </summary>
		/// <param name="testCode">The task which may thrown an exception.</param>
		/// <returns>Returns the exception that was thrown by the code; null, otherwise.</returns>
#if XUNIT_NULLABLE
		protected static async Task<Exception?> RecordExceptionAsync(Func<Task> testCode)
#else
		protected static async Task<Exception> RecordExceptionAsync(Func<Task> testCode)
#endif
		{
			GuardArgumentNotNull(nameof(testCode), testCode);

			try
			{
				await testCode();
				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}

#if XUNIT_VALUETASK
		/// <summary>
		/// Records any exception which is thrown by the given task.
		/// </summary>
		/// <param name="testCode">The task which may thrown an exception.</param>
		/// <returns>Returns the exception that was thrown by the code; null, otherwise.</returns>
#if XUNIT_NULLABLE
		protected static async ValueTask<Exception?> RecordExceptionAsync(Func<ValueTask> testCode)
#else
		protected static async ValueTask<Exception> RecordExceptionAsync(Func<ValueTask> testCode)
#endif
		{
			GuardArgumentNotNull(nameof(testCode), testCode);

			try
			{
				await testCode();
				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}

		/// <summary>
		/// Records any exception which is thrown by the given task.
		/// </summary>
		/// <param name="testCode">The task which may thrown an exception.</param>
		/// <typeparam name="T">The type of the ValueTask return value.</typeparam>
		/// <returns>Returns the exception that was thrown by the code; null, otherwise.</returns>
#if XUNIT_NULLABLE
		protected static async ValueTask<Exception?> RecordExceptionAsync<T>(Func<ValueTask<T>> testCode)
#else
		protected static async ValueTask<Exception> RecordExceptionAsync<T>(Func<ValueTask<T>> testCode)
#endif
		{
			GuardArgumentNotNull(nameof(testCode), testCode);

			try
			{
				await testCode();
				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}
#endif
	}
}
