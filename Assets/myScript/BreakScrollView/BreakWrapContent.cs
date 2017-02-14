using UnityEngine;
using System.Collections.Generic;

public class BreakWrapContent : MonoBehaviour {

    //这个脚本基本上就只是一个UIWrapContent，只是本来在UIWrapContent用到的一些数据需要从BreakScrollView获取

    BreakScrollView breakScroll;
    BreakScrollView BreakScroll
    {
        get
        {
            if (breakScroll == null)
                breakScroll = transform.parent.parent.GetComponent<BreakScrollView>();
            return breakScroll;
        }
    }
    
    protected int itemSize { get { return BreakScroll.itemSize; } }
    protected int minIndex { get { return BreakScroll.minIndex; } }
    protected int maxIndex { get { return BreakScroll.maxIndex; } }

    protected bool mHorizontal { get { return BreakScroll.mHorizontal; } }

    protected bool cullContent { get { return BreakScroll.cullContent; } }

    protected UIScrollView mScroll;
    protected UIPanel mPanel;
    protected bool mFirstTime = true;
    protected  List<Transform> mChilds;


	protected virtual void Start()
    {
        CacheScrollView();
        CacheChilds();
        adjustWrapPos();
        ResetChildPostion();
        mPanel.onClipMove += OnMove;
        WrapContent();

    }

    void CacheScrollView()
    {
        mScroll = transform.parent.GetComponent<UIScrollView>();
        mPanel = transform.parent.GetComponent<UIPanel>();
    }

    void CacheChilds()
    {
        
        if (mChilds == null)
            mChilds = new List<Transform>();
        mChilds.Clear();
        for(int i=0,Imax=transform.childCount;i<Imax;i++)
        {
            mChilds.Add(transform.GetChild(i));
        }
    }

    /// <summary>
    /// 调整Wrap的起始坐标，使得ScrollView的复位坐标为0 详情见 http://blog.csdn.net/mutou_222/article/details/54975521 
    /// </summary>
    public void adjustWrapPos()
    {
        if (mScroll == null)
            CacheScrollView();
        if (mChilds == null)
            CacheChilds();

        Bounds b = NGUIMath.CalculateRelativeWidgetBounds(mScroll.transform, mChilds[0], true);
        Vector2 pos = transform.localPosition;
        if (mHorizontal)
        {
            pos.x = -(mPanel.GetViewSize().x / 2f - mPanel.baseClipRegion.x - b.extents.x - mPanel.clipSoftness.x);
        }
        else
        {
            pos.y = mPanel.GetViewSize().y / 2f + mPanel.baseClipRegion.y - b.extents.y - mPanel.clipSoftness.y;
        }
        transform.localPosition = pos;
    }


    [ContextMenu("ResetChildPosition")]    
    public virtual void ResetChildPostion()
    {
        if (mChilds == null)
            CacheChilds();
        for(int i=0,Imax=mChilds.Count;i<Imax;i++)
        {
            var child = mChilds[i];
            Vector2 pos = Vector2.zero;
            if (mHorizontal)
                pos.x = i * itemSize;
            else
                pos.y = -i * itemSize;
            child.localPosition = pos;
            UpdateItem(child, i);
        }
    }


    public void RefreshChildData()
    {
        for(int i=0,Imax=mChilds.Count;i<Imax;i++)
        {
            UpdateItem(mChilds[i], i);
        }
    }


    protected virtual void OnMove(UIPanel panel)
    {
        WrapContent();
    }

