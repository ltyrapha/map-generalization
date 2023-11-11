using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.ConversionTools;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.esriSystem;

namespace CreatTin
{
    public class DApplication
    {
        public static DApplication App = new DApplication();
        public static GeometryEnvironmentClass GeometryEnvironment = new GeometryEnvironmentClass();
        public Geoprocessor Geoprosessor { get; internal set; }
        DApplication()
        {
            string tempPath = System.Environment.GetEnvironmentVariable("TEMP");
            System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(tempPath);
            tempPath = info.FullName;
            IWorkspaceFactory shpfileWSF = new ShapefileWorkspaceFactoryClass();
            TempWorkspace = shpfileWSF.OpenFromFile(tempPath, 0);
            Geoprosessor = new ESRI.ArcGIS.Geoprocessor.Geoprocessor();
            currentLayer = null;
            trackID = 1;
        }

        public IWorkspace TempWorkspace { get; private set; }

        public ILayer currentLayer { get; set; }

        public int trackID { get; set; }
        
        /// <summary>
        /// 递归得到所有图层
        /// </summary>
        /// <param name="layer">ICompositeLayer</param>
        /// <param name="condition">条件，为空的时候获得所有不是群组图层（ICompositeLayer）的层</param>
        /// <returns>递归所有图层</returns>
        internal static ILayer[] GetAllLayer(ICompositeLayer layer, Func<ILayer, bool> condition = null)
        {
            List<ILayer> ls = new List<ILayer>();

            for (int i = 0; i < layer.Count; i++)
            {
                ILayer l = layer.get_Layer(i);

                if (l is ICompositeLayer)
                {
                    ls.AddRange(GetAllLayer(l as ICompositeLayer, condition));
                }
                else if (condition == null || condition(l))
                {
                    ls.Add(l);
                }
            }
            return ls.ToArray();
        }


    }
}
