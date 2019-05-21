using UnityEngine;
using UnityEngine.UI;

public class DemoItem : MonoBehaviour
{
    [SerializeField]
    private Image _image;
    [SerializeField]
    private Text _text;

    private void Awake()
    {
        if (_image == null)
        {
            _image = GetComponentInChildren<Image>();
        }

        if (_text == null)
        {
            _text = GetComponentInChildren<Text>();
        }

        _text.text = "-";
    }

    public void SetContent(string text, Color bgColor)
    {
        _text.text = text;
        _image.color = bgColor;
    }
}
