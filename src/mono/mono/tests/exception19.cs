using System;

public class TestFinallyException {

	public static int Main (string[] args)
	{
		int ret = -1;

		try {
		} finally {
			try {
				try {
				} finally {
					throw new Exception ();
				}
				ret = 1;
			} catch (Exception) {
				ret = 0;
			}
		}

		return ret;
	}

}
