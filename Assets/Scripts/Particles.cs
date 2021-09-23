using UnityEngine;
using static Reward2D;

public class Particles : MonoBehaviour
{
    public float lifeSpan;
    public float dist;
    public float density;
    public float p_height;
    readonly private float baseH = 0.0185f;
    private uint seed;

    bool flagChange = true;

    ParticleSystem particleSystem;
    // Start is called before the first frame update
    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();

        particleSystem.Stop();

        seed = (uint)UnityEngine.Random.Range(1, 10000);
        PlayerPrefs.SetInt("Optic Flow Seed", (int)seed);
        //particleSystem.randomSeed = seed;

        particleSystem.Play();
    }
    void Start()
    {
        lifeSpan = PlayerPrefs.GetFloat("Life Span");
        dist = PlayerPrefs.GetFloat("Draw Distance");
        density = PlayerPrefs.GetFloat("Density");
        p_height = PlayerPrefs.GetFloat("Triangle Height"); // <-- this is really player height I'm just bad at naming variables



        var main = particleSystem.main;
        var emission = particleSystem.emission;
        var shape = particleSystem.shape;

        main.startLifetime = lifeSpan;
        main.startSize = p_height * baseH;
        main.maxParticles = Mathf.RoundToInt(Mathf.Pow(dist, 2.0f) * Mathf.PI * density / p_height);

        emission.rateOverTime = Mathf.CeilToInt(main.maxParticles / 10000.0f) / lifeSpan * 10000.0f / (p_height);

        shape.randomPositionAmount = dist;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //var main = particleSystem.main;
        //var emission = particleSystem.emission;
        //if (SharedReward.isBegin)
        //{
        //    if (flagChange)
        //    {
        //        density /= 50.0f;
        //        main.maxParticles = Mathf.RoundToInt(Mathf.Pow(dist, 2.0f) * Mathf.PI * density / p_height);
        //        emission.rateOverTime = Mathf.CeilToInt(main.maxParticles / 10000.0f) / lifeSpan * 10000.0f / (p_height);
        //        particleSystem.Clear();
        //    }
        //    else
        //    {
        //        density *= 50.0f;
        //        main.maxParticles = Mathf.RoundToInt(Mathf.Pow(dist, 2.0f) * Mathf.PI * density / p_height);
        //        emission.rateOverTime = Mathf.CeilToInt(main.maxParticles / 10000.0f) / lifeSpan * 10000.0f / (p_height);
        //        particleSystem.Clear();
        //        //emission.rateOverTime = main.maxParticles;
        //        //emission.rateOverTime = Mathf.CeilToInt(main.maxParticles / 10000.0f) / lifeSpan * 10000.0f / (p_height);
        //    }
        //    flagChange = !flagChange;
        //}
    }
}
