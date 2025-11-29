using UnityEngine;
using System;

public class OrbBehavior : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Renderer orbRenderer;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color targetColor = Color.red;

    private MaterialPropertyBlock _propBlock;
    private bool _isTarget;

    // Other scripts (spawner / game manager) can listen to this
    public event Action<OrbBehavior> WasPressed;

    public bool IsTarget => _isTarget;

    private void Awake()
    {
        if (orbRenderer == null)
            orbRenderer = GetComponentInChildren<Renderer>();

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
        ApplyColor(_isTarget ? targetColor : idleColor);
    }

    public void Press()
    {
        // Only react if it was actually the "active" (red) orb
        if (!_isTarget) return;
        
        // Turn off this orb when pressed
        SetTarget(false);
        
        // TODO: play animation, particles, sound, etc.
        Debug.Log($"Orb pressed: {name}, IsTarget = {_isTarget}");

        WasPressed?.Invoke(this);
    }

    private void ApplyColor(Color color)
    {
        if (orbRenderer == null) return;

        orbRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_BaseColor", color); // or "_Color" depending on shader
        orbRenderer.SetPropertyBlock(_propBlock);
    }
}
