using UnityEngine;
using PolySpatial.Template;

/// <summary>
/// Adds SpatialUI pinch interaction to orbs.
/// This component should be on the same GameObject as OrbBehavior.
/// It connects the SpatialUI PressEnd event to the OrbBehavior.Press() method.
/// </summary>
[RequireComponent(typeof(OrbBehavior))]
public class OrbSpatialUI : SpatialUI
{
    private OrbBehavior _orbBehavior;

    private void Awake()
    {
        _orbBehavior = GetComponent<OrbBehavior>();
        if (_orbBehavior == null)
        {
            Debug.LogError($"OrbSpatialUI on {gameObject.name} requires an OrbBehavior component!", this);
        }
    }

    private void OnEnable()
    {
        // Ensure OrbBehavior is found (in case Awake didn't run)
        if (_orbBehavior == null)
        {
            _orbBehavior = GetComponent<OrbBehavior>();
            if (_orbBehavior == null)
            {
                Debug.LogError($"OrbSpatialUI on {gameObject.name} requires an OrbBehavior component!", this);
            }
        }
    }

    public override void PressEnd()
    {
        Debug.Log($"OrbSpatialUI: PressEnd called on {gameObject.name}");
        base.PressEnd();
        
        // Call the orb's Press method when pinched
        if (_orbBehavior != null)
        {
            Debug.Log($"OrbSpatialUI: Calling Press() on OrbBehavior for {gameObject.name}");
            _orbBehavior.Press();
        }
        else
        {
            Debug.LogError($"OrbSpatialUI: _orbBehavior is null on {gameObject.name}!");
        }
    }
}

