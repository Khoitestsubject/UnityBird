using UnityEngine;

// D = 1s reflect shield, 5s cooldown (upgradable to 2s).
// Actual reflection happens in Bullet.Update by checking IsActive + radius.
public class ShieldSystem : MonoBehaviour {

    public float duration = 1f;
    public float cooldown = 5f;
    public float radius = 1.0f;

    private float activeLeft;
    private float cdLeft;
    private GameObject vis;
    private BirdControl bird;

    public bool IsActive { get { return activeLeft > 0f; } }
    public float CooldownLeft { get { return Mathf.Max(0f, cdLeft); } }

    void Start() {
        bird = GetComponent<BirdControl>();

        vis = RuntimeFx.MakeDot("shield_fx", transform.position, radius * 2f, new Color(0.3f, 0.9f, 1f, 0.35f), true, 5).gameObject;
        vis.transform.parent = transform;
        vis.transform.localPosition = Vector3.zero;
        vis.SetActive(false);
    }

    void Update() {
        GameMain gm = GameMain.I;

        if (cdLeft > 0f) cdLeft -= Time.deltaTime;

        if (activeLeft > 0f) {
            activeLeft -= Time.deltaTime;
            if (activeLeft <= 0f) vis.SetActive(false);
        }

        if (gm == null || gm.State != GameMain.GameState.Playing) return;
        if (bird == null || bird.IsDead) return;

        if (Input.GetKeyDown(KeyCode.D) && cdLeft <= 0f && activeLeft <= 0f) {
            activeLeft = duration;
            cdLeft = cooldown;
            vis.SetActive(true);
        }
    }
}
