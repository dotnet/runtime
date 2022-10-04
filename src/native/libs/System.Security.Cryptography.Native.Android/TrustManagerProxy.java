package net.dot.android.crypto;

import java.security.cert.X509Certificate;
import java.security.cert.CertificateException;
import javax.net.ssl.X509TrustManager;
import android.util.Log;

class TrustManagerProxy implements X509TrustManager
{
    private int csharpObjectHandle;
    private X509TrustManager internalTrustManager;

    public TrustManagerProxy(int csharpObjectHandle, X509TrustManager internalTrustManager)
    {
        this.csharpObjectHandle = csharpObjectHandle;
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
        Log.i("DOTNET", "TrustManagerProxy.checkServerTrusted called");

        int errors = 0;

        try
        {
            Log.i("DOTNET", "calling internal trust manager");
            internalTrustManager.checkClientTrusted(chain, authType);
            Log.i("DOTNET", "internal trust manager didn't throw");
        }
        catch (CertificateException ex)
        {
            Log.i("DOTNET", "internal trust manager has thrown an exception");
            errors |= 4; // RemoteCertificateChainErrors
        }

        // TODO use the default hostname verifier to check the hostname

        Log.i("DOTNET", "calling the callback");
        boolean accepted = validateRemoteCertificate(csharpObjectHandle, chain, errors);
        Log.i("DOTNET", "accepted? " + accepted);
        if (!accepted)
        {
            Log.i("DOTNET", "callback rejected the certificate");
            throw new CertificateException("The remote certificate was rejected by the provided RemoteCertificateValidationCallback.");
        }
        else
        {
            Log.i("DOTNET", "callback approved the certificate");
        }
    }

    public X509Certificate[] getAcceptedIssuers()
    {
        return internalTrustManager.getAcceptedIssuers();
    }

    static native boolean validateRemoteCertificate(int csharpObjectHandle, X509Certificate[] chain, int errors);
}
