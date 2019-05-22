using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// List View组件，提供垂直UI列表和水平UI列表功能。
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class ListView : MonoBehaviour
{
    // Todo 已知问题：
    // 在没有勾选FixedElementLength的情况下，
    // 添加元素后在添加动画协程执行完之前将其移除，
    // 会导致Layout Group的Content长度计算错误，
    // 因为当添加元素时按照元素的全尺寸增长Content长度，
    // 当移除元素时，按照元素的当前尺寸减小Content长度，
    // 而元素的当前尺寸是小于其全尺寸的（协程未完成）。

    #region 属性

    /// <summary>
    /// 内边距。
    /// </summary>
    public RectOffset Padding
    {
        get { return _padding; }
        set
        {
            _padding = value;
            _content.padding = _padding;
            CalcContentLength();
        }
    }

    /// <summary>
    /// 行间距。
    /// </summary>
    public float Spacing
    {
        get { return _spacing; }
        set
        {
            _spacing = value;
            _content.spacing = _spacing;
            CalcContentLength();
        }
    }

    /// <summary>
    /// ListView中的元素数量。
    /// </summary>
    public int ItemCount
    {
        get { return _items.Count; }
    }

    /// <summary>
    /// 移除元素的方法，默认销毁元素。
    /// 为此Action赋值来实现自定义的元素移除方法。
    /// </summary>
    public Action<GameObject> RemoveMethod = item => Destroy(item);

    #endregion


    #region Inspector属性

    [Tooltip("内部组件，请勿修改。")]
    [SerializeField]
    private ScrollRect _scrollRect;
    [Tooltip("内部组件，请勿修改。")]
    [SerializeField]
    private HorizontalOrVerticalLayoutGroup _content;

    [Header("列表布局")]
    [Tooltip("使用垂直List View还是水平List View？默认为垂直List View。")]
    [SerializeField]
    private Layout _layout = Layout.Vertical;
    [Tooltip("List View的内边距。")]
    [SerializeField]
    private RectOffset _padding;
    [Tooltip("List View的行间距。")]
    [Range(0, 5000)]
    [SerializeField]
    private float _spacing = 0.0f;

    [Header("元素属性")]
    [Tooltip("新增的元素是否添加到List View的头部？默认将新增元素添加到List View的尾部。")]
    public bool NewElementOnTop = false;
    [Tooltip("List View中所有元素的尺寸是否相同？如果不是，则每次添加和移除元素时计算元素尺寸。")]
    public bool FixedElementLength = true;
    [Tooltip("添加和移除List View元素时的动画时长，小于等于0时不播放动画。")]
    [Range(0, 1)]
    public float AnimationTime = 0.2f;

    #endregion


    #region 私有字段

    // 每个元素的尺寸（高度或宽度）
    private float _itemLenght = -1;
    // ListView中已有的元素
    private List<GameObject> _items = new List<GameObject>();

    #endregion


    #region 初始化和校验

    private void Awake()
    {
        InitLayout();

        _content.padding = _padding;
        _content.spacing = _spacing;
    }

    private void Start()
    {
        CalcContentLength();
    }

    private void OnValidate()
    {
        InitLayout();

        _content.padding = _padding;
        _content.spacing = _spacing;

        CalcContentLength();
    }

    #endregion


    #region 元素操作

    // 添加

    /// <summary>
    /// 添加ListView元素。若不指定元素位置，则根据 NewElementOnTop 属性自动选择新元素的位置：
    /// 如果 NewElementOnTop 属性为 false ，则将新元素添加到底部；否则将新元素添加到顶部。
    /// </summary>
    /// <param name="item"></param>
    /// <param name="index"></param>
    public void AddItem(GameObject item, int index = -1)
    {
        item.transform.SetParent(_content.transform);

        if (index < 0)
        {
            // 添加到默认位置
            if (NewElementOnTop)
            {
                _items.Insert(0, item);
                item.transform.SetAsFirstSibling();
            }
            else
            {
                _items.Add(item);
                item.transform.SetAsLastSibling();
            }
        }
        else
        {
            if (index > ItemCount)
            {
                index = ItemCount;
                UnityEngine.Debug.LogWarningFormat("ListView.AddItem()：给定索引超出ListView元素数量，被自动裁剪为【{0}】。", index);
            }

            // 需要改变列表元素顺序
            _items.Insert(index, item);
            item.transform.SetSiblingIndex(index);
        }

        AdjustContentLength(item.transform, 1);

        if (AnimationTime > 0)
        {
            StartCoroutine(IEAddItemAnim(item));
        }
    }

    /// <summary>
    /// 将元素添加到ListView顶部。
    /// </summary>
    /// <param name="item"></param>
    public void AddItemToTop(GameObject item)
    {
        AddItem(item, 0);
    }

    /// <summary>
    /// 将元素添加到ListView底部。
    /// </summary>
    /// <param name="item"></param>
    public void AddItemToBottom(GameObject item)
    {
        AddItem(item, ItemCount);
    }

    // 移除

    /// <summary>
    /// 移除ListView元素。
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool RemoveItem(GameObject item)
    {
        bool ret = _items.Contains(item);

        if (ret)
        {
            _items.Remove(item);

            AdjustContentLength(item.transform, -1);
            RemoveListItem(item);
        }

        return ret;
    }

    /// <summary>
    /// 移除ListView元素。
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool RemoveItem(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            UnityEngine.Debug.LogWarningFormat("ListView.RemoveListItem()：没有索引为【{0}】的元素，移除失败。", index);
            return false;
        }

        GameObject obj = _items[index];
        _items.RemoveAt(index);

        AdjustContentLength(obj.transform, -1);
        RemoveListItem(obj);

        return true;
    }

    /// <summary>
    /// 从ListView顶部移除元素。
    /// </summary>
    /// <param name="count"></param>
    public void RemoveTop(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            RemoveItem(0);
        }
    }

    /// <summary>
    /// 从ListView底部移除元素。
    /// </summary>
    /// <param name="count"></param>
    public void RemoveBottom(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            RemoveItem(ItemCount - 1);
        }
    }

    /// <summary>
    /// 移除ListView的所有元素。
    /// </summary>
    /// <returns></returns>
    public int RemoveAllItems()
    {
        int count = _items.Count;

        for (int i = count - 1; i >= 0; i--)
        {
            GameObject obj = _items[i];
            _items.RemoveAt(i);

            AdjustContentLength(obj.transform, -1);
            RemoveListItem(obj);
        }

        return count;
    }

    // 获取

    /// <summary>
    /// 根据索引获取ListView元素。
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public GameObject GetItem(int index)
    {
        return _items[index];
    }

    /// <summary>
    /// 在ListView中查找符合条件的元素，并返回找到的第一个元素。
    /// </summary>
    /// <param name="check">用于判断元素是否符合要求的方法，如果符合要求则返回true，否则返回false</param>
    /// <returns></returns>
    public GameObject FindItem(Func<GameObject, bool> check)
    {
        GameObject target = null;

        foreach (GameObject item in _items)
        {
            if (check(item))
            {
                target = item;
                break;
            }
        }

        return target;
    }

    /// <summary>
    /// 在ListView中查找符合条件的元素，并返回找到的所有元素。
    /// </summary>
    /// <param name="check">用于判断元素是否符合要求的方法，如果符合要求则返回true，否则返回false</param>
    /// <returns></returns>
    public GameObject[] FindItems(Func<GameObject, bool> check)
    {
        List<GameObject> targets = new List<GameObject>();

        foreach (GameObject item in _items)
        {
            if (check(item))
            {
                targets.Add(item);
            }
        }

        return targets.ToArray();
    }

    // 定位

    /// <summary>
    /// 将ListView视图定位到指定索引的元素的位置。
    /// </summary>
    /// <param name="index">目标元素索引</param>
    public void LocateTo(int index)
    {
        if (index <= 0)
        {
            LocateTo(0.0f);
        }
        else if (index >= ItemCount)
        {
            LocateTo(1.0f);
        }
        else
        {
            LocateTo((index + 1.0f) / ItemCount);
        }
    }

    /// <summary>
    /// 将ListView视图定位到指定百分比位置。
    /// </summary>
    /// <param name="percent">百分比</param>
    public void LocateTo(float percent)
    {
        percent = Mathf.Clamp01(percent);

        if (_layout == Layout.Vertical)
        {
            _scrollRect.verticalNormalizedPosition = percent;
        }
        else
        {
            _scrollRect.horizontalNormalizedPosition = percent;
        }
    }

    /// <summary>
    /// 根据给定的规则对ListView元素进行排序。
    /// </summary>
    /// <param name="comparison"></param>
    public void Sort(Comparison<GameObject> comparison)
    {
        _items.Sort(comparison);

        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].transform.SetSiblingIndex(i);
        }
    }

    #endregion


    #region 私有辅助方法

    // 初始化布局
    private void InitLayout()
    {
        var contentV = GetComponentInChildren<VerticalLayoutGroup>(true);
        var contentH = GetComponentInChildren<HorizontalLayoutGroup>(true);

        _scrollRect = GetComponent<ScrollRect>();

        if (_layout == Layout.Vertical)
        {
            _scrollRect.vertical = true;
            _scrollRect.horizontal = false;
            _scrollRect.content = contentV.transform as RectTransform;

            contentV.gameObject.SetActive(true);
            contentH.gameObject.SetActive(false);
            _content = contentV;
        }
        else
        {
            _scrollRect.vertical = false;
            _scrollRect.horizontal = true;
            _scrollRect.content = contentH.transform as RectTransform;

            contentV.gameObject.SetActive(false);
            contentH.gameObject.SetActive(true);
            _content = contentH;
        }
    }

    // 计算Content区域的初始长度
    private void CalcContentLength()
    {
        float contentLength = 0;

        int itemCount = _content.transform.childCount;
        contentLength += (itemCount - 1) * _spacing;

        foreach (var obj in _content.transform)
        {
            RectTransform rect = obj as RectTransform;

            if (_layout == Layout.Vertical)
            {
                contentLength += rect.sizeDelta.y;
            }
            else
            {
                contentLength += rect.sizeDelta.x;
            }
        }

        if (_layout == Layout.Vertical)
        {
            contentLength += _padding.top + _padding.bottom;
        }
        else
        {
            contentLength += _padding.left + _padding.right;
        }

        RectTransform contentRect = _content.transform as RectTransform;
        Vector2 contentSizeDelta = contentRect.sizeDelta;
        if (_layout == Layout.Vertical)
        {
            contentSizeDelta.y = contentLength;
        }
        else
        {
            contentSizeDelta.x = contentLength;
        }
        contentRect.sizeDelta = contentSizeDelta;
    }

    // 在添加或移除元素时计算调整Content区域的高度或宽度
    private void AdjustContentLength(Transform itemTrans, int power)
    {
        if (!FixedElementLength || _itemLenght < 0)
        {
            // 需要手动计算新增项高度
            RectTransform rect = itemTrans as RectTransform;
            if (_layout == Layout.Vertical)
            {
                _itemLenght = rect.sizeDelta.y;
            }
            else
            {
                _itemLenght = rect.sizeDelta.x;
            }
        }

        power = power < 0 ? -1 : 1;

        RectTransform contentRect = _content.transform as RectTransform;
        Vector2 contentSizeDelta = contentRect.sizeDelta;
        if (_layout == Layout.Vertical)
        {
            contentSizeDelta.y += (_itemLenght + _spacing) * power;
        }
        else
        {
            contentSizeDelta.x += (_itemLenght + _spacing) * power;
        }
        contentRect.sizeDelta = contentSizeDelta;
    }

    // 判断是否需要动画，并移除列表元素
    private void RemoveListItem(GameObject item)
    {
        if (AnimationTime > 0)
        {
            StartCoroutine(IERemoveItemAnim(item));
        }
        else
        {
            item.transform.SetParent(null);
            RemoveMethod(item);
        }
    }

    // 逐渐放大列表元素Y轴
    private IEnumerator IEAddItemAnim(GameObject item)
    {
        // HorizontalOrVerticalLayoutGroup 以元素的 Width 或 Height 属性计算位置，
        // 不以 Scale.x 或 Scale.y 计算位置，因此要同时修改 sizeDelta 。
        RectTransform rect = item.transform as RectTransform;
        float originLength;
        Vector2 currSize, currScale;

        if (_layout == Layout.Vertical)
        {
            originLength = rect.sizeDelta.y;
            currSize = new Vector2(rect.sizeDelta.x, 0);
            currScale = new Vector3(1, 0, 1);
        }
        else
        {
            originLength = rect.sizeDelta.x;
            currSize = new Vector2(0, rect.sizeDelta.y);
            currScale = new Vector3(0, 1, 1);
        }

        float timer = 0;
        rect.sizeDelta = currSize;
        rect.localScale = currSize;

        while (timer < AnimationTime)
        {
            timer += Time.deltaTime;

            if (_layout == Layout.Vertical)
            {
                currScale.y = timer / AnimationTime;
                currSize.y = originLength * currScale.y;
            }
            else
            {
                currScale.x = timer / AnimationTime;
                currSize.x = originLength * currScale.x;
            }

            rect.localScale = currScale;
            rect.sizeDelta = currSize;

            yield return null;
        }

        if (_layout == Layout.Vertical)
        {
            currScale.y = 1;
            currSize.y = originLength;
        }
        else
        {
            currScale.x = 1;
            currSize.x = originLength;
        }
        rect.localScale = currScale;
        rect.sizeDelta = currSize;
    }

    // 逐渐缩小列表元素Y轴，最终销毁元素
    private IEnumerator IERemoveItemAnim(GameObject item)
    {
        // HorizontalOrVerticalLayoutGroup 以元素的 Width 或 Height 属性计算位置，
        // 不以 Scale.x 或 Scale.y 计算位置，因此要同时修改 sizeDelta 。
        RectTransform rect = item.transform as RectTransform;
        float originLength;
        Vector2 currSize, currScale;

        if (_layout == Layout.Vertical)
        {
            originLength = rect.sizeDelta.y;
            currSize = rect.sizeDelta;
            currScale = rect.localScale;
        }
        else
        {
            originLength = rect.sizeDelta.x;
            currSize = rect.sizeDelta;
            currScale = rect.localScale;
        }

        float timer = 0;
        while (timer < AnimationTime)
        {
            timer += Time.deltaTime;

            if (_layout == Layout.Vertical)
            {
                currScale.y = 1 - timer / AnimationTime;
                currSize.y = originLength * currScale.y;
            }
            else
            {
                currScale.x = 1 - timer / AnimationTime;
                currSize.x = originLength * currScale.x;
            }

            rect.localScale = currScale;
            rect.sizeDelta = currSize;

            yield return null;
        }

        rect.SetParent(null);
        RemoveMethod(item);
    }

    #endregion


    private enum Layout
    {
        Vertical, // 使用VerticalLayoutGroup
        Horizontal, // 使用HorizontalLayoutGroup
    }

}
