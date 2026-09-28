// Finding: MailAddress keeps quoted-pair backslashes in DisplayName ("\"quoted\"" -> DisplayName \"quoted\"), but ToString()
// escapes backslashes again, so every parse/format round trip doubles them and the display name keeps changing.
// Run: dotnet run 22-MailAddress-DisplayNameRoundTrip.cs
using System.Net.Mail;

string text = "\"\\\"quoted\\\"\" <q@x.test>";
Console.WriteLine($"input:   {text}");
string current = text;
string? firstDisplayName = null, lastDisplayName = null;
for (int round = 1; round <= 3; round++)
{
    var address = new MailAddress(current);
    firstDisplayName ??= address.DisplayName;
    lastDisplayName = address.DisplayName;
    current = address.ToString();
    Console.WriteLine($"round {round}: DisplayName = {address.DisplayName}   ToString() = {current}");
}

Console.WriteLine(firstDisplayName != lastDisplayName ? "REPRODUCED: the display name changes on every round trip." : "NOT REPRODUCED");
