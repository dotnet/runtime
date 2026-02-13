// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package net.dot.android.crypto;

import android.net.http.X509TrustManagerExtensions;
import java.security.cert.CertificateException;
import java.security.cert.X509Certificate;
import javax.net.ssl.X509TrustManager;

/**
 * Wraps the platform's X509TrustManager so that Android's trust infrastructure
 * (including network-security-config.xml) is consulted during TLS handshakes.
 *
 * Trust model: the platform's verdict is combined with managed (.NET) validation
 * to be MORE strict, never less:
 *
 *  - Platform rejects the chain -> chainTrustedByPlatform=false is passed to the
 *    managed callback, which pre-seeds sslPolicyErrors with RemoteCertificateChainErrors.
 *    Managed validation cannot clear this flag.
 *
 *  - Platform accepts the chain -> chainTrustedByPlatform=true, but managed validation
 *    (X509Chain.Build) still runs independently and can introduce its own errors.
 *
 * The RemoteCertificateValidationCallback always receives the union of both assessments.
 */
public final class DotnetProxyTrustManager implements X509TrustManager {
    private final long sslStreamProxyHandle;
    private final X509TrustManager platformTrustManager;
    private final String targetHost;

    public DotnetProxyTrustManager(long sslStreamProxyHandle, X509TrustManager platformTrustManager, String targetHost) {
        this.sslStreamProxyHandle = sslStreamProxyHandle;
        this.platformTrustManager = platformTrustManager;
        this.targetHost = targetHost;
    }

    public void checkClientTrusted(X509Certificate[] chain, String authType)
            throws CertificateException {
        boolean platformTrusted = isClientTrustedByPlatformTrustManager(chain, authType);
        if (!verifyRemoteCertificate(sslStreamProxyHandle, platformTrusted)) {
            throw new CertificateException();
        }
    }

    public void checkServerTrusted(X509Certificate[] chain, String authType)
            throws CertificateException {
        boolean platformTrusted = isServerTrustedByPlatformTrustManager(chain, authType);
        if (!verifyRemoteCertificate(sslStreamProxyHandle, platformTrusted)) {
            throw new CertificateException();
        }
    }

    /**
     * Checks the server's certificate chain against the platform trust manager.
     * Returns true if the platform trusts the chain, false otherwise.
     * A false result does NOT abort the handshake — it is forwarded to the managed
     * SslStream validation code as the chainTrustedByPlatform flag.
     */
    private boolean isServerTrustedByPlatformTrustManager(X509Certificate[] chain, String authType) {
        try {
            if (targetHost != null) {
                X509TrustManagerExtensions extensions = new X509TrustManagerExtensions(platformTrustManager);
                extensions.checkServerTrusted(chain, authType, targetHost);
            } else {
                platformTrustManager.checkServerTrusted(chain, authType);
            }
            return true;
        } catch (CertificateException e) {
            return false;
        }
    }

    private boolean isClientTrustedByPlatformTrustManager(X509Certificate[] chain, String authType) {
        try {
            platformTrustManager.checkClientTrusted(chain, authType);
            return true;
        } catch (CertificateException e) {
            return false;
        }
    }

    public X509Certificate[] getAcceptedIssuers() {
        // Return an empty array to avoid restricting which client certificates the TLS layer
        // considers acceptable. The actual trust validation is done in checkServerTrusted/checkClientTrusted.
        return new X509Certificate[0];
    }

    static native boolean verifyRemoteCertificate(long sslStreamProxyHandle, boolean chainTrustedByPlatform);
}
