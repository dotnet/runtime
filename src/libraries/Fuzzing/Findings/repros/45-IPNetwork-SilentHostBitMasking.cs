// Finding: IPNetwork silently masks host bits instead of rejecting them, contradicting its own documentation. The class summary
// says "The constructor and the parsing methods will throw in case there are non-zero bits after the prefix", the IPNetwork(IPAddress,
// int) constructor is documented to throw ArgumentException "The specified baseAddress has non-zero bits after the network prefix",
// and Parse/TryParse are documented to throw FormatException for "the address contains non-zero bits after the network prefix". In
// fact ClearNonZeroBitsAfterNetworkPrefix silently zeroes those bits, so new IPNetwork(192.168.1.5, 24) and
// IPNetwork.Parse("192.168.1.5/24") both succeed and return 192.168.1.0/24, and TryParse returns true. Code that relies on the
// documented rejection to catch a malformed network (for example an ACL or allowlist entry) accepts it as a wider network instead.
// Run: dotnet run 45-IPNetwork-SilentHostBitMasking.cs
using System.Net;

bool reproduced = false;

// Constructor.
try
{
    var network = new IPNetwork(IPAddress.Parse("192.168.1.5"), 24);
    Console.WriteLine($"new IPNetwork(192.168.1.5, 24) returned {network} instead of throwing ArgumentException");
    reproduced = true;
}
catch (ArgumentException)
{
    Console.WriteLine("new IPNetwork(192.168.1.5, 24) threw ArgumentException (matches the docs)");
}

// Parse.
try
{
    IPNetwork network = IPNetwork.Parse("192.168.1.5/24");
    Console.WriteLine($"IPNetwork.Parse(\"192.168.1.5/24\") returned {network} instead of throwing FormatException");
    reproduced = true;
}
catch (FormatException)
{
    Console.WriteLine("IPNetwork.Parse threw FormatException (matches the docs)");
}

// TryParse.
if (IPNetwork.TryParse("2001:db8::1/32", out IPNetwork v6))
{
    Console.WriteLine($"IPNetwork.TryParse(\"2001:db8::1/32\") returned true -> {v6} (host bits silently dropped)");
    reproduced = true;
}

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");
