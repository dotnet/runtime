// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package net.dot;

import android.app.Activity;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.widget.RelativeLayout;
import android.widget.TextView;
import android.graphics.Color;

public class MainActivity extends Activity
{
    @Override
    protected void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);

        final String entryPointLibName = "%EntryPointLibName%";
        final TextView textView = new TextView(this);
        textView.setTextSize(20);

        RelativeLayout rootLayout = new RelativeLayout(this);
        RelativeLayout.LayoutParams tvLayout =
                new RelativeLayout.LayoutParams(
                        RelativeLayout.LayoutParams.WRAP_CONTENT,
                        RelativeLayout.LayoutParams.WRAP_CONTENT);

        tvLayout.addRule(RelativeLayout.CENTER_HORIZONTAL);
        tvLayout.addRule(RelativeLayout.CENTER_VERTICAL);
        rootLayout.addView(textView, tvLayout);
        setContentView(rootLayout);

        if (entryPointLibName == "" || entryPointLibName.startsWith("%")) {
            textView.setText("ERROR: EntryPointLibName was not set.");
            return;
        } else {
            textView.setText("Running " + entryPointLibName + "...");
        }

        final Activity ctx = this;
        new Handler(Looper.getMainLooper()).postDelayed(new Runnable() {
            @Override
            public void run() {
                int retcode = MonoRunner.initialize(entryPointLibName, new String[0], ctx);
                textView.setText("Mono Runtime returned: " + retcode);
            }
        }, 1000);
    }
}
