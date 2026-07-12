using UnityEngine;

// A = fire from the bird's mouth, W = reload (manual only, per design).
// Upgrades switch the mode: Sniper (100 dmg, 1 shot, 5s reload) or
// Triple (3-way spread, 15 dmg each).
public class WeaponSystem : MonoBehaviour {

    public enum Mode { Normal, Sniper, Triple }

    public Mode mode = Mode.Normal;
    public int magSize = 10;
    public int ammo = 10;
    public int damage = 30;
    public float reloadTime = 2f;

    public bool Reloading { get; private set; }
    public float ReloadLeft { get; private set; }

    private BirdControl bird;

    void Start() {
        bird = GetComponent<BirdControl>();
    }

    public void SetMode(Mode m) {
        mode = m;
        if (m == Mode.Normal) { damage = 30; magSize = 10; reloadTime = 2f; }
        if (m == Mode.Sniper) { damage = 100; magSize = 1; reloadTime = 5f; }
        if (m == Mode.Triple) { damage = 15; magSize = 10; reloadTime = 2f; }
        ammo = magSize;
        Reloading = false;
    }

    void Update() {
        GameMain gm = GameMain.I;
        if (gm == null || gm.State != GameMain.GameState.Playing) return;
        if (bird == null || bird.IsDead) return;

        if (Reloading) {
            ReloadLeft -= Time.deltaTime;
            if (ReloadLeft <= 0f) {
                Reloading = false;
                ammo = magSize;
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.A) && ammo > 0) Fire();

        if (Input.GetKeyDown(KeyCode.W) && ammo < magSize) {
            Reloading = true;
            ReloadLeft = reloadTime;
        }
    }

    private void Fire() {
        ammo--;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Vector3 mouth = transform.position + new Vector3(sr.bounds.extents.x + 0.08f, 0.03f, 0f);
        Color fire = new Color(1f, 0.45f, 0.05f);

        if (mode == Mode.Triple) {
            for (int i = -1; i <= 1; i++) {
                Vector3 dir = Quaternion.Euler(0f, 0f, i * 12f) * Vector3.right;
                Bullet.Spawn(Bullet.Side.Player, mouth, dir, 9f, damage, 0.16f, fire);
            }
        } else if (mode == Mode.Sniper) {
            Bullet.Spawn(Bullet.Side.Player, mouth, Vector3.right, 12f, damage, 0.32f, fire);
        } else {
            Bullet.Spawn(Bullet.Side.Player, mouth, Vector3.right, 9f, damage, 0.18f, fire);
        }
    }
}
