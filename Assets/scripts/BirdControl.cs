using UnityEngine;
using System.Collections;
using DG.Tweening;

public class BirdControl : MonoBehaviour {

	public int rotateRate = 10;
	public float upSpeed = 10;
    public GameObject scoreMgr;

    public AudioClip jumpUp;
    public AudioClip hit;
    public AudioClip score;

    public bool inGame = false;

    public int maxHealth = 300;
    public int Health { get; private set; }
    public bool IsDead { get { return dead; } }

	private bool dead = false;
	private bool landed = false;
    private float lastHitTime = -99f;

    private Sequence birdSequence;
    private SpriteRenderer sr;

    // Use this for initialization
    void Start () {
        Health = maxHealth;
        sr = GetComponent<SpriteRenderer>();

        float birdOffset = 0.05f;
        float birdTime = 0.3f;
        float birdStartY = transform.position.y;

        birdSequence = DOTween.Sequence();

        birdSequence.Append(transform.DOMoveY(birdStartY + birdOffset, birdTime).SetEase(Ease.Linear))
            .Append(transform.DOMoveY(birdStartY - 2 * birdOffset, 2 * birdTime).SetEase(Ease.Linear))
            .Append(transform.DOMoveY(birdStartY, birdTime).SetEase(Ease.Linear))
            .SetLoops(-1);
    }

	// Update is called once per frame
	void Update () {
        if (!inGame)
        {
            return;
        }
        birdSequence.Kill();

		if (!dead)
		{
			if (Input.GetButtonDown("Fire1"))
			{
                JumpUp();
			}
		}

		if (!landed)
		{
			float v = transform.GetComponent<Rigidbody2D>().velocity.y;

			float rotate = Mathf.Min(Mathf.Max(-90, v * rotateRate + 60), 30);

			transform.rotation = Quaternion.Euler(0f, 0f, rotate);
		}
		else
		{
			transform.GetComponent<Rigidbody2D>().rotation = -90;
		}
	}

	void OnTriggerEnter2D (Collider2D other)
	{
		if (other.name == "land" || other.name == "pipe_up" || other.name == "pipe_down")
		{
            if (!dead)
            {
                TakeDamage(50);

                // bounce off the ground so the bird cannot sit on it draining HP
                if (other.name == "land" && !dead)
                {
                    JumpUp();
                }
            }

			if (other.name == "land" && dead)
			{
				transform.GetComponent<Rigidbody2D>().gravityScale = 0;
				transform.GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0);

				landed = true;
			}
		}
	}

    public void TakeDamage(int dmg)
    {
        if (dead || !inGame) return;

        GameMain gm = GameMain.I;
        if (gm != null && (gm.GodMode || gm.State != GameMain.GameState.Playing)) return;

        // brief invulnerability so one pipe cannot drain HP in a single pass
        if (Time.time - lastHitTime < 0.8f) return;
        lastHitTime = Time.time;

        Health -= dmg;
        AudioSource.PlayClipAtPoint(hit, Vector3.zero);

        if (Health <= 0)
        {
            Health = 0;
            Die();
        }
        else
        {
            StopCoroutine("Flash");
            StartCoroutine("Flash");
        }
    }

    public void Heal(int amount)
    {
        if (dead) return;
        Health = Mathf.Min(maxHealth, Health + amount);
    }

    public void HealFull()
    {
        if (dead) return;
        Health = maxHealth;
    }

    private IEnumerator Flash()
    {
        for (int i = 0; i < 4; i++)
        {
            sr.color = new Color(1f, 0.35f, 0.35f);
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Die()
    {
        dead = true;

        GameObject[] objs = GameObject.FindGameObjectsWithTag("movable");
        foreach (GameObject g in objs)
        {
            g.BroadcastMessage("GameOver");
        }

        GetComponent<Animator>().SetTrigger("die");

        GameMain gm = GameMain.I;
        if (gm != null)
        {
            gm.OnGameOver();
        }
    }

    public void JumpUp()
    {
        transform.GetComponent<Rigidbody2D>().velocity = new Vector2(0, upSpeed);
        AudioSource.PlayClipAtPoint(jumpUp, Vector3.zero);
    }

	public void GameOver()
	{
		dead = true;
	}
}
