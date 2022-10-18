using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Melanzana.CodeSign
{
    public class CodeSignOptions
    {
        /// <summary>
        /// Gets or sets the developer certificate used for signing.
        /// </summary>
        /// <remarks>
        /// Set to `null` to use adhoc signing.
        /// </remarks>
        public X509Certificate2? DeveloperCertificate { get; set; }

        /// <summary>
        /// Gets or sets a private key used for signing.
        /// </summary>
        /// <remarks>
        /// This property can be used to augment the developer certificate set through
        /// <see cref="DeveloperCertificate"/>. It can either be used when the developer
        /// certificate itself doesn't contain the private key or the private key signing
        /// is provided through external service or device.
        /// </remarks>
        public AsymmetricAlgorithm? PrivateKey { get; set; }

        /// <summary>
        /// Gets or sets the entitlements to be embedded in the signature.
        /// </summary>
        public Entitlements? Entitlements { get; set; }

        /// <summmary>
        /// Gets or sets whether the nested exectuables should be signed.
        /// </summary>
        public bool Deep { get; set; }
    }
}