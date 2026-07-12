using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Enemy reuses the player bird's sprite, tinted and mirrored, so no new art
// is needed. Flies in from the right, hovers on a slot and shoots at the player.
public class EnemyBird : MonoBehaviour {

    public static List<EnemyBird> Alive = new List<EnemyBird>();

    public int hp;
    public bool boss;
    public float hitRadius;

    private Vector3 slot;
    private float bobT;
    private float bobSeed;
    private float shootT;
    private bool inPlace;
    private SpriteRenderer sr;
    private Color baseColor;

    public static EnemyBird Spawn(bool boss) {
        GameMain gm = GameMain.I;

        GameObject go = new GameObject(boss ? "enemy_boss" : "enemy_bird");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

        SpriteRenderer birdSr = gm.Bird.GetComponent<SpriteRenderer>();
        sr.sprite = birdSr.sprite;
        sr.sortingLayerName = birdSr.sortingLayerName;
        sr.sortingOrder = birdSr.sortingOrder;
        sr.color = boss ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.55f, 0.55f);

        // negative X scale mirrors the sprite (SpriteRenderer.flipX needs Unity 5.3+)
        float scale = boss ? 3f : 1f;
        go.transform.localScale = new Vector3(-scale, scale, 1f);

        EnemyBird e = go.AddComponent<EnemyBird>();
        e.boss = boss;
        e.hp = boss ? 500 : 100;
        e.hitRadius = boss ? 0.9f : 0.35f;
        e.sr = sr;
        e.baseColor = sr.color;
        e.bobSeed = Random.Range(0f, 6f);
        e.shootT = Random.Range(1.5f, 3f);

        float slotYMin = boss ? 0.45f : 0.35f;
        float slotYMax = boss ? 0.7f : 0.85f;
        e.slot = gm.ViewportToWorld(Random.Range(0.55f, 0.88f), Random.Range(slotYMin, slotYMax));
        go.transform.position = gm.ViewportToWorld(1.15f, Random.Range(0.4f, 0.8f));

        Alive.Add(e);
        return e;
    }

    void Update() {
        GameMain gm = GameMain.I;
        if (gm == null) return;

        bobT += Time.deltaTime;

        if (!inPlace) {
            transform.position = Vector3.MoveTowards(transform.position, slot, 2.5f * Time.deltaTime);
            if (Vector3.Distance(transform.position, slot) < 0.05f) inPlace = true;
        } else {
            transform.position = slot + new Vector3(0f, Mathf.Sin(bobT * 2f + bobSeed) * 0.25f, 0f);
        }

        if (gm.State != GameMain.GameState.Playing) return;

        shootT -= Time.deltaTime;
        if (shootT <= 0f) {
            shootT = boss ? Random.Range(1.2f, 2f) : Random.Range(2f, 3.5f);
            Shoot(gm);
        }
    }

    private void Shoot(GameMain gm) {
        Vector3 dir = gm.BirdPos - transform.position;
        Vector3 muzzle = transform.position + dir.normalized * (boss ? 0.8f : 0.3f);

        if (boss) {
            Bullet.Spawn(Bullet.Side.Enemy, muzzle, dir, 4.5f, 30, 0.3f, new Color(1f, 0.3f, 0.1f));
        } else {
            Bullet.Spawn(Bullet.Side.Enemy, muzzle, dir, 3.5f, 10, 0.18f, new Color(1f, 0.6f, 0.2f));
        }
    }

    public void TakeDamage(int dmg) {
        if (hp <= 0) return;
        hp -= dmg;
        if (hp <= 0) {
            Die();
        } else {
            StopAllCoroutines();
            StartCoroutine(Flash());
        }
    }

    private IEnumerator Flash() {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.07f);
        sr.color = baseColor;
    }

    private void Die() {
        GameMain gm = GameMain.I;
        if (gm != null) {
            AudioSource.PlayClipAtPoint(gm.Bird.score, Vector3.zero);
            gm.Bird.Heal(gm.healPerKill);
        }

        SpriteRenderer puff = RuntimeFx.MakeDot("kill_fx", transform.position, boss ? 2f : 0.7f, new Color(1f, 0.8f, 0.2f, 0.7f), true, 4);
        Destroy(puff.gameObject, 0.25f);
        Destroy(gameObject);
    }

    void OnDestroy() {
        Alive.Remove(this);
    }
}
