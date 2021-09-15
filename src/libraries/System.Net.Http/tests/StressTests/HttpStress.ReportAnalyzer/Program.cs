using HttpStress.ReportAnalyzer;

if (CommandLineOptions.TryParse(args, out var options))
{
    if (options!.Test)
    {
        TestRunner.RunAll();
        return;
    }
}
