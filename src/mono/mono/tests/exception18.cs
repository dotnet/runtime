using System;

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
				int.Parse (null);
			} catch (Exception) {
				throw;
			}
		} catch (Exception ex) {
			int frames = FrameCount (ex);
			if (frames != 4)
				throw new Exception (String.Format("Exception carried {0} frames along with it when it should have reported four.", frames));
		}

	}
}
