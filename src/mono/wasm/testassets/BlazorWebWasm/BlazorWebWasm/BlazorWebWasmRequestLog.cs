public record BlazorWebWasmRequestLog(
    DateTime Timestamp,
    string Method,
    string Path,
    int StatusCode
);
