## About

<!-- A description of the package and where one can find more documentation -->

Provides dataflow components that are collectively referred to as the *TPL Dataflow Library*.
This dataflow model promotes actor-based programming by providing in-process message passing for coarse-grained dataflow and pipelining tasks.

## Key Features

<!-- The key features of this package -->

* Foundation for message passing and parallelizing CPU-intensive and I/O-intensive applications that have high throughput and low latency.
* Provides multiple block types for various dataflow operations (e.g., `BufferBlock`, `ActionBlock`, `TransformBlock`).
* Dataflow blocks support linking to form *networks*, allowing you to create complex processing topologies.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

This sample demonstrates a dataflow pipeline that downloads the book "The Iliad of Homer" from a website and searches the text to match individual words with words that reverse the first word's characters.

```csharp
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;

var nonLetterRegex = new Regex(@"\P{L}", RegexOptions.Compiled);
var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });

// Setup blocks

// Downloads the requested resource as a string.
TransformBlock<string, string> downloadString = new TransformBlock<string, string>(async uri =>
{
    Console.WriteLine("Downloading '{0}'...", uri);

    return await client.GetStringAsync(uri);
});

// Separates the specified text into an array of words.
TransformBlock<string, string[]> createWordList = new TransformBlock<string, string[]>(text =>
{
    Console.WriteLine("Creating word list...");

    // Remove common punctuation by replacing all non-letter characters with a space character.
    text = nonLetterRegex.Replace(text, " ");

    // Separate the text into an array of words.
    return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
});

// Removes short words.
TransformBlock<string[], string[]> filterWordList = new TransformBlock<string[], string[]>(words =>
{
    Console.WriteLine("Filtering word list...");

    return words
       .Where(word => word.Length > 3)
       .ToArray();
});

// Finds all words in the specified collection whose reverse also exists in the collection.
TransformManyBlock<string[], string> findReversedWords = new TransformManyBlock<string[], string>(words =>
{
    Console.WriteLine("Finding reversed words...");

    var wordsSet = new HashSet<string>(words);

    return from word in wordsSet
           let reverse = string.Concat(word.Reverse())
           where word != reverse && wordsSet.Contains(reverse)
           select word;
});

// Prints the provided reversed words to the console.
ActionBlock<string> printReversedWords = new ActionBlock<string>(reversedWord =>
{
    Console.WriteLine("Found reversed words {0}/{1}", reversedWord, string.Concat(reversedWord.Reverse()));
});


// Connect the dataflow blocks to form a pipeline.
var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

downloadString.LinkTo(createWordList, linkOptions);
createWordList.LinkTo(filterWordList, linkOptions);
filterWordList.LinkTo(findReversedWords, linkOptions);
findReversedWords.LinkTo(printReversedWords, linkOptions);

// Post data to the pipeline, "The Iliad of Homer" by Homer.
downloadString.Post("http://www.gutenberg.org/cache/epub/16452/pg16452.txt");

// Mark the head of the pipeline as complete.
downloadString.Complete();

// Wait for the last block in the pipeline to process all messages.
printReversedWords.Completion.Wait();

// Output:
// Downloading 'http://www.gutenberg.org/cache/epub/16452/pg16452.txt'...
// Creating word list...
// Filtering word list...
// Finding reversed words...
// Found reversed words parts/strap
// Found reversed words deer/reed
// Found reversed words deem/meed
// Found reversed words flow/wolf
// ...

```

More details can be found on [Dataflow (Task Parallel Library)](https://learn.microsoft.com/dotnet/standard/parallel-programming/dataflow-task-parallel-library) and [Walkthrough: Creating a Dataflow Pipeline](https://learn.microsoft.com/dotnet/standard/parallel-programming/walkthrough-creating-a-dataflow-pipeline) pages.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Threading.Tasks.Dataflow.ISourceBlock<TOutput>`
* `System.Threading.Tasks.Dataflow.ITargetBlock<TInput>`
* `System.Threading.Tasks.Dataflow.IPropagatorBlock<TInput,TOutput>`
* `System.Threading.Tasks.Dataflow.ActionBlock<TInput>`
* `System.Threading.Tasks.Dataflow.BatchBlock<T>`
* `System.Threading.Tasks.Dataflow.BatchedJoinBlock<T1, T2>`
* `System.Threading.Tasks.Dataflow.BroadcastBlock<T>`
* `System.Threading.Tasks.Dataflow.BufferBlock<T>`
* `System.Threading.Tasks.Dataflow.JoinBlock<T1, T2>`
* `System.Threading.Tasks.Dataflow.TransformBlock<TInput, TOutput>`
* `System.Threading.Tasks.Dataflow.TransformManyBlock<TInput, TOutput>`
* `System.Threading.Tasks.Dataflow.WriteOnceBlock<T>`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/standard/parallel-programming/dataflow-task-parallel-library)
* [API documentation](https://learn.microsoft.com/dotnet/api/system.threading.tasks.dataflow)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Threading.Tasks.Dataflow is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
