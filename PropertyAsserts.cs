#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.ComponentModel;
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
		/// Verifies that the provided object raised <see cref="INotifyPropertyChanged.PropertyChanged"/>
		/// as a result of executing the given test code.
		/// </summary>
		/// <param name="object">The object which should raise the notification</param>
		/// <param name="propertyName">The property name for which the notification should be raised</param>
		/// <param name="testCode">The test code which should cause the notification to be raised</param>
		/// <exception cref="PropertyChangedException">Thrown when the notification is not raised</exception>
		public static void PropertyChanged(
			INotifyPropertyChanged @object,
			string propertyName,
			Action testCode)
		{
			GuardArgumentNotNull(nameof(@object), @object);
			GuardArgumentNotNull(nameof(propertyName), propertyName);
			GuardArgumentNotNull(nameof(testCode), testCode);

			var propertyChangeHappened = false;

			PropertyChangedEventHandler handler = (sender, args) => propertyChangeHappened |= string.IsNullOrEmpty(args.PropertyName) || propertyName.Equals(args.PropertyName, StringComparison.OrdinalIgnoreCase);

			@object.PropertyChanged += handler;

			try
			{
				testCode();
				if (!propertyChangeHappened)
					throw new PropertyChangedException(propertyName);
			}
			finally
			{
				@object.PropertyChanged -= handler;
			}
		}

		/// <summary/>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("You must call Assert.PropertyChangedAsync (and await the result) when testing async code.", true)]
		public static void PropertyChanged(
			INotifyPropertyChanged @object,
			string propertyName,
			Func<Task> testCode)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Verifies that the provided object raised <see cref="INotifyPropertyChanged.PropertyChanged"/>
		/// as a result of executing the given test code.
		/// </summary>
		/// <param name="object">The object which should raise the notification</param>
		/// <param name="propertyName">The property name for which the notification should be raised</param>
		/// <param name="testCode">The test code which should cause the notification to be raised</param>
		/// <exception cref="PropertyChangedException">Thrown when the notification is not raised</exception>
		public static async Task PropertyChangedAsync(
			INotifyPropertyChanged @object,
			string propertyName,
			Func<Task> testCode)
		{
			GuardArgumentNotNull(nameof(@object), @object);
			GuardArgumentNotNull(nameof(propertyName), propertyName);
			GuardArgumentNotNull(nameof(testCode), testCode);

			var propertyChangeHappened = false;

			PropertyChangedEventHandler handler = (sender, args) => propertyChangeHappened |= string.IsNullOrEmpty(args.PropertyName) || propertyName.Equals(args.PropertyName, StringComparison.OrdinalIgnoreCase);

			@object.PropertyChanged += handler;

			try
			{
				await testCode();
				if (!propertyChangeHappened)
					throw new PropertyChangedException(propertyName);
			}
			finally
			{
				@object.PropertyChanged -= handler;
			}
		}
	}
}
