using UnityEngine;
using System.Collections.Generic;

// Manual distance-based hits: no colliders needed, so nothing in the binary
// scene has to change for bullets to work.
public class Bullet : MonoBehaviour {

    public enum Side { Player, Enemy }

    public Side side;
    public int damage;
    public Vector2 dir;
    public float speed;

    private float life;

    public static Bullet Spawn(Side side, Vector3 pos, Vector2 dir, float speed, int damage, float size, Color color) {
        SpriteRenderer sr = RuntimeFx.MakeDot("bullet", pos, size, color, false, 3);
        Bullet b = sr.gameObject.AddComponent<Bullet>();
        b.side = side;
        b.dir = dir.normalized;
        b.speed = speed;
        b.damage = damage;
        return b;
    }

    void Update() {
        GameMain gm = GameMain.I;
        if (gm == null) {
            Destroy(gameObject);
            return;
        }

        life += Time.deltaTime;
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        if (side == Side.Enemy) {
            float d = Vector2.Distance(transform.position, gm.BirdPos);

            // parry: shield sends the bullet back as a player bullet
            if (gm.Shield != null && gm.Shield.IsActive && d < gm.Shield.radius) {
                side = Side.Player;
                dir = -dir;
                speed *= 1.3f;
                GetComponent<SpriteRenderer>().color = new Color(0.4f, 1f, 1f);
                return;
            }

            if (d < 0.35f) {
                gm.Bird.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        } else {
            List<EnemyBird> list = EnemyBird.Alive;
            for (int i = list.Count - 1; i >= 0; i--) {
                EnemyBird e = list[i];
                if (e == null) continue;
                if (Vector2.Distance(transform.position, e.transform.position) < e.hitRadius) {
                    e.TakeDamage(damage);
                    Destroy(gameObject);
                    return;
                }
            }
        }

        if (life > 6f || Mathf.Abs(transform.position.x) > gm.HalfWidth + 2f || Mathf.Abs(transform.position.y) > 10f) {
            Destroy(gameObject);
        }
    }
}
