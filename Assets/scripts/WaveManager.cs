using UnityEngine;
using System.Collections;

// Wave 1: 5 birds, wave 2: 10 birds, wave 3: one boss.
public class WaveManager : MonoBehaviour {

    public int CurrentWave { get; private set; }

    private int pendingSpawns;
    private bool active;

    public bool WaveActive { get { return active; } }

    public void StartWave(int wave) {
        CurrentWave = wave;
        pendingSpawns = wave == 1 ? 5 : (wave == 2 ? 10 : 1);
        active = true;
        StartCoroutine(SpawnWave(wave));
    }

    private IEnumerator SpawnWave(int wave) {
        bool boss = wave == 3;
        int count = pendingSpawns;

        for (int i = 0; i < count; i++) {
            EnemyBird.Spawn(boss);
            pendingSpawns--;
            yield return new WaitForSeconds(0.9f);
        }
    }

    void Update() {
        if (active && pendingSpawns <= 0 && EnemyBird.Alive.Count == 0) {
            active = false;
            GameMain.I.OnWaveCleared();
        }
    }

    public void StopAll() {
        active = false;
        StopAllCoroutines();
    }
}
