using System;
using System.Threading.Tasks;

Console.WriteLine("Hello World!");

Console.WriteLine("Args:");
for (int i = 0; i < args.Length; i++) {
    Console.WriteLine($"  args[{i}] = {args[i]}");
}
