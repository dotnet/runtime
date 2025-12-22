public class TestAsset
{
    public string Name { get; init; }
    public string RunnableProjectSubPath { get; init; }
    public static readonly TestAsset WasmBasicTestApp = new() { Name = "WasmBasicTestApp", RunnableProjectSubPath = "App" };
    public static readonly TestAsset BlazorBasicTestApp = new() { Name = "BlazorBasicTestApp", RunnableProjectSubPath = "App" };
    public static readonly TestAsset LibraryModeTestApp = new() { Name = "LibraryMode" };
    public static readonly TestAsset BlazorWebWasm = new() { Name = "BlazorWebWasm", RunnableProjectSubPath = "BlazorWebWasm" };
    public static readonly TestAsset WasmBrowserRunMainOnly = new() { Name = "WasmBrowserRunMainOnly" };
}
