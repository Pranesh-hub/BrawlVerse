using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Background Music")]
    public AudioClip bgmMenu;
    public AudioClip bgmInGame;
    public AudioClip bgmWin;
    public AudioClip bgmLose;

    [Header("Sound Effects")]
    public AudioClip walkSound;
    public AudioClip jumpSound;
    public AudioClip kickSound;
    public AudioClip stompSound;
    public AudioClip uppercutSound;
    public AudioClip killSound;
    public AudioClip deathSound;
    public AudioClip pushSound;
    public AudioClip headbuttSound;
    public AudioClip hitSound;
    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length < 2)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                bgmSource = sources[0];
                sfxSource = sources[1];
            }

            bgmSource.loop = true;
            PlayBGM(bgmMenu); // Play menu BGM immediately
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // BGM switching
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // Sound effects
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.PlayOneShot(clip);
    }

    // Optional helpers
    public void PlayWalk() => PlaySFX(walkSound);
    public void PlayJump() => PlaySFX(jumpSound);
    public void PlayKick() => PlaySFX(kickSound);
    public void PlayStomp() => PlaySFX(stompSound);
    public void PlayUppercut() => PlaySFX(uppercutSound);
    public void PlayPush() => PlaySFX(pushSound);
    public void PlayHeadbutt() => PlaySFX(headbuttSound);
    public void PlayKill() => PlaySFX(killSound);
    public void PlayDeath() => PlaySFX(deathSound);
    public void playhit() => PlaySFX(hitSound);
    public void PlayGameOverWin() => PlayBGM(bgmWin);
    public void PlayGameOverLose() => PlayBGM(bgmLose);
}