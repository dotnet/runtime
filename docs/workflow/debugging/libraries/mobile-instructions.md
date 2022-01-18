Debugging Libraries on Mobile
==========================

## Debugging System.Private.CoreLib on Android

`System.Console.Write`/`System.Console.WriteLine` cannot be used in `System.Private.CoreLib`. Instead, use `Internal.Console.Write` to log. After running a test or program that utilizes the relevant `System.Private.CoreLib` code, the logs can be found through the generated adb log or viewed directly through adb logcat.
