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
              
        //���뽨���ļ���Ŀ��
        List<IPolygon> m_PolyList;//��¼���뽨���Ķ����Ŀ��
        IEnvelope m_Envelope;//Ŀ��ֲ���Χ�ռ�

        TinClass m_tin;//����������������

        public CreatTin()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "CreatTin"; //localizable text
            base.m_caption = "CreatTin";  //localizable text 
            base.m_message = "��ѡ��Ŀ�깹��������";  //localizable text
            base.m_toolTip = "����������";  //localizable text
            base.m_name = "����������";   //unique id, non-localizable (e.g. "MyCategory_MyCommand")

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
            
            //�Ըò����ĳ�Ա�������г�ʼ��
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
            //////////////��ʼ��������������ر����ĸ�ֵ�ȹ���///////////////
            /////////////�ò��ִ���׫д�뿪��ƽ̨��أ��ɲ���ע//////////////
            /////////////////////////////////////////////////////////////////
            InitialEnvironment();
         
            //ִ����ز���
            //Excute_sample();//ʾ��
            Excute_merge();

        }
       
        #endregion

        public void Excute_sample()
        {
            //////////////////////////////////////////////////////////////////////////////
            //////////////////��ȡ���뽨����Ŀ�꣨�˴�Ϊ�����Ŀ�꣩//////////////////////
            ///�˴�����ش����װΪһ���������������ڸ�ֵ����m_PolyList��m_Envelope///////
            //////////////////////////////////////////////////////////////////////////////
            DataPrepare();

            //����ѡ�еĶ���ο���
            RefreshArea(m_Envelope);//ˢ�׻�ͼ����
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
            /////////////////Step2:��ʼ����//////////////////////////////
            /////////////////////////////////////////////////////////////               
            m_tin.InitNew(m_Envelope);//�����ʼ��
            bool is_success = m_tin.StartInMemoryEditing();//�����ڴ�༭ģʽ

            //��ȡ��������ĵ㣬��װ�ص������������У���ɽ�������
            #region
            /*for (int i = 0; i < m_PolyList.Count; i++)
            {
                IPolygon pPolygon = m_PolyList[i];//��ö���ζ���

                //��ȡ�������Ӿ������ĵ���Ϊ�ö���ε����ĵ�
                IEnvelope pExtend = (pPolygon as IGeometry).Envelope;
                IPoint pt_center = new PointClass();
                pt_center.X = 0.5 * (pExtend.XMax + pExtend.XMin);
                pt_center.Y = 0.5 * (pExtend.YMax + pExtend.YMin);
                pt_center.Z = 0;//Zֵ��0  

                int tagValue = i;//tagValue�洢�õ��Ӧ�Ķ�����±�
                ITinNode node = new TinNodeClass();
                m_tin.AddPointZ(pt_center, tagValue, node);//���ν�����뵽TIN��
            }*/

            #endregion       
            
            //��ȡ����ε㼯����װ�ص������������У���ɽ�������
            #region
            for (int i = 0; i < m_PolyList.Count; i++)
            {
                IPolygon pPolygon = m_PolyList[i];//��ö���ζ���

                pPolygon.Generalize(1);//��һ�����ص�ѹ������
                IPointCollection pPtInPolygon = pPolygon as IPointCollection;
                InsertPoint(pPtInPolygon, 10);//�Զ���α߽���нڵ����

                for (int k = 0; k < pPtInPolygon.PointCount - 1; k++)
                {
                    IPoint pt = pPtInPolygon.get_Point(k);
                    pt.Z = 0;//Zֵ��0

                    int tagValue = i;//tagValue�洢�õ��Ӧ�Ķ�����±�
                    ITinNode node = new TinNodeClass();
                    m_tin.AddPointZ(pt, tagValue, node);//���ν�����뵽TIN��
                }
            }
            #endregion

            //����������������
            #region
            for (int i = 1; i <= m_tin.TriangleCount; i++)
            {
                ITinTriangle tinTriangle = m_tin.GetTriangle(i);
                if (!tinTriangle.IsInsideDataArea)//�ж��������Ƿ������ݷֲ�����֮��
                    continue;
                IRgbColor pOutlineColor = new RgbColorClass();
                pOutlineColor.Red = 220; pOutlineColor.Green = 220; pOutlineColor.Blue = 220;
                DrawTinTriangle(tinTriangle, 2, null, pOutlineColor);
            }
            MessageBox.Show("TIN_Triangles");
            #endregion

            ////���������������ζ���
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

            ////��֦
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

            //���������������α�
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

            //����Vͼ
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
            /////////////////Step3:��ȡ�������ṹ��ϵ//////////////////////////
            ///////////////////////////////////////////////////////////////////    

            //����1������ĳ�����������㣬��ѯ�������������
            #region
            //ITinNode oneTinNode = m_tin.GetNode(34);
            //IRgbColor pNodeColor = new RgbColorClass();
            //pNodeColor.Red = 255; pNodeColor.Green = 0; pNodeColor.Blue = 0;
            //DrawTinNode(oneTinNode, 7, pNodeColor);
            //MessageBox.Show("the selected Node!");

            //List<ITinTriangle> TriangleList_Node = GetIncidentTriangles(oneTinNode);//�����ö������ӵ�������
            //IRgbColor pTinOutlineColor_Node = new RgbColorClass();
            //pTinOutlineColor_Node.Red = 0; pTinOutlineColor_Node.Green = 0; pTinOutlineColor_Node.Blue = 255;
            //for (int i = 0; i < TriangleList_Node.Count; i++)
            //{
            //    DrawTinTriangle(TriangleList_Node[i], 1, null, pTinOutlineColor_Node);
            //}
            //MessageBox.Show("the neighboring Triangles of the TinNode!");
            #endregion

            //����2������ĳ�������αߣ���ѯ�����ڽӵ�������
            #region
            //ITinEdge oneTinEdge = m_tin.GetEdge(13);
            //IRgbColor pEdgeColor = new RgbColorClass();
            //pEdgeColor.Red = 255; pEdgeColor.Green = 0; pEdgeColor.Blue = 0;
            //DrawTinEdge(oneTinEdge, 2, pEdgeColor);
            //MessageBox.Show("the selected Edge!");

            //List<ITinTriangle> TriangleList_edge = GetIncidentTriangle(oneTinEdge);//�����ñ��ڽӵ�������
            //IRgbColor pTinOutlineColor_edge = new RgbColorClass();
            //pTinOutlineColor_edge.Red = 0; pTinOutlineColor_edge.Green = 0; pTinOutlineColor_edge.Blue = 255;
            //for (int i = 0; i < TriangleList_edge.Count; i++)
            //{
            //    DrawTinTriangle(TriangleList_edge[i], 1, null, pTinOutlineColor_edge);
            //}
            //MessageBox.Show("the neighboring Triangles of the TinEdge!");
            #endregion

            //����3������ĳ�������Σ���ѯ�ڽӵ�������
            #region
            //ITinTriangle oneTinTriangle = m_tin.GetTriangle(89);
            //IRgbColor pTriangleColor = new RgbColorClass();
            //pTriangleColor.Red = 255; pTriangleColor.Green = 0; pTriangleColor.Blue = 0;
            //DrawTinTriangle(oneTinTriangle, 2, null, pTriangleColor);
            //MessageBox.Show("the selected Triangle!");

            //List<ITinTriangle> TriangleList_triangle = GetIncidentTriangle(oneTinTriangle);//�������������ڽӵ�����������
            //IRgbColor pTinOutlineColor_triangle = new RgbColorClass();
            //pTinOutlineColor_triangle.Red = 0; pTinOutlineColor_triangle.Green = 0; pTinOutlineColor_triangle.Blue = 255;
            //for (int i = 0; i < TriangleList_triangle.Count; i++)
            //{
            //    DrawTinTriangle(TriangleList_triangle[i], 1, null, pTinOutlineColor_triangle);
            //}
            //MessageBox.Show("the neighboring Triangles of the TinTriangle!");
            #endregion

            //����4������ĳ������Σ���ѯ��ö�����໥�ڽӵĶ����
            #region
            //int index_objPoly = 3;
            //IRgbColor pColorPoly = new RgbColorClass();
            //pColorPoly.Red = 255; pColorPoly.Green = 0; pColorPoly.Blue = 0;
            //DrawPolygon((m_PolyList[index_objPoly]), 2, null, pColorPoly);
            //MessageBox.Show("the selected Polygon!");

            //List<int> IndexList_neigbor = new List<int>();
            //for (int i = 1; i < m_tin.EdgeCount; i++)
            //{
            //    ITinEdge oneEdge = m_tin.GetEdge(i);//�������е������������α�
            //    if (!oneEdge.IsInsideDataArea)
            //        continue;
            //    ITinNode from_Node = oneEdge.FromNode;//��øñߵ���ʼ����ֹ����
            //    ITinNode end_Node = oneEdge.ToNode;
            //    int from_Node_tag = from_Node.TagValue;//��ʼ�����¼��tagֵ�����ö��������ĸ��±�Ķ���Σ�
            //    int end_Node_tag = end_Node.TagValue;//��ֹ�����¼��tagֵ�����ö��������ĸ��±�Ķ���Σ�

            //    if (from_Node_tag == index_objPoly && end_Node_tag != index_objPoly)//�����ʼ������ѯ����Σ�����ֹ���������һ�������
            //    {
            //        if (!IndexList_neigbor.Contains(end_Node_tag))
            //        {
            //            IndexList_neigbor.Add(end_Node_tag);
            //        }
            //    }

            //    else if (from_Node_tag != index_objPoly && end_Node_tag == index_objPoly)//�����ֹ������ѯ����Σ�����ʼ���������һ�������
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
            //////////////////��ȡ���뽨����Ŀ�꣨�˴�Ϊ�����Ŀ�꣩//////////////////////
            ///�˴�����ش����װΪһ���������������ڸ�ֵ����m_PolyList��m_Envelope///////
            //////////////////////////////////////////////////////////////////////////////
            DataPrepare();
            //����ѡ�еĶ���ο���
            RefreshArea(m_Envelope);//ˢ�׻�ͼ����
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
            /////////////////Step2:��ʼ����//////////////////////////////
            /////////////////////////////////////////////////////////////               
            m_tin.InitNew(m_Envelope);//�����ʼ��
            bool is_success = m_tin.StartInMemoryEditing();//�����ڴ�༭ģʽ

            //��ȡ����ε㼯����װ�ص������������У���ɽ�������
            #region
            for (int i = 0; i < m_PolyList.Count; i++)
            {
                IPolygon pPolygon = m_PolyList[i];//��ö���ζ���

                pPolygon.Generalize(1);//��һ�����ص�ѹ������
                IPointCollection pPtInPolygon = pPolygon as IPointCollection;
                InsertPoint(pPtInPolygon, 30);//�Զ���α߽���нڵ����

                for (int k = 0; k < pPtInPolygon.PointCount - 1; k++)
                {
                    IPoint pt = pPtInPolygon.get_Point(k);
                    pt.Z = 0;//Zֵ��0

                    int tagValue = i;//tagValue�洢�õ��Ӧ�Ķ�����±�
                    ITinNode node = new TinNodeClass();
                    m_tin.AddPointZ(pt, tagValue, node);//���ν�����뵽TIN��
                }
            }
            #endregion


            //****************����������������**************������ʵ���㷨�ķָ��ߣ�

            //����������������
            //#region
            //for (int i = 1; i <= m_tin.TriangleCount; i++)
            //{
            //    ITinTriangle tinTriangle = m_tin.GetTriangle(i);
            //    if (!tinTriangle.IsInsideDataArea)//�ж��������Ƿ������ݷֲ�����֮��
            //        continue;
            //    IRgbColor pOutlineColor = new RgbColorClass();
            //    pOutlineColor.Red = 220; pOutlineColor.Green = 220; pOutlineColor.Blue = 220;
            //    DrawTinTriangle(tinTriangle, 2, null, pOutlineColor);
            //}
            //MessageBox.Show("TIN_Triangles");
            //#endregion

            //���������������ζ���
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

            //�������޼��������ıߣ������������ıߴ洢���б���
            #region
            List<ITinEdge> goodTinEdge = new List<ITinEdge>();  //�洢�����������������ߣ����ڷ���

            for (int i = 1; i <= m_tin.EdgeCount; i++)
            {
                ITinEdge oneEdge = m_tin.GetEdge(i);
                if (oneEdge.Length < 50)//ɸѡ���Ƚ�С����ֵ�ı�
                {
                    if (oneEdge.FromNode.TagValue != oneEdge.ToNode.TagValue)//���Ӳ�ͬ�Ķ���α߷���goodTinEdge��,��������һ���ߵ��������λ��������ͬ�Ķ����
                        goodTinEdge.Add(oneEdge);
                    //���Ʊ߿���
                    IRgbColor fillColor = new RgbColorClass();
                    fillColor.Red = 199; fillColor.Green = 203; fillColor.Blue = 241;
                    DrawTinEdge(oneEdge, 2, fillColor);
                }
            }
            MessageBox.Show("��֦���");
            #endregion

            //������ε������±�����һ��������
            #region
            List<int> m_PolyList_Tag = new List<int>();
            for (int i = 0; i < m_PolyList.Count; i++)
            {
                m_PolyList_Tag.Add(i);
            }
            #endregion

            //�������ж���Σ��������
            #region
            List<List<int>> groupedPolygons = new List<List<int>>(); //��ά����洢���������±�
            int c = 1;
            while (m_PolyList_Tag.Count > 0)
            {
                List<int> groupedPolygon = new List<int>();
                //�Ƚ�����ѯ����μ������У�ͬʱ�ڶ�����±��б���ɾ�����±�ֵ��ÿ�β�ѯ���һ��
                groupedPolygon.Add(m_PolyList_Tag[m_PolyList_Tag.Count - 1]);
                m_PolyList_Tag.Remove(m_PolyList_Tag[m_PolyList_Tag.Count - 1]);
                int current = 0;
                while (current != groupedPolygon.Count)//�������Ԫ��δ�������꣬���������꣬groupedPolygon.Count�Ͳ��������ӣ�current+1���ǡ����֮�����
                {
                    //int newnum = groupedpolygon.count - oldcount;
                    //oldcount = groupedpolygon.count;
                    GroupPolygon(groupedPolygon[current], goodTinEdge, m_PolyList_Tag, groupedPolygon);
                    current++;

                }
                groupedPolygons.Add(groupedPolygon);
                //MessageBox.Show("�������һ��");//Ӧ����22��
                //��������
                for (int i = 0; i < groupedPolygon.Count; i++)
                {
                    IRgbColor pColorPoly = new RgbColorClass();
                    pColorPoly.Red = 22 * c; pColorPoly.Green = 22 * (c+5); pColorPoly.Blue = 22 * (c+10);
                    DrawPolygon((m_PolyList[groupedPolygon[i]]), 2, null, pColorPoly);

                }
                c+=1;
            }
            MessageBox.Show("����ȫ�����");
            #endregion

            //��ȡ����ε㼯�����ն���η�������֯��Ⱥ������������
            #region
            List<List<IPoint>> pointLists = new List<List<IPoint>>();
            for (int i = 0; i < groupedPolygons.Count; i++)
            {
                //��ȡ������ε㼯
                #region
                List<IPoint> pointList = new List<IPoint>();
                List<int> groupedPolygon = groupedPolygons[i];//��ŵ�i��
                for (int j = 0; j < groupedPolygon.Count; j++)//��ÿ����е㼯�洢
                {
                    IPolygon pPolygon = m_PolyList[groupedPolygon[j]];//��ö���ζ���
                    pPolygon.Generalize(1);//��һ�����ص�ѹ������
                    IPointCollection pPtInPolygon = pPolygon as IPointCollection;
                    InsertPoint(pPtInPolygon, 30);//�Զ���α߽���нڵ����

                    for (int k = 0; k < pPtInPolygon.PointCount - 1; k++)
                    {
                        pointList.Add(pPtInPolygon.get_Point(k));

                    }
                }
                #endregion

                //�Ե㼯��������,ʹ����������С�ĵ�λ����λ
                #region
                pointList.Sort(delegate(IPoint p1, IPoint p2)
                {
                    int a = p1.Y.CompareTo(p2.Y);//����y��������
                    if (a == 0)
                        a = p1.X.CompareTo(p2.X);//y������ͬ�Ͱ�x����
                    return a;
                });
                #endregion

                //ˮƽ��Grahamɨ��
                #region
                Stack<IPoint> resPoint = new Stack<IPoint>();
                resPoint.Push(pointList[0]);
                resPoint.Push(pointList[1]);
                //����ɨ��
                for (int n = 2; n < pointList.Count; n++)//�Ե㼯��ÿһ������б���
                {
                    while (resPoint.Count >= 2)//�ж�pointList[n]�Ƿ���͹����
                    {
                        IPoint b = resPoint.Pop();//����ջ�����Ƴ�
                        IPoint a = resPoint.Peek();//����ջ�������Ƴ�
                        IPoint temp = pointList[n];
                        //����͹���ϵ������������Ȼ�����ʱ�뷽��
                        if (b.X == temp.X && b.Y == temp.Y)
                        {
                            break;
                        }
                        if (multi(a, b, temp) >= 0)//��Ҫ��ת��at��ab��ʱ�뷽��
                        {
                            resPoint.Push(b);
                            break;
                        }
                    }
                    resPoint.Push(pointList[n]);
                }

                //��pointList������ջ��Ԫ��ɾ����������ŵ�����ɨ��
                for (int n = pointList.Count - 1; n > 0; n--)//�������ɾ��Ԫ�أ����Թ���ǰ��ĵ㣬��Ϊ������Ҫ
                {
                    if (resPoint.Contains(pointList[n]))
                        pointList.RemoveAt(n);
                }
                //����ɨ��
                for (int n = pointList.Count - 1; n >= 0; n--)//�����������ΪresPoint�Ѿ��ڵ��������ĵط�
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
                        if (multi(a, b, temp) >= 0)//��Ҫ��ת
                        {
                            resPoint.Push(b);
                            break;
                        }
                    }
                    resPoint.Push(pointList[n]);
                }

                resPoint.Pop();//������ĵ��ǵ�0���㣬ջ�����У�����ȥ
                #endregion

                pointList.Clear();
                while (resPoint.Count != 0)
                {
                    pointList.Add(resPoint.Pop());
                }
                pointLists.Add(pointList);
            }
            MessageBox.Show("ɸѡ���");
            #endregion

            //�����
            #region
            List<IPolygon> newPolygonList = new List<IPolygon>();
            for (int i = 0; i < pointLists.Count; i++)
            {
                List<IPoint> pointList = pointLists[i];
                IPolygon newPolygon = PointToPolygon(pointList);
                newPolygonList.Add(newPolygon);
            }
            MessageBox.Show("ת�����");

            //�����ϲ���Ķ����
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
            //��õ�ǰͼ����Ϣ����ͼ�������Ŀ�������ǽ�������Ķ���
            ILayer pLayer = DApplication.App.currentLayer;
            if (pLayer == null)
            {
                MessageBox.Show("��ѡ��Ŀ��ͼ�㣡");
                return;
            }
            m_pFeatureLayer = pLayer as IFeatureLayer;


            //��ʵʩ���潨���ȹ���ǰ���Զ���Ĳ�����Ա�����������
            if (m_PolyList.Count > 1) m_PolyList.Clear();
            if (m_tin != null) m_tin.SetEmpty();//��������������
        }

        /// <summary>
        /// 
        /// </summary>
        public void DataPrepare()
        {
            IFeatureClass pFeatureClass = m_pFeatureLayer.FeatureClass;//��ͼ����Ϣ���Ҫ�ؼ�����Ϣ
            IGeoDataset pGeoDataset = pFeatureClass as IGeoDataset;
            m_Envelope = pGeoDataset.Extent;//������ݼ�Ŀ��ֲ��ķ�Χ��Ϣ

            //��õ�ǰѡ��ͼ���еĶ����Ŀ��
            IMap currentMap = (m_application.Document as IMxDocument).ActivatedView.FocusMap;
            IEnumFeatureSetup pEnumFeatureSetup = currentMap.FeatureSelection as IEnumFeatureSetup;
            IEnumFeature pfeatureList = pEnumFeatureSetup as IEnumFeature;
            pfeatureList.Reset();

            IFeature pfeature = pfeatureList.Next();
            while (!(pfeature == null))
            {
                if ((pfeature.Class as IFeatureClass).FeatureClassID != pFeatureClass.FeatureClassID)//�ж�ѡ���Ҫ��Ŀ���Ƿ����ڵ�ǰͼ��
                    continue;
                if (pfeature.ShapeCopy.GeometryType != esriGeometryType.esriGeometryPolygon)//�ж�ѡ���Ҫ��Ŀ�꼸�������Ƿ��Ƕ����
                    continue;
                m_PolyList.Add(pfeature.Shape as IPolygon);
                pfeature = pfeatureList.Next();
            }

            if (m_PolyList.Count < 1)
            {
                MessageBox.Show("��ѡ��Ҫ��Ŀ�꣡");
                return;
            }    
        }
         
        /// <summary>
        /// ����һ������������
        /// </summary>
        /// <param name="oneNode"></param>���Ƶ��������������
        /// <param name="nSize"></param>��С
        /// <param name="pColor"></param>��ɫ
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
        /// ����һ�������������α�
        /// </summary>
        /// <param name="oneTinEdge"></param>�����α�
        /// <param name="nWidth"></param>�߿�
        /// <param name="pColor"></param>��ɫ
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
        /// ����һ��������������
        /// </summary>
        /// <param name="oneTinTriangle"></param>������
        /// <param name="nWidth"></param>�߿�
        /// <param name="pFillColor"></param>���ɫ
        /// <param name="pOutlineColor"></param>�߽�ɫ
        public void DrawTinTriangle(ITinTriangle oneTinTriangle, int nWidth, IRgbColor pFillColor, IRgbColor pOutlineColor)
        {
            if (oneTinTriangle == null)
                return;

            IRing pRing = new RingClass();
            oneTinTriangle.QueryAsRing(pRing);  //��������������ת��ΪIRing����            
            IGeometry g = pRing as IGeometry;
            PolygonClass pPolygon = new PolygonClass();
            pPolygon.AddGeometries(1, ref g);
            DrawPolygon(pPolygon as IPolygon, nWidth, pFillColor, pOutlineColor);
        }

        /// <summary>
        /// ����һ�������
        /// </summary>
        /// <param name="pt"></param>��
        /// <param name="nSize"></param>�ߴ�
        /// <param name="pColor"></param>��ɫ
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
        /// ����һ������
        /// </summary>
        /// <param name="pLine"></param>�����Ƶ���
        /// <param name="nWidth"></param>���ƿ��
        /// <param name="pColor"></param>������ɫ
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
        /// ����һ�������
        /// </summary>
        /// <param name="pPoly"></param>�����ƵĶ����
        /// <param name="nWidth"></param>���ƿ��
        /// <param name="pFillColor"></param>�����ɫ
        /// <param name="pOutlineColor"></param>�߽���ɫ
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
        /// ��ĳһ�����������ˢ��
        /// </summary>
        /// <param name="pEnvelope">ˢ������</param>
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
        /// �Ե㴮���м���
        /// </summary>
        /// <param name="ptList"></param>����ĵ㴮����
        /// <param name="Max_length"></param>���ܺ����ڽڵ����������ֵ
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
        /// ��������֮��ľ���
        /// </summary>
        /// <param name="fromPt"></param>��ʼ��
        /// <param name="toPt"></param>��ֹ��
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
        /// ����ָ�������ڽӵ�������
        /// </summary>
        /// <param name="node"></param>�������ѯ������������
        /// <returns></returns>���صĲ�ѯ���
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
        /// ����ָ���ߵ�����������
        /// </summary>
        /// <param name="edge">������������ĳ����</param>
        /// <returns></returns>���صĲ�ѯ���
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
        /// ����ĳ���������ڽӵ�������
        /// </summary>
        /// <param name="triangle"></param>����ѯ��������
        /// <returns></returns>���صĲ�ѯ���
        private List<ITinTriangle> GetIncidentTriangle(ITinTriangle triangle)
        {
            List<ITinTriangle> triangleList = new List<ITinTriangle>();
            //��������������ڽӵ����������εı�ţ����ڽ������β��������Ӧ out Ϊ0
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
        /// ����ĳ������β�ѯ��ö�����໥�ڽӵĶ���Σ������Ǵ洢��IndexList_neigbor�дӶ���Ϊһ��
        /// </summary>
        /// <param name="index_objPoly"></param>����ѯ������±�
        /// <param name="goodTinEdge"></param>������������������
        /// <param name="m_PolyList_Tag"></param>�洢��δ���������±���б�
        /// <param name="IndexList_neigbor"></param>�洢�ڽӶ�����±�
        public void GroupPolygon(int index_objPoly, List<ITinEdge> goodTinEdge, List<int> m_PolyList_Tag, List<int> IndexList_neigbor)
        {
            for (int i = goodTinEdge.Count-1; i >= 0; i--)
            {
                ITinEdge oneEdge = goodTinEdge[i];//����������е������������αߣ���Ϊ��ɾ������
                if (!oneEdge.IsInsideDataArea)
                    continue;
                ITinNode from_Node = oneEdge.FromNode;//��øñߵ���ʼ����ֹ����
                ITinNode end_Node = oneEdge.ToNode;
                int from_Node_tag = from_Node.TagValue;//��ʼ�����¼��tagֵ(���ö��������ĸ��±�Ķ����)
                int end_Node_tag = end_Node.TagValue;//��ֹ�����¼��tagֵ�����ö��������ĸ��±�Ķ���Σ�

                if (from_Node_tag == index_objPoly && end_Node_tag != index_objPoly)//��������һ���ߵ�from_node���ڵĶ�������ڴ���ѯ����Σ�end_node�����ڣ����end_node���ڵĶ���μ����ڽӶ��������
                {
                    if (!IndexList_neigbor.Contains(end_Node_tag))
                    {
                        IndexList_neigbor.Add(end_Node_tag);
                        m_PolyList_Tag.Remove(end_Node_tag);//���ѷ���Ķ���ζ�Ӧ�±��Ƴ�
                        goodTinEdge.Remove(goodTinEdge[i]);//һ����ֻ��ȷ��һ�Զ���ε��ڽӹ�ϵ��������ȷ���ڽӹ�ϵ���֮��ı��������壬����ɾ��
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
        /// ����������ab��ap�����Ŀ���ǵó���p������ab���ĸ����򣬴���0��ʱ�룬С��0˳ʱ��
        /// </summary>
        /// <param name="a"></param>����ab���
        /// <param name="b"></param>����ab�յ�
        /// <param name="p"></param>�����p
        /// <returns></returns>���ز�����
        public double multi(IPoint a, IPoint b, IPoint p)
        {
            return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
        }

        /// <summary>
        /// ����Ⱥת��Ϊ����Σ����ض���ζ���
        /// </summary>
        /// <param name="pts"></param>��ɶ���εĵ㼯
        public IPolygon PointToPolygon(List<IPoint> pts)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;
            for (int i = 0; i < pts.Count; i++)//��ת��Ϊ��
            {
                ring1.AddPoint(pts[i], ref missing, ref missing);
            }
            IGeometryCollection pointPolygon = new PolygonClass();//���弸�����
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);//��ӻ�
            IPolygon polyGonGeo = pointPolygon as IPolygon;
            polyGonGeo.SimplifyPreserveFromTo();//���棬���ֻ���ʼ�����λ��
            return polyGonGeo;
        }
    }
}
