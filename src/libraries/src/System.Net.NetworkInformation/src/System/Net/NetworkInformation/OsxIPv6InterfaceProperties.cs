namespace System.Net.NetworkInformation
{
    internal class OsxIPv6InterfaceProperties : UnixIPv6InterfaceProperties
    {
        private readonly int _mtu;

        public OsxIPv6InterfaceProperties(OsxNetworkInterface oni, int mtu)
            : base(oni)
        {
            _mtu = mtu;
        }

        public override int Mtu { get { return _mtu; } }

        public override long GetScopeId(ScopeLevel scopeLevel)
        {
            throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        }
    }
}
