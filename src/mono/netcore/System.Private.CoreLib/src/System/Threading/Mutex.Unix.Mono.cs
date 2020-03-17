// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.IO;

namespace System.Threading
{
	partial class Mutex
	{
		Mutex (IntPtr handle) => Handle = handle;

		public void ReleaseMutex ()
		{
			if (!ReleaseMutex_internal (Handle))
				throw new ApplicationException (SR.Arg_SynchronizationLockException);
		}

		void CreateMutexCore (bool initiallyOwned, string name, out bool createdNew) =>
			Handle = CreateMutex_internal (initiallyOwned, name, out createdNew);

		unsafe static IntPtr CreateMutex_internal (bool initiallyOwned, string name, out bool created)
		{
			fixed (char *fixed_name = name)
				return CreateMutex_icall (initiallyOwned, fixed_name,
					name?.Length ?? 0, out created);
		}

		static OpenExistingResult OpenExistingWorker (string name, out Mutex result) 
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			result = null;
			if ((name.Length == 0) ||
				(name.Length > 260)) {
				return OpenExistingResult.NameInvalid;
			}
			
			MonoIOError error;
			IntPtr handle = OpenMutex_internal (name, out error);
			if (handle == IntPtr.Zero) {
				if (error == MonoIOError.ERROR_FILE_NOT_FOUND) {
					return OpenExistingResult.NameNotFound;
				} else if (error == MonoIOError.ERROR_ACCESS_DENIED) {
					throw new UnauthorizedAccessException ();
				} else {
					return OpenExistingResult.PathNotFound;
				}
			}
			
			result = new Mutex (handle);
			return OpenExistingResult.Success;
		}

		unsafe static IntPtr OpenMutex_internal (string name, out MonoIOError error)
		{
			fixed (char *fixed_name = name)
				return OpenMutex_icall (fixed_name, name?.Length ?? 0, 0x000001 /* MutexRights.Modify */, out error);
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		private unsafe static extern IntPtr CreateMutex_icall (bool initiallyOwned, char *name, int name_length, out bool created);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		private unsafe static extern IntPtr OpenMutex_icall (char *name, int name_length, int rights, out MonoIOError error);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		private static extern bool ReleaseMutex_internal (IntPtr handle);
	}
}
