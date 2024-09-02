namespace System.Net.NetworkInformation
{
    internal static class HostInformationPal
    {
        public static string GetHostName()
        {
            return Interop.Sys.GetHostName();
        }

        public static string GetDomainName()
        {
            string domainName = Interop.Sys.GetDomainName();
            if (domainName == "(none)")
            {
                return string.Empty;
            }
            return domainName;
        }
    }
}
