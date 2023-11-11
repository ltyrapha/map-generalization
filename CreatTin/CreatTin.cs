using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using System.Collections.Generic;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.DataSourcesRaster;

namespace CreatTin
{
    /// <summary>
    /// Command that works in ArcMap/Map/PageLayout
    /// </summary>
    [Guid("1daba799-24c1-44b8-af81-3c6783818422")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("CreatTin.CreatTin")]
    public sealed class CreatTin : BaseCommand
    {
        #region COM Registration Function(s)
        [ComRegisterFunction()]
        [ComVisible(false)]
        static void RegisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryRegistration(registerType);

            //
            // TODO: Add any COM registration code here
            //
        }

        [ComUnregisterFunction()]
        [ComVisible(false)]
        static void UnregisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryUnregistration(registerType);

            //
            // TODO: Add any COM unregistration code here
            //
        }

        #region ArcGIS Component Category Registrar generated code
        /// <summary>
        /// Required method for ArcGIS Component Category registration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryRegistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Register(regKey);
            ControlsCommands.Register(regKey);
        }
        /// <summary>
        /// Required method for ArcGIS Component Category unregistration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryUnregistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Unregister(regKey);
            ControlsCommands.Unregister(regKey);
        }

        #endregion
        #endregion
        private IApplication m_application;
        private static IActiveView m_pActiveView;
        private IFeatureLayer m_pFeatureLayer;
              
        //参与建网的几何目标
        List<IPolygon> m_PolyList;//记录参与建网的多边形目标
        IEnvelope m_Envelope;//目标分布范围空间

        TinClass m_tin;//创建的三角网对象

        public CreatTin()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "CreatTin"; //localizable text
            base.m_caption = "CreatTin";  //localizable text 
            base.m_message = "对选择目标构建三角网";  //localizable text
            base.m_toolTip = "三角网分析";  //localizable text
            base.m_name = "三角网分析";   //unique id, non-localizable (e.g. "MyCategory_MyCommand")

            try
            {
                //
                // TODO: change bitmap name if necessary
                //
                string bitmapResourceName = GetType().Name + ".bmp";
                base.m_bitmap = new Bitmap(GetType(), bitmapResourceName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message, "Invalid Bitmap");
            }
        }

        #region Overridden Class Methods

        /// <summary>
        /// Occurs when this command is created
        /// </summary>
        /// <param name="hook">Instance of the application</param>
        public override void OnCreate(object hook)
        {
            if (hook == null)
                return;
            
            //对该插件类的成员变量进行初始化
            m_application = hook as IApplication;
            m_pActiveView = (m_application.Document as IMxDocument).ActivatedView;
            m_PolyList = new List<IPolygon>();
            m_Envelope = null;
            m_tin = new TinClass();
        }
        
        /// <summary>
        /// Occurs when this command is clicked
        /// </summary>
        public override void OnClick()
        {
            /////////////////////////////////////////////////////////////////
            //////////////初始化环境，包括相关变量的赋值等工作///////////////
            /////////////该部分代码撰写与开发平台相关，可不关注//////////////
            /////////////////////////////////////////////////////////////////
            InitialEnvironment();
         
            //执行相关操作
            //Excute_sample();//示例
            Excute_merge();

        }
       
        #endregion

        public void Excute_sample()
        {
            //////////////////////////////////////////////////////////////////////////////
            //////////////////获取参与建网的目标（此处为多边形目标）//////////////////////
            ///此处将相关代码封装为一个函数，核心在于赋值变量m_PolyList和m_Envelope///////
            //////////////////////////////////////////////////////////////////////////////
            DataPrepare();

            //绘制选中的多边形看看
            RefreshArea(m_Envelope);//刷白绘图区域
            for (int i = 0; i < m_PolyList.Count; i++)
            {
                IPolygon pPolygon = m_PolyList[i];
                IRgbColor outlineColor = new RgbColorClass();
                outlineColor.Red = 150; outlineColor.Green = 150; outlineColor.Blue = 150;
                IRgbColor fillColor = new RgbColorClass();
                fillColor.Red = 235; fillColor.Blue = 235; fillColor.Green = 235;
                DrawPolygon(pPolygon, 1, fillColor, outlineColor);
            }
            MessageBox.Show("the Polygons for TIN!");


            /////////////////////////////////////////////////////////////
            /////////////////Step2:开始建网//////////////////////////////
            /////////////////////////////////////////////////////////////               
            m_tin.InitNew(m_Envelope);//对象初始化
            bool is_success = m_tin.StartInMemoryEditing();//开启内存编辑模式

            //获取多边形中心点，并装载到三角网对象中，完成建网工作
            #region
            /*for (int i = 0; i < m_PolyList.Count; i++)
            {
                IPolygon pPolygon = m_PolyList[i];//获得多边形对象

                //提取多边形外接矩形中心点作为该多边形的中心点
                IEnvelope pExtend = (pPolygon as IGeometry).Envelope;
                IPoint pt_center = new PointClass();
                pt_center.X = 0.5 * (pExtend.XMax + pExtend.XMin);
                pt_center.Y = 0.5 * (pExtend.YMax + pExtend.YMin);
                pt_center.Z = 0;//Z值赋0  

                int tagValue = i;//tagValue存储该点对应的多边形下标
                ITinNode node = new TinNodeClass();
                m_tin.AddPointZ(pt_center, tagValue, node);//依次将点加入到TIN中
            }*/

            #endregion       
            
            //获取多边形点集，并装载到三角网对象中，完成建网工作
            #region
            for (int i = 0; i < m_PolyList.Count; i++)
            {
                IPolygon pPolygon = m_PolyList[i];//获得多边形对象

                pPolygon.Generalize(1);//做一个保守的压缩处理
                IPointCollection pPtInPolygon = pPolygon as IPointCollection;
                InsertPoint(pPtInPolygon, 10);//对多边形边界进行节点加密

                for (int k = 0; k < pPtInPolygon.PointCount - 1; k++)
                {
                    IPoint pt = pPtInPolygon.get_Point(k);
                    pt.Z = 0;//Z值赋0

                    int tagValue = i;//tagValue存储该点对应的多边形下标
                    ITinNode node = new TinNodeClass();
                    m_tin.AddPointZ(pt, tagValue, node);//依次将点加入到TIN中
                }
            }
            #endregion

            //绘制三角网三角形
            #region
            for (int i = 1; i <= m_tin.TriangleCount; i++)
            {
                ITinTriangle tinTriangle = m_tin.GetTriangle(i);
                if (!tinTriangle.IsInsideDataArea)//判断三角形是否在数据分布区域之内
                    continue;
                IRgbColor pOutlineColor = new RgbColorClass();
                pOutlineColor.Red = 220; pOutlineColor.Green = 220; pOutlineColor.Blue = 220;
                DrawTinTriangle(tinTriangle, 2, null, pOutlineColor);
            }
            MessageBox.Show("TIN_Triangles");
            #endregion

            ////绘制三角网三角形顶点
            #region
            for (int i = 1; i <= m_tin.NodeCount; i++)
            {
                ITinNode oneNode = m_tin.GetNode(i);
                if (!oneNode.IsInsideDataArea)
                    continue;
                IRgbColor pColor = new RgbColorClass();
                pColor.Red = 100; pColor.Green = 100; pColor.Blue = 100;
                DrawTinNode(oneNode, 7, pColor);
            }
            MessageBox.Show("TIN_Nodes!");
            #endregion

            ////剪枝
            //for (int i = 1; i <= m_tin.EdgeCount; i++)
            //{
            //    ITinEdge oneEdge = m_tin.GetEdge(i);
            //    ITinNode from_Node = oneEdge.FromNode;
            //    ITinNode end_Node = oneEdge.ToNode;
            //    int from_Node_tag = from_Node.TagValue;
            //    int end_Node_tag = end_Node.TagValue;
            //    if (!oneEdge.IsInsideDataArea)
            //        continue;
            //    if (oneEdge.Length < 60 || from_Node_tag == end_Node_tag)
            //        m_tin.SetEdgeTagValue(i, 1);
            //    else m_tin.SetEdgeTagValue(i, 0);
            //}

            //for (int i = 1; i <= m_tin.EdgeCount; i++)
            //{
            //    ITinEdge oneEdge = m_tin.GetEdge(i);
            //    if (!oneEdge.IsInsideDataArea)
            //        continue;
            //    IRgbColor pColor = new RgbColorClass();
            //    pColor.Red = 131; pColor.Green = 203; pColor.Blue = 172;
            //    if (oneEdge.TagValue == 1)
            //        DrawTinEdge(oneEdge, 1, pColor);
            //}
            //MessageBox.Show("Finish cutting!");

            //绘制三角网三角形边
            #region
            //for (int i = 1; i <= m_tin.EdgeCount; i++)
            //{
            //    ITinEdge oneEdge = m_tin.GetEdge(i);
            //    if (!oneEdge.IsInsideDataArea)
            //        continue;
            //    IRgbColor pColor = new RgbColorClass();
            //    pColor.Red = 150; pColor.Green = 150; pColor.Blue = 150;
            //    DrawTinEdge(oneEdge, 1, pColor);
            //}
            //MessageBox.Show("TIN_Edges!");
            #endregion

            //绘制V图
            //for (int i = 1; i <= m_tin.NodeCount; i++)
            //{
            //    ITinNode oneNode = m_tin.GetNode(i);
            //    if (!oneNode.IsInsideDataArea)
            //        continue;
            //    IPolygon pVoronoi = oneNode.GetVoronoiRegion(null);
            //    IRgbColor pColor = new RgbColorClass();
            //    pColor.Red = 100; pColor.Green = 100; pColor.Blue = 100;
            //    DrawPolygon(pVoronoi, 2, null, pColor);
            //}

            //MessageBox.Show("the Voronoi Polygon!");

            //for (int i = 0; i < m_PolyList.Count; i++)
            //{
            //    IPolygon pPolygon = m_PolyList[i];
            //    IRgbColor outlineColor = new RgbColorClass();
            //    outlineColor.Red = 100; outlineColor.Green = 100; outlineColor.Blue = 100;
            //    IRgbColor fillColor = new RgbColorClass();
            //    fillColor.Red = 235; fillColor.Blue = 235; fillColor.Green = 235;
            //    DrawPolygon(pPolygon, 1, fillColor, outlineColor);
            //}
            //MessageBox.Show("the Polygons for TIN!");


            ///////////////////////////////////////////////////////////////////
            /////////////////Step3:获取三角网结构关系//////////////////////////
            ///////////////////////////////////////////////////////////////////    

            //情形1：给定某个三角网顶点，查询相关联的三角形
            #region
            //ITinNode oneTinNode = m_tin.GetNode(34);
            //IRgbColor pNodeColor = new RgbColorClass();
            //pNodeColor.Red = 255; pNodeColor.Green = 0; pNodeColor.Blue = 0;
            //DrawTinNode(oneTinNode, 7, pNodeColor);
            //MessageBox.Show("the selected Node!");

            //List<ITinTriangle> TriangleList_Node = GetIncidentTriangles(oneTinNode);//获得与该顶点连接的三角形
            //IRgbColor pTinOutlineColor_Node = new RgbColorClass();
            //pTinOutlineColor_Node.Red = 0; pTinOutlineColor_Node.Green = 0; pTinOutlineColor_Node.Blue = 255;
            //for (int i = 0; i < TriangleList_Node.Count; i++)
            //{
            //    DrawTinTriangle(TriangleList_Node[i], 1, null, pTinOutlineColor_Node);
            //}
            //MessageBox.Show("the neighboring Triangles of the TinNode!");
            #endregion

            //情形2：给定某个三角形边，查询两侧邻接的三角形
            #region
            //ITinEdge oneTinEdge = m_tin.GetEdge(13);
            //IRgbColor pEdgeColor = new RgbColorClass();
            //pEdgeColor.Red = 255; pEdgeColor.Green = 0; pEdgeColor.Blue = 0;
            //DrawTinEdge(oneTinEdge, 2, pEdgeColor);
            //MessageBox.Show("the selected Edge!");

            //List<ITinTriangle> TriangleList_edge = GetIncidentTriangle(oneTinEdge);//获得与该边邻接的三角形
            //IRgbColor pTinOutlineColor_edge = new RgbColorClass();
            //pTinOutlineColor_edge.Red = 0; pTinOutlineColor_edge.Green = 0; pTinOutlineColor_edge.Blue = 255;
            //for (int i = 0; i < TriangleList_edge.Count; i++)
            //{
            //    DrawTinTriangle(TriangleList_edge[i], 1, null, pTinOutlineColor_edge);
            //}
            //MessageBox.Show("the neighboring Triangles of the TinEdge!");
            #endregion

            //情形3：给定某个三角形，查询邻接的三角形
            #region
            //ITinTriangle oneTinTriangle = m_tin.GetTriangle(89);
            //IRgbColor pTriangleColor = new RgbColorClass();
            //pTriangleColor.Red = 255; pTriangleColor.Green = 0; pTriangleColor.Blue = 0;
            //DrawTinTriangle(oneTinTriangle, 2, null, pTriangleColor);
            //MessageBox.Show("the selected Triangle!");

            //List<ITinTriangle> TriangleList_triangle = GetIncidentTriangle(oneTinTriangle);//获得与该三角形邻接的其它三角形
            //IRgbColor pTinOutlineColor_triangle = new RgbColorClass();
            //pTinOutlineColor_triangle.Red = 0; pTinOutlineColor_triangle.Green = 0; pTinOutlineColor_triangle.Blue = 255;
            //for (int i = 0; i < TriangleList_triangle.Count; i++)
            //{
            //    DrawTinTriangle(TriangleList_triangle[i], 1, null, pTinOutlineColor_triangle);
            //}
            //MessageBox.Show("the neighboring Triangles of the TinTriangle!");
            #endregion

            //情形4：给定某个多边形，查询与该多边形相互邻接的多边形
            #region
            //int index_objPoly = 3;
            //IRgbColor pColorPoly = new RgbColorClass();
            //pColorPoly.Red = 255; pColorPoly.Green = 0; pColorPoly.Blue = 0;
            //DrawPolygon((m_PolyList[index_objPoly]), 2, null, pColorPoly);
            //MessageBox.Show("the selected Polygon!");

            //List<int> IndexList_neigbor = new List<int>();
            //for (int i = 1; i < m_tin.EdgeCount; i++)
            //{
            //    ITinEdge oneEdge = m_tin.GetEdge(i);//遍历所有的三角网三角形边
            //    if (!oneEdge.IsInsideDataArea)
            //        continue;
            //    ITinNode from_Node = oneEdge.FromNode;//获得该边的起始、终止顶点
            //    ITinNode end_Node = oneEdge.ToNode;
            //    int from_Node_tag = from_Node.TagValue;//起始顶点记录的tag值（即该顶点属于哪个下标的多边形）
            //    int end_Node_tag = end_Node.TagValue;//终止顶点记录的tag值（即该顶点属于哪个下标的多边形）

            //    if (from_Node_tag == index_objPoly && end_Node_tag != index_objPoly)//如果起始点代表查询多边形，而终止点代表另外一个多边形
            //    {
            //        if (!IndexList_neigbor.Contains(end_Node_tag))
            //        {
            //            IndexList_neigbor.Add(end_Node_tag);
            //        }
            //    }

            //    else if (from_Node_tag != index_objPoly && end_Node_tag == index_objPoly)//如果终止点代表查询多边形，而起始点代表另外一个多边形
            //    {
            //        if (!IndexList_neigbor.Contains(from_Node_tag))
            //        {
            //            IndexList_neigbor.Add(from_Node_tag);
            //        }                
            //    }            
            //}

            //pColorPoly.Red = 255; pColorPoly.Green = 0; pColorPoly.Blue = 0;
            //for (int i = 0; i < IndexList_neigbor.Count; i++)
            //{
            //    DrawPolygon((m_PolyList[IndexList_neigbor[i]]), 1, null, pColorPoly);
            //}
            //MessageBox.Show("the neighboring polygons of the selected polygon!");
            #endregion
        }

        public void Excute_merge()
        {
            //////////////////////////////////////////////////////////////////////////////
            //////////////////获取参与建网的目标（此处为多边形目标）//////////////////////
            ///此处将相关代码封装为一个函数，核心在于赋值变量m_PolyList和m_Envelope///////
            //////////////////////////////////////////////////////////////////////////////
            DataPrepare();
            //绘制选中的多边形看看
            RefreshArea(m_Envelope);//刷白绘图区域
            for (int i = 0; i < m_PolyList.Count; i++)
            {
                IPolygon pPolygon = m_PolyList[i];
                IRgbColor outlineColor = new RgbColorClass();
                outlineColor.Red = 150; outlineColor.Green = 150; outlineColor.Blue = 150;
                IRgbColor fillColor = new RgbColorClass();
                fillColor.Red = 235; fillColor.Blue = 235; fillColor.Green = 235;
                DrawPolygon(pPolygon, 1, fillColor, outlineColor);
            }
            MessageBox.Show("the Polygons for TIN!");

            /////////////////////////////////////////////////////////////
            /////////////////Step2:开始建网//////////////////////////////
            /////////////////////////////////////////////////////////////               
            m_tin.InitNew(m_Envelope);//对象初始化
            bool is_success = m_tin.StartInMemoryEditing();//开启内存编辑模式

            //获取多边形点集，并装载到三角网对象中，完成建网工作
            #region
            for (int i = 0; i < m_PolyList.Count; i++)
            {
                IPolygon pPolygon = m_PolyList[i];//获得多边形对象

                pPolygon.Generalize(1);//做一个保守的压缩处理
                IPointCollection pPtInPolygon = pPolygon as IPointCollection;
                InsertPoint(pPtInPolygon, 30);//对多边形边界进行节点加密

                for (int k = 0; k < pPtInPolygon.PointCount - 1; k++)
                {
                    IPoint pt = pPtInPolygon.get_Point(k);
                    pt.Z = 0;//Z值赋0

                    int tagValue = i;//tagValue存储该点对应的多边形下标
                    ITinNode node = new TinNodeClass();
                    m_tin.AddPointZ(pt, tagValue, node);//依次将点加入到TIN中
                }
            }
            #endregion


            //****************在以下区域加入代码**************（这是实现算法的分割线）

            //绘制三角网三角形
            //#region
            //for (int i = 1; i <= m_tin.TriangleCount; i++)
            //{
            //    ITinTriangle tinTriangle = m_tin.GetTriangle(i);
            //    if (!tinTriangle.IsInsideDataArea)//判断三角形是否在数据分布区域之内
            //        continue;
            //    IRgbColor pOutlineColor = new RgbColorClass();
            //    pOutlineColor.Red = 220; pOutlineColor.Green = 220; pOutlineColor.Blue = 220;
            //    DrawTinTriangle(tinTriangle, 2, null, pOutlineColor);
            //}
            //MessageBox.Show("TIN_Triangles");
            //#endregion

            //绘制三角网三角形顶点
            #region
            for (int i = 1; i <= m_tin.NodeCount; i++)
            {
                ITinNode oneNode = m_tin.GetNode(i);
                if (!oneNode.IsInsideDataArea)
                    continue;
                IRgbColor pColor = new RgbColorClass();
                pColor.Red = 100; pColor.Green = 100; pColor.Blue = 100;
                DrawTinNode(oneNode, 4, pColor);
            }
            MessageBox.Show("TIN_Nodes!");
            #endregion

            //按长度修剪三角网的边，将符合条件的边存储到列表中
            #region
            List<ITinEdge> goodTinEdge = new List<ITinEdge>();  //存储符合条件的三角网边，用于分组

            for (int i = 1; i <= m_tin.EdgeCount; i++)
            {
                ITinEdge oneEdge = m_tin.GetEdge(i);
                if (oneEdge.Length < 50)//筛选长度仅小于阈值的边
                {
                    if (oneEdge.FromNode.TagValue != oneEdge.ToNode.TagValue)//连接不同的多边形边放入goodTinEdge中,即三角网一条边的两个结点位于两个不同的多边形
                        goodTinEdge.Add(oneEdge);
                    //绘制边看看
                    IRgbColor fillColor = new RgbColorClass();
                    fillColor.Red = 199; fillColor.Green = 203; fillColor.Blue = 241;
                    DrawTinEdge(oneEdge, 2, fillColor);
                }
            }
            MessageBox.Show("剪枝完成");
            #endregion

            //将多边形的所有下标存放于一个数组中
            #region
            List<int> m_PolyList_Tag = new List<int>();
            for (int i = 0; i < m_PolyList.Count; i++)
            {
                m_PolyList_Tag.Add(i);
            }
            #endregion

            //遍历所有多边形，将其分组
            #region
            List<List<int>> groupedPolygons = new List<List<int>>(); //二维数组存储各组多边形下标
            int c = 1;
            while (m_PolyList_Tag.Count > 0)
            {
                List<int> groupedPolygon = new List<int>();
                //先将待查询多边形加入组中，同时在多边形下标列表中删除该下标值，每次查询最后一个
                groupedPolygon.Add(m_PolyList_Tag[m_PolyList_Tag.Count - 1]);
                m_PolyList_Tag.Remove(m_PolyList_Tag[m_PolyList_Tag.Count - 1]);
                int current = 0;
                while (current != groupedPolygon.Count)//如果所有元素未被遍历完，即若遍历完，groupedPolygon.Count就不会再增加，current+1后就恰好与之相等了
                {
                    //int newnum = groupedpolygon.count - oldcount;
                    //oldcount = groupedpolygon.count;
                    GroupPolygon(groupedPolygon[current], goodTinEdge, m_PolyList_Tag, groupedPolygon);
                    current++;

                }
                groupedPolygons.Add(groupedPolygon);
                //MessageBox.Show("分组完成一次");//应出现22次
                //画出各组
                for (int i = 0; i < groupedPolygon.Count; i++)
                {
                    IRgbColor pColorPoly = new RgbColorClass();
                    pColorPoly.Red = 22 * c; pColorPoly.Green = 22 * (c+5); pColorPoly.Blue = 22 * (c+10);
                    DrawPolygon((m_PolyList[groupedPolygon[i]]), 2, null, pColorPoly);

                }
                c+=1;
            }
            MessageBox.Show("分组全部完成");
            #endregion

            //获取多边形点集，按照多边形分组结果组织点群并按坐标排序
            #region
            List<List<IPoint>> pointLists = new List<List<IPoint>>();
            for (int i = 0; i < groupedPolygons.Count; i++)
            {
                //获取各多边形点集
                #region
                List<IPoint> pointList = new List<IPoint>();
                List<int> groupedPolygon = groupedPolygons[i];//存放第i组
                for (int j = 0; j < groupedPolygon.Count; j++)//对每组进行点集存储
                {
                    IPolygon pPolygon = m_PolyList[groupedPolygon[j]];//获得多边形对象
                    pPolygon.Generalize(1);//做一个保守的压缩处理
                    IPointCollection pPtInPolygon = pPolygon as IPointCollection;
                    InsertPoint(pPtInPolygon, 30);//对多边形边界进行节点加密

                    for (int k = 0; k < pPtInPolygon.PointCount - 1; k++)
                    {
                        pointList.Add(pPtInPolygon.get_Point(k));

                    }
                }
                #endregion

                //对点集做升序处理,使横纵坐标最小的点位于首位
                #region
                pointList.Sort(delegate(IPoint p1, IPoint p2)
                {
                    int a = p1.Y.CompareTo(p2.Y);//按点y坐标升序
                    if (a == 0)
                        a = p1.X.CompareTo(p2.X);//y坐标相同就按x升序
                    return a;
                });
                #endregion

                //水平序Graham扫描
                #region
                Stack<IPoint> resPoint = new Stack<IPoint>();
                resPoint.Push(pointList[0]);
                resPoint.Push(pointList[1]);
                //右链扫描
                for (int n = 2; n < pointList.Count; n++)//对点集中每一个点进行遍历
                {
                    while (resPoint.Count >= 2)//判断pointList[n]是否在凸包上
                    {
                        IPoint b = resPoint.Pop();//返回栈顶并移除
                        IPoint a = resPoint.Peek();//返回栈顶但不移除
                        IPoint temp = pointList[n];
                        //不在凸包上的两种情况：相等或在逆时针方向：
                        if (b.X == temp.X && b.Y == temp.Y)
                        {
                            break;
                        }
                        if (multi(a, b, temp) >= 0)//需要左转，at在ab逆时针方向
                        {
                            resPoint.Push(b);
                            break;
                        }
                    }
                    resPoint.Push(pointList[n]);
                }

                //在pointList将已入栈的元素删除，避免干扰到左链扫描
                for (int n = pointList.Count - 1; n > 0; n--)//倒序遍历删除元素，且略过最前面的点，因为左链需要
                {
                    if (resPoint.Contains(pointList[n]))
                        pointList.RemoveAt(n);
                }
                //左链扫描
                for (int n = pointList.Count - 1; n >= 0; n--)//倒序遍历，因为resPoint已经在点序列最大的地方
                {
                    while (resPoint.Count >= 2)
                    {
                        IPoint b = resPoint.Pop();
                        IPoint a = resPoint.Peek();
                        IPoint temp = pointList[n];
                        if (b.X == temp.X && b.Y == temp.Y)
                        {
                            break;
                        }
                        if (multi(a, b, temp) >= 0)//需要左转
                        {
                            resPoint.Push(b);
                            break;
                        }
                    }
                    resPoint.Push(pointList[n]);
                }

                resPoint.Pop();//最上面的点是第0个点，栈底已有，故舍去
                #endregion

                pointList.Clear();
                while (resPoint.Count != 0)
                {
                    pointList.Add(resPoint.Pop());
                }
                pointLists.Add(pointList);
            }
            MessageBox.Show("筛选完成");
            #endregion

            //点变面
            #region
            List<IPolygon> newPolygonList = new List<IPolygon>();
            for (int i = 0; i < pointLists.Count; i++)
            {
                List<IPoint> pointList = pointLists[i];
                IPolygon newPolygon = PointToPolygon(pointList);
                newPolygonList.Add(newPolygon);
            }
            MessageBox.Show("转化完成");

            //画出合并后的多边形
            for (int i = 0; i < newPolygonList.Count; i++)
            {
                IRgbColor polyOutlineColor = new RgbColorClass();
                polyOutlineColor.Red = 0; polyOutlineColor.Green = 0; polyOutlineColor.Blue = 0;
                IRgbColor polyFillColor = new RgbColorClass();
                polyFillColor.Red = 153; polyFillColor.Green = 217; polyFillColor.Blue = 234;
                DrawPolygon(newPolygonList[i], 2, polyFillColor, polyOutlineColor);
            }
            #endregion
        }

        public void InitialEnvironment()
        {
            //获得当前图层信息（该图层包含的目标是我们建网处理的对象）
            ILayer pLayer = DApplication.App.currentLayer;
            if (pLayer == null)
            {
                MessageBox.Show("请选择目标图层！");
                return;
            }
            m_pFeatureLayer = pLayer as IFeatureLayer;


            //在实施后面建网等工作前，对定义的插件类成员变量进行清空
            if (m_PolyList.Count > 1) m_PolyList.Clear();
            if (m_tin != null) m_tin.SetEmpty();//重置三角网对象
        }

        /// <summary>
        /// 
        /// </summary>
        public void DataPrepare()
        {
            IFeatureClass pFeatureClass = m_pFeatureLayer.FeatureClass;//有图层信息获得要素集合信息
            IGeoDataset pGeoDataset = pFeatureClass as IGeoDataset;
            m_Envelope = pGeoDataset.Extent;//获得数据集目标分布的范围信息

            //获得当前选择图层中的多边形目标
            IMap currentMap = (m_application.Document as IMxDocument).ActivatedView.FocusMap;
            IEnumFeatureSetup pEnumFeatureSetup = currentMap.FeatureSelection as IEnumFeatureSetup;
            IEnumFeature pfeatureList = pEnumFeatureSetup as IEnumFeature;
            pfeatureList.Reset();

            IFeature pfeature = pfeatureList.Next();
            while (!(pfeature == null))
            {
                if ((pfeature.Class as IFeatureClass).FeatureClassID != pFeatureClass.FeatureClassID)//判断选择的要素目标是否属于当前图层
                    continue;
                if (pfeature.ShapeCopy.GeometryType != esriGeometryType.esriGeometryPolygon)//判断选择的要素目标几何类型是否是多边形
                    continue;
                m_PolyList.Add(pfeature.Shape as IPolygon);
                pfeature = pfeatureList.Next();
            }

            if (m_PolyList.Count < 1)
            {
                MessageBox.Show("请选择要素目标！");
                return;
            }    
        }
         
        /// <summary>
        /// 绘制一个三角网顶点
        /// </summary>
        /// <param name="oneNode"></param>绘制的三角网顶点对象
        /// <param name="nSize"></param>大小
        /// <param name="pColor"></param>颜色
        public void DrawTinNode(ITinNode oneNode, int nSize, IRgbColor pColor)
        {
            if (oneNode == null)
                return;
            IPoint pt = new PointClass();
            pt.X = oneNode.X;
            pt.Y = oneNode.Y;

            DrawPoint(pt, nSize, pColor);
        }
        
        /// <summary>
        /// 绘制一条三角网三角形边
        /// </summary>
        /// <param name="oneTinEdge"></param>三角形边
        /// <param name="nWidth"></param>线宽
        /// <param name="pColor"></param>颜色
        public void DrawTinEdge(ITinEdge oneTinEdge,int nWidth,IRgbColor pColor)
        {
            if (oneTinEdge == null)
                return;

            IPoint ptFrom = new PointClass();
            IPoint ptEnd = new PointClass();

            ptFrom.X = oneTinEdge.FromNode.X;
            ptFrom.Y = oneTinEdge.FromNode.Y;
            ptEnd.X = oneTinEdge.ToNode.X;
            ptEnd.Y = oneTinEdge.ToNode.Y;

            IPolyline pPolyline = new PolylineClass();
            (pPolyline as IPointCollection).AddPoint(ptFrom);
            (pPolyline as IPointCollection).AddPoint(ptEnd);

            DrawPolyLine(pPolyline, nWidth, pColor);
        }

        /// <summary>
        /// 绘制一个三角网三角形
        /// </summary>
        /// <param name="oneTinTriangle"></param>三角形
        /// <param name="nWidth"></param>线宽
        /// <param name="pFillColor"></param>填充色
        /// <param name="pOutlineColor"></param>边界色
        public void DrawTinTriangle(ITinTriangle oneTinTriangle, int nWidth, IRgbColor pFillColor, IRgbColor pOutlineColor)
        {
            if (oneTinTriangle == null)
                return;

            IRing pRing = new RingClass();
            oneTinTriangle.QueryAsRing(pRing);  //将三角网三角形转换为IRing对象            
            IGeometry g = pRing as IGeometry;
            PolygonClass pPolygon = new PolygonClass();
            pPolygon.AddGeometries(1, ref g);
            DrawPolygon(pPolygon as IPolygon, nWidth, pFillColor, pOutlineColor);
        }

        /// <summary>
        /// 绘制一个点对象
        /// </summary>
        /// <param name="pt"></param>点
        /// <param name="nSize"></param>尺寸
        /// <param name="pColor"></param>颜色
        public void DrawPoint(IPoint pt, int nSize, IRgbColor pColor)
        {
            IScreenDisplay pDisplay = m_pActiveView.ScreenDisplay;
            pDisplay.StartDrawing(pDisplay.hDC, 0);

            ISimpleMarkerSymbol simplePtSym = new SimpleMarkerSymbolClass();
            simplePtSym.Style = esriSimpleMarkerStyle.esriSMSCircle;
            simplePtSym.Color = pColor as IColor;
            simplePtSym.Size = nSize;
            pDisplay.SetSymbol(simplePtSym as ISymbol);
            pDisplay.DrawPoint(pt);

            pDisplay.FinishDrawing();
        }

        /// <summary>
        /// 绘制一条线线
        /// </summary>
        /// <param name="pLine"></param>待绘制的线
        /// <param name="nWidth"></param>绘制宽度
        /// <param name="pColor"></param>绘制颜色
        public void DrawPolyLine(IPolyline pLine, int nWidth, IRgbColor pColor)
        {
            IScreenDisplay pDisplay = m_pActiveView.ScreenDisplay;
            pDisplay.StartDrawing(pDisplay.hDC, 0);


            ISimpleLineSymbol simpleLineSym = new SimpleLineSymbolClass();
            simpleLineSym.Color = pColor as IColor;
            simpleLineSym.Width = nWidth;
            pDisplay.SetSymbol(simpleLineSym as ISymbol);

            pDisplay.DrawPolyline(pLine);

            pDisplay.FinishDrawing();
        }

        /// <summary>
        /// 绘制一个多边形
        /// </summary>
        /// <param name="pPoly"></param>待绘制的多边形
        /// <param name="nWidth"></param>绘制宽度
        /// <param name="pFillColor"></param>填充颜色
        /// <param name="pOutlineColor"></param>边界颜色
        public void DrawPolygon(IPolygon pPoly, int nWidth, IRgbColor pFillColor, IRgbColor pOutlineColor)
        {
            IScreenDisplay pDisplay = m_pActiveView.ScreenDisplay;
            pDisplay.StartDrawing(pDisplay.hDC, 0);

            ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
            simpleLineSymbol.Width = nWidth;
            simpleLineSymbol.Color = pOutlineColor;
            //simpleLineSymbol.Style = borderStyle;
            ISimpleFillSymbol simplePolySym = new SimpleFillSymbol();
            simplePolySym.Outline = simpleLineSymbol;
            // simplePolySym.Style = fillStyle;
            if (pFillColor == null)
            {
                simplePolySym.Style = esriSimpleFillStyle.esriSFSHollow;
            }
            else
            {
                simplePolySym.Color = pFillColor;
            }

            pDisplay.SetSymbol(simplePolySym as ISymbol);
            pDisplay.DrawPolygon(pPoly);
            pDisplay.FinishDrawing();
        }

        /// <summary>
        /// 对某一矩形区域进行刷白
        /// </summary>
        /// <param name="pEnvelope">刷白区域</param>
        public void RefreshArea(IEnvelope pEnvelope)
        {
            IScreenDisplay pDisplay = m_pActiveView.ScreenDisplay;
            pDisplay.StartDrawing(pDisplay.hDC, 0);

            IRgbColor pColor = new RgbColorClass();
            pColor.Red = 255;
            pColor.Green = 255;
            pColor.Blue = 255;

            ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
            simpleLineSymbol.Width = 10;
            simpleLineSymbol.Color = pColor;
            //simpleLineSymbol.Style = borderStyle;
            ISimpleFillSymbol simplePolySym = new SimpleFillSymbol();
            simplePolySym.Outline = simpleLineSymbol;
            // simplePolySym.Style = fillStyle;
            simplePolySym.Color = pColor;
            pDisplay.SetSymbol(simplePolySym as ISymbol);
            pDisplay.DrawRectangle(pEnvelope);
            pDisplay.FinishDrawing();
 
        }

        /// <summary>
        /// 对点串进行加密
        /// </summary>
        /// <param name="ptList"></param>处理的点串对象
        /// <param name="Max_length"></param>加密后相邻节点间的最长距离阈值
        public void InsertPoint(IPointCollection ptList, double Max_length)
        {
            if (ptList == null)
                return;

            int nCount = ptList.PointCount;
            List<IPoint> resultPtList = new List<IPoint>();
            for (int i = 0; i < nCount; i++)
            {
                IPoint pt = ptList.get_Point(i);
                resultPtList.Add(pt);
            }

            for (int i = 0; i < resultPtList.Count - 1; i++)
            {
                IPoint pt_pre = resultPtList[i];
                IPoint pt_back = resultPtList[i + 1];
                double dis = DistanceBetweenTwoPoints(pt_pre, pt_back);

                if (dis > Max_length)
                {
                    IPoint pt_mid = new PointClass();
                    pt_mid.X = 0.5 * (pt_pre.X + pt_back.X);
                    pt_mid.Y = 0.5 * (pt_pre.Y + pt_back.Y);
                    resultPtList.Insert(i + 1, pt_mid);
                    i = i - 1;
                }
            }
            ptList.RemovePoints(0, nCount);
            for (int i = 0; i < resultPtList.Count; i++)
            {
                ptList.AddPoint(resultPtList[i]);
            }
            resultPtList.Clear();

        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        /// <param name="fromPt"></param>起始点
        /// <param name="toPt"></param>终止点
        /// <returns></returns>
        public double DistanceBetweenTwoPoints(IPoint fromPt, IPoint toPt)
        {
            double x1 = fromPt.X;
            double y1 = fromPt.Y;
            double x2 = toPt.X;
            double y2 = toPt.Y;

            double dis = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            return dis;
        }

   
        /// <summary>
        /// 返回指定顶点邻接的三角形
        /// </summary>
        /// <param name="node"></param>输入待查询的三家网顶点
        /// <returns></returns>返回的查询结果
        private List<ITinTriangle> GetIncidentTriangles(ITinNode node)
        {
            
            ITinTriangleArray triArray = node.GetIncidentTriangles();
            List<ITinTriangle> triangleList = new List<ITinTriangle>();
            for (int i = 0; i < triArray.Count; i++)
            {
                ITinTriangle tinTriangle = triArray.get_Element(i);
                if (!tinTriangle.IsInsideDataArea)
                    continue;
                triangleList.Add(tinTriangle);
            }
            return triangleList;
        }

        /// <summary>
        /// 返回指定边的左右三角形
        /// </summary>
        /// <param name="edge">输入三角网的某条边</param>
        /// <returns></returns>返回的查询结果
        private List<ITinTriangle> GetIncidentTriangle(ITinEdge edge)
        {
            List<ITinTriangle> triangleList = new List<ITinTriangle>();

            ITinTriangle rightTri = edge.RightTriangle;
            if (rightTri.IsInsideDataArea)
                triangleList.Add(rightTri);        
    
            ITinTriangle leftTri = edge.LeftTriangle;
            if (leftTri.IsInsideDataArea)
                triangleList.Add(leftTri);
            
            return triangleList;
        }

        /// <summary>
        /// 返回某个三角形邻接的三角形
        /// </summary>
        /// <param name="triangle"></param>待查询的三角形
        /// <returns></returns>返回的查询结果
        private List<ITinTriangle> GetIncidentTriangle(ITinTriangle triangle)
        {
            List<ITinTriangle> triangleList = new List<ITinTriangle>();
            //返回这个三角形邻接的三个三角形的编号，若邻接三角形不存在则对应 out 为0
            int t1, t2, t3;
            triangle.QueryAdjacentTriangleIndices(out t1, out t2, out t3);

            if (t1 != 0 && m_tin.GetTriangle(t1).IsInsideDataArea)
                triangleList.Add(m_tin.GetTriangle(t1));
            if (t2 != 0 && m_tin.GetTriangle(t2).IsInsideDataArea)
                triangleList.Add(m_tin.GetTriangle(t2));
            if (t3 != 0 && m_tin.GetTriangle(t3).IsInsideDataArea)
                triangleList.Add(m_tin.GetTriangle(t3));

            return triangleList;
        }

        /// <summary>
        /// 给定某个多边形查询与该多边形相互邻接的多边形，将它们存储到IndexList_neigbor中从而分为一组
        /// </summary>
        /// <param name="index_objPoly"></param>待查询多边形下标
        /// <param name="goodTinEdge"></param>符合条件的三角网边
        /// <param name="m_PolyList_Tag"></param>存储有未分组多边形下标的列表
        /// <param name="IndexList_neigbor"></param>存储邻接多边形下标
        public void GroupPolygon(int index_objPoly, List<ITinEdge> goodTinEdge, List<int> m_PolyList_Tag, List<int> IndexList_neigbor)
        {
            for (int i = goodTinEdge.Count-1; i >= 0; i--)
            {
                ITinEdge oneEdge = goodTinEdge[i];//倒序遍历所有的三角网三角形边，因为有删除操作
                if (!oneEdge.IsInsideDataArea)
                    continue;
                ITinNode from_Node = oneEdge.FromNode;//获得该边的起始、终止顶点
                ITinNode end_Node = oneEdge.ToNode;
                int from_Node_tag = from_Node.TagValue;//起始顶点记录的tag值(即该顶点属于哪个下标的多边形)
                int end_Node_tag = end_Node.TagValue;//终止顶点记录的tag值（即该顶点属于哪个下标的多边形）

                if (from_Node_tag == index_objPoly && end_Node_tag != index_objPoly)//三角网的一条边的from_node所在的多边形属于待查询多边形，end_node不属于，则把end_node属于的多边形加入邻接多边形数组
                {
                    if (!IndexList_neigbor.Contains(end_Node_tag))
                    {
                        IndexList_neigbor.Add(end_Node_tag);
                        m_PolyList_Tag.Remove(end_Node_tag);//将已分组的多边形对应下标移除
                        goodTinEdge.Remove(goodTinEdge[i]);//一条边只会确定一对多边形的邻接关系，所以在确定邻接关系后对之后的遍历无意义，可以删除
                    }
                }

                else if (end_Node_tag == index_objPoly && from_Node_tag != index_objPoly)
                {
                    if (!IndexList_neigbor.Contains(from_Node_tag))
                    {
                        IndexList_neigbor.Add(from_Node_tag);
                        m_PolyList_Tag.Remove(from_Node_tag);
                        goodTinEdge.Remove(goodTinEdge[i]);
                    }
                }

            }
        }

        /// <summary>
        /// 计算两向量ab、ap叉积，目的是得出点p在向量ab的哪个方向，大于0逆时针，小于0顺时针
        /// </summary>
        /// <param name="a"></param>向量ab起点
        /// <param name="b"></param>向量ab终点
        /// <param name="p"></param>待查点p
        /// <returns></returns>返回叉积结果
        public double multi(IPoint a, IPoint b, IPoint p)
        {
            return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
        }

        /// <summary>
        /// 将点群转化为多边形，返回多边形对象
        /// </summary>
        /// <param name="pts"></param>组成多边形的点集
        public IPolygon PointToPolygon(List<IPoint> pts)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;
            for (int i = 0; i < pts.Count; i++)//点转化为环
            {
                ring1.AddPoint(pts[i], ref missing, ref missing);
            }
            IGeometryCollection pointPolygon = new PolygonClass();//定义几何组合
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);//添加环
            IPolygon polyGonGeo = pointPolygon as IPolygon;
            polyGonGeo.SimplifyPreserveFromTo();//简化面，保持环起始点参数位置
            return polyGonGeo;
        }
    }
}
