package com.kozhedub.atlantic4145;

import android.app.Activity;
import android.os.Bundle;
import android.view.View;
import android.widget.TextView;
import android.graphics.Color;

public class MainActivity extends Activity {
    @Override public void onCreate(Bundle b) {
        super.onCreate(b);
        try {
            getWindow().getDecorView().setSystemUiVisibility(
                View.SYSTEM_UI_FLAG_FULLSCREEN |
                View.SYSTEM_UI_FLAG_HIDE_NAVIGATION |
                View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY |
                View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN |
                View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION |
                View.SYSTEM_UI_FLAG_LAYOUT_STABLE
            );
            setContentView(new NavalGameView(this));
        } catch (Throwable t) {
            TextView tv = new TextView(this);
            tv.setBackgroundColor(Color.rgb(8,18,28));
            tv.setTextColor(Color.WHITE);
            tv.setTextSize(16f);
            tv.setPadding(32,32,32,32);
            tv.setText("Ошибка запуска:\n\n" + android.util.Log.getStackTraceString(t));
            setContentView(tv);
        }
    }
}
