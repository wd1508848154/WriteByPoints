using System;
using System.Collections.Generic;
using DG.Tweening;
using Framework.CharacterWriter;
using NUnit.Framework;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Range = UnityEngine.RangeAttribute;

/// <summary>
/// 计算关键点
/// </summary>
public class CalPoint : MonoBehaviour
{
    /// <summary>
    /// 每个笔画的数据
    /// </summary>
    class StrokeData
    {
        /// <summary>
        /// 是否正确写完
        /// </summary>
        public bool isRight;

        /// <summary>
        /// 描点的数据列表
        /// </summary>
        public List<KeyPointData> pointList = new List<KeyPointData>();

        public StrokeData()
        {
        }

        public StrokeData(List<KeyPointData> plist)
        {
            pointList.AddRange(plist);
        }
    }

    /// <summary>
    /// 关键点数据
    /// </summary>
    class KeyPointData
    {
        /// <summary>
        /// 位置
        /// </summary>
        public Vector2 pos;

        /// <summary>
        /// 是否已经被覆盖
        /// </summary>
        public bool isCovered;
    }

    /// <summary>
    /// 描点的UI预设
    /// </summary>
    public Image slipPointPfb;

    /// <summary>
    /// 所有的描点数据，与笔画数量一致
    /// </summary>
    private List<List<Transform>> slipPointsList = new List<List<Transform>>();

    /// <summary>
    /// 描点到关键点的有效距离
    /// </summary>
    [SerializeField] [UnityEngine.Range(10, 50)] [Tooltip("描点到关键点的有效范围")]
    private int slipPointToKeyPointLegalDis = 40;

    /// <summary>
    /// 当前笔画的最后一个关键点较之常规关键点的半径倍数
    /// </summary>
    [SerializeField] [UnityEngine.Range(1.5f, 4f)]
    private int theLastKeyPointOfStrokeDisTimes = 2;

    /// <summary>
    /// 是否忽略掉超出最后一个关键点范围的其他描点
    /// </summary>
    [SerializeField] [Tooltip("是否忽略掉超出最后一个关键点范围的其他描点 true:即使超出，也算写对（擦掉超出的描点）；false:超出就算错！")]
    private bool ifIgnoreSlipPointsOutOfRange = false;

    /// <summary>
    /// 忽略掉超出最后一个关键点范围的其他描点占有效描点百分比
    /// 如，有效描点数量是100个，允许超出是20，就是说，超出的数量在20个以内都可以。
    /// </summary>
    [SerializeField] [Range(0, 100)] private int ignoreOutOfRangePercent;

    #region 描点之间的角度差值

    /// <summary>
    /// 描点之间角度差值范围
    /// </summary>
    [SerializeField] [Range(10, 80)] [Tooltip("下一个描点和该关键点下的首个描点的角度差值")]
    private float angleOffsetBetweenSlips = 45;

    /// <summary>
    /// 当前笔画书写中的两个关键点之间的角度
    /// </summary>
    private float curKeyPointAngle;

    /// <summary>
    /// 计算后的角度差值范围
    /// </summary>
    private Vector2 recalculatedAngleRange = Vector2.zero;

    #endregion

    #region 笔画数据

    #endregion

    /// <summary>
    /// 笔画数据，文本所有的关键点
    /// </summary>
    private List<StrokeData> strokesList = new List<StrokeData>();

    /// <summary>
    /// 关键点预设
    /// </summary>
    public RectTransform keyPointPfb;

    /// <summary>
    /// 关键点预设的根节点
    /// </summary>
    public RectTransform keyPointsRoot;

    /// <summary>
    /// 当前笔画内，关键点目标点的下标，描点走向的那个目标点，即从 1 开始
    /// </summary>
    private int curTargetedKeyPointIndex;

    /// <summary>
    /// 当前的笔画下标
    /// </summary>
    private int curStrokeIndex;

    /// <summary>
    /// 描点列表，基于笔画
    /// </summary>
    private List<List<Vector3>> allSlipPointsPath = new List<List<Vector3>>();

