using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smartCircuitsVeabim
{
    public class onCheckCircuits : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            app.Application.DocumentOpened += Application_DocumentOpened; ;
        }

        private void Application_DocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
        {
            Application app = sender as Application;
            UIApplication uiApp = new UIApplication(app);
            Document doc = uiApp.ActiveUIDocument.Document;
            Utilits.Doc = doc;
            //app.DocumentChanged += new EventHandler<DocumentChangedEventArgs>(OnDocumentChanged);

            //Создание словаря
            MapConnectorsToCircuits dicConnCirc = new MapConnectorsToCircuits();
            //Develop.dicConnectCirc = dicConnCirc;

            //Перебор всех коннекторов для заполнения коннекторов
            //ElementId idConnect = ElementId.InvalidElementId;
            //string idCircuits = "";

            List<ElementId> circuitsId = new List<ElementId>();

            FilteredElementCollector filtConnectors = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TelephoneDevices).WhereElementIsNotElementType();
            foreach (var el in filtConnectors)
            {
                string strIdCirc = el.LookupParameter("ID_Circuits").AsString();
                //Проверка если в коннекторе нет цепей
                if (strIdCirc != null)
                {
                    string[] spl = strIdCirc.Split('?');
                    for (int i = 0; i < spl.Length; i++)
                    {
                        string strid = spl[i];
                        if (Int32.TryParse(strid, out int intId))
                        {
                            ElementId idCirc = new ElementId(Int32.Parse(strid));
                            dicConnCirc.Add(el.Id, idCirc);
                            circuitsId.Add(idCirc);
                        }

                    }
                }

            }

            Develop.dicConnectCirc = dicConnCirc;
            CircUpdater updater = new CircUpdater(uiApp.ActiveAddInId);
            Develop.updater = updater;
            //Регистрация апдатера
            if (!UpdaterRegistry.IsUpdaterRegistered(updater.GetUpdaterId())) UpdaterRegistry.RegisterUpdater(updater);

            if (circuitsId.Count != 0)
            {
                //Добавление триггера на электрические цепи

                Element elem = doc.GetElement(circuitsId.First());
                Parameter parNumber = elem.get_Parameter(BuiltInParameter.RBS_ELEC_CIRCUIT_NUMBER);
                //Parameter parIdConn = elem.GetParameters("IdConnectors").First();
                UpdaterRegistry.AddTrigger(updater.GetUpdaterId(), doc, circuitsId, Element.GetChangeTypeParameter(parNumber));
                //UpdaterRegistry.AddTrigger(updater.GetUpdaterId(), doc, circuitsId, Element.GetChangeTypeParameter(parIdConn));

                UpdaterRegistry.AddTrigger(updater.GetUpdaterId(), doc, circuitsId, Element.GetChangeTypeElementDeletion());


                updater.dic = dicConnCirc;
            }
            if (filtConnectors.Count() != 0)
            {
                //добавление триггера на коннекторы
                //ElementCategoryFilter filt = new ElementCategoryFilter(BuiltInCategory.OST_TelephoneDevices);
                IList<FilterRule> ruls = new List<FilterRule>();
                FilterRule rule = ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.ELEM_FAMILY_PARAM), "Эмуляция потребителя без нагрузки", false);
                ruls.Add(rule);
                rule = ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInCategory.OST_TelephoneDevices), (int)BuiltInCategory.OST_TelephoneDevices);
                ruls.Add(rule);
                ElementParameterFilter filter = new ElementParameterFilter(ruls);
                //FilteredElementCollector collector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TelephoneDevices).WherePasses(filter);

                //var fs = new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(x => x.Name == "Bunch");
                UpdaterRegistry.AddTrigger(updater.GetUpdaterId(), doc, filter, Element.GetChangeTypeElementDeletion());
            }
        }
        public string GetName()
        {
            throw new NotImplementedException();
        }
    }
}
