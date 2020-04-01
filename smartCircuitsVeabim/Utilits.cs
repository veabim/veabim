using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.ExtensibleStorage;
using System.Runtime.InteropServices;

namespace smartCircuitsVeabim
{
    public class Utilits
    {
        private static Document _doc;
        public static Document Doc
        {
            set
            {
                if (value is Document)
                {
                    _doc = value;
                }
            }
        }
        public static List<Element> GetElectricalEquipment(Document doc)
        {
            FilteredElementCollector collector = GetElementsOfType(doc, typeof(FamilyInstance), BuiltInCategory.OST_ElectricalEquipment);
            //возращаем список вместо IList
            return new List<Element>(collector.ToElements());
        }
        public static FilteredElementCollector GetElementsOfType(Document doc, Type type, BuiltInCategory bic)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(bic);
            collector.OfClass(type);
            return collector;
        }
        public static string PluralSuffix(int n)
        {
            return 1 == n ? "" : "ов";
        }
        public static string DotOrColon(int n)
        {
            return 0 == n ? "." : ":";
        }
        // Возвращает строку описания, включая идентификатор элемента для данного элемента.
        public static string ElementDescriptionAndId(Element e)
        {
            string description = e.GetType().Name;
            if (null != e.Category)
            {
                description += " " + e.Category.Name;
            }
            string identity = e.Id.IntegerValue.ToString();
            if (null != e.Name)
            {
                identity = e.Name + " " + identity;
            }
            return string.Format("{0} <{1}>", description, identity);
        }
        //Возращает строку описания элемента для конечного узла браузера электрической системы.
        public static string BrowserDescription(Element e)
        {
            //проверка на экземпляр семейства
            FamilyInstance inst = e as FamilyInstance; //если элемент экземпляр семейства, то получаем имя семейства + имя экземпляра  
            return (null == inst ? e.Category.Name : inst.Symbol.Family.Name) + " " + e.Name; // , если нет, то получаем имя категории 
        }
        /// <summary>
        /// Устанавливает коэффициент запаса длинны кабеля
        /// </summary>
        private static void FillLen(FilteredElementCollector allCircuits, double k_len)
        {
            foreach (Element circuit in allCircuits)
            {
                Parameter k_l = circuit.LookupParameter("k_запаса длины кабеля");
                if (k_l.AsDouble() == 0)
                    k_l.Set(k_len);
            }
        }
        /// <summary>
        /// Получения отсортированного списка цепей щита
        /// </summary>
        public static IEnumerable<ElectricalSystem> GetSortedCircuits(FamilyInstance board, out ElectricalSystem circBoard)
        {
            circBoard = null;
            ElectricalSystemSet fullCircuits = board.MEPModel.ElectricalSystems; //Получение всех цепей щита
            IList<ElectricalSystem> sortCircuit = new List<ElectricalSystem>();
            string boardName = board.Name;
            foreach (ElectricalSystem circ in fullCircuits)
            {
                string s = circ.PanelName;
                if (s == boardName) sortCircuit.Add(circ);
                else circBoard = circ;
            }
            return sortCircuit.OrderBy(x => x.StartSlot);
        }
        //Метод для updater
        public static void UpdateConnectorsAll(MapConnectorsToCircuits dic, List<ElementId> lis)
        {
            foreach (ElementId idConn in lis)
            {
                string idCircuitsStr = "";
                List<string> sort = new List<string>();
                foreach (ElementId idCirc in dic[idConn])
                {
                    var el = _doc.GetElement(idCirc);
                    if (el == null) continue;
                    string s = el.Name;
                    sort.Add(s);
                    string idCircStr = idCirc.ToString() + "?";
                    idCircuitsStr += idCircStr;
                }
                if (sort == null) continue;
                sort.Sort();
                string namesCircuits = string.Join(" ", sort.ToArray());
                Element connector = _doc.GetElement(idConn);
                connector.LookupParameter("Комментарии").Set(namesCircuits);
                connector.LookupParameter("ID_Circuits").Set(idCircuitsStr);
            }
        }
        //Метод для немедленного обновления (сразу при назначении коннектора)
        public static void UpdateConnectorsAll(MapConnectorsToCircuits dic, IList<Reference> lis)
        {
            foreach (Reference idConn in lis)
            {
                //создание точек локаций коннекторов
                double dos = 100000000;
                double dis = 0;
                double d;
                XYZ p = new XYZ(-25, 20, 0);
                List<string> sort = new List<string>();
                string idCircuitsStr = "";
                //List<string> idCircuits = new List<string>();
                foreach (ElementId idCirc in dic[idConn.ElementId])
                {
                    ElectricalSystem circ = _doc.GetElement(idCirc) as ElectricalSystem;
                    //Проверка. Если элемента с этим ID не существует
                    if (circ == null) continue;
                    var pointsCirc = circ.GetCircuitPath();
                    string s = circ.Name;
                    sort.Add(s);
                    string idCircStr = idCirc.ToString() + "?";
                    idCircuitsStr += idCircStr;
                }
                if (sort == null) continue;
                sort.Sort();
                string namesCircuits = string.Join(" ", sort.ToArray());
                Element connector = _doc.GetElement(idConn);
                connector.LookupParameter("Комментарии").Set(namesCircuits);
                connector.LookupParameter("ID_Circuits").Set(idCircuitsStr);
            }
        }
        public static void UpdateConnectors(MapConnectorsToCircuits dic, List<ElementId> lis)
        {
            foreach (ElementId idConn in lis)
            {
                List<string> sort = new List<string>();
                foreach (ElementId idCirc in dic[idConn])
                {
                    string s = _doc.GetElement(idCirc).Name;
                    sort.Add(s);
                }
                sort.Sort();
                int nSort = sort.Count;
                Element connector = _doc.GetElement(idConn);

                for (int i = 1; i < 4; i++)
                {
                    if (nSort - i >= 0)
                    {
                        connector.LookupParameter($"SC_Circuit_{i}").Set(sort[i - 1]);
                    }
                    else
                    {
                        connector.LookupParameter($"SC_Circuit_{i}").Set("");
                    }
                }
            }
        }
        public static void UpdateConnectors(MapConnectorsToCircuits dic, IList<Reference> lis)
        {
            foreach (Reference idConn in lis)
            {
                List<string> sort = new List<string>();
                foreach (ElementId idCirc in dic[idConn.ElementId])
                {

                    sort.Add(_doc.GetElement(idCirc).Name);
                }
                sort.Sort();
                Element connector = _doc.GetElement(idConn);


                for (int i = 1; i < 4; i++)
                {
                    if (sort.Count - i >= 0)
                    {
                        connector.LookupParameter($"SC_Circuit_{i}").Set(sort[i - 1]);
                    }
                    else
                    {
                        connector.LookupParameter($"SC_Circuit_{i}").Set("");
                    }
                }
            }
        }
        public static IEnumerable<ElectricalSystem> GetSortedCircuits(FamilyInstance board)
        {
            ElectricalSystemSet fullCircuits = board.MEPModel.ElectricalSystems; //Получение всех цепей щита
            IList<ElectricalSystem> sortCircuit = new List<ElectricalSystem>();
            string boardName = board.Name;
            foreach (ElectricalSystem circ in fullCircuits)
            {
                string s = circ.PanelName;
                if (s == boardName) sortCircuit.Add(circ);
            }
            return sortCircuit.OrderBy(x => x.StartSlot);
        }
        public static IEnumerable<ElectricalSystem> GetCircuits(FamilyInstance board)
        {
            ElectricalSystemSet fullCircuits = board.MEPModel.ElectricalSystems; //Получение всех цепей щита
            return fullCircuits.Cast<ElectricalSystem>();
        }
        /// <summary>
        /// Получения отсортированного списка цепей щита
        /// </summary>
        public static IEnumerable<ElectricalSystem> GetSortedCircuits2(FamilyInstance board, out int circuitNaming)
        {
            int names_circuits = board.get_Parameter(BuiltInParameter.RBS_ELEC_CIRCUIT_NAMING).AsInteger();
            string nameCircuitSplit = null; //Разделитель обозначения цепей
            switch (names_circuits)
            {
                case 2:
                    string namePanel = board.get_Parameter(BuiltInParameter.RBS_ELEC_PANEL_NAME).AsString();
                    string separatorPrefix = board.get_Parameter(BuiltInParameter.RBS_ELEC_CIRCUIT_PREFIX_SEPARATOR).AsString();
                    nameCircuitSplit = namePanel + separatorPrefix;
                    break;
                case 0:
                    string prefixCircuits = board.get_Parameter(BuiltInParameter.RBS_ELEC_CIRCUIT_PREFIX).AsString();
                    separatorPrefix = board.get_Parameter(BuiltInParameter.RBS_ELEC_CIRCUIT_PREFIX_SEPARATOR).AsString();
                    nameCircuitSplit = prefixCircuits + separatorPrefix;
                    break;
                default:
                    TaskDialog.Show("Debug", "Не удалось получить наименование цепей");
                    break;
            }
            int lenName = nameCircuitSplit.Length;
            ElectricalSystemSet fullCircuits = board.MEPModel.ElectricalSystems; //Получение всех цепей щита
            SortedList<int, ElectricalSystem> sortCirc = new SortedList<int, ElectricalSystem>();
            foreach (ElectricalSystem circ in fullCircuits)
            {
                string nameCirc = circ.get_Parameter(BuiltInParameter.RBS_ELEC_CIRCUIT_NUMBER).AsString();
                bool b = nameCirc.Contains(nameCircuitSplit);
                //info.AppendFormat("\n{0}, {1}", b, circ.Name);
                if (nameCirc.Contains(nameCircuitSplit))
                {
                    string _numStr = nameCirc.Remove(0, lenName);
                    int index = _numStr.IndexOf(","); // если группа трёхфазная, то возратится значение > 0
                    string numStr = (index < 0) ? _numStr : _numStr.Remove(index);

                    int numCirc;
                    bool success = Int32.TryParse(numStr, out numCirc);
                    if (success)
                    {
                        sortCirc.Add(numCirc, circ);
                    }
                    else TaskDialog.Show("Debug", $"{circ.Name}");
                }
            }
            circuitNaming = names_circuits;
            return sortCirc.Values; //отсортированные цепи
        }
        /// <summary>
        /// Получение значения автомаркировки
        /// </summary>
        public static bool GetAutonew(FamilyInstance board)
        {
            int autonew = board.LookupParameter("TS_Автомаркировка электрических устройств щита").AsInteger();
            bool hasval = board.LookupParameter("TS_Автомаркировка электрических устройств щита").HasValue;
            if (!hasval) { autonew = 1; }
            bool an = autonew == 1;
            return an;
        }
        /// <summary>
        /// Получение элементов ключевой спецификации по её имени
        /// </summary>
        public static Element ViewKeyElement(string nameTabl)
        {
            FilterRule rule = ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.VIEW_NAME), nameTabl, false);
            var filter = new ElementParameterFilter(rule);
            FilteredElementCollector keysp = new FilteredElementCollector(_doc);
            keysp.OfClass(typeof(ViewSchedule)).WherePasses(filter);
            if (keysp.Count() != 1) return null;

            //var sd = filter.PassesFilter(keysp.FirstElement());
            return keysp.FirstElement();
        }
        /// <summary>
        /// Перевод милеметров в футы
        /// </summary>
        public static double Ft(double mm)
        {
            return (mm * 0.001) * (1 / 0.3048);
        }
        /// <summary>
        /// Перевод вольтампер в число
        /// </summary>
        public static double VA_D(double va)
        {
            return UnitUtils.ConvertFromInternalUnits(va, DisplayUnitType.DUT_KILOVOLT_AMPERES);
        }
        /// <summary>
        /// Получение экземпляра типа семейства элемента (ноф ФамилиТайп)
        /// </summary>
        public static Element FamType(Family fam, string name)
        {
            Element ans = null;
            var id = fam.GetFamilySymbolIds();
            foreach (ElementId i in id)
            {
                Element el = _doc.GetElement(i);
                string s = el.get_Parameter(BuiltInParameter.SYMBOL_NAME_PARAM).AsString();
                if (s == name)
                {
                    ans = el;
                    break;
                }
            }
            return ans;
        }
        /// <summary>
        /// Получучение типа семейства аннотации
        /// </summary>
        public static AnnotationSymbolType GetFamAn(string name)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc);
            collector.OfCategory(BuiltInCategory.OST_GenericAnnotation);
            collector.WhereElementIsElementType();

            AnnotationSymbolType fam = null;
            foreach (Element el in collector)
            {
                AnnotationSymbolType elAnSType = el as AnnotationSymbolType;
                string famName = elAnSType.Family.Name;
                if (famName == name)
                {
                    fam = elAnSType;
                    break;
                }
            }
            return fam;
        }
        /// <summary>
        /// Получение или создание семейства аннотации аппарата в линии
        /// </summary>
        public static AnnotationSymbolType GreateFamElSh(AnnotationSymbolType famElsh, IList<string> _done, Element elMainTable)
        {
            string name_ap_z = elMainTable.LookupParameter("KAV.SP.Тип, марка, обозначение документа").AsString();
            Debug.Assert(name_ap_z != null, "Необходимо заполнить параметр: KAV.SP.Тип, марка, обозначение документа");
            Element apZ = FamType(famElsh.Family, name_ap_z);
            if (apZ == null)
            {
                AnnotationSymbolType newApZ = FamType(famElsh.Family, "—") as AnnotationSymbolType;
                apZ = newApZ.Duplicate(name_ap_z);
            }
            if (!_done.Contains(apZ.Name))
            {
                _done.Add(apZ.Name);
                string nazn_ap_z = elMainTable.LookupParameter("KAV.Назначение аппарата защиты").AsString();
                string tip_ap_zz = elMainTable.LookupParameter("KAV.Mar.Тип аппарата защиты").AsString();
                string tok_rasc_ap_z = elMainTable.LookupParameter("KAV.Mar.Ток уставки расцепителя").AsString();
                int kol_pol_ap_z = elMainTable.LookupParameter("KAV.Mar.Количество полюсов аппарата защиты").AsInteger();
                string pred_kom_st_ap_z = elMainTable.LookupParameter("KAV.Mar.Предельная коммутационная стойкость").AsString();
                string tok_har = elMainTable.LookupParameter("KAV.Mar.Токовая характеристика").AsString();
                string tok_har_ap_z = tok_har != "-" ? tok_har : "";

                apZ.LookupParameter("УГО.Mar.Тип аппарата защиты").Set(tip_ap_zz);
                apZ.LookupParameter("УГО.Mar.Ток уставки расцепителя").Set(tok_rasc_ap_z);
                apZ.LookupParameter("УГО.Mar.Количество полюсов аппарата защиты").Set(kol_pol_ap_z.ToString());
                apZ.LookupParameter("УГО.Mar.Предельная коммутационная стойкость").Set(pred_kom_st_ap_z);
                apZ.LookupParameter("УГО.Mar.Токовая характеристика").Set(tok_har_ap_z);


                int naznAp = 0;
                try { naznAp = apZ.LookupParameter(nazn_ap_z).AsInteger(); } // проверка у семейства аннотации видимость 
                catch (NullReferenceException)
                {
                    string message = string.Format("В семействе аннотации: {0}, отсутствует параметр видимости назначения аппарата: {1}, для типа {2}",
                        famElsh.Family.Name, nazn_ap_z, name_ap_z);
                    throw new NullReferenceException(message.ToString());
                }

                if (naznAp != 1)
                {
                    apZ.LookupParameter("диф").Set(0);
                    apZ.LookupParameter("Амперметр").Set(0);
                    apZ.LookupParameter("Вольтметр").Set(0);
                    apZ.LookupParameter("конденсатор").Set(0);
                    apZ.LookupParameter("мфип_ки").Set(0);
                    apZ.LookupParameter("счетчик_ки").Set(0);
                    apZ.LookupParameter("фильтр").Set(0);
                    apZ.LookupParameter("узо").Set(0);
                    apZ.LookupParameter("узип").Set(0);
                    apZ.LookupParameter("трансформаторы тока").Set(0);
                    apZ.LookupParameter("счетчик").Set(0);
                    apZ.LookupParameter("рубильник").Set(0);
                    apZ.LookupParameter("предохранитель").Set(0);
                    apZ.LookupParameter("переключатель").Set(0);
                    apZ.LookupParameter("силовой контакт пускателя").Set(0);
                    apZ.LookupParameter("независимый расцепитель").Set(0);
                    apZ.LookupParameter("мфип").Set(0);
                    apZ.LookupParameter("магнитный пускатель").Set(0);
                    apZ.LookupParameter("испытательная коробка").Set(0);
                    apZ.LookupParameter("выключатель").Set(0);
                    apZ.LookupParameter("привод").Set(0);
                    apZ.LookupParameter(nazn_ap_z).Set(1);
                }

            }
            AnnotationSymbolType famAn = apZ as AnnotationSymbolType;
            return famAn;
        }
        /// <summary>
        /// Наименование цепей и номеров автоматов
        /// </summary>
        public static void Naming(FamilyInstance board)
        {
            string schemaGuid = "720080CB-DA99-40DC-9415-E53F280AA1F8";
            //Проверка есть ли Схема в проекте, если нет, то создать её
            Schema sch = Schema.Lookup(new Guid(schemaGuid));
            bool noSch = false;
            if (sch == null) noSch = true;
            var fullCircuits = GetSortedCircuits(board);
            //обозначение цепей и потребителей
            int circNamingInt = board.get_Parameter(BuiltInParameter.RBS_ELEC_CIRCUIT_NAMING).AsInteger();  // Какой метод обозначения цепей выбран
            int o = 1;
            string nam = "";
            switch (circNamingInt)
            {
                case 2:
                    nam = board.LookupParameter("Имя панели").AsString() + board.LookupParameter("Разделитель префикса цепи").AsString();
                    break;
                case 0:
                    nam = board.LookupParameter("Префикс цепи").AsString() + board.LookupParameter("Разделитель префикса цепи").AsString();
                    break;
            }
            //Имя цепей и аппаратов
            int n_avt = board.LookupParameter("TS_Начальный номер автомата").AsInteger();
            int n_kont = board.LookupParameter("TS_Начальный номер контактора").AsInteger();
            int n_uzo = board.LookupParameter("TS_Начальный номер УЗО").AsInteger();
            int n_rub = board.LookupParameter("TS_Начальный номер рубильника").AsInteger();
            //Если начальные номера не выставлены, то им присваивается 1ый номер
            if (n_avt == 0) { ++n_avt; }
            if (n_kont == 0) { ++n_kont; }
            if (n_uzo == 0) { ++n_uzo; }
            if (n_rub == 0) { ++n_rub; }
            //автомаркировка электрических устройств
            bool autonew = GetAutonew(board);
            Element view_AAZGL = Utilits.ViewKeyElement("❷ ● 1-й элемент в линии");
            Debug.Assert(view_AAZGL != null, "Не найдена ключевая спецификация 1-ый элемент в линии");
            FilteredElementCollector string_table_app = new FilteredElementCollector(_doc, view_AAZGL.Id);
            foreach (ElectricalSystem circ in fullCircuits)
            {
                if (circ.CircuitType == CircuitType.Circuit)
                {
                    //string nameCirc = nam + o.ToString();
                    circ.LookupParameter("TS_Имя цепи").Set(nam + o.ToString());

                    //18.09 Добавление наименование элементам
                    foreach (Element el in circ.Elements)
                    {
                        el.LookupParameter("TS_Имя цепи").Set(nam + o.ToString());
                    }
                    if (autonew)
                    {
                        string naznApZ = circ.LookupParameter("KAV.Назначение аппарата защиты").AsString();
                        switch (naznApZ)
                        {
                            case "узо":
                                circ.LookupParameter("KAV.Номер автомата").Set("QFD" + n_avt++.ToString());
                                break;
                            case "выключатель":
                                circ.LookupParameter("KAV.Номер автомата").Set("QF" + n_avt++.ToString());
                                break;
                            default:
                                circ.LookupParameter("KAV.Номер автомата").Set("");
                                break;
                        }

                        ElementId secondId = circ.LookupParameter("Аналог контакторов групповых линий").AsElementId();
                        if (secondId.IntegerValue != -1)
                        {
                            bool flag = false;
                            string nazn_second_ap = "";
                            Element secondEl = _doc.GetElement(secondId);
                            foreach (Element el in string_table_app)
                            {
                                //string s = el.LookupParameter("Ключевое имя").AsString();
                                if (el.Name == secondEl.Name)
                                {
                                    nazn_second_ap = el.LookupParameter("KAV.Назначение аппарата защиты").AsString();

                                    flag = true;
                                    break;
                                }
                            }
                            if (!flag) throw new NullReferenceException("Ключ 2-го элемента не найден"); // если элемент отсутствует в главной ключ. спец.
                            switch (nazn_second_ap)
                            {
                                case "узо":
                                    string sr = "FD" + n_uzo++.ToString();
                                    circ.LookupParameter("KAV.Номер контактора").Set(sr);
                                    break;
                                case "силовой контакт пускателя":
                                    string sk = "KM" + n_kont++.ToString();
                                    circ.LookupParameter("KAV.Номер контактора").Set(sk);
                                    break;
                                case "выключатель":
                                    string sa = "QF" + n_avt++.ToString();
                                    circ.LookupParameter("KAV.Номер контактора").Set(sa);
                                    break;
                                default:
                                    circ.LookupParameter("KAV.Номер контактора").Set("нетНазн");
                                    break;
                            }
                        }
                        else circ.LookupParameter("KAV.Номер контактора").Set("");
                        // Для номерации 3го аппарата в цепи
                        ElementId thirdId = circ.LookupParameter("3-й элемент в линии").AsElementId();
                        if (thirdId.IntegerValue != -1)
                        {
                            bool flag = false;
                            string nazn_third_ap = "";
                            Element thirdEl = _doc.GetElement(thirdId);
                            foreach (Element el in string_table_app)
                            {
                                //string s = el.LookupParameter("Ключевое имя").AsString();
                                if (el.Name == thirdEl.Name)
                                {
                                    nazn_third_ap = el.LookupParameter("KAV.Назначение аппарата защиты").AsString();

                                    flag = true;
                                    break;
                                }
                            }
                            if (!flag) throw new NullReferenceException("Ключ 3-го элемента не найден"); // если элемент отсутствует в главной ключ. спец.
                            switch (nazn_third_ap)
                            {
                                case "узо":
                                    string sr = "FD" + n_uzo++.ToString();
                                    circ.LookupParameter("KAV.Номер 3-го элемента в линии").Set(sr);
                                    break;
                                case "силовой контакт пускателя":
                                    string sk = "KM" + n_kont++.ToString();
                                    circ.LookupParameter("KAV.Номер 3-го элемента в линии").Set(sk);
                                    break;
                                case "выключатель":
                                    string sa = "QF" + n_avt++.ToString();
                                    circ.LookupParameter("KAV.Номер 3-го элемента в линии").Set(sa);
                                    break;
                                default:
                                    circ.LookupParameter("KAV.Номер 3-го элемента в линии").Set("нетНазн");
                                    break;
                            }
                        }
                        else circ.LookupParameter("KAV.Номер 3-го элемента в линии").Set("");
                        ++o;
                    }
                    else if (!noSch)//если цепь резервная и схема создавалась
                    {
                        //Организовать работу с сущностями
                        Entity ent;
                        if (CheckStorageExists(circ, sch, out ent)) //если в элемент записывалась сущность
                        {
                            //Прочесть схему и назначить новое значения
                            ent.Set<string>("NameCicuit", nam + o++.ToString());
                            //Получение 1го элемента из Хронилища
                            string nameFApp = ent.Get<string>("FirstApparat");
                            //Блок получения 1-го резервного аппарата
                            if (nameFApp != "") //заменить на (!=)...  //если в поле уже записан аппарат
                            {

                                string nazn_first_rez_ap = "";
                                bool flag = false;
                                foreach (Element el in string_table_app)
                                {
                                    //string s = el.Name;
                                    if (el.Name == nameFApp)
                                    {
                                        nazn_first_rez_ap = el.LookupParameter("KAV.Назначение аппарата защиты").AsString();

                                        flag = true;
                                        break;
                                    }
                                }
                                if (!flag) throw new NullReferenceException("Ключ для резервного элемента не найден"); // если элемент отсутствует в главной ключ. спец.

                                switch (nazn_first_rez_ap)
                                {
                                    case "узо":
                                        //Задать в поле номер автомата
                                        ent.Set<string>("NumberFA", "QFD" + n_avt++.ToString());
                                        break;
                                    case "выключатель":
                                        //Задать в поле номер автомата
                                        ent.Set<string>("NumberFA", "QF" + n_avt++.ToString());
                                        break;
                                    default:
                                        ent.Set<string>("NumberFA", ""); //затирание существующих значений
                                        break;
                                }
                            }
                            else ent.Set<string>("NumberFA", "QF" + n_avt++.ToString());

                            //Получение 2го элемента из Хронилища
                            string nameSApp = ent.Get<string>("SecondApparat");
                            //Блок получения 2-го резервного аппарата
                            if (nameSApp != "") //заменить на (!=)...  //если в поле уже записан аппарат
                            {
                                string nazn_second_rez_ap = "";
                                bool flag = false;
                                foreach (Element el in string_table_app)
                                {
                                    if (el.Name == nameSApp)
                                    {
                                        nazn_second_rez_ap = el.LookupParameter("KAV.Назначение аппарата защиты").AsString();
                                        flag = true;
                                        break;
                                    }
                                }
                                if (!flag) throw new NullReferenceException("Ключ для резервного элемента не найден"); // если элемент отсутствует в главной ключ. спец.

                                switch (nazn_second_rez_ap)
                                {
                                    case "узо":
                                        //Задать в поле номер автомата
                                        ent.Set<string>("NumberSA", "QFD" + n_avt++.ToString());
                                        break;
                                    case "выключатель":
                                        //Задать в поле номер автомата
                                        ent.Set<string>("NumberSA", "QF" + n_avt++.ToString());
                                        break;
                                    case "силовой контакт пускателя":
                                        ent.Set<string>("NumberSA", "KM" + n_kont++.ToString());
                                        break;
                                    default:
                                        ent.Set<string>("NumberSA", "");
                                        //Задать в поле номер автомата
                                        break;
                                }
                            }

                            //Получение 3го элемента из Хронилища
                            string nameTApp = ent.Get<string>("ThirdApparat");
                            //Блок получения 2-го резервного аппарата
                            if (nameTApp != "") //если в поле уже записан аппарат
                            {
                                string nazn_third_rez_ap = "";
                                bool flag = false;
                                foreach (Element el in string_table_app)
                                {
                                    if (el.Name == nameTApp)
                                    {
                                        nazn_third_rez_ap = el.LookupParameter("KAV.Назначение аппарата защиты").AsString();
                                        flag = true;
                                        break;
                                    }
                                }
                                if (!flag) throw new NullReferenceException("Ключ для резервного элемента не найден"); // если элемент отсутствует в главной ключ. спец.

                                switch (nazn_third_rez_ap)
                                {
                                    case "узо":
                                        //Задать в поле номер автомата
                                        ent.Set<string>("NumberTA", "QFD" + n_avt++.ToString());
                                        break;
                                    case "выключатель":
                                        //Задать в поле номер автомата
                                        ent.Set<string>("NumberTA", "QF" + n_avt++.ToString());
                                        break;
                                    case "силовой контакт пускателя":
                                        ent.Set<string>("NumberTA", "KM" + n_kont++.ToString());
                                        break;
                                    default:
                                        ent.Set<string>("NumberTA", "");
                                        //Задать в поле номер автомата
                                        break;
                                }
                            }
                            //Запись сущности в элемент
                            circ.SetEntity(ent);
                        }
                        else
                        {
                            o++;
                            n_avt++;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Наименование только цепей
        /// </summary>
        public static void Naming2(FamilyInstance board)
        {
            //обозначение цепей и потребителей
            int circNamingInt = board.get_Parameter(BuiltInParameter.RBS_ELEC_CIRCUIT_NAMING).AsInteger();  // Какой метод обозначения цепей выбран
            int o = 1;
            string nam = "";
            switch (circNamingInt)
            {
                case 2:
                    nam = board.LookupParameter("Имя панели").AsString() + board.LookupParameter("Разделитель префикса цепи").AsString();
                    break;
                case 0:
                    nam = board.LookupParameter("Префикс цепи").AsString() + board.LookupParameter("Разделитель префикса цепи").AsString();
                    break;
            }
            var fullCircuits = GetSortedCircuits(board);
            foreach (ElectricalSystem circ in fullCircuits)
            {
                if (circ.CircuitType == CircuitType.Circuit)
                {
                    //string nameCirc = nam + o.ToString();
                    circ.LookupParameter("TS_Имя цепи").Set(nam + o.ToString());

                    //18.09 Добавление наименование элементам
                    foreach (Element el in circ.Elements)
                    {
                        el.LookupParameter("TS_Имя цепи").Set(nam + o.ToString());
                    }
                    o++;
                }
                else
                {
                    o++;
                }
            }
        }
    }
}
