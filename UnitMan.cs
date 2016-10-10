using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Globalization;

namespace AeroGIS.Common {
    // The names should be exactly as inside XML and must me always synchronized
    enum StdVar { Mass, Volume, HarvestMass, Yield, Density, AppVolRate, HarvestRate, PaddockArea, AppMassRate,
                    DefaultPlantingRate }
    enum StdList { PurchasingUnits, AppVolRate, AppMassRate, Concentration }

    partial class UnitMan {
        private static char[] Digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        private const string strDigits = "0123456789";
        private Dictionary<string, UOM> UOMs;
        private Dictionary<string, string> Variables;
        private Dictionary<string, string[]> Lists;        
        private string _Culture;
        public string Culture { get { return _Culture; } }
        private string _Description;
        public string Description;        

        public void Load(XmlDocument ADoc) {
            
            Lists = new Dictionary<string, string[]>();
            Variables = new Dictionary<string, string>();
                                                
            XmlNode root = ADoc.SelectSingleNode("UnitSystem");
            XmlNode xn = root.SelectSingleNode("Culture");
            _Culture = xn.InnerText;
            xn = root.SelectSingleNode("Description");
            _Description = xn.InnerText;
            xn = root.SelectSingleNode("UOMDefinitions");                                    
            XmlNode uomroot = ADoc.SelectSingleNode("UnitsOfMeasure");
            string uomstr = "";
            try {
                XmlNode x = uomroot.SelectSingleNode("MasterUnits");
                UOMs = new Dictionary<string, UOM>();
                // Load primary UOMs
                foreach (XmlNode xu in x.ChildNodes) {
                    uomstr = xu.InnerText;
                    if (xu.NodeType != XmlNodeType.Comment) {
                        UOM u = new UOM();
                        u.Create(xu);
                        u.IsPrimary = true;
                        u.Scale = 1.0;
                        if (xu.Name == "ComposedUOM") u.IsComposed = true;
                        u.Domain = xu.SelectSingleNode("Domain").InnerText;
                        UOMs.Add(u.Signature, u);
                    }
                }
                // Load Inherited UOMs
                x = uomroot.SelectSingleNode("InheritedUnits");
                foreach (XmlNode xu in x.ChildNodes) {
                    uomstr = xu.InnerText;
                    if (xu.NodeType != XmlNodeType.Comment) {
                        UOM u = new UOM();
                        u.Create(xu);
                        u.IsPrimary = false;
                        u.IsComposed = false;
                        u.Scale = Convert.ToDouble(xu.SelectSingleNode("Scale").InnerText, NumberFormatInfo.InvariantInfo);
                        u.Master = UOMs[xu.SelectSingleNode("Master").InnerText] as UOM;
                        u.Domain = u.Master.Domain;
                        UOMs.Add(u.Signature, u);
                    }
                }
            }
            catch (Exception e) {
                Log.Err("Units manager failed to load unit [" + uomstr + "]", e);
                return;
            }
            // Load standard precision variables
            try {
                XmlNode rxn = ADoc.SelectSingleNode("UnitSystem/Variables");
                foreach (XmlNode vxn in rxn.ChildNodes) {                    
                    if (vxn.NodeType != XmlNodeType.Comment) {
                        string varName = vxn.Attributes["Name"].Value;
                        XmlNode uxn = vxn.ChildNodes[0];
                        Variables.Add(varName, uxn.InnerText);
                    }
                }                
            }
            catch (Exception e) {
                Log.Err("Units manager failed to standard variables", e);                
            }

        }                

        public double Normalize(double x, string uom) {
            UOM u = UOMs[uom] as UOM;            
            return x * u.Scale;
        }

        public void DecomposeFractionalUOM(string AFractionalUOM, out string ANumerator, out string ADenominator) {
            // 1. Search for first exponent
            int expIdx = AFractionalUOM.IndexOfAny(Digits);
            // 2. Divide string in two
            string num = AFractionalUOM.Substring(0, expIdx + 1);
            string den = AFractionalUOM.Substring(expIdx + 1);
            // 3. Remove unity exponent if any from numerator
            if (num[expIdx] == '1') num = AFractionalUOM.Substring(0, expIdx);
            // 4. Inverse sign of denominator
            den = den.Replace("-", "");
            // 5. Remove unity exponent from denominator
            int lstDen = den.Length - 1;
            if (den[lstDen] == '1') den = den.Substring(0, lstDen);
            ANumerator = num; ADenominator = den;
        }        

