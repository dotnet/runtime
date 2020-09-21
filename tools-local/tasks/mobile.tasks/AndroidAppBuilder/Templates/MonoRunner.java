// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
import android.os.Environment;
import android.net.Uri;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.BufferedInputStream;
import java.util.zip.ZipEntry;
import java.util.zip.ZipInputStream;

public class MonoRunner extends Instrumentation
{
    static MonoRunner inst;

    static {
        System.loadLibrary("monodroid");
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
        String filesDir = context.getFilesDir().getAbsolutePath();
        String cacheDir = context.getCacheDir().getAbsolutePath();
        File docsPath  = context.getExternalFilesDir(Environment.DIRECTORY_DOCUMENTS);
        if (docsPath == null) {
            docsPath = context.getCacheDir();
        }
        String docsDir = docsPath.getAbsolutePath();

        // unzip libs and test files to filesDir
        unzipAssets(context, filesDir, "assets.zip");

        Log.i("DOTNET", "initRuntime");
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

    static void unzipAssets(Context context, String toPath, String zipName) {
        AssetManager assetManager = context.getAssets();
        try {
            InputStream inputStream = assetManager.open(zipName);
            ZipInputStream zipInputStream = new ZipInputStream(new BufferedInputStream(inputStream));
            ZipEntry zipEntry;
            byte[] buffer = new byte[4096];
            while ((zipEntry = zipInputStream.getNextEntry()) != null) {
                String fileOrDirectory = zipEntry.getName();
                Uri.Builder builder = new Uri.Builder();
                builder.scheme("file");
                builder.appendPath(toPath);
                builder.appendPath(fileOrDirectory);
                String fullToPath = builder.build().getPath();
                if (zipEntry.isDirectory()) {
                    File directory = new File(fullToPath);
                    directory.mkdirs();
                    continue;
                }
                Log.i("DOTNET", "Extracting asset to " + fullToPath);
                int count = 0;
                FileOutputStream fileOutputStream = new FileOutputStream(fullToPath);
                while ((count = zipInputStream.read(buffer)) != -1) {
                    fileOutputStream.write(buffer, 0, count);
                }
                fileOutputStream.close();
                zipInputStream.closeEntry();
            }
            zipInputStream.close();
        } catch (IOException e) {
            Log.e("DOTNET", e.getLocalizedMessage());
        }
    }

    native int initRuntime(String libsDir, String cacheDir, String docsDir);
}
