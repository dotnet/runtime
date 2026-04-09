## About

<!-- A description of the package and where one can find more documentation -->

Provides APIs for speech recognition and synthesis built on the [Microsoft Speech API](https://learn.microsoft.com/previous-versions/windows/desktop/ms723627(v=vs.85)) in Windows.  Not supported on other platforms.

This package is provided primarily for compatibility with code being ported from .NET Framework and is not accepting new features.

## Key Features

<!-- The key features of this package -->

* Recognize speech as text in a given language and grammar.
* Synthesize text as speech.
* Support for [Speech Recognition Grammar v1.0](https://www.w3.org/TR/speech-grammar/) documents

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

### Synthesis example
```C#
using System.Speech.Synthesis;

// Initialize a new instance of the SpeechSynthesizer.
SpeechSynthesizer synth = new SpeechSynthesizer();

// Configure the audio output.
synth.SetOutputToDefaultAudioDevice();

// Speak a string, synchronously
synth.Speak("Hello World!");

// Speak a string asynchronously
var prompt = synth.SpeakAsync("Goodnight Moon!");

while (!prompt.IsCompleted)
{
    Console.WriteLine("speaking...");
    Thread.Sleep(500);
}
```

### Recognition example
```C#
// Create a new SpeechRecognitionEngine instance.
using SpeechRecognizer recognizer = new SpeechRecognizer();
using ManualResetEvent exit = new ManualResetEvent(false);

// Create a simple grammar that recognizes "red", "green", "blue", or "exit".
Choices choices = new Choices();
choices.Add(new string[] { "red", "green", "blue", "exit" });

// Create a GrammarBuilder object and append the Choices object.
GrammarBuilder gb = new GrammarBuilder();
gb.Append(choices);

// Create the Grammar instance and load it into the speech recognition engine.
Grammar g = new Grammar(gb);
recognizer.LoadGrammar(g);

// Register a handler for the SpeechRecognized event.
recognizer.SpeechRecognized += (s, e) =>
{
    Console.WriteLine($"Recognized: {e.Result.Text}, Confidence: {e.Result.Confidence}");
    if (e.Result.Text == "exit")
    {
        exit.Set();
    }
};

// Emulate
Console.WriteLine("Emulating \"red\".");
recognizer.EmulateRecognize("red");

Console.WriteLine("Speak red, green, blue, or exit please...");

exit.WaitOne();
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Speech.Recognition.SpeechRecognizer`
* `System.Speech.Synthesis.SpeechSynthesizer`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/previous-versions/office/developer/speech-technologies/hh361625(v%3doffice.14))
* [Speech.Recognition API documentation](https://learn.microsoft.com/dotnet/api/system.speech.recognition)
* [Speech.Synthesis API documentation](https://learn.microsoft.com/dotnet/api/system.speech.synthesis)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Speech is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
