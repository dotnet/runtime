## About

Provides methods for performing mathematical operations over _tensors_. This library offers both high-level tensor types and low-level primitives for working with multi-dimensional numeric data.  Many operations are accelerated to use SIMD (Single instruction, multiple data) operations supported by the CPU where available.

## Key Features

* High-level tensor types: `Tensor<T>`, `TensorSpan<T>`, `ReadOnlyTensorSpan<T>` for working with multi-dimensional arrays
* Low-level tensor primitives: `TensorPrimitives` for efficient span-based operations
* Generic support for various numeric types (float, double, int, etc.)
* Element-wise arithmetic: Add, Subtract, Multiply, Divide, Exp, Log, Cosh, Tanh, etc.
* Tensor arithmetic: CosineSimilarity, Distance, Dot, Normalize, Softmax, Sigmoid, etc.
* SIMD-accelerated operations for improved performance

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

// Using TensorPrimitives for low-level span operations
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

// Using higher-level Tensor types for multi-dimensional operations
float[] data1 = [1f, 2f, 3f, 4f, 5f, 6f];
float[] data2 = [6f, 5f, 4f, 3f, 2f, 1f];
Tensor<float> tensor1 = Tensor.Create(data1, [2, 3]); // 2x3 tensor
Tensor<float> tensor2 = Tensor.Create(data2, [2, 3]); // 2x3 tensor
Tensor<float> result = tensor1 + tensor2;
```

## Main Types

The main types provided by this library are:

* `System.Numerics.Tensors.TensorPrimitives` - Low-level operations on spans of numeric data
* `System.Numerics.Tensors.Tensor<T>` - Generic tensor class for multi-dimensional arrays
* `System.Numerics.Tensors.TensorSpan<T>` - Span-like view over tensor data
* `System.Numerics.Tensors.ReadOnlyTensorSpan<T>` - Read-only span-like view over tensor data
* `System.Numerics.Tensors.Tensor` - Static class with high-level tensor operations

## Additional Documentation

* [API documentation](https://learn.microsoft.com/dotnet/api/system.numerics.tensors)

## Feedback & Contributing

System.Numerics.Tensors is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
