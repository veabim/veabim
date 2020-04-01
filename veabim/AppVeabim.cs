using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace veabim
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AppVeabim : IExternalApplication
    {
        private string pathFolder;
        private string pathFolderGravion;
        private string funcPath;
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            AddInId m_appId = new AddInId(new Guid("F2386B36-453D-4D08-A03B-2C5F78DF0421"));
            string assemblyfullfilepath = Assembly.GetExecutingAssembly().Location;
            string assamblyPath = Path.GetDirectoryName(assemblyfullfilepath);
            funcPath = Directory.GetDirectories(assamblyPath).Where(x => x.Contains("VEABIM")).FirstOrDefault();
            AddMenu(application, assemblyfullfilepath);
            return Result.Succeeded;
        }

        private void AddMenu(UIControlledApplication application, string assemblyfullfilepath)
        {
            #region Создание вкладки в ленте
            String tabName = "VEABIM";
            application.CreateRibbonTab(tabName);
            #endregion

            var panelExt_1 = application.CreateRibbonPanel(tabName, "Инструменты");
            panelExt_1.Enabled = true;
            panelExt_1.Visible = true;
            panelExt_1.AddItem(new PushButtonData("Умные цепи", "Цепи", Path.Combine(pathFolder, "smartCircuitsVeabim.dll"), "smartCircuitsVeabim.smartCircuitsVeabimCmd"));
            panelExt_1.AddSeparator();
            panelExt_1.AddItem(new PushButtonData("Создание однолинейных схем", "Создать", Path.Combine(pathFolder, "oneLineDiagramVeabim.dll"), "oneLineDiagramVeabim.oneLineDiagramVeabimCmd"));
            panelExt_1.AddSeparator();
            panelExt_1.AddItem(new PushButtonData("ZZZZZ", "ZZZ ZZ", Path.Combine(pathFolder, "zzzzzzzzzVeabim.dll"), "zzzzzzzzzzVeabim.zzzzzzzzzCmd"));
        }
    }
}
