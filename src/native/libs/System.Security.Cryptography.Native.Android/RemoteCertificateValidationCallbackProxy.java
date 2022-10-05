package net.dot.android.crypto;

import java.security.cert.X509Certificate;
import java.security.cert.CertificateException;
import javax.net.ssl.HostnameVerifier;
import javax.net.ssl.HttpsURLConnection;
import javax.net.ssl.X509TrustManager;
import android.util.Log;

class RemoteCertificateValidationCallbackProxy implements X509TrustManager
{
    private int dotnetRemoteCertificateValidator;
    private X509TrustManager internalTrustManager;

    public RemoteCertificateValidationCallbackProxy(int dotnetRemoteCertificateValidator, X509TrustManager internalTrustManager)
    {
        this.dotnetRemoteCertificateValidator = dotnetRemoteCertificateValidator;
        this.internalTrustManager = internalTrustManager;
    }

    public void checkClientTrusted(X509Certificate[] chain, String authType)
        throws CertificateException
    {
        internalTrustManager.checkClientTrusted(chain, authType);
    }

    public void checkServerTrusted(X509Certificate[] chain, String authType)
        throws CertificateException
    {
        int errors = 0;

        try
        {
            internalTrustManager.checkClientTrusted(chain, authType);
        }
        catch (CertificateException ex)
        {
            errors |= 4; // RemoteCertificateChainErrors
        }

        // TODO use the default hostname verifier to check the hostname
        // TODO how do I get the request URL?! - in Xamarin the trust manager is created once we have the HTTP request
        // but this verifier might have to deal with raw TCP streams where there is no hostname
        HostnameVerifier hostnameVerifier = HttpsURLConnection.getDefaultHostnameVerifier();
        if (!hostnameVerifier.verify("", null))
        {

        }

        boolean accepted = validateRemoteCertificate(dotnetRemoteCertificateValidator, chain, errors);
        if (!accepted)
        {
            throw new CertificateException("The remote certificate was rejected by the provided RemoteCertificateValidationCallback.");
        }
    }

    public X509Certificate[] getAcceptedIssuers()
    {
        return internalTrustManager.getAcceptedIssuers();
    }

    static native boolean validateRemoteCertificate(int dotnetRemoteCertificateValidator, X509Certificate[] chain, int errors);
}
