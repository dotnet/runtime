using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal partial class FindPal
    {
        internal static IFindPal OpenPal(X509Certificate2Collection findFrom, X509Certificate2Collection copyTo, bool validOnly)
        {
            return new OpenSslCertificateFinder(findFrom, copyTo, validOnly);
        }
    }
}
