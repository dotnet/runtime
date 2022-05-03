Debugging System.Private.CoreLib
==========================

`System.Console.Write`/`System.Console.WriteLine` cannot be used in `System.Private.CoreLib`. Instead, use `Internal.Console.Write` to add temporary logging for printf-style debugging.

### Android
The logs can be found through the generated Android Debug Bridge log or viewed directly through ADB logcat.
