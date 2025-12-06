using UnityEngine;
using System;
using System.Collections;

public class OrbBehavior : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Renderer orbRenderer;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color targetColor = Color.red;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private MaterialPropertyBlock _propBlock;
    private bool _isTarget;

    // Other scripts (spawner / game manager) can listen to this
    public event Action<OrbBehavior> WasPressed;

    public bool IsTarget => _isTarget;

    private void Awake()
    {
        if (orbRenderer == null)
            orbRenderer = GetComponentInChildren<Renderer>();

        // Auto-find AudioSource if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Debug audio source setup
        if (audioSource != null)
        {
            Debug.Log($"OrbBehavior ({name}): AudioSource found. Clip: {(audioSource.clip != null ? audioSource.clip.name : "NULL")}, " +
                     $"Volume: {audioSource.volume}, Mute: {audioSource.mute}, Enabled: {audioSource.enabled}, " +
                     $"PlayOnAwake: {audioSource.playOnAwake}");
        }
        else
        {
            Debug.LogWarning($"OrbBehavior ({name}): No AudioSource found! Audio will not play.");
        }

        _propBlock = new MaterialPropertyBlock();
        ApplyColor(idleColor);
    }

    private void OnEnable()
    {
        _isTarget = false;
        ApplyColor(idleColor);
    }

    public void SetTarget(bool isTarget)
    {
        _isTarget = isTarget;
        
        if (isTarget)
        {
            // When becoming the target, ensure it's active and red
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            ApplyColor(targetColor);
        }
        else
        {
            // When no longer the target, deactivate immediately so only red orbs are visible
            ApplyColor(idleColor);
            gameObject.SetActive(false);
        }
    }

    public void Press()
    {
        Debug.Log($"OrbBehavior.Press() called on {name}, _isTarget = {_isTarget}");
        
        // Only react if it was actually the "active" (red) orb
        if (!_isTarget)
        {
            Debug.Log($"OrbBehavior: {name} is not the target, ignoring press");
            return;
        }
        
        // Play audio BEFORE deactivating the GameObject (PlayOneShot works even if GameObject is deactivated)
        if (audioSource != null && audioSource.clip != null)
        {
            Debug.Log($"OrbBehavior ({name}): Attempting to play audio. " +
                     $"Clip: {audioSource.clip.name}, Volume: {audioSource.volume}, Mute: {audioSource.mute}, " +
                     $"Enabled: {audioSource.enabled}, IsPlaying: {audioSource.isPlaying}, " +
                     $"GameObject Active: {gameObject.activeSelf}, GameObject ActiveInHierarchy: {gameObject.activeInHierarchy}, " +
                     $"Spatial Blend: {audioSource.spatialBlend}, 3D Sound: {audioSource.spatialize}, " +
                     $"Min Distance: {audioSource.minDistance}, Max Distance: {audioSource.maxDistance}");
            
            // Check for AudioListener
            AudioListener listener = FindFirstObjectByType<AudioListener>();
            if (listener == null)
            {
                Debug.LogWarning($"OrbBehavior ({name}): No AudioListener found in scene! Audio may not be audible.");
            }
            else
            {
                float distanceToListener = Vector3.Distance(transform.position, listener.transform.position);
                Debug.Log($"OrbBehavior ({name}): AudioListener found at distance {distanceToListener:F2}m. " +
                         $"Within max distance ({audioSource.maxDistance}): {distanceToListener <= audioSource.maxDistance}");
            }
            
            if (audioSource.mute)
            {
                Debug.LogWarning($"OrbBehavior ({name}): AudioSource is muted! Audio will not play.");
            }
            else if (!audioSource.enabled)
            {
                Debug.LogWarning($"OrbBehavior ({name}): AudioSource component is disabled! Audio will not play.");
            }
            else
            {
                // Use PlayOneShot instead of Play() - it works even if GameObject gets deactivated
                // PlayOneShot doesn't require the AudioSource to stay active
                audioSource.PlayOneShot(audioSource.clip, audioSource.volume);
                Debug.Log($"OrbBehavior ({name}): audioSource.PlayOneShot() called with clip '{audioSource.clip.name}' at volume {audioSource.volume}. " +
                         $"IsPlaying after call: {audioSource.isPlaying}");
                
                // Check again after a frame to see if it started playing
                StartCoroutine(CheckAudioPlaying());
            }
        }
        else if (audioSource == null)
        {
            Debug.LogWarning($"OrbBehavior ({name}): audioSource is null! Cannot play audio.");
        }
        else if (audioSource.clip == null)
        {
            Debug.LogError($"OrbBehavior ({name}): AudioSource has no clip assigned! Cannot play audio.");
        }
        
        // Turn off this orb when pressed (this deactivates the GameObject, but PlayOneShot should still work)
        SetTarget(false);
        
        // TODO: play animation, particles, etc.
        Debug.Log($"Orb pressed: {name}, WasPressed event has {WasPressed?.GetInvocationList().Length ?? 0} subscribers");

        WasPressed?.Invoke(this);
        Debug.Log($"OrbBehavior: WasPressed event invoked for {name}");
    }

    private void ApplyColor(Color color)
    {
        if (orbRenderer == null) return;

        orbRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_BaseColor", color); // or "_Color" depending on shader
        orbRenderer.SetPropertyBlock(_propBlock);
    }

    /// <summary>
    /// Coroutine to check if audio is actually playing after Play() is called.
    /// </summary>
    private IEnumerator CheckAudioPlaying()
    {
        yield return new WaitForEndOfFrame();
        
        if (audioSource != null)
        {
            if (audioSource.isPlaying)
            {
                Debug.Log($"OrbBehavior ({name}): ✅ Audio is playing successfully! " +
                         $"Clip: {audioSource.clip.name}, Time: {audioSource.time:F3}s, " +
                         $"Volume: {audioSource.volume}, OutputAudioMixerGroup: {(audioSource.outputAudioMixerGroup != null ? audioSource.outputAudioMixerGroup.name : "None")}");
            }
            else
            {
                Debug.LogWarning($"OrbBehavior ({name}): ❌ Audio is NOT playing after Play() call! " +
                               $"Clip: {(audioSource.clip != null ? audioSource.clip.name : "NULL")}, " +
                               $"Volume: {audioSource.volume}, Mute: {audioSource.mute}, " +
                               $"Enabled: {audioSource.enabled}, Time: {audioSource.time}, " +
                               $"GameObject Active: {gameObject.activeSelf}, " +
                               $"OutputAudioMixerGroup: {(audioSource.outputAudioMixerGroup != null ? audioSource.outputAudioMixerGroup.name : "None")}");
                
                // Try alternative: PlayOneShot
                if (audioSource.clip != null)
                {
                    Debug.Log($"OrbBehavior ({name}): Attempting PlayOneShot as fallback...");
                    audioSource.PlayOneShot(audioSource.clip, audioSource.volume);
                    yield return new WaitForEndOfFrame();
                    Debug.Log($"OrbBehavior ({name}): After PlayOneShot, IsPlaying: {audioSource.isPlaying}");
                }
            }
        }
    }
}
