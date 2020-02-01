using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;

class CustomException : Exception
{
	public int Value;
	public CustomException (int value) => this.Value = value;
}

class C
{
	static CustomException e;

	[MethodImpl(MethodImplOptions.NoInlining)]
	static void ThrowCustomException ()
	{
		throw new CustomException(1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static void Throw ()
	{
		try {
			ThrowCustomException ();
		} catch (CustomException ex) {
			e = ex;
			// so, the original exception has two frames
			CheckTrace (e, 2);
		}
	}

	static int FrameCount (Exception ex)
	{
			string fullTrace = ex.StackTrace;
			if (fullTrace == null)
				throw new Exception ("Empty trace found!");

			string[] frames = fullTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

			// Ignore metadata
			frames = frames.Where (l => !l.StartsWith ("[")).ToArray ();

			return frames.Length;
	}

	// throws on false anyway
	[MethodImpl(MethodImplOptions.NoInlining)]
	static bool CheckTrace (Exception ex, int numFramesToExpect, bool whatToReturn = true)
	{
		int frames = FrameCount (ex);
		if (frames != numFramesToExpect) {
			throw new Exception ($"Exception carried {frames} frames along with it when it should have reported {numFramesToExpect}. Full trace:\n---\n{ex.StackTrace}\n---");
		}

		return whatToReturn;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static void ThrowCustomExceptionWithValueButFailsToCatchWithFilter ()
	{
		try {
			ThrowCustomException (1);
		} catch (CustomException ce) when (ce.Value == 9999) {
			throw new Exception ("This should NEVER be hit");
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static void ThrowCustomException (int value)
	{
		throw new CustomException (value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static void ThrowFileNotFoundException ()
	{
		throw new FileNotFoundException ();
	}

	public static void Main ()
	{
		Throw ();

		// Single frame
		// Filter returns false
		// Original exception @e had two frames, but after throwing here it
		// should have just 1
		try {
			throw e;
		} catch (Exception ex) when (CheckTrace (ex, 1, true)) {
			CheckTrace (ex, 1);
		}

		// Throw + filter fails, then filter matches at next level
		try {
			ThrowCustomExceptionWithValueButFailsToCatchWithFilter ();
		} catch (CustomException ex) when (CheckTrace (ex, 3, true)) {
			CheckTrace (ex, 3);
		}

		// Throw + filter fails, then filter matches, exception re-thrown and caught
		try {
			try {
				ThrowCustomExceptionWithValueButFailsToCatchWithFilter ();
			} catch (CustomException ex) when (CheckTrace (ex, 3, true)) {
				CheckTrace (ex, 3);

				// this will truncate the trace now
				throw ex;
			}
		} catch (Exception ex) {
			CheckTrace (ex, 1);
		}

		// Throw, filter matches, throw exception as-is, caught
		try {
			try {
				ThrowFileNotFoundException ();
			} catch (Exception e) when (CheckTrace (e, 2)) {
				// trace should remain as is
				throw;
			}
		} catch (Exception ex) {
			CheckTrace (ex, 2);
		}
	}
}
