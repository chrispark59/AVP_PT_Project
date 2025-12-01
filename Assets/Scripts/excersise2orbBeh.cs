using UnityEngine;
using System;

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
        
        // Play audio if available
        if (audioSource != null)
        {
            audioSource.Play();
        }
        
        // Turn off this orb when pressed
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
}
