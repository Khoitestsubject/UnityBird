using UnityEngine;
using System.Collections;

public class PipeSpawner : MonoBehaviour {

	public float spawnTime = 5f;		// The amount of time between each spawn.
	public float spawnDelay = 3f;		// The amount of time before spawning starts.
	public float extraGap = 0.5f;		// Extra vertical opening between the two pipes (world units).
	public GameObject pipe;
	public float[] heights;


	void Start ()
	{
	}

    // Coroutine instead of InvokeRepeating so spawnTime changes (wider gaps
    // after each wave) take effect on the next spawn.
    public void StartSpawning()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(spawnDelay);
        while (true)
        {
            Spawn();
            yield return new WaitForSeconds(spawnTime);
        }
    }

	void Spawn ()
	{
		int heightIndex = Random.Range(0, heights.Length);
		Vector2 pos = new Vector2(transform.position.x, heights[heightIndex]);

		GameObject go = Instantiate(pipe, pos, transform.rotation) as GameObject;
		WidenGap(go);
	}

	// Pushes the two pipes apart by extraGap in total, so the opening can be
	// tuned from the Inspector without editing the binary prefab. Which one is
	// upper is decided by Y position, not by name, in case naming is swapped.
	private void WidenGap(GameObject go)
	{
		if (extraGap <= 0f || go == null) return;

		Transform up = null;
		Transform down = null;
		foreach (Transform t in go.GetComponentsInChildren<Transform>())
		{
			if (t.name == "pipe_up") up = t;
			if (t.name == "pipe_down") down = t;
		}
		if (up == null || down == null) return;

		Transform top = up.position.y >= down.position.y ? up : down;
		Transform bottom = top == up ? down : up;

		top.position += new Vector3(0f, extraGap * 0.5f, 0f);
		bottom.position -= new Vector3(0f, extraGap * 0.5f, 0f);
	}

	public void GameOver()
	{
		StopAllCoroutines();
	}
}
