## About

<!-- A description of the package and where one can find more documentation -->

Provides support for accessing Windows system event notifications, which can be crucial for applications to respond to changes in the system or user environment​.
Through this assembly, applications can subscribe to a set of global system events provided by the `SystemEvents` class, gaining the ability to react to changes like system power mode alterations, user preference modifications, and session switches, among others.

## Key Features

<!-- The key features of this package -->

* Access to a set of global system events
* Notification of changes in user preferences
* Notification of system power mode changes
* Notification of session switches

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

An example of how to use the `SystemEvents` class to react to system changes:

```csharp
using Microsoft.Win32;

// Set the SystemEvents class to receive event notification when a user
// preference changes, the palette changes, or when display settings change.
SystemEvents.UserPreferenceChanging += UserPreferenceChanging;
SystemEvents.PaletteChanged += PaletteChanged;
SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;

// For demonstration purposes, this application sits idle waiting for events.
Console.WriteLine("This application is waiting for system events.");
Console.WriteLine("Press <Enter> to terminate this application.");
Console.ReadLine();

// This method is called when a user preference changes.
static void UserPreferenceChanging(object sender, UserPreferenceChangingEventArgs e)
{
    Console.WriteLine($"The user preference is changing. Category={e.Category}");
}

// This method is called when the palette changes.
static void PaletteChanged(object sender, EventArgs e)
{
    Console.WriteLine("The palette changed.");
}

// This method is called when the display settings change.
static void DisplaySettingsChanged(object sender, EventArgs e)
{
    Console.WriteLine("The display settings changed.");
}
```

In this example, the methods will be invoked whenever the user modifies one of several system settings.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Win32.SystemEvents`
* `Microsoft.Win32.PowerModeChangedEventHandler`
* `Microsoft.Win32.SessionEndedEventHandler`
* `Microsoft.Win32.UserPreferenceChangedEventHandler`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.win32.systemevents)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Win32.SystemEvents is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
