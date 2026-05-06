module System.Text.Json.Tests.FSharp.Helpers

open System.IO
open System.Text
open System.Text.Json

type JsonSerializer with
    static member SerializeAsync<'T>(value : 'T, options : JsonSerializerOptions) = async {
        let! ct = Async.CancellationToken
        use mem = new MemoryStream()
        do! JsonSerializer.SerializeAsync(mem, value, options, ct) |> Async.AwaitTask
        return Encoding.UTF8.GetString(mem.ToArray())
    }

    static member DeserializeAsync<'T>(json : string, options : JsonSerializerOptions) = async {
        let! ct = Async.CancellationToken
        use mem = new MemoryStream(Encoding.UTF8.GetBytes json)
        return! JsonSerializer.DeserializeAsync<'T>(mem, options).AsTask() |> Async.AwaitTask
    }
