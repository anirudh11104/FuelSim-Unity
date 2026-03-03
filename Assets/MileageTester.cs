using UnityEngine;

public class MileageTester : MonoBehaviour
{
    public MileagePredictor predictor;   // ⭐ THIS creates the box in Inspector

    void Start()
    {
        float mileage = predictor.PredictMileage(650f, 210f);
        Debug.Log("Mileage = " + mileage);
    }
}
