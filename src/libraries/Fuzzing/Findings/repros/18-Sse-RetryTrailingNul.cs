#:package System.Net.ServerSentEvents@11.0.0-rc.1.26425.128
// Finding: the spec only accepts a "retry" field value that consists of ASCII digits, but SseParser uses long.TryParse, which
// ignores trailing NUL characters, so "retry: 7\0" sets the reconnection interval to 7 ms.
// Run: dotnet run 18-Sse-RetryTrailingNul.cs
using System.Net.ServerSentEvents;
using System.Text;

var parser = SseParser.Create(new MemoryStream(Encoding.UTF8.GetBytes("retry: 7\0\ndata: x\n\n")));
SseItem<string> item = parser.Enumerate().Single();
Console.WriteLine($"retry: 7\\0 -> item.ReconnectionInterval = {item.ReconnectionInterval?.TotalMilliseconds.ToString() ?? "null"} ms, parser.ReconnectionInterval = {parser.ReconnectionInterval}; expected the field to be ignored");
Console.WriteLine(item.ReconnectionInterval is not null ? "REPRODUCED: a non-digit retry value is accepted." : "NOT REPRODUCED");