        /*public static SMValue Normalize(SMValue x) { // This normalizes even not listed UOMs!
            SMUOM srcU = new SMUOM(x.UOM);
            string masterU = ""; // This will compose it now!
            double masterScale = 1.0;
            for (int ct = 0; ct < srcU.Order; ct++) {
                string src = srcU.GetUnit(ct);
                UOM suom = UOMs[src] as UOM;
                masterU += suom.Signature + srcU.GetPower().ToString();
                if (suom.Scale != 1.0) {
                    double scale = Math.Pow(suom.Scale, srcU.GetPower(ct));
                    masterScale *= scale;
                }
            }
            SMValue val = new SMValue(masterU, x * masterScale);
            return val;
        }*/
        /// <summary>
        /// Compose not existing UOM and add to the table to avoid composing next time
        /// </summary>
        /// <param name="ANewUOM"></param>
        /// <returns></returns>
        private UOM Compose(string ANewUOM) {
            // 1. Normalise the UOM and enshure the master UOM exists
            SMUOM src = new SMUOM(ANewUOM);
            string master = "";
            double scale = 1.0;
            for (int ct = 0; ct < src.Order; ct++) {
                string unit = src.GetUnit(ct);
                int power = src.GetPower(ct);
                UOM suom = UOMs[unit + ((int)Math.Abs(power)).ToString()] as UOM;
                if (suom == null) suom = UOMs[unit] as UOM; 
                if (suom.IsPrimary) master += unit + power.ToString();
                else {
                    SMUOM sm_master = new SMUOM(suom.Master.Signature);
                    master += sm_master.GetUnit(0);
                    if(power < 0) master += "-" + sm_master.GetPower(0).ToString();
                    else master += sm_master.GetPower(0).ToString();
                }
                if (suom.Scale != 1.0) {
                    //double scl = Math.Pow(suom.Scale, (double)src.GetPower(ct));
                    if (power > 0) scale *= suom.Scale;
                    else scale /= suom.Scale;
                }
            }
            UOM masterUOM = UOMs[master] as UOM;
            if(masterUOM == null) Log.Err("Master UOM is not found for Unit of Measurement: " + ANewUOM);
            UOM r = new UOM();
            r.Name = ANewUOM;
            r.Signature = ANewUOM;
            r.Plural = ANewUOM;
            r.Label = ANewUOM;  // Need to compose labeles here
            r.IsComposed = true;
            r.IsPrimary = false;
            r.Domain = masterUOM.Domain;
            r.Scale = scale;
            r.Master = masterUOM; //.Signature;
            UOMs.Add(r.Signature, r);
            return r;
        }

        public double Translate(double x, string ASrcUOM, string ADstUOM) {
            if (ASrcUOM == ADstUOM) return x;
            UOM srcu = UOMs[ASrcUOM] as UOM;
            UOM dstu = UOMs[ADstUOM] as UOM;
            if (srcu == null) {
                srcu = Compose(ASrcUOM);
                //Log.Err("Unsupported UOM " + ASrcUOM + "\r\nPlease, contact Fairport Support Service to add the support of this unit of measurement.");
            }
            if (dstu == null) {
                dstu = Compose(ADstUOM);
                //Log.Err("Unsupported UOM " + ADstUOM + "\r\nPlease, contact Fairport Support Service to add the support of this unit of measurement.");
            }

            if(srcu.Compatible(dstu)) return x * srcu.Scale / dstu.Scale;

            Log.Err("UnitMan failed to convert incompatible units: " +
                srcu.Name + " to " + dstu.Name);
            return x;
        }

        public string MasterOf(string ASimpleUOM) {
            if(!UOMs.ContainsKey(ASimpleUOM)) return "";
            UOM u = UOMs[ASimpleUOM] as UOM;
            if (!u.IsPrimary) return "";
            return u.Master.Signature;
        }

        public string DomainOf(string AnyUOM) {
            // Power one can be skipeed. Check it. 
            try {
                string sgn = AnyUOM;
                // WARNING: This code is not applicable to String Theory. The maximum amount of space dimentions is 10!!!
                if (sgn[sgn.Length - 1] == '1' && sgn[sgn.Length - 2] != '-') sgn = AnyUOM.Substring(0, AnyUOM.Length - 1);
                if (!UOMs.ContainsKey(sgn)) return "";
                UOM u = UOMs[sgn] as UOM;
                return u.Domain;
            }
            catch (Exception ex) {
                Log.Err("Wrong UOM Domain Requested", ex);
                return "";
            }
        }

        public string LabelOf(string AnyUOM) {
            if(UOMs.ContainsKey(AnyUOM)) return (UOMs[AnyUOM] as UOM).Label;
            return AnyUOM;
        }

        public string LabelOf(StdVar AVariable) {
            string key = AVariable.ToString();
            if (!Variables.ContainsKey(key)) {
                Log.Err("Precision Variable: " + key + " is undefined. " +
                    "Please, update the definition file UnitSystem.xml");
                return "";
            }
            return LabelOf((string)Variables[key]);
        }

        private void LoadList(string AListName, XmlDocument ADoc) {
            XmlNode xn = ADoc.SelectSingleNode("UnitSystem/UOMLists");
            XmlNode lxn = xn.SelectSingleNode("UOMList[@Name='" + AListName + "']");
            string[] units = new string[lxn.ChildNodes.Count];
            int ct = 0;
            foreach (XmlNode xc in lxn.ChildNodes) {
                units[ct++] = xc.InnerText;
            }
            Lists.Add(AListName, units);
        }
        
        public string GetStdUOM(StdVar APrecVariable) {
            string s = APrecVariable.ToString();
            return (string)Variables[s];
        }

        public bool Contains(string AUnit) { return UOMs.ContainsKey(AUnit); }

        public double Farenheit2C(double FarTemp) { return (FarTemp - 32.0) / 1.8; }
        
        private class UOM {
            public string Name;
            public string Signature;
            public string Plural;
            public string Label;
            public string Domain;
            public bool IsPrimary;
            public bool IsComposed;
            public double Scale;
            public UOM Master;

            public void Create(XmlNode xn) {
                try {
                    Name = xn.SelectSingleNode("Name").InnerText;
                    Signature = xn.SelectSingleNode("Signature").InnerText;
                    Plural = xn.SelectSingleNode("Plural").InnerText;
                    Label = xn.SelectSingleNode("Label").InnerText;
                }
                catch (Exception ex) {
                    Log.Err("Invalid UOM Definition: " + xn.InnerXml, ex);
                }
            }

            public bool Compatible(UOM u) {
                if (IsPrimary) {
                    if (u.IsPrimary) return false;
                    return u.Master.Signature == Signature;
                }
                else {
                    if (u.IsPrimary) return u.Signature == this.Master.Signature;
                    return u.Master.Signature == Master.Signature;
                }
            }
        }
            
    }
}
