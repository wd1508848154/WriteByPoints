using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointTool : MonoBehaviour
{
    public static PointTool inst;
    private void Awake()
    {
        inst = this;
    }

    public GameObject m_LinePfb ,m_LinePfb2;
    
    
    /// <summary>
    /// 创建一条两点之间的线
    /// </summary>
    /// <param name="start">起始点</param>
    /// <param name="end">结束点</param>
    public void CreateLine(Vector2 start, Vector2 end,bool flag = true)
    {
        Debug.Log(start +"  88 "+end);
        GameObject pre = flag ? m_LinePfb : m_LinePfb2;
        //实例化需要显示的线段图片pfb
        GameObject line = Instantiate(pre) ;
        line.transform.SetParent(pre.transform.parent);
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        //设置位置和角度
        rect.localPosition = GetBetweenPoint(start, end);
        rect.localRotation = Quaternion.AngleAxis(-GetAngle(start,end), Vector3.forward); 
        //设置线段图片大小
        var distance = Vector2.Distance(end, start);
        Debug.Log(distance);
        rect.sizeDelta = new Vector2(20, Math.Max(1, distance));
        //调整显示层级
        line.transform.SetAsFirstSibling();
        pre.gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 获取两个坐标点之间的夹角
    /// </summary>
    /// <param name="start">起始点</param>
    /// <param name="end">结束点</param>
    /// <returns></returns>
    public float GetAngle(Vector2 start, Vector2 end)
    {
        var dir = end - start;
        var dirV2 = new Vector2(dir.x, dir.y);
        var angle = Vector2.SignedAngle(dirV2, Vector2.down);
        return angle;
    }

    /// <summary>
    /// 获取上下相邻两个坐标点中间的坐标点
    /// </summary>
    /// <param name="start">起始点</param>
    /// <param name="end">结束点</param>
    /// <returns></returns>
    private Vector2 GetBetweenPoint(Vector2 start, Vector2 end)
    {
        //两点之间垂直距离
        float distance = end.y - start.y;
        float y = start.y + distance / 2;
        float x = start.x;

        if (start.x != end.x)
        {
            //斜率值
            float k = (end.y - start.y) / (end.x - start.x);
            //根据公式 y = kx + b ， 求b
            float b = start.y - k * start.x;
            x = (y - b) / k;
        }
        
        Vector2 point = new Vector2(x, y);
        return point;
    }


}
