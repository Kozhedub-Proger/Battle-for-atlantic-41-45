package com.kozhedub.atlantic4145;

import android.app.Activity;
import android.graphics.Color;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.view.Window;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.TextView;

public class MainActivity extends Activity {
    private NavalGLView game;
    private TextView enemyInfo;
    private TextView status;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
        getWindow().getDecorView().setSystemUiVisibility(
                View.SYSTEM_UI_FLAG_FULLSCREEN |
                View.SYSTEM_UI_FLAG_HIDE_NAVIGATION |
                View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY |
                View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN |
                View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION |
                View.SYSTEM_UI_FLAG_LAYOUT_STABLE);

        FrameLayout root = new FrameLayout(this);
        game = new NavalGLView(this);
        root.addView(game, new FrameLayout.LayoutParams(-1, -1));

        TextView title = label("СРАЖЕНИЯ В АТЛАНТИКЕ 41–45", 19, 0xFFEDE4C1);
        FrameLayout.LayoutParams titleLp = new FrameLayout.LayoutParams(-2, -2, Gravity.TOP | Gravity.CENTER_HORIZONTAL);
        titleLp.topMargin = dp(12);
        root.addView(title, titleLp);

        TextView player = label("HMS KING GEORGE V   •   БРОНЯ 100%", 15, 0xFFC8F1CE);
        FrameLayout.LayoutParams playerLp = new FrameLayout.LayoutParams(-2, -2, Gravity.TOP | Gravity.LEFT);
        playerLp.leftMargin = dp(16); playerLp.topMargin = dp(16);
        root.addView(player, playerLp);

        enemyInfo = label("BISMARCK   •   ЦЕЛОСТНОСТЬ 100%", 15, 0xFFFFC5BF);
        FrameLayout.LayoutParams enemyLp = new FrameLayout.LayoutParams(-2, -2, Gravity.TOP | Gravity.RIGHT);
        enemyLp.rightMargin = dp(16); enemyLp.topMargin = dp(16);
        root.addView(enemyInfo, enemyLp);

        status = label("Проведите пальцем — камера   •   щипок — масштаб", 14, Color.WHITE);
        status.setBackgroundColor(0x6605121B);
        status.setPadding(dp(12), dp(7), dp(12), dp(7));
        FrameLayout.LayoutParams statusLp = new FrameLayout.LayoutParams(-2, -2, Gravity.BOTTOM | Gravity.CENTER_HORIZONTAL);
        statusLp.bottomMargin = dp(18);
        root.addView(status, statusLp);

        Button fire = new Button(this);
        fire.setText("ОГОНЬ ГК");
        fire.setTextColor(Color.WHITE);
        fire.setTextSize(16);
        fire.setBackgroundColor(0xCC8A2F29);
        fire.setOnClickListener(v -> {
            if (game.fireMainBattery()) status.setText("ЗАЛП! Сопровождайте снаряды камерой");
        });
        FrameLayout.LayoutParams fireLp = new FrameLayout.LayoutParams(dp(150), dp(58), Gravity.BOTTOM | Gravity.RIGHT);
        fireLp.rightMargin = dp(18); fireLp.bottomMargin = dp(16);
        root.addView(fire, fireLp);

        LinearLayout zoom = new LinearLayout(this);
        zoom.setOrientation(LinearLayout.HORIZONTAL);
        Button minus = smallButton("−");
        Button plus = smallButton("+");
        minus.setOnClickListener(v -> game.zoom(2.2f));
        plus.setOnClickListener(v -> game.zoom(-2.2f));
        zoom.addView(minus, new LinearLayout.LayoutParams(dp(58), dp(52)));
        zoom.addView(plus, new LinearLayout.LayoutParams(dp(58), dp(52)));
        FrameLayout.LayoutParams zoomLp = new FrameLayout.LayoutParams(-2, -2, Gravity.BOTTOM | Gravity.LEFT);
        zoomLp.leftMargin = dp(18); zoomLp.bottomMargin = dp(18);
        root.addView(zoom, zoomLp);

        game.setListener(new NavalGLView.GameListener() {
            @Override public void onHit(int hp, boolean sunk) {
                runOnUiThread(() -> {
                    enemyInfo.setText("BISMARCK   •   ЦЕЛОСТНОСТЬ " + hp + "%");
                    status.setText(sunk ? "ЦЕЛЬ УНИЧТОЖЕНА — BISMARCK ТОНЕТ" : "ПОПАДАНИЕ! Есть повреждения");
                });
            }
            @Override public void onMiss() {
                runOnUiThread(() -> status.setText("ПРОМАХ — всплески по корме цели"));
            }
        });

        setContentView(root);
    }

    private TextView label(String text, int sp, int color) {
        TextView v = new TextView(this); v.setText(text); v.setTextSize(sp); v.setTextColor(color); return v;
    }
    private Button smallButton(String text) {
        Button b = new Button(this); b.setText(text); b.setTextSize(24); b.setTextColor(Color.WHITE); b.setBackgroundColor(0xAA102430); return b;
    }
    private int dp(int n) { return (int)(n * getResources().getDisplayMetrics().density + .5f); }
    @Override protected void onResume() { super.onResume(); if (game != null) game.onResume(); }
    @Override protected void onPause() { if (game != null) game.onPause(); super.onPause(); }
}
