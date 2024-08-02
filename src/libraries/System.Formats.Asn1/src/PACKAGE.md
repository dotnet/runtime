## About

<!-- A description of the package and where one can find more documentation -->

Provides functionality for parsing, encoding, and decoding data using Abstract Syntax Notation One (ASN.1).
ASN.1 is a standard interface description language for defining data structures that can be serialized and deserialized in a cross-platform way.

## Key Features

<!-- The key features of this package -->

* Parse ASN.1 data into .NET types.
* Encode .NET types into ASN.1 format.
* Support for BER, CER, DER: Handles Basic Encoding Rules (BER), Canonical Encoding Rules (CER), and Distinguished Encoding Rules (DER).

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Parsing ASN.1 Data:

```csharp
using System.Formats.Asn1;

// Sample ASN.1 encoded data (DER format)
byte[] asn1Data = [0x30, 0x09, 0x02, 0x01, 0x01, 0x02, 0x01, 0x02, 0x02, 0x01, 0x03];

// Create an AsnReader to parse the data
AsnReader reader = new(asn1Data, AsnEncodingRules.DER);

// Parse the sequence
AsnReader sequenceReader = reader.ReadSequence();

// Read integers from the sequence
var firstInt = sequenceReader.ReadInteger();
var secondInt = sequenceReader.ReadInteger();
var thirdInt = sequenceReader.ReadInteger();

Console.WriteLine($"First integer: {firstInt}");
Console.WriteLine($"Second integer: {secondInt}");
Console.WriteLine($"Third integer: {thirdInt}");

// First integer: 1
// Second integer: 2
// Third integer: 3
```

Encoding ASN.1 Data:

```csharp
// Create an AsnWriter to encode data
AsnWriter writer = new(AsnEncodingRules.DER);

// Write a sequence
writer.PushSequence();

// Write integers to the sequence
writer.WriteInteger(1);
writer.WriteInteger(2);
writer.WriteInteger(3);

// Pop the sequence to finalize it
writer.PopSequence();

// Get the encoded data
byte[] encodedData = writer.Encode();

Console.WriteLine($"Encoded ASN.1 Data: {BitConverter.ToString(encodedData)}");

// Encoded ASN.1 Data: 30-09-02-01-01-02-01-02-02-01-03
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Formats.Asn1.AsnReader`
* `System.Formats.Asn1.AsnWriter`
* `System.Formats.Asn1.AsnEncodingRules`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.formats.asn1)
* [X.680 - Abstract Syntax Notation One (ASN.1): Specification of basic notation](https://www.itu.int/rec/T-REC-X.680)
* [X.690 - ASN.1 encoding rules: Specification of Basic Encoding Rules (BER), Canonical Encoding Rules (CER) and Distinguished Encoding Rules (DER)](https://www.itu.int/rec/T-REC-X.690)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Formats.Asn1 is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
