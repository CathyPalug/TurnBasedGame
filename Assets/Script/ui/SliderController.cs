using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
    public Slider thisSlider;
    public Text valueText;
    private void Start()
    {
        thisSlider = GetComponent<Slider>();
    }
    private void LateUpdate()
    {
        valueText.text = $"{thisSlider.value}%";
    }
    public void ChangeSliderValue(int _value)
    {
        thisSlider.value += _value;
    }
}
