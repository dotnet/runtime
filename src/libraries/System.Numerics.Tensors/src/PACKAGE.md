## About

Provides methods for performing mathematical operations over _tensors_ represented as spans.  These methods are accelerated to use SIMD (Single instruction, multiple data) operations supported by the CPU where available.

## Key Features

* Numerical operations on tensors represented as `ReadOnlySpan<float>`
* Element-wise arithmetic: Add, Subtract, Multiply, Divide, Exp, Log, Cosh, Tanh, etc.
* Tensor arithmetic: CosineSimilarity, Distance, Dot, Normalize, Softmax, Sigmoid, etc.

## How to Use

```C#
using System.Numerics.Tensors;

var movies = new[] {
    new { Title="The Lion King", Embedding= new [] { 0.10022575f, -0.23998135f } },
    new { Title="Inception", Embedding= new [] { 0.10327095f, 0.2563685f } },
    new { Title="Toy Story", Embedding= new [] { 0.095857024f, -0.201278f } },
    new { Title="Pulp Function", Embedding= new [] { 0.106827796f, 0.21676421f } },
    new { Title="Shrek", Embedding= new [] { 0.09568083f, -0.21177962f } }
};
var queryEmbedding = new[] { 0.12217915f, -0.034832448f };

var top3MoviesTensorPrimitives =
    movies
        .Select(movie =>
            (
                movie.Title,
                Similarity: TensorPrimitives.CosineSimilarity(queryEmbedding, movie.Embedding)
            ))
        .OrderByDescending(movies => movies.Similarity)
        .Take(3);

foreach (var movie in top3MoviesTensorPrimitives)
{
    Console.WriteLine(movie);
}
```

## Main Types

The main types provided by this library are:

* `System.Numerics.Tensors.TensorPrimitives`

## Additional Documentation

* [API documentation](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.tensors)

## Feedback & Contributing

System.Numerics.Tensors is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
