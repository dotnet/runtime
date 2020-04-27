// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

package net.dot;

import android.app.AlertDialog;
import android.app.Activity;
import android.os.Bundle;

public class MainActivity extends Activity
{
    @Override
    protected void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);

        AlertDialog.Builder dlgAlert = new AlertDialog.Builder(this);
        dlgAlert.setMessage("Use `adb shell am instrument -w " + getApplicationContext().getPackageName() + "net.dot.MonoRunner` to run the tests.");
        dlgAlert.create().show();
    }
}
