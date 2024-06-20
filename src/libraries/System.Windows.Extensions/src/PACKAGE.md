## About

<!-- A description of the package and where one can find more documentation -->

Provides miscellaneous Windows-specific types.

This collection of types facilitates interactions with unique features provided by the Windows operating system, including playing sounds, selecting X509 certificates in a user-friendly manner, among other features.

## Key Features

<!-- The key features of this package -->

* Controls playback of a sound from a .wav file.
* Retrieves sounds associated with a set of Windows operating system sound-event types.
* User-friendly handling of X509 certificates.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Below are examples demonstrating the usage of the key types provided by this package.

### Playing a .wav File

```csharp
using System.Media;

SoundPlayer player = new SoundPlayer("sound.wav");
player.Play();

// Wait while the sound plays.
Console.ReadKey();
```

### Playing a System Sound

```csharp
using System.Media;

SystemSounds.Asterisk.Play();
SystemSounds.Beep.Play();
SystemSounds.Exclamation.Play();
SystemSounds.Hand.Play();
SystemSounds.Question.Play();
```

### Displaying a Certificate Selection Dialog

```csharp
using System.Security.Cryptography.X509Certificates;

X509Store store = new X509Store(StoreName.My);
store.Open(OpenFlags.ReadOnly);

X509Certificate2Collection selectedCerts = X509Certificate2UI.SelectFromCollection(
    store.Certificates,
    "Select Certificate",
    "Select a certificate from the following list:",
    X509SelectionFlag.SingleSelection
);
store.Close();

if (selectedCerts.Count == 0)
{
    Console.WriteLine("No certificate selected.");
}
else
{
    Console.WriteLine($"Certificate selected: {selectedCerts[0].Subject}");
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Media.SoundPlayer`
* `System.Media.SystemSounds`
* `System.Security.Cryptography.X509Certificates.X509Certificate2UI`
* `System.Xaml.Permissions.XamlAccessLevel`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* API documentation
  * [System.Media Namespace](https://learn.microsoft.com/dotnet/api/system.media)
  * [System.Security.Cryptography.X509Certificates Namespace](https://learn.microsoft.com/dotnet/api/system.security.cryptography.x509certificates)
  * [XamlAccessLevel Class](https://learn.microsoft.com/dotnet/api/system.xaml.permissions.xamlaccesslevel)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Windows.Extensions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
