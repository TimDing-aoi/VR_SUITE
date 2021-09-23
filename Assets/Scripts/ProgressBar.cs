using UnityEngine;
using UnityEngine.Events;
using static ObjectPooler;

public class ProgressBar : FillBar
{

    // Event to invoke when the progress bar fills up
    private UnityEvent onProgressComplete;
    public GameObject panel;

    // Create a property to handle the slider's value
    public new float CurrentValue
    {
        get
        {
            return base.CurrentValue;
        }
        set
        {
            // If the value exceeds the max fill, invoke the completion function
            if (value >= 0.99995)
            {
                onProgressComplete.Invoke();
            }

            // Remove any overfill (i.e. 105% fill -> 5% fill)
            base.CurrentValue = value % slider.maxValue;
        }
    }

    void Start()
    {
        // Initialize onProgressComplete and set a basic callback
        if (onProgressComplete == null)
        {
            onProgressComplete = new UnityEvent();
            onProgressComplete.AddListener(OnProgressComplete);
        }
    }

    void Update()
    {
        CurrentValue = ObjectPooler.SharedInstance.fill;
    }

    // The method to call when the progress bar fills up
    void OnProgressComplete()
    {
        panel.SetActive(false);
    }
}