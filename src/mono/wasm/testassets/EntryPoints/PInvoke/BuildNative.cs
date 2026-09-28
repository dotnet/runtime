using System;
using System.Runtime.InteropServices;

Console.WriteLine($"TestOutput -> square: {square(5)}");
return 42;

[DllImport("simple")]
static extern int square(int x);
