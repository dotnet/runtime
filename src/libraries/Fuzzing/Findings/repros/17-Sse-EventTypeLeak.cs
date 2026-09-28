#:package System.Net.ServerSentEvents@11.0.0-rc.1.26425.128
// Finding: when SseParser dispatches on a blank line with an empty data buffer it doesn't reset the event type buffer, which the
// WHATWG spec requires ("If the data buffer is an empty string, set the data buffer and the event type buffer to the empty string
// and return"). The event type (and the per-item EventId/ReconnectionInterval) therefore leak into the next event.
// Run: dotnet run 17-Sse-EventTypeLeak.cs
using System.Net.ServerSentEvents;
using System.Text;

string stream = "event: foo\n\ndata: x\n\nid: 7\n\ndata: y\n\nretry: 1500\n\ndata: z\n\n";
var parser = SseParser.Create(new MemoryStream(Encoding.UTF8.GetBytes(stream)));
var items = parser.Enumerate().ToList();
foreach (SseItem<string> item in items)
{
    Console.WriteLine($"data '{item.Data}': EventType '{item.EventType}', EventId {item.EventId ?? "null"}, ReconnectionInterval {item.ReconnectionInterval?.TotalMilliseconds.ToString() ?? "null"}");
}

Console.WriteLine("expected: every EventType is 'message' (each 'event:'/'id:'/'retry:' line is followed by a blank line with no data)");
Console.WriteLine(items[0].EventType != "message" ? "REPRODUCED: the event type leaks across an empty dispatch." : "NOT REPRODUCED");
