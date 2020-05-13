// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

package net.dot;

import android.app.Instrumentation;
import android.content.Context;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.content.res.AssetManager;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.app.Activity;
import android.os.Bundle;
import android.os.Environment;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

public class MonoRunner extends Instrumentation
{
    static MonoRunner inst;

    static {
        System.loadLibrary("runtime-android");
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        start();
    }

    @Override
    public void onStart() {
        super.onStart();

        MonoRunner.inst = this;
        Context context = getContext();
        AssetManager am = context.getAssets();
        String filesDir = context.getFilesDir().getAbsolutePath();
        String cacheDir = context.getCacheDir().getAbsolutePath();
        String docsDir  = context.getExternalFilesDir(Environment.DIRECTORY_DOCUMENTS).getAbsolutePath();

        copyAssetDir(am, "", filesDir);

        int retcode = initRuntime(filesDir, cacheDir, docsDir);
        runOnMainSync(new Runnable() {
            public void run() {
                Bundle result = new Bundle();
                result.putInt("return-code", retcode);
                // Xharness cli expects "test-results-path" with test results
                result.putString("test-results-path", docsDir + "/testResults.xml");
                finish(retcode, result);
            }
        });
    }

    static void copyAssetDir(AssetManager am, String path, String outpath) {
        try {
            String[] res = am.list(path);
            for (int i = 0; i < res.length; ++i) {
                String fromFile = res[i];
                String toFile = outpath + "/" + res[i];
                try {
                    InputStream fromStream = am.open(fromFile);
                    Log.w("DOTNET", "\tCOPYING " + fromFile + " to " + toFile);
                    copy(fromStream, new FileOutputStream(toFile));
                } catch (FileNotFoundException e) {
                    new File(toFile).mkdirs();
                    copyAssetDir(am, fromFile, toFile);
                    continue;
                }
            }
        }
        catch (Exception e) {
            Log.w("DOTNET", "EXCEPTION", e);
        }
    }

    static void copy(InputStream in, OutputStream out) throws IOException {
        byte[] buff = new byte [1024];
        for (int len = in.read(buff); len != -1; len = in.read(buff))
            out.write(buff, 0, len);
        in.close();
        out.close();
    }

    native int initRuntime(String libsDir, String cacheDir, String docsDir);
}