    //基本上是原原本本的WrapContent了，修改了一些界限判定
    void WrapContent()
    {
        float extents = itemSize * mChilds.Count * 0.5f;
        Vector3[] corners = mPanel.worldCorners;

        for (int i = 0; i < 4; ++i)
        {
            Vector3 v = corners[i];
            v = transform.InverseTransformPoint(v);
            corners[i] = v;
        }

        Vector3 center = Vector3.Lerp(corners[0], corners[2], 0.5f);
        bool allWithinRange = true;
        float ext2 = extents * 2f;

        if (mHorizontal)
        {
            float min = corners[0].x - itemSize;
            float max = corners[2].x + itemSize;

            for (int i = 0, imax = mChilds.Count; i < imax; ++i)
            {
                Transform t = mChilds[i];
                float distance = t.localPosition.x - center.x;

                if (distance < -extents)
                {
                    Vector3 pos = t.localPosition;
                    pos.x += ext2;
                    distance = pos.x - center.x;
                    int realIndex = BreakScroll.GetRealIndex(pos, mScroll.transform.GetSiblingIndex());

                    if (minIndex == maxIndex || BreakScroll.CheckIndexRange(mScroll.transform.GetSiblingIndex(),realIndex))
                    {
                        t.localPosition = pos;
                        UpdateItem(t, i);
                    }
                    else allWithinRange = false;
                }
                else if (distance > extents)
                {
                    Vector3 pos = t.localPosition;
                    pos.x -= ext2;
                    distance = pos.x - center.x;
                    int realIndex = BreakScroll.GetRealIndex(pos, mScroll.transform.GetSiblingIndex());

                    if (minIndex == maxIndex || BreakScroll.CheckIndexRange(mScroll.transform.GetSiblingIndex(), realIndex))
                    {
                        t.localPosition = pos;
                        UpdateItem(t, i);
                    }
                    else allWithinRange = false;
                }
                else if (mFirstTime) UpdateItem(t, i);

                if (cullContent)
                {
                 //   distance += mPanel.clipOffset.x - transform.localPosition.x;
                    if (!UICamera.IsPressed(t.gameObject))
                    {
                        Vector3 pos = t.localPosition;
                        bool inRange =BreakScroll.CheckIndexRange(mScroll.transform.GetSiblingIndex(),
                    BreakScroll.GetRealIndex(t.localPosition, mScroll.transform.GetSiblingIndex()));
                        NGUITools.SetActive(t.gameObject, ( inRange&& pos.x> min && pos.x < max), false);
                    } 
                }
            }
        }
        else
        {
            float min = corners[0].y - itemSize;
            float max = corners[2].y + itemSize;

            for (int i = 0, imax = mChilds.Count; i < imax; ++i)
            {
                Transform t = mChilds[i];
                float distance = t.localPosition.y - center.y;

                if (distance < -extents)
                {
                    Vector3 pos = t.localPosition;
                    pos.y += ext2;
                    distance = pos.y - center.y;
                    int realIndex = BreakScroll.GetRealIndex(pos, mScroll.transform.GetSiblingIndex());

                    if (minIndex == maxIndex || BreakScroll.CheckIndexRange(mScroll.transform.GetSiblingIndex(), realIndex))
                    {
                        t.localPosition = pos;
                        UpdateItem(t, i);
                    }
                    else allWithinRange = false;
                }
                else if (distance > extents)
                {
                    Vector3 pos = t.localPosition;
                    pos.y -= ext2;
                    distance = pos.y - center.y;
                    int realIndex = BreakScroll.GetRealIndex(pos, mScroll.transform.GetSiblingIndex());

                    if (minIndex == maxIndex || BreakScroll.CheckIndexRange(mScroll.transform.GetSiblingIndex(), realIndex))
                    {
                        t.localPosition = pos;
                        UpdateItem(t, i);
                    }
                    else allWithinRange = false;
                }
                else if (mFirstTime) UpdateItem(t, i);

                if (cullContent)
                {
                //    distance += mPanel.clipOffset.y - transform.localPosition.y;
                    if (!UICamera.IsPressed(t.gameObject))
                    {
                        Vector3 pos = t.localPosition;
                        bool inRange = BreakScroll.CheckIndexRange(mScroll.transform.GetSiblingIndex(),
                    BreakScroll.GetRealIndex(pos, mScroll.transform.GetSiblingIndex()));
                        NGUITools.SetActive(t.gameObject, (inRange&& pos.y > min && pos.y < max), false);
                    }
                        
                }
            }
        }
        mScroll.restrictWithinPanel = !allWithinRange;
    }

    void UpdateItem(Transform trans,int index)
    {
        if (mScroll == null)
            CacheScrollView();
        BreakScroll.UpdateItem(trans.gameObject, index, mScroll.transform.GetSiblingIndex());
    }

}
