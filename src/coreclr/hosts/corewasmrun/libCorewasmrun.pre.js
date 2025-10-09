var dotnetInternals = [
    {
        Module: Module,
    },
    [],
];
Module.preRun = () => {
    ENV["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "true";
};