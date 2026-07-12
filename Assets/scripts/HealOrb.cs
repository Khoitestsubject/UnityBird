using UnityEngine;

// Green glowing orb that drifts in from the right; heal amount scales with size.
public class HealOrb : MonoBehaviour {

    public int healAmount;

    private float t;
    private float speed = 1.5f;
    private float baseY;
    private float radius;

    public static void Spawn() {
        GameMain gm = GameMain.I;
        if (gm == null) return;

        float size = Random.Range(0.25f, 0.6f);
        int heal = Mathf.RoundToInt(Mathf.Lerp(10f, 30f, (size - 0.25f) / 0.35f));

        Vector3 pos = gm.ViewportToWorld(1.05f, Random.Range(0.3f, 0.85f));
        SpriteRenderer sr = RuntimeFx.MakeDot("heal_orb", pos, size, new Color(0.3f, 1f, 0.4f, 0.9f), true, 2);

        HealOrb o = sr.gameObject.AddComponent<HealOrb>();
        o.healAmount = heal;
        o.baseY = pos.y;
        o.radius = size * 0.5f + 0.25f;
    }

    void Update() {
        GameMain gm = GameMain.I;
        if (gm == null) return;

        t += Time.deltaTime;
        float x = transform.position.x - speed * Time.deltaTime;
        transform.position = new Vector3(x, baseY + Mathf.Sin(t * 2f) * 0.3f, 0f);

        if (Vector2.Distance(transform.position, gm.BirdPos) < radius) {
            gm.Bird.Heal(healAmount);
            AudioSource.PlayClipAtPoint(gm.Bird.score, Vector3.zero);
            Destroy(gameObject);
            return;
        }

        if (x < -gm.HalfWidth - 1f) Destroy(gameObject);
    }
}
