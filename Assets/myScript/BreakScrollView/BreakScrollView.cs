 using UnityEngine;
using System.Collections.Generic;
using System;


public class BreakScrollView : MonoBehaviour {

    public delegate void OnInitializeItem(GameObject obj, int wrapIndex, int scrollIndex, int realIndex);

    public int itemSize = 100;

    public int minIndex=0;

    public int maxIndex=0;

    public bool cullContent=true;

    public bool mHorizontal = false;

    public OnInitializeItem onInitializeItem;

    protected List<UIScrollView> mScrolls;
    protected List<UIPanel> mPanels;
    protected List<BreakWrapContent> mWraps;
    protected List<float> mOffsets;
    //到当前ScrollView为止，每个ScrollView能完整放下的Item的个数之和
    protected List<int> mCapacity;
    //到当前ScrollView为止，ScrollView Size的总长度
    protected List<float> mScrollLengths;
    

    protected virtual void Awake()
    {
        OnValidate();
        CacheAndInitScrollView();
        ResetScrollPosition();
        mPanels[0].onClipMove += OnHeadMove;
    }

    void CacheAndInitScrollView()
    {
        if (mScrolls == null|| mCapacity==null||mPanels==null||mWraps==null|| mScrollLengths==null)
        {
            mScrolls = new List<UIScrollView>();
            mPanels = new List<UIPanel>();
            mWraps = new List<BreakWrapContent>();
            mCapacity = new List<int>();
            mScrollLengths = new List<float>();
        }
            
        mScrolls.Clear();
        mPanels.Clear();
        mWraps.Clear();
        mCapacity.Clear();
        mScrollLengths.Clear();
        
        for(int i=0,Imax=transform.childCount;i<Imax;i++)
        {
            var child = transform.GetChild(i);
           
            var scroll = child.GetComponent<UIScrollView>();
            var panel = child.GetComponent<UIPanel>();
            
            var wrap = scroll.GetComponentInChildren<BreakWrapContent>();
            if (wrap == null)
                wrap = scroll.transform.GetChild(0).gameObject.AddComponent<BreakWrapContent>();

            int num = i > 0 ? mCapacity[i - 1] : 0;
            float length = 0;
            if(mHorizontal)
            {
                scroll.movement = UIScrollView.Movement.Horizontal ;
                
                num += (int)(panel.GetViewSize().x / (float)itemSize);
                length = panel.GetViewSize().x;
            }
            else
            {
                scroll.movement = UIScrollView.Movement.Vertical;
               
                num += (int)(panel.GetViewSize().y / (float)itemSize);
                length = panel.GetViewSize().y;
            }
            mScrolls.Add(scroll);
            mPanels.Add(panel);
            mWraps.Add(wrap);
            mCapacity.Add(num);
            mScrollLengths.Add((i > 0 ? mScrollLengths[i - 1] : 0) + length);
        }
    }

    [ContextMenu("ResetScrollPosition")]
    public virtual void ResetScrollPosition()
    {
        if (mScrolls == null || mCapacity == null || mPanels == null || mWraps == null|| mScrollLengths==null)
            CacheAndInitScrollView();
        if (mOffsets == null)
            mOffsets = new List<float>();
        mOffsets.Clear();

        float offset = 0;
        for(int i=0,Imax=mScrolls.Count;i<Imax;i++)
        {
            Vector2 size = i>0?(mPanels[i - 1].GetViewSize()):Vector2.zero;
            Vector2 pos = mScrolls[i].transform.localPosition;
            if(mHorizontal)
            {
                offset += size.x - ((int)(size.x / itemSize)) * itemSize;
                pos.x = -offset;
                if (!Application.isPlaying)
                {
                    mScrolls[i].transform.localPosition = pos;
                    mPanels[i].clipOffset = new Vector2(-pos.x, pos.y);
                }
            }
            else
            {
                offset += size.y - ((int)(size.y / itemSize)) * itemSize;
                pos.y = offset;
                if (!Application.isPlaying)
                {
                    mScrolls[i].transform.localPosition = pos;
                    mPanels[i].clipOffset = new Vector2(pos.x, -pos.y);
                }
            }
            mOffsets.Add(offset);
            SpringPanel.Begin(mScrolls[i].gameObject, pos, 10);
        }
    }

    [ContextMenu("ResetWrapChildPosition")]
    public virtual void ResetWrapChildPosition()
    {
        if (mScrolls == null || mCapacity == null || mPanels == null || mWraps == null|| mScrollLengths==null)
            CacheAndInitScrollView();

        for(int i=0,Imax=mWraps.Count;i<Imax;i++)
        {
            mWraps[i].ResetChildPostion();
        }
    }

    public void SpringTo(float distance)
    {
        for(int i=0,Imax=mScrolls.Count;i<Imax;i++)
        {
            Vector3 pos = mScrolls[i].transform.localPosition;
            if(mHorizontal)
            {
                pos.x += distance;
            }else
            {
                pos.y += distance;
            }
            SpringPanel.Begin(mScrolls[i].gameObject, pos, 10);
        }
    }

    public int GetRealIndex(Vector3 localposition,int scrollIndex)
    {
        if (mScrolls == null || mCapacity == null || mPanels == null || mWraps == null|| mScrollLengths==null)
            CacheAndInitScrollView();

        int capacity =scrollIndex>0? mCapacity[scrollIndex - 1]:0;
        if (mHorizontal)
        {
            return Mathf.RoundToInt(localposition.x / itemSize) + capacity;
        }else
        {
            return Mathf.RoundToInt(-localposition.y / itemSize) + capacity;
        }
    }

    void OnValidate()
    {
        if (maxIndex < minIndex)
            maxIndex = minIndex;
        if (minIndex > maxIndex)
            maxIndex = minIndex;
    }


    public bool CheckIndexRange(int scrollIndex,int realIndex)
    {
        int curMaxIndex = maxIndex - Mathf.FloorToInt((mScrollLengths[mScrollLengths.Count - 1] - mScrollLengths[scrollIndex])/itemSize);
        int curMinIndex = minIndex + (scrollIndex>0?mCapacity[scrollIndex-1]:0);
        return realIndex >= curMinIndex && realIndex < curMaxIndex;
    }


    public void UpdateItem(GameObject obj,int index,int scrollIndex)
    {
        int realIndex = GetRealIndex(obj.transform.localPosition, scrollIndex);
       
        if (minIndex!=maxIndex&&!CheckIndexRange(scrollIndex,realIndex))
        {
            NGUITools.SetActive(obj, false);
            return;
        }else 
        {
            obj.GetComponent<UILabel>().text = realIndex.ToString();
            if (onInitializeItem!=null)
            onInitializeItem(obj, index, scrollIndex, realIndex);
        }
    }

    void OnHeadMove(UIPanel panel)
    {
        Vector3 pos0 = panel.transform.localPosition;
        for (int i = 1, Imax = mScrolls.Count; i < Imax; i++)
        {
            Vector3 pos = mScrolls[i].transform.localPosition;
            Vector2 clipOffset = mPanels[i].clipOffset;
            if (mHorizontal)
            {
                pos.x = pos0.x - mOffsets[i];
                mScrolls[i].transform.localPosition = pos;
                clipOffset.x = -pos.x;
                mPanels[i].clipOffset = clipOffset;
            }
            else
            {
                pos.y = pos0.y + mOffsets[i];
                mScrolls[i].transform.localPosition = pos;
                clipOffset.y = -pos.y;
                mPanels[i].clipOffset = clipOffset;
            } 
        }
    }

}
