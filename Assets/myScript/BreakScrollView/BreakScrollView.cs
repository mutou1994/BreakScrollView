 using UnityEngine;
using System.Collections.Generic;
using System;


public class BreakScrollView : MonoBehaviour {
    //跟UIWrapContent一样的用来设置Item的一个回调
    public delegate void OnInitializeItem(GameObject obj, int wrapIndex, int scrollIndex, int realIndex);

    public int itemSize = 100;

    public int minIndex=0;

    public int maxIndex=0;

    public bool cullContent=true;//是否隐藏超出边界的Item的标记

    public bool mHorizontal = false;//滑动方向

    public OnInitializeItem onInitializeItem;//设置Item的回调

    protected List<UIScrollView> mScrolls;//众多的ScrollView
    protected List<UIPanel> mPanels;//众多的Panel
    protected List<BreakWrapContent> mWraps;//众多的WrapContent

    //到当前ScrollView为止所需要的偏移补偿 也就是ScrollView的坐标需要偏移多少才能对接上一个ScrollView
    protected List<float> mOffsets;

    //到当前ScrollView为止，每个ScrollView能完整放下的Item的个数之和
    protected List<int> mCapacity;
    //到当前ScrollView为止，ScrollView Size的总长度
    protected List<float> mScrollLengths;
    

    protected virtual void Awake()
    {
        OnValidate();//用来限制minIndex和maxIndex，完全是从WrapContent抄来的
        CacheAndInitScrollView();//缓存各ScrollView
        ResetScrollPosition();//各ScrollView的复位 初始化它们的偏移量，设置好初始坐标
        mPanels[0].onClipMove += OnHeadMove;//由第一个ScrollView的滑动 带动所有ScrollView的滑动，才能动的一致
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
                
                //累计到当前SV为止能完整放下的Item个数之和 包括当前的
                num += (int)(panel.GetViewSize().x / (float)itemSize);
                length = panel.GetViewSize().x;//当前SV的size
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
            mScrollLengths.Add((i > 0 ? mScrollLengths[i - 1] : 0) + length);//累计到当前SV的长度之和 包括当前
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
        {   //这步是关键 只要加回到上一个SV为止能够完整放下的Item数就可以了。因为不完整的那个是和下一个SV共享的
            //所以其实应该算成是下一个SV能完整放下的个数里 而下一个SV不能完整放下的那个又应该算到下下个SV，
            //所以只需要加回能完整放下的个数就行了。得到的就是realIndex
            //而localPos/itemSize得到的则是 假设当前SV是独立的情况下得到的realIndex，然而这个SV并不是独立的，
            //它的第一个realIndex应该是上一个SV末尾不完整的那个Item的realIndex
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

    /// <summary>
    /// 检查realIndex是否超出了所在SV的min和max 
    /// 比如第0个Item应该是在第一个SV，最后一个Item在最后一个SV 中间的也都一样 有min和max
    /// </summary>
    /// <param name="scrollIndex"></param>
    /// <param name="realIndex"></param>
    /// <returns></returns>
    public bool CheckIndexRange(int scrollIndex,int realIndex)
    {
        //maxIndex应该是
        int curMaxIndex = maxIndex - Mathf.FloorToInt((mScrollLengths[mScrollLengths.Count - 1] - mScrollLengths[scrollIndex])/itemSize);
        //minIndex很显然的就是当前SV的起始Item的realIndex了。
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
