//
// pinvoke13.cs
//
//   Tests for pinvoke name mangling
//
using System;
using System.Runtime.InteropServices;

public class Tests
{
	/*
	 * These tests exercise the search order associated with the different charset values.
	 */

	/* This should call NameManglingAnsi */
	[DllImport("libtest", CharSet=CharSet.Ansi)]
	private static extern int NameManglingAnsi (string data);

	/* This should call NameManglingAnsi2A */
	[DllImport ("libtest", CharSet=CharSet.Ansi)]
	private static extern int NameManglingAnsi2 (string data);

	/* This should call NameManglingUnicodeW */
	[DllImport ("libtest", CharSet=CharSet.Unicode)]
	private static extern int NameManglingUnicode (string data);

	/* This should call NameManglingUnicode2 */
	[DllImport ("libtest", CharSet=CharSet.Unicode)]
	private static extern int NameManglingUnicode2 (string data);

	/* This should call NameManglingAutoW under windows, and NameManglingAuto under unix */
	[DllImport ("libtest", CharSet=CharSet.Auto)]
	private static extern int NameManglingAuto (string s);

	public static int Main (String[] args) {
		int res;

		res = NameManglingAnsi ("ABC");
		if (res != 198)
			return 1;
		res = NameManglingAnsi2 ("ABC");
		if (res != 198)
			return 2;
		res = NameManglingUnicode ("ABC");
		if (res != 198)
			return 3;
		res = NameManglingUnicode2 ("ABC");
		if (res != 198)
			return 4;

		res = NameManglingAuto ("ABC");
		if (res != 0)
			return 5;
		
		return 0;
	}
}