    /// <summary>
    /// 计算角度的参照描点
    /// </summary>
    private Vector2 curSlipPointPosForCalAngle;

    /// <summary>
    /// 手动设置的笔画们的根节点
    /// </summary>
    public RectTransform handStrokesRoot;

    /// <summary>
    /// 创建描点预的最短距离
    /// </summary>
    private float createSlopPointPfbDis = 10;


    /// <summary>
    /// 整个汉字写完了
    /// </summary>
    private bool ifTheWordWritedDone;


    /// <summary>
    /// 文本及其组件的根节点
    /// </summary>
    public Transform wordPanelRoot;

    #region 一些标示

    /// <summary>
    /// 是否显示关键点
    /// </summary>
    private bool IfShowKeyPoints = false;

    #endregion

    /// <summary>
    /// 重制状态
    /// </summary>
    void ResetState()
    {
        ifCurMoveCheckedOver = false;
        ifCurMoveedPathOver = false;
    }

    /// <summary>
    /// 清除当前笔画的描点数据
    /// </summary>
    void ClearCurStrokeSlipPoints()
    {
        if (slipPointsList.Count <= curStrokeIndex)
        {
            return;
        }

        foreach (var sp in slipPointsList[curStrokeIndex])
        {
            if (sp != null)
            {
                sp.gameObject.SetActive(false);
            }
        }

        allSlipPointsPath[curStrokeIndex].Clear();
    }


    void Start()
    {
        InitWordData();
    }

    /// <summary>
    /// 初始化文字数据
    /// </summary>
    void InitWordData()
    {
        strokesList.Clear();
        ///读取本地预设的笔画

        for (int strokeindex = 0; strokeindex < handStrokesRoot.childCount; strokeindex++)
        {
            Transform onestroke = handStrokesRoot.transform.GetChild(strokeindex);
            StrokeData strokeData = new StrokeData();
            ///笔画数
            int keypointtotal = onestroke.childCount;
            for (var keypointindex = 0; keypointindex < keypointtotal; keypointindex++)
            {
                var allpoint = new List<KeyPointData>();


                KeyPointData ps = new KeyPointData();
                ps.pos = onestroke.GetChild(keypointindex).localPosition;
                allpoint.Add(ps);
                RectTransform st = Instantiate<RectTransform>(keyPointPfb, keyPointPfb.parent);
                st.localPosition = ps.pos;
                st.localScale = Vector3.one;
                st.sizeDelta = new Vector2(slipPointToKeyPointLegalDis * (keypointindex == keypointtotal - 1 ? 2 * theLastKeyPointOfStrokeDisTimes : 2),
                    slipPointToKeyPointLegalDis * (keypointindex == keypointtotal - 1 ? 2 * theLastKeyPointOfStrokeDisTimes : 2));
                st.gameObject.SetActive(true);

                strokeData.pointList.AddRange(allpoint);
            }


            strokesList.Add(strokeData);
        }

        //TODO 文本数据读取配置  暂没处理

        DoStrokeTip(true);
        keyPointsRoot.gameObject.SetActive(IfShowKeyPoints);
    }


    void UpdateKeyPointRange()
    {
        foreach (RectTransform kp in keyPointPfb.parent)
        {
            // kp.sizeDelta = new Vector2(keyPointRadius * 2, keyPointRadius * 2);
        }
    }

    private Tween tipTween;
    public Transform tipTweenItem;

    /// <summary>
    /// 笔画提示
    /// </summary>
    /// <param name="ifTip"></param>
    private void DoStrokeTip(bool ifTip)
    {
        if (tipTween != null)
        {
            tipTween.Kill();
        }

        tipTweenItem.gameObject.SetActive(ifTip);
        if (ifTip)
        {
            List<Vector3> list = new List<Vector3>();
            foreach (var sd in strokesList[curStrokeIndex].pointList)
            {
                list.Add(sd.pos);
            }

            tipTweenItem.localPosition = list[0];
            tipTween = tipTweenItem.DOLocalPath(list.ToArray(), 100, PathType.CatmullRom).SetSpeedBased().SetLoops(-1);
        }
    }

    List<Vector3> GetCurKeyPoints()
    {
        List<Vector3> ps = new List<Vector3>();


        return ps;
    }

    void UpdateRange()
    {
        strokesList[curStrokeIndex].pointList[curTargetedKeyPointIndex - 1].isCovered = true;
        curKeyPointAngle = angle_360(strokesList[curStrokeIndex].pointList[curTargetedKeyPointIndex].pos,
            strokesList[curStrokeIndex].pointList[curTargetedKeyPointIndex - 1].pos);
        recalculatedAngleRange.x = curKeyPointAngle - angleOffsetBetweenSlips;
        recalculatedAngleRange.y = curKeyPointAngle + angleOffsetBetweenSlips;


        Debug.Log("UpdateRange  ** " + strokesList[curStrokeIndex].pointList[curTargetedKeyPointIndex].pos +
                  strokesList[curStrokeIndex].pointList[curTargetedKeyPointIndex - 1].pos);
        Debug.Log(" UpdateRange  " + curKeyPointAngle + "  " + recalculatedAngleRange);
    }

    /// <summary>
    /// 两点的角度合理
    /// </summary>
    /// <param name="p2"></param>
    /// <param name="p1"></param>
    /// <returns></returns>
    private bool IfInLegleRange(Vector3 p2, Vector3 p1)
    {
        float curAngle = angle_360(p2, p1);
        bool isok = curAngle >= recalculatedAngleRange.x && curAngle <= recalculatedAngleRange.y;
        Debug.Log("IfInLegleRange   " + curAngle + "   " + recalculatedAngleRange + "   " + isok);
        return isok;
    }

    private bool ifCanDraw;

    /// <summary>
    /// 本个笔画是否检测完成
    /// </summary>
    private bool ifCurMoveCheckedOver;

    /// <summary>
    /// 是否描点结束
    /// </summary>
    private bool ifCurMoveedPathOver;

    /// <summary>
    /// 本次检测是否正确
    /// </summary>
    private bool ifCurMoveRight;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DoStartStroke(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            DoStroking(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            DrawEnd();
        }

        if (Input.GetMouseButtonDown(1))
        {
            IfShowKeyPoints = !IfShowKeyPoints;
            keyPointsRoot.gameObject.SetActive(IfShowKeyPoints);
            if (IfShowKeyPoints)
            {
                UpdateKeyPointRange();
            }
        }
    }

    void DrawEnd()
    {
        Debug.Log(ifCanDraw);
        if (ifTheWordWritedDone || !ifCanDraw)
        {
            return;
        }

        ifCanDraw = false;
        ifCurMoveedPathOver = true;
        //自动检测结果
        CheckResult();
    }

    void CheckResult()
    {
        curTargetedKeyPointIndex = 1;
        curSlipPointPosForCalAngle = allSlipPointsPath[curStrokeIndex][0];
        UpdateRange();
        CalResult();
    }

    void CallResult(bool isok, string info = "")
    {
        Debug.LogError("            CallResult  isok:" + isok + "   info  " + info);

        ifCurMoveRight = isok;
        ifCurMoveCheckedOver = true;
        if (ifCurMoveRight)
        {
            strokesList[curStrokeIndex].isRight = true;
            if (curStrokeIndex == strokesList.Count - 1)
            {
                Debug.LogError("         一个完整的字写完了  ");
                ifTheWordWritedDone = true;
            }
            else
            {
                ResetState();
                UpdateCurHandMoveIndex();
                DoStrokeTip(true);
                Debug.LogError($"      请  开始 第 {curStrokeIndex} 笔书写 ，一共是 {strokesList.Count} 笔");
            }
        }
        else
        {
            ClearCurStrokeSlipPoints();
            ResetState();
            DoStrokeTip(true);
        }
    }

    /// <summary>
    /// 是否是最后一个关键目标点
    /// </summary>
    /// <returns></returns>
    bool CheckIfCurKeyPointTargetIndexCanAdd()
    {
        return curTargetedKeyPointIndex < strokesList[curStrokeIndex].pointList.Count - 1;
    }

    /// <summary>
    /// 是否是当前笔画的最后一个关键点
    /// </summary>
    /// <returns></returns>
    bool IfCurKeyPointTargetIndexIsTheLast()
    {
        return curTargetedKeyPointIndex == strokesList[curStrokeIndex].pointList.Count - 1;
    }

    /// <summary>
    /// 计算结果
    /// </summary>
    void CalResult()
    {
        if (allSlipPointsPath[curStrokeIndex].Count <= 1)
        {
            Debug.Log("only one point");
            CallResult(false, "只有一个描点");
            return;
        }

        for (int i = 1; i < allSlipPointsPath[curStrokeIndex].Count; i++)
        {
            //如果，这个点一个到第一个点的角度有效
            if (IfInLegleRange(allSlipPointsPath[curStrokeIndex][i], curSlipPointPosForCalAngle))
            {
                //这个点在关键目标点范围内
                if (IfLegalDisToKeyPoint(strokesList[curStrokeIndex].pointList[curTargetedKeyPointIndex].pos, allSlipPointsPath[curStrokeIndex][i]))
                {
                    //下个点不在关键目标点范围内，检测下一个关键点
                    if (i < allSlipPointsPath[curStrokeIndex].Count - 1)
                    {
                        // 下一个点不在下个关键点的有效范围
                        if (!IfLegalDisToKeyPoint(strokesList[curStrokeIndex].pointList[curTargetedKeyPointIndex].pos, allSlipPointsPath[curStrokeIndex][i + 1]))
                        {
                            curSlipPointPosForCalAngle = allSlipPointsPath[curStrokeIndex][i];
                            Debug.LogError(curSlipPointPosForCalAngle + "  curMoveStartPos   " + i);
                            //如果下标还没到最后,更新角度检测
                            if (CheckIfCurKeyPointTargetIndexCanAdd())
                            {
                                //需要更新关键目标点
                                curTargetedKeyPointIndex++;

                                UpdateRange();
                            }
                            //已经是最后的关键点，下一个点超出范围，书写错误
                            else
                            {
                                int nextindex = i + 1;
                                ///如果其他的描点，超出最后一个关键点范围

                                //方式一  认为对，把多余的描点擦出掉
                                if (ifIgnoreSlipPointsOutOfRange)
                                {
                                    if (ClearSlipPointsOutOfTheLastKeyPointLeagle(nextindex))
                                    {
                                        CallResult(true, $"擦掉超出最后关键点有效范围的其他描点  {allSlipPointsPath[curStrokeIndex][nextindex]} 下标： {nextindex} ");
                                    }
                                    else
                                    {
                                        CallResult(false, $"擦掉超出最后关键点有效范围的其他描点  {allSlipPointsPath[curStrokeIndex][nextindex]} 下标： {nextindex} ");
                                    }
                                }
                                //方式二  认为错误
                                else
                                {
                                    CallResult(false, $"超出最后关键点有效范围。描点 {allSlipPointsPath[curStrokeIndex][nextindex]} 下标： {nextindex} ");
                                }

                                return;
                            }
                        }
                    }
                    else
                    {
                        if (IfCurKeyPointTargetIndexIsTheLast())
                        {
                            CallResult(true, $"最后一个点 i= {i} 描点总数： {allSlipPointsPath[curStrokeIndex].Count}，刚好在目标关键点有效区域内！");
                            return;
                        }
                        else
                        {
                            CallResult(false,
                                $"描点没有覆盖到这个关键点 curKeyPointTargetIndex = {curTargetedKeyPointIndex}  关键点总数={strokesList[curStrokeIndex].pointList.Count}" +
                                $"i = {i}  path.Count = {allSlipPointsPath[curStrokeIndex].Count} ");
                            return;
                        }
                    }
                }
            }
            else
            {
                CallResult(false, $"角度无效 {allSlipPointsPath[curStrokeIndex][i]} 下标： {i} curHandMoveIndex {curStrokeIndex}");
                return;
            }
        }

        if (!ifCurMoveCheckedOver)
        {
            if (isAllKeyPointsCovered())
            {
                CallResult(true, $"覆盖完成");
            }
            else
            {
                CallResult(false, $"描点没有覆盖到这个关键点 curKeyPointTargetIndex = {curTargetedKeyPointIndex}  关键点总数={strokesList[curStrokeIndex].pointList.Count}");
            }
        }
    }

    /// <summary>
    /// 如果超出的描点数量在容忍的范围内，就认为是正确的，并擦除多余的描点
    /// </summary>
    /// <param name="startIndex"></param>
    /// <returns></returns>
    private bool ClearSlipPointsOutOfTheLastKeyPointLeagle(int startIndex)
    {
        bool isok = false;
        int cc = allSlipPointsPath[curStrokeIndex].Count;
        //计算超出百分比
        int total = slipPointsList[curStrokeIndex].FindAll(s => s.gameObject.activeInHierarchy).Count;
        float innerRatio = (total - startIndex) / (float) startIndex;
        if ((innerRatio * 100) <= ignoreOutOfRangePercent)
        {
            //处理擦除
            List<Transform> outList = slipPointsList[curStrokeIndex].GetRange(startIndex, slipPointsList[curStrokeIndex].Count - startIndex);
            foreach (var p in outList)
            {
                p.gameObject.SetActive(false);
            }

            isok = true;
        }


        return isok;
    }

    /// <summary>
    /// 是否所有的关键点都被覆盖
    /// </summary>
    /// <returns></returns>
    bool isAllKeyPointsCovered()
    {
        return strokesList[curStrokeIndex].pointList.Find(s => s.isCovered == false) == null;
    }

    /// <summary>
    /// 当前笔画内是否最后一个目标关键点
    /// </summary>
    /// <returns></returns>
    bool IfCurMoveTheLastKeyPoint()
    {
        return curTargetedKeyPointIndex == strokesList[curStrokeIndex].pointList.Count - 1;
    }

    /// <summary>
    /// 描点是否在关键点的有效范围
    /// </summary>
    /// <param name="keyPoint">关键点</param>
    /// <param name="slipPoint">描点</param>
    /// <returns></returns>
    bool IfLegalDisToKeyPoint(Vector2 keyPoint, Vector2 slipPoint)
    {
        float dis = Vector2.Distance(slipPoint, keyPoint);

        float rad = IfCurMoveTheLastKeyPoint() ? slipPointToKeyPointLegalDis * theLastKeyPointOfStrokeDisTimes : slipPointToKeyPointLegalDis;
        bool isok = dis <= rad;

        Debug.Log(" IfLegalDisToKeyPoint       dis=" + dis + "  " + keyPoint + "   " + slipPoint + "   " + isok + "   " + rad);

        return isok;
    }

    /// <summary>
    /// 更新当前笔画的下标
    /// </summary>
    void UpdateCurHandMoveIndex()
    {
        for (int i = 0; i < strokesList.Count; i++)
        {
            if (!strokesList[i].isRight)
            {
                curStrokeIndex = i;
                break;
            }
        }
    }

    /// <summary>
    /// 开始书写
    /// </summary>
    /// <param name="pos"></param>
    public void DoStartStroke(Vector2 pos)
    {
        if (ifTheWordWritedDone)
        {
            return;
        }


        ifCanDraw = false;
        ///第一个描点，必须在第一个关键点有效范围内，才能开始书写
        bool isok = IfLegalDisToKeyPoint(strokesList[curStrokeIndex].pointList[0].pos, MouseToUI(pos));
        if (isok == false)
        {
            Debug.Log(pos + " is too far from point 1 " + strokesList[curStrokeIndex].pointList[0].pos);
            return;
        }


        ifCanDraw = true;

        DoStrokeTip(false);
        //Draw.Inst.StartWrite(Input.mousePosition);
    }


