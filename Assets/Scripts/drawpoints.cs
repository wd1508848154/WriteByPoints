using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class drawpoints : MonoBehaviour
{
   // string info ="515 667  609 679 864 678  886 678 891 688  898 701 879 716  813 765 752 745  556 709 166 664  142 663 160 643  193 610 237 620  343 648 465 662  466 663 468 662  515 667" +


   public RectTransform pre1, pre2;
    // Start is called before the first frame update
    void Start()
    {
        Fun1();
      fun2();
        
        
    }

    void Fun1()
    {
        List<Vector2> points = new List<Vector2>();
        points.Add(new Vector2(515f, 667f));
        points.Add(new Vector2(609f, 679f));
        points.Add(new Vector2(864f, 678f));
        points.Add(new Vector2(886f, 678f));
        points.Add(new Vector2(891f, 688f));
        points.Add(new Vector2(898f, 701f));
        points.Add(new Vector2(879f, 716f));
        points.Add(new Vector2(813f, 765f));
        points.Add(new Vector2(752f, 745f));
        points.Add(new Vector2(556f, 709f));
        points.Add(new Vector2(166f, 664f));
        points.Add(new Vector2(142f, 663f));
        points.Add(new Vector2(160f, 643f));
        points.Add(new Vector2(193f, 610f));
        points.Add(new Vector2(237f, 620f));
        points.Add(new Vector2(343f, 648f));
        points.Add(new Vector2(465f, 662f));
        points.Add(new Vector2(466f, 663f));
        points.Add(new Vector2(468f, 662f));
        points.Add(new Vector2(515f, 667f));

        DoDraw(points,true);
    }
    void DoDraw(List<Vector2> points,bool flag)
    {
        for (var p = 0; p <points.Count;p++)
        {
            RectTransform g = GameObject.Instantiate(flag?pre1:pre2, flag?pre1.parent:pre2.parent);
            g.localPosition = points[p];
            g.localScale = Vector3.one;
            if (p > 0)
            {
                PointTool.inst.CreateLine(points[p], points[p-1],flag);
            }
            
        }
    }
    // Update is called once per frame
    void fun2()
    {
        List<Vector2> points = new List<Vector2>();

        string str = "531 651 Q 736 675 868 663 Q 893 662 899 670 Q 906 683 894 696 " +
                     "Q 863 724 817 744 Q 801 750 775 740 Q 712 725 483 694 Q 185 660 168" +
                     " 657 Q 162 658 156 657 Q 141 657 141 645 Q 140 632 160 618 Q 178 605 211" +
                     " 594 Q 221 590 240 599 Q 348 629 470 644 L 531 651";

            List<int> intlist = new List<int>();
        string[] array = str.Split(' ');
        foreach (var VARIABLE in array)
        {
            if (VARIABLE.Trim().Length > 1)
            {
                intlist.Add(int.Parse(VARIABLE));
            }
        }

        for (var iv = 0; iv < intlist.Count;iv +=2)
        {
            points.Add(new Vector2(intlist[iv],intlist[iv+1]));
        }
        
        DoDraw(points,false);
    }
    
  
}

/*
      531, 651 
      736, 675 
      868 ,663 
      893 ,662 
      899, 670 
      906 ,683 
      894, 696 
      863, 724 
      817, 744 
      801 ,750 
      775 ,740 
      712, 725 
      483, 694 
      185, 660 
      168, 657 
      162 ,658 
      156 ,657 
      141 ,657 
      141 ,645 
      140 ,632 
      160 ,618 
      178 ,605 
      211 ,594 
      221, 590 
      240, 599 
      348, 629 
      470, 644 
      531, 651
          
          */
