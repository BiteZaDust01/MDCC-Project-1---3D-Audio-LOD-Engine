using UnityEngine;

public class PlayerRaycaster : MonoBehaviour
{
    public float interactionRange = 5f;
    private Transform currentTarget;
    private Material targetMaterial;
    private bool isHovering = false;

    void Update()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        bool lookingAtSomething = false;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            // Check what we hit
            RadioLODController radio = hit.transform.GetComponent<RadioLODController>();
            RVLODController rv = hit.transform.GetComponent<RVLODController>();

            if (radio != null || rv != null)
            {
                lookingAtSomething = true;

                // If we looked at a NEW object, grab its specific material for the glow effect
                if (currentTarget != hit.transform)
                {
                    currentTarget = hit.transform;
                    targetMaterial = currentTarget.GetComponentInChildren<Renderer>().material;
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (radio != null)
                    {
                        // Radio Logic (Toggle Pause/Play)
                        if (radio.audioSource.isPlaying)
                        {
                            radio.audioSource.Pause();
                            radio.resultText.text = "Radio Paused.";
                        }
                        else
                        {
                            radio.audioSource.Play();
                            if (radio.isShowingCompressed)
                                radio.RunExperiment(Vector3.Distance(transform.position, radio.transform.position));
                        }
                    }
                    else if (rv != null)
                    {
                        // RV Logic (Single shot compressed play)
                        rv.PlayCompressed();
                    }
                }
            }
        }

        //     // --- GLOW & TEXT LOGIC ---
        //     if (lookingAtSomething && !isHovering)
        //     {
        //         isHovering = true;
        //         if (targetMaterial != null)
        //         {
        //             targetMaterial.EnableKeyword("_EMISSION");
        //             targetMaterial.SetColor("_EmissionColor", new Color(0.3f, 0.3f, 0.3f));
        //         }

        //         RadioLODController radio = currentTarget?.GetComponent<RadioLODController>();
        //         if (radio != null && radio.hoverLabel != null) radio.hoverLabel.SetActive(true);
        //     }
        //     else if (!lookingAtSomething && isHovering)
        //     {
        //         isHovering = false;
        //         if (targetMaterial != null) targetMaterial.SetColor("_EmissionColor", Color.black);

        //         RadioLODController radio = currentTarget?.GetComponent<RadioLODController>();
        //         if (radio != null && radio.hoverLabel != null) radio.hoverLabel.SetActive(false);

        //         currentTarget = null;
        //         targetMaterial = null;
        //     }

        //     // Billboard trick for floating text
        //     if (isHovering && currentTarget != null)
        //     {
        //         RadioLODController radio = currentTarget.GetComponent<RadioLODController>();
        //         if (radio != null && radio.hoverLabel != null)
        //             radio.hoverLabel.transform.rotation = Camera.main.transform.rotation;
        //     }
        // }
        // --- GLOW & TEXT LOGIC ---
        if (lookingAtSomething && !isHovering)
        {
            isHovering = true;
            if (targetMaterial != null)
            {
                targetMaterial.EnableKeyword("_EMISSION");
                targetMaterial.SetColor("_EmissionColor", new Color(0.3f, 0.3f, 0.3f));
            }

            // Turn on Radio text if it's a Radio
            RadioLODController radio = currentTarget?.GetComponent<RadioLODController>();
            if (radio != null && radio.hoverLabel != null) radio.hoverLabel.SetActive(true);

            // Turn on RV text if it's an RV
            RVLODController rv = currentTarget?.GetComponent<RVLODController>();
            if (rv != null && rv.hoverLabel != null) rv.hoverLabel.SetActive(true);
        }
        else if (!lookingAtSomething && isHovering)
        {
            isHovering = false;
            if (targetMaterial != null) targetMaterial.SetColor("_EmissionColor", Color.black);

            // Turn off Radio text
            RadioLODController radio = currentTarget?.GetComponent<RadioLODController>();
            if (radio != null && radio.hoverLabel != null) radio.hoverLabel.SetActive(false);

            // Turn off RV text
            RVLODController rv = currentTarget?.GetComponent<RVLODController>();
            if (rv != null && rv.hoverLabel != null) rv.hoverLabel.SetActive(false);

            currentTarget = null;
            targetMaterial = null;
        }

        // --- BILLBOARD TRICK (Always face camera) ---
        if (isHovering && currentTarget != null)
        {
            RadioLODController radio = currentTarget.GetComponent<RadioLODController>();
            if (radio != null && radio.hoverLabel != null)
                radio.hoverLabel.transform.rotation = Camera.main.transform.rotation;

            RVLODController rv = currentTarget.GetComponent<RVLODController>();
            if (rv != null && rv.hoverLabel != null)
                rv.hoverLabel.transform.rotation = Camera.main.transform.rotation;
        }
    }
}