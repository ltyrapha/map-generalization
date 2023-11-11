using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.SystemUI;
using System.Windows.Forms;
using System.Collections.Generic;

namespace CreatTin
{
    /// <summary>
    /// Command that works in ArcMap/Map/PageLayout
    /// </summary>
    [Guid("bee0390a-c1e2-4991-bdc0-82712a210752")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("CreatTin.Command1")]
    public sealed class Command1 : BaseCommand,IToolControl
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

        private IHookHelper m_hookHelper = null;
        private IApplication m_application;
        private ComboBox cb;
        private Dictionary<string, ILayer> dclayers;
        IMap map = null;

        public Command1()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = ""; //localizable text
            base.m_caption = "";  //localizable text 
            base.m_message = "下拉框，选择待处理的GPS轨迹数据层";  //localizable text
            base.m_toolTip = "图层选择";  //localizable text
            base.m_name = this.GetType().ToString();   //unique id, non-localizable (e.g. "MyCategory_MyCommand")

            cb = new ComboBox();
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            // cb.Font = new System.Drawing.Font("微软雅黑", 9.0F);
            cb.Size = new System.Drawing.Size(120, 27);
            cb.SelectedIndexChanged += new EventHandler(cb_SelectedIndexChanged);
            cb.Click += new EventHandler(cb_Click);
            dclayers = new Dictionary<string, ILayer>();

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

        void cb_SelectedIndexChanged(object sender, EventArgs e)
        {
            DApplication.App.currentLayer = dclayers[cb.SelectedItem.ToString()] as ILayer;
        }

        void cb_Click(object sender, EventArgs e)
        {
            this.OnClick();
            this.cb.BackColor = System.Drawing.Color.AliceBlue;
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
            m_application = hook as IApplication;
            try
            {
                m_hookHelper = new HookHelperClass();
                m_hookHelper.Hook = hook;
                if (m_hookHelper.ActiveView == null)
                    m_hookHelper = null;
            }
            catch
            {
                m_hookHelper = null;
            }

            if (m_hookHelper == null)
                base.m_enabled = false;
            else
                base.m_enabled = true;

            // TODO:  Add other initialization code
        }

        /// <summary>
        /// Occurs when this command is clicked
        /// </summary>
        public override void OnClick()
        {
            // TODO: Add Command1.OnClick implementation
            if (m_application != null)
            {
                map = (m_application.Document as IMxDocument).ActiveView.FocusMap;

                IEnumLayer layers = map.get_Layers();
                ILayer layer = null; layers.Reset();
                dclayers = new Dictionary<string, ILayer>();
                this.cb.Items.Clear();
                while ((layer = layers.Next()) != null)
                {
                    if (!layer.Visible)
                        continue;
                    if (!dclayers.ContainsKey(layer.Name))
                    {
                        dclayers.Add(layer.Name, layer);
                        this.cb.Items.Add(layer.Name);
                    }
                }
                /*
                BindingSource bs = new BindingSource();
                bs.DataSource = dclayers;
                this.cb.DataSource = new BindingSource(dclayers, null);
                this.cb.DisplayMember = "Key";
                this.cb.ValueMember = "Value";
                */

                if (this.cb.Items.Count > 0)
                {
                    this.cb.SelectedIndex = 0;
                    cb_SelectedIndexChanged(this.cb, new EventArgs());
                }
            }
        }

        #endregion
        public bool OnDrop(esriCmdBarType barType)
        {
            return true;
            //throw new NotImplementedException();
        }

        public void OnFocus(ICompletionNotify complete)
        {
            //throw new NotImplementedException();
        }

        public int hWnd
        {
            get { return cb.Handle.ToInt32(); }
        }
    }
}
