namespace System.Xml.Tests
{
    internal sealed class AllowDefaultResolverContext : IDisposable
    {
        private const string SwitchName = "Switch.System.Xml.AllowDefaultResolver";

        public AllowDefaultResolverContext() => AppContext.SetSwitch(SwitchName, isEnabled: true);

        public void Dispose() => AppContext.SetSwitch(SwitchName, isEnabled: false);
    }
}