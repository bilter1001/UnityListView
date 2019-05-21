using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoMain : MonoBehaviour
{
    public ListView listViewVertical;
    public ListView listViewHorizontal;
    public DemoItem itemVPrefab;
    public DemoItem itemHPrefab;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (Input.GetKey(KeyCode.LeftShift)) // shift + v: remove
            {
                RemoveItem(listViewVertical);
            }
            else // v: add
            {
                AddItem(listViewVertical, itemVPrefab);
            }
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (Input.GetKey(KeyCode.LeftShift)) // shift + h: remove
            {
                RemoveItem(listViewHorizontal);
            }
            else // h: add
            {
                AddItem(listViewHorizontal, itemHPrefab);
            }
        }
    }

    private void AddItem(ListView lv, DemoItem prefab)
    {
        var color = new Color()
        {
            r = Random.Range(0.0f, 1.0f),
            g = Random.Range(0.0f, 1.0f),
            b = Random.Range(0.0f, 1.0f),
            a = 1.0f,
        };

        var item = Instantiate(prefab);
        item.SetContent(color.ToString(), color);

        lv.AddItem(item.gameObject);
    }

    private void RemoveItem(ListView lv)
    {
        lv.RemoveBottom();
    }
}