    /// <summary>
    /// 鼠标位置转换ui位置
    /// </summary>
    /// <param name="mouse"></param>
    /// <returns></returns>
    Vector2 MouseToUI(Vector2 mouse)
    {
        var t = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(wordPanelRoot as RectTransform, Input.mousePosition, Camera.main, out t);

        return t;
    }

    /// <summary>
    /// 书写中
    /// </summary>
    /// <param name="pos"></param>
    public void DoStroking(Vector3 pos)
    {
        if (!ifCanDraw || ifCurMoveCheckedOver || ifCurMoveedPathOver || ifTheWordWritedDone)
        {
            return;
        }

        AddSlipPoint(MouseToUI(pos));
    }

    /// <summary>
    /// 增加描点
    /// </summary>
    /// <param name="p"></param>
    void AddSlipPoint(Vector3 p)
    {
        if (allSlipPointsPath.Count <= curStrokeIndex)
        {
            allSlipPointsPath.Add(new List<Vector3>());
        }

        if (!allSlipPointsPath[curStrokeIndex].Contains(p))
        {
            if (allSlipPointsPath[curStrokeIndex].Count > 0)
            {
                float dis = Vector2.Distance(p, allSlipPointsPath[curStrokeIndex][allSlipPointsPath[curStrokeIndex].Count - 1]);

                if (dis > createSlopPointPfbDis)
                {
                    DoAddSlipPoint(p);
                }
            }
            else
            {
                DoAddSlipPoint(p);
            }
        }
    }

    /// <summary>
    /// 添加描点到数据
    /// </summary>
    /// <param name="pos"></param>
    void DoAddSlipPoint(Vector2 pos)
    {
        if (slipPointsList.Count <= curStrokeIndex)
        {
            slipPointsList.Add(new List<Transform>());
        }

        Transform ste = slipPointsList[curStrokeIndex].Find(s => s.gameObject.activeInHierarchy == false);
        if (ste == null)
        {
            Image st = Instantiate<Image>(slipPointPfb, slipPointPfb.transform.parent);
            st.rectTransform.localPosition = pos;
            st.gameObject.SetActive(true);
            ste = st.transform;
            slipPointsList[curStrokeIndex].Add(ste.transform);
        }
        else
        {
            ste.GetComponent<RectTransform>().localPosition = pos;
            ste.gameObject.SetActive(true);
        }


        allSlipPointsPath[curStrokeIndex].Add(ste.localPosition);
        //处理两个点之间连线
        int c = allSlipPointsPath[curStrokeIndex].Count;

        if (c > 1)
        {
            Vector2 vs = new Vector2(allSlipPointsPath[curStrokeIndex][c - 2].x, allSlipPointsPath[curStrokeIndex][c - 2].y);

            Vector2 ve = new Vector2(allSlipPointsPath[curStrokeIndex][c - 1].x, allSlipPointsPath[curStrokeIndex][c - 1].y);

            // pointTool.CreateLine(vs,ve);
        }
    }


    /// <summary>
    /// 计算角度
    /// </summary>
    /// <param name="from_">目标点</param>
    /// <param name="to_">起始点</param>
    /// <returns></returns>
    float angle_360(Vector3 from_, Vector3 to_)
    {
        //两点的x、y值
        float x = from_.x - to_.x;
        float y = from_.y - to_.y;

        //斜边长度
        float hypotenuse = Mathf.Sqrt(Mathf.Pow(x, 2f) + Mathf.Pow(y, 2f));

        //求出弧度
        float cos = x / hypotenuse;
        float radian = Mathf.Acos(cos);

        //用弧度算出角度    
        float angle = 180 / (Mathf.PI / radian);

        if (y < 0)
        {
            angle = -angle;
        }
        else if ((y == 0) && (x < 0))
        {
            angle = 180;
        }

        // Debug.Log(angle + "    " + from_ + "   " + to_);
        return angle;
    }
}