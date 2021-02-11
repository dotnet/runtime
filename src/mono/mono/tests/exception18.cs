using System;
using System.Linq;
using System.Runtime.CompilerServices;

class C
{
	static Exception e;

	static void Throw ()
	{
		try {
			int.Parse (null);
		} catch (Exception ex) {
			e = ex;
		}
	}

	static int FrameCount (Exception ex)
	{
			string fullTrace = ex.StackTrace;
			string[] frames = fullTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

			// Ignore metadata
			frames = frames.Where (l => !l.StartsWith ("[")).ToArray ();

			return frames.Length;
	}

	public static void Main ()
	{
		Throw ();

		try {
			throw e;
		} catch (Exception ex) {
			int frames = FrameCount (ex);
			if (frames != 1)
				throw new Exception (String.Format("Exception carried {0} frames along with it when it should have reported one.", frames));
		}

		try {
			try {
				double.Parse ("foo");
			} catch (Exception) {
				throw;
			}
		} catch (Exception ex) {
			int frames = FrameCount (ex);
			if (frames != 4)
				throw new Exception (String.Format("Exception carried {0} frames along with it when it should have reported four.", frames));
		}

		try {
			new C ().M1a ();
		} catch (Exception ex) {
			int frames = FrameCount (ex);
			if (frames != 4)
				throw new Exception (String.Format("Exception carried {0} frames along with it when it should have reported four.", frames));
		}

		try {
			new C ().M1b ();
		} catch (Exception ex) {
			int frames = FrameCount (ex);
			if (frames != 3)
				throw new Exception (String.Format("Exception carried {0} frames along with it when it should have reported three.", frames));
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void M1a ()
	{
		M2a ();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void M1b ()
	{
		M2b ();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void M2a ()
	{
		try {
			M3 ();
		} catch {
			throw;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void M2b ()
	{
		try {
			M3 ();
		} catch (Exception ex) {
			throw ex;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void M3 ()
	{
		throw new NotImplementedException ();
	}
}
