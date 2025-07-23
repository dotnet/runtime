public class TestAsset
{
    public string Name { get; init; }
    public string RunnableProjectSubPath { get; init; }
    public static readonly TestAsset WasmBasicTestApp = new() { Name = "WasmBasicTestApp", RunnableProjectSubPath = "App" };
    public static readonly TestAsset BlazorBasicTestApp = new() { Name = "BlazorBasicTestApp", RunnableProjectSubPath = "App" };
    public static readonly TestAsset LibraryModeTestApp = new() { Name = "LibraryMode" };
}