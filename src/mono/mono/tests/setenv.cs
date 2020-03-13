using System;
using System.Runtime.InteropServices;

namespace Test {

	public class Test {
		[DllImport("libc")]
		static extern int setenv(string name, string value, int overwrite);
		[DllImport("libc")]
		static extern IntPtr getenv(string name);

		static int Main() {
			try {
				string name = "mono_test";
				string value = "val";

				setenv (name, value, 1);
				string ret = Marshal.PtrToStringAnsi (getenv (name));

				if (ret != value)
					return 1;
			}
			catch (EntryPointNotFoundException) {
				/* setenv is not available on some platforms */
			}
			catch (DllNotFoundException) {
				/* libc might not be accessible by that name */
			}

			return 0;
		}
	}
}
