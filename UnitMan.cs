// Copyright © 2016 Mikhail Beloshapkin
// All rights reserved.
//
// Author: Mikhail Beloshapkin mbeloshapkin@gmail.com
//
// Licensed under the Microsoft Public License (Ms-PL)
//

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace AeroGIS.Common {
    // The names should be exactly as inside XML and must me always synchronized
    enum StdVar { Mass, Volume, HarvestMass, Yield, Density, AppVolRate, HarvestRate, PaddockArea, AppMassRate,
                    DefaultPlantingRate }
    enum StdList { PurchasingUnits, AppVolRate, AppMassRate, Concentration }

    [DebuggerDisplay("UnitMan({_ISO639Code})")]
    partial class UnitMan {
        protected string _ISO639Code = "";        
        public string ISO639Code { get { return _ISO639Code; } } // ISO 639-2 three letters languge code
        protected string _Description;
        public string Description { get { return _Description; } }
        protected string UOMRegex;

        private static char[] Digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        private const string strDigits = "0123456789";
        protected Dictionary<string, UOM> UOMs;
        protected Dictionary<string, string> Variables;
        protected Dictionary<string, string[]> Lists;                
        

        /// <summary>
        /// Load a unit system from XML document.
        /// </summary>        
        public virtual void Load(XmlDocument ADoc) {

            // Process UnitsSystem node
            XmlNode root = ADoc.DocumentElement;
            _ISO639Code = root.Attributes["ISO639Code"].InnerText;
            _Description = root.Attributes["Description"].InnerText;
            UOMRegex = root.Attributes["UOMRegEx"].InnerText;
            // Version attribute introduced in v2.0, so if the attribute exits then version is 2.0 or higher
            if (root.Attributes["Version"] == null) throw new Exception("Wrong version of UOM definition XML " + root.Name + ". Version 2.0 or higher required.");



            XmlNode xnUOMs = root.SelectSingleNode("UnitsOfMeasure");

            XmlNodeList xlist = xnUOMs.SelectNodes("MasterUnits/PrimaryUOM");
            UOMs = new Dictionary<string, UOM>();

            // Load primary master UOMs
            foreach (XmlNode xu in xlist) {
                UOM u = new UOM();
                u.Load(xu);
                u.Domain = xu.Attributes["Domain"].InnerText;
                u.Scale = 1.0;
                u.IsComposed = false;
                u.IsMaster = true;
                UOMs.Add(u.Signature, u);
            }

            // Load composed master UOMs
            xlist = xnUOMs.SelectNodes("MasterUnits/ComposedUOM");
            foreach (XmlNode xu in xlist) {
                UOM u = new UOM();
                u.Load(xu);
                u.Domain = xu.Attributes["Domain"].InnerText;
                u.IsMaster = true;
                u.IsComposed = true;
                u.Scale = 1.0;
                UOMs.Add(u.Signature, u);
            }

            // Load Inherited UOMs
            xlist = xnUOMs.SelectNodes("InheritedUnits/UOM");
            foreach (XmlNode xu in xlist) {
                UOM u = new UOM();
                u.Load(xu);
                u.Master = UOMs[xu.Attributes["Master"].InnerText] as UOM;
                u.Domain = u.Master.Domain;
                u.IsMaster = false;
                u.IsComposed = true;
                u.Scale = Convert.ToDouble(xu.Attributes["Scale"].InnerText, NumberFormatInfo.InvariantInfo);                
                UOMs.Add(u.Signature, u);
            }

            //////////////// TODO: Move to somewhere
            Lists = new Dictionary<string, string[]>();
            Variables = new Dictionary<string, string>();

            // Load standard precision variables

            XmlNode rxn = ADoc.SelectSingleNode("UnitSystem/Variables");
            foreach (XmlNode vxn in rxn.ChildNodes) {
                if (vxn.NodeType != XmlNodeType.Comment) {
                    string varName = vxn.Attributes["Name"].Value;
                    XmlNode uxn = vxn.ChildNodes[0];
                    Variables.Add(varName, uxn.InnerText);
                }
            }
            ///////////////////
        }             

        protected double Normalize(double x, string uom) {
            UOM u = UOMs[uom] as UOM;            
            return x * u.Scale;
        }

        protected void DecomposeFractionalUOM(string AFractionalUOM, out string ANumerator, out string ADenominator) {
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
        protected UOM Compose(string ANewUOM) {
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
            r.IsMaster = false;
            r.Domain = masterUOM.Domain;
            r.Scale = scale;
            r.Master = masterUOM; //.Signature;
            UOMs.Add(r.Signature, r);
            return r;
        }

        /// <summary>
        /// Translate a value from one UOM to another. Source and destination UOMs should be compatible, i.e. they shall be of the same domain.
        /// </summary>
        /// <param name="x">Any value</param>
        /// <param name="ASrcUOM">UOM in wich the value is represented</param>
        /// <param name="ADstUOM">UOM in which return value wil be represented</param>
        /// <returns>Source value translated into destination UOM</returns>
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

            if(srcu.IsCompatible(dstu)) return x * srcu.Scale / dstu.Scale;

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

        /// <summary>
        /// Get domain name of UOM
        /// </summary>        
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

        /// <summary>
        /// Get human readable label for UOM
        /// </summary>        
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

        /// <summary>
        /// Check if an UOM supported.
        /// </summary>        
        public bool Contains(string AnUOM) { return UOMs.ContainsKey(AnUOM); }

        public double Farenheit2C(double FarTemp) { return (FarTemp - 32.0) / 1.8; }
        // Add debug attribute
        protected class UOM {
            public string Name;
            public string Signature;
            public string Plural;
            public string Label;
            public string Domain;
            public bool IsMaster;
            public bool IsComposed;
            public bool IsPrimary { get { return IsMaster && !IsComposed; } }
            public double Scale;
            public UOM Master;

            public void Load(XmlNode xn) {
                Name = xn.Attributes["Name"].InnerText;
                Signature = xn.Attributes["Signature"].InnerText;
                Plural = xn.Attributes["Plural"].InnerText;
                Label = xn.Attributes["Label"].InnerText;                
            }            

            public bool IsCompatible(UOM u) {
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
