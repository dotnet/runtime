## About

Provides extension methods for `System.Net.Http.HttpClient` and `System.Net.Http.HttpContent` that facilitate serialization and deserialization of HTTP requests using System.Text.Json.

## Key Features

* Extension methods for deserializing HTTP response JSON bodies.
* Extension methods for serializing HTTP request JSON bodies.
* Extension methods for deserializing JSON from `HttpContent` instances.

## How to Use

```C#
using System.Net.Http.Json;

using var client = new HttpClient();

// Get the list of all books
Book[] books = await client.GetFromJsonAsync<Book[]>("https://api.contoso.com/books");

// Send a POST request to add a new book
var book = new Book(id: 42, "Title", "Author", publishedYear: 2023);
HttpResponseMessage response = await client.PostAsJsonAsync($"https://api.contoso.com/books/{book.id}", book);

if (response.IsSuccessStatusCode)
    Console.WriteLine("Book added successfully.");
else
    Console.WriteLine($"HTTP request failed with status code: {response.StatusCode}");

public record Book(int id, string title, string author, int publishedYear);
```

## Main Types

The main types provided by this library are:

* `HttpClientJsonExtensions`
* `HttpContentJsonExtensions`

## Additional Documentation

* [API documentation](https://learn.microsoft.com/dotnet/api/system.net.http.json)

## Related Packages

* [System.Text.Json](https://www.nuget.org/packages/System.Text.Json)

## Feedback & Contributing

System.Net.Http.Json is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
