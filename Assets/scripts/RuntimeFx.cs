using UnityEngine;

// Runtime-generated circle sprites so bullets/orbs/shield need no new art assets.
public static class RuntimeFx {

    private static Sprite circle;
    private static Sprite glow;

    public static Sprite Circle {
        get {
            if (circle == null) circle = MakeCircle(false);
            return circle;
        }
    }

    public static Sprite Glow {
        get {
            if (glow == null) glow = MakeCircle(true);
            return glow;
        }
    }

    private static Sprite MakeCircle(bool soft) {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float r = size * 0.5f - 1f;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float a;
                if (soft) {
                    a = Mathf.Clamp01(1f - d / r);
                    a = a * a;
                } else {
                    a = d <= r ? 1f : 0f;
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    // diameter is in world units; sorting layer is copied from the player bird
    // so effects render on the same layer as gameplay sprites
    public static SpriteRenderer MakeDot(string name, Vector3 pos, float diameter, Color color, bool soft, int orderOffset) {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * diameter;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = soft ? Glow : Circle;
        sr.color = color;
        sr.sortingLayerName = GameMain.FxLayer;
        sr.sortingOrder = GameMain.FxOrder + orderOffset;

        return sr;
    }
}
