using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class GameMain : MonoBehaviour {

    public enum GameState { Ready, Playing, Upgrading, GameOver, Victory }

    public static GameMain I;

    // sorting layer for runtime FX, captured from the player bird in Start
    public static string FxLayer = "ui";
    public static int FxOrder = 0;

    public GameObject bird;
    public GameObject readyPic;
    public GameObject tipPic;
    public GameObject scoreMgr;
    public GameObject pipeSpawner;

    // Drag the start_btn sprite (Assets/sprites/atlas) onto this slot in the Inspector.
    public Sprite restartSprite;

    // HP restored to the player for each enemy killed
    public int healPerKill = 40;

    public GameState State { get; private set; }
    public bool GodMode { get; private set; }

    public BirdControl Bird { get; private set; }
    public WeaponSystem Weapon { get; private set; }
    public ShieldSystem Shield { get; private set; }
    public WaveManager Waves { get; private set; }

    private GameObject restartBtn;
    private bool btnPressed = false;
    private bool canRestart = false;
    private float nextOrbTime = -1f;
    private bool shieldUpTaken = false;

    private GUIStyle titleStyle, hudStyle, btnStyle, hintStyle;

    public Vector3 BirdPos { get { return bird.transform.position; } }

    public float HalfWidth {
        get {
            Camera c = Camera.main;
            return c.orthographicSize * c.aspect;
        }
    }

    public Vector3 ViewportToWorld(float vx, float vy) {
        Vector3 p = Camera.main.ViewportToWorldPoint(new Vector3(vx, vy, 10f));
        p.z = 0f;
        return p;
    }

    void Awake() {
        I = this;
        Time.timeScale = 1f;
        State = GameState.Ready;

        // the plain score system is replaced by the wave system; deactivating
        // in Awake keeps ScoreMgr.Start from ever spawning digit sprites
        if (scoreMgr != null) scoreMgr.SetActive(false);
    }

    void Start() {
        SpriteRenderer birdSr = bird.GetComponent<SpriteRenderer>();
        FxLayer = birdSr.sortingLayerName;
        FxOrder = birdSr.sortingOrder;

        Bird = bird.GetComponent<BirdControl>();
        Weapon = bird.AddComponent<WeaponSystem>();
        Shield = bird.AddComponent<ShieldSystem>();
        Waves = gameObject.AddComponent<WaveManager>();

        CreateRestartButton();
    }

    void Update() {
        if (State == GameState.Ready && Input.GetButtonDown("Fire1") && !IsMouseOverSkillBtn()) {
            StartGame();
            return;
        }

        if (State == GameState.Playing && nextOrbTime > 0f && Time.time >= nextOrbTime) {
            HealOrb.Spawn();
            ScheduleNextOrb();
        }

        if (canRestart && (State == GameState.GameOver || State == GameState.Victory)) {
            HandleRestartInput();
        }
    }

    private void StartGame() {
        State = GameState.Playing;

        Bird.inGame = true;
        Bird.JumpUp();

        readyPic.GetComponent<SpriteRenderer>().material.DOFade(0f, 0.2f);
        tipPic.GetComponent<SpriteRenderer>().material.DOFade(0f, 0.2f);

        pipeSpawner.GetComponent<PipeSpawner>().StartSpawning();

        ScheduleNextOrb();
        Invoke("BeginFirstWave", 3f);
    }

    private void BeginFirstWave() {
        if (State == GameState.Playing) Waves.StartWave(1);
    }

    private void ScheduleNextOrb() {
        nextOrbTime = Time.time + Random.Range(10f, 20f);
    }

    public void OnWaveCleared() {
        if (State != GameState.Playing) return;

        Bird.HealFull();

        if (Waves.CurrentWave >= 3) {
            State = GameState.Victory;
            StartCoroutine(ShowEndButtons());
            return;
        }

        StartCoroutine(OpenUpgradeMenu());
    }

    private IEnumerator OpenUpgradeMenu() {
        yield return new WaitForSeconds(1f);
        if (State != GameState.Playing) yield break;
        State = GameState.Upgrading;
        Time.timeScale = 0f;
    }

    private void PickUpgrade(int id) {
        if (id == 0) {
            Shield.cooldown = 2f;
            shieldUpTaken = true;
        }
        if (id == 1) Weapon.SetMode(WeaponSystem.Mode.Sniper);
        if (id == 2) Weapon.SetMode(WeaponSystem.Mode.Triple);

        Time.timeScale = 1f;
        State = GameState.Playing;

        // wider spacing between pipe columns each round
        pipeSpawner.GetComponent<PipeSpawner>().spawnTime += 1.2f;

        Invoke("BeginNextWave", 2f);
    }

    private void BeginNextWave() {
        if (State == GameState.Playing) Waves.StartWave(Waves.CurrentWave + 1);
    }

    public void OnGameOver() {
        if (State == GameState.GameOver) return;
        State = GameState.GameOver;
        Time.timeScale = 1f;
        if (Waves != null) Waves.StopAll();
        StartCoroutine(ShowEndButtons());
    }

    private IEnumerator ShowEndButtons() {
        yield return new WaitForSeconds(0.6f);

        if (restartBtn != null) {
            restartBtn.SetActive(true);
            Vector3 p = ViewportToWorld(0.5f, 0.38f);
            restartBtn.transform.position = new Vector3(p.x, p.y, 0f);
        }

        // swallow the click that killed the bird so it cannot restart instantly
        yield return new WaitForSeconds(0.3f);
        canRestart = true;
    }

    private void CreateRestartButton() {
        if (restartSprite == null) return;

        restartBtn = new GameObject("restart_btn");
        SpriteRenderer sr = restartBtn.AddComponent<SpriteRenderer>();
        sr.sprite = restartSprite;
        sr.sortingLayerName = "ui";
        sr.sortingOrder = 10;
        restartBtn.SetActive(false);
    }

    private void HandleRestartInput() {
        if (restartBtn == null) {
            if (Input.GetMouseButtonDown(0)) Restart();
            return;
        }

        SpriteRenderer sr = restartBtn.GetComponent<SpriteRenderer>();

        if (Input.GetMouseButtonDown(0) && IsOverButton(sr)) {
            btnPressed = true;
            restartBtn.transform.position -= new Vector3(0f, 0.03f, 0f);
        } else if (Input.GetMouseButtonUp(0) && btnPressed) {
            restartBtn.transform.position += new Vector3(0f, 0.03f, 0f);
            btnPressed = false;
            if (IsOverButton(sr)) Restart();
        }
    }

    private bool IsOverButton(SpriteRenderer sr) {
        Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        wp.z = sr.bounds.center.z;
        return sr.bounds.Contains(wp);
    }

    private void Restart() {
        Time.timeScale = 1f;
        Application.LoadLevel(Application.loadedLevel);
    }

    // ---------- GUI ----------

    private Rect SkillBtnRect() {
        return new Rect(Screen.width * 0.5f - 90f, Screen.height * 0.06f, 180f, 42f);
    }

    private bool IsMouseOverSkillBtn() {
        if (State != GameState.Ready) return false;
        Vector2 m = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return SkillBtnRect().Contains(m);
    }

    private void EnsureStyles() {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.06f);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        hudStyle = new GUIStyle(GUI.skin.label);
        hudStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.024f);
        hudStyle.fontStyle = FontStyle.Bold;
        hudStyle.normal.textColor = Color.white;

        btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.022f);
        btnStyle.wordWrap = true;

        hintStyle = new GUIStyle(hudStyle);
        hintStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.02f);
    }

    void OnGUI() {
        EnsureStyles();
        float W = Screen.width;
        float H = Screen.height;

        if (State == GameState.Ready) {
            GUI.backgroundColor = GodMode ? new Color(0.4f, 1f, 0.4f) : Color.white;
            string label = GodMode ? "Skill Issue: BẬT (bất tử)" : "Skill Issue";
            if (GUI.Button(SkillBtnRect(), label, btnStyle)) GodMode = !GodMode;
            GUI.backgroundColor = Color.white;

            GUI.Label(new Rect(10f, H - 60f, W - 20f, 50f), "Chuột trái: bay   |   A: bắn   |   W: nạp đạn   |   D: khiên phản đòn", hintStyle);
            return;
        }

        DrawHud(W, H);

        if (State == GameState.Upgrading) DrawUpgradeMenu(W, H);

        if (State == GameState.GameOver) {
            DrawShadowLabel(new Rect(0f, H * 0.28f, W, 80f), "GAME OVER", titleStyle, new Color(1f, 0.25f, 0.2f));
            if (restartBtn == null && canRestart) {
                if (GUI.Button(new Rect(W * 0.5f - 80f, H * 0.5f, 160f, 48f), "Chơi lại", btnStyle)) Restart();
            }
        }

        if (State == GameState.Victory) {
            DrawShadowLabel(new Rect(0f, H * 0.28f, W, 80f), "CHIẾN THẮNG!", titleStyle, new Color(1f, 0.9f, 0.2f));
            if (restartBtn == null && canRestart) {
                if (GUI.Button(new Rect(W * 0.5f - 80f, H * 0.5f, 160f, 48f), "Chơi lại", btnStyle)) Restart();
            }
        }
    }

    private void DrawShadowLabel(Rect r, string text, GUIStyle style, Color color) {
        Color old = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        GUI.Label(new Rect(r.x + 3f, r.y + 3f, r.width, r.height), text, style);
        style.normal.textColor = color;
        GUI.Label(r, text, style);
        style.normal.textColor = old;
    }

    private void DrawHud(float W, float H) {
        float bw = W * 0.3f;
        float frac = Bird != null ? (float)Bird.Health / Bird.maxHealth : 0f;
        DrawBar(12f, 12f, bw, 20f, frac, new Color(0.9f, 0.2f, 0.2f));

        string hpText = "Máu: " + (Bird != null ? Bird.Health : 0) + "/" + (Bird != null ? Bird.maxHealth : 100);
        if (GodMode) hpText += "   (BẤT TỬ)";
        GUI.Label(new Rect(16f, 34f, 400f, 30f), hpText, hudStyle);

        string ammoText = "";
        if (Weapon != null) {
            if (Weapon.Reloading) ammoText = "Đang nạp đạn... " + Weapon.ReloadLeft.ToString("0.0") + "s";
            else if (Weapon.ammo <= 0) ammoText = "HẾT ĐẠN — nhấn W để nạp";
            else ammoText = "Đạn: " + Weapon.ammo + "/" + Weapon.magSize;
        }
        GUI.Label(new Rect(16f, 62f, 400f, 30f), ammoText, hudStyle);

        string shieldText = "";
        if (Shield != null) {
            if (Shield.IsActive) shieldText = "Khiên: ĐANG PHẢN ĐÒN";
            else if (Shield.CooldownLeft > 0f) shieldText = "Khiên: " + Shield.CooldownLeft.ToString("0.0") + "s";
            else shieldText = "Khiên: SẴN SÀNG (D)";
        }
        GUI.Label(new Rect(16f, 90f, 400f, 30f), shieldText, hudStyle);

        if (Waves != null && Waves.CurrentWave > 0 && State != GameState.Victory) {
            string w = "Wave " + Waves.CurrentWave + "/3 — Địch: " + EnemyBird.Alive.Count;
            GUI.Label(new Rect(W - 320f, 12f, 308f, 30f), w, hudStyle);
        }
    }

    private void DrawBar(float x, float y, float w, float h, float frac, Color color) {
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(x + 2f, y + 2f, (w - 4f) * Mathf.Clamp01(frac), h - 4f), Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void DrawUpgradeMenu(float W, float H) {
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0f, 0f, W, H), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawShadowLabel(new Rect(0f, H * 0.16f, W, 60f), "CHỌN NÂNG CẤP", titleStyle, Color.white);

        float bw = Mathf.Min(420f, W * 0.8f);
        float bh = 64f;
        float x = W * 0.5f - bw * 0.5f;
        float y = H * 0.32f;

        if (!shieldUpTaken) {
            if (GUI.Button(new Rect(x, y, bw, bh), "Khiên nhanh: hồi chiêu khiên còn 2s", btnStyle)) { PickUpgrade(0); return; }
            y += bh + 14f;
        }
        if (Weapon.mode != WeaponSystem.Mode.Sniper) {
            if (GUI.Button(new Rect(x, y, bw, bh), "Đạn xuyên phá: 100 dmg, 1 viên, nạp đạn 5s", btnStyle)) { PickUpgrade(1); return; }
            y += bh + 14f;
        }
        if (Weapon.mode != WeaponSystem.Mode.Triple) {
            if (GUI.Button(new Rect(x, y, bw, bh), "Bắn 3 luồng: 15 dmg mỗi viên", btnStyle)) { PickUpgrade(2); return; }
        }
    }
}
