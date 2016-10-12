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
using System.Text.RegularExpressions;

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

            XmlNodeList xlist = xnUOMs.SelectNodes("MasterUnits/UOM");
            UOMs = new Dictionary<string, UOM>();

            // Load master UOMs
            foreach (XmlNode xu in xlist) {
                UOM u = new UOM();
                u.Load(xu, true);                
                UOMs.Add(u.Signature, u);
            }
            
            // Load Scaled UOMs
            xlist = xnUOMs.SelectNodes("ScaledUnits/UOM");
            foreach (XmlNode xu in xlist) {
                UOM u = new UOM();
                u.Load(xu, false);                                                                                
                UOMs.Add(u.Signature, u);
            }

#if DEBUG
            foreach(string s in UOMs.Keys) CheckSignature(s); // That thows exception if invalid signature in XML doc
#endif
            //////////////// TODO: Move to somewhere
            Lists = new Dictionary<string, string[]>();
            Variables = new Dictionary<string, string>();

            // Load standard variables

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

        /// <summary>
        /// Drcompose a complex UOM into atomic UOMs  
        /// </summary>
        /// <param name="AComplexUOMSignature">UOM signature of any complexity</param>        
        protected Atom[] Atomize(string AComplexUOMSignature) {
            string[] uoms = Regex.Split(AComplexUOMSignature, @"[-,1-3]+"); // Extract uoms
            string[] pows = Regex.Split(AComplexUOMSignature, UOMRegex + "+"); // Extract powers
            Atom[] ret = new Atom[uoms.Length - 1];
            for (int ct = 0; ct < ret.Length; ct++) {
                ret[ct] = new Atom(uoms[ct], int.Parse(pows[ct + 1]));
            }
            return ret;
        }

        /// <summary>
        /// Make UOM signature of atoms
        /// </summary>        
        protected string Recombine(Atom[] AnAtoms){
            string rstr = AnAtoms[0].Signature;
            for (int ct = 1; ct < AnAtoms.Length; ct++) rstr += AnAtoms[ct];
            return rstr;
        }        
        
        /// <summary>
        /// Compose UOM from pure signature which is not defined in list/ Find master UOMs, calculate scale, generate label. The signature could be of any complexity.
        /// </summary>
        /// <param name="ANewSignature">A new signature</param>
        /// <returns>Newly composed UOM. Please, note, the new UOM signature could differ on source signature.</returns>
        protected UOM Compose(string ANewSignature) {
#if DEBUG
            CheckSignature(ANewSignature);
#endif
            Atom[] atoms = Atomize(ANewSignature);                        
            
            // Search for master UOMs
            UOM[] srcu = new UOM[atoms.Length];
            for (int ct = 0; ct < atoms.Length; ct++) {
                string au = atoms[ct].AtomicUOM;
                if (!UOMs.ContainsKey(au)) throw new Exception("UnitMan failed to compose new UOM for signature " +
                     ANewSignature + ". Atomic UOM " + au + " not found.");
                srcu[ct] = UOMs[au];
            }                           
            // 1. Normalise the UOM and enshure the master UOM exists
            SMUOM src = new SMUOM(ANewSignature);
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
            if(masterUOM == null) Log.Err("Master UOM is not found for Unit of Measurement: " + ANewSignature);
            UOM r = new UOM();
            r.Name = ANewSignature;
            r.Signature = ANewSignature;
            r.Plural = ANewSignature;
            r.Label = ANewSignature;  // Need to compose labeles here
            r.IsComposed = true;
            //r.IsMaster = false;
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
        /// Get human readable label for UOM. If UOM signature not found in the dictionry then the signature will be returned instead label.
        /// </summary>        
        public string LabelOf(string AnUOMSignature) {
            if(UOMs.ContainsKey(AnUOMSignature)) return (UOMs[AnUOMSignature] as UOM).Label;
            return AnUOMSignature;
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
        
        #region Protected classes
        /// <summary>
        /// Check if an UOM supported.
        /// </summary>        
        public bool Contains(string AnUOMSignature) { return UOMs.ContainsKey(AnUOMSignature); }

        [DebuggerDisplay("{Name} {Signature} {Master}")]
        protected class UOM {
            public string Name;
            public string Signature;
            public string Plural;
            public string Label;
            public string Domain;
            private bool _IsMaster;
            public bool IsMaster { get { return _IsMaster; } }
            public bool IsComposed;
            private double _Scale;
            public double Scale;
            public UOM Master = null;
            public string MasterSignature { get { if (IsMaster) return Signature; else return Master.Signature; } }

            public void Load(XmlNode xn, bool ItsMaster) {
                Name = xn.Attributes["Name"].InnerText;
                Signature = xn.Attributes["Signature"].InnerText;
                Plural = xn.Attributes["Plural"].InnerText;
                Label = xn.Attributes["Label"].InnerText;
                _IsMaster = ItsMaster;
                if (_IsMaster) { _Scale = 1.0; Domain = xn.Attributes["Domain"].InnerText; }
                else _Scale = LoadVal(xn, "Scale");
            }

            public static double LoadVal(XmlNode xn, string attr) {
                return Convert.ToDouble(xn.Attributes["Label"].InnerText, NumberFormatInfo.InvariantInfo);
            }

            public bool IsCompatible(UOM u) { return MasterSignature == u.MasterSignature; }
        }


        [DebuggerDisplay("Atom {Signature}")]
        protected class Atom : IComparable<Atom> {
            public string AtomicUOM;
            public int Exponent;
            public string Signature { get { return AtomicUOM + Exponent; } }

            public Atom(string AnAtomicUOM, int APower) { AtomicUOM = AnAtomicUOM; Exponent = APower; }
            /// <summary>
            /// Sort order for atoms is: positive powers sorted by alphabet, negative powers sorted by alphabet.
            /// That provides the strong signatures for automatically composed UOMs.
            /// </summary>            
            public int CompareTo(Atom other) {
                if (Exponent > 0 && other.Exponent < 0) return -1;
                if (Exponent < 0 && other.Exponent > 0) return 1;
                return AtomicUOM.CompareTo(other.AtomicUOM);
            }
        }
        #endregion

        #region Debug only
#if DEBUG
        protected void CheckSignature(string ASignature) {
            Atom[] atoms = Atomize(ASignature);
            Array.Sort(atoms);
            if(Recombine(atoms) != ASignature) throw new Exception("Wrong sequence of atoms in UOM Signature " + ASignature);            
        }
#endif
        #endregion

        #region Code to move into inherited class
        protected Dictionary<string, string> Variables;
        protected Dictionary<string, string[]> Lists;

        public string LabelOf(StdVar AVariable) {
            string key = AVariable.ToString();
            if (!Variables.ContainsKey(key)) {
                Log.Err("Precision Variable: " + key + " is undefined. " +
                    "Please, update the definition file UnitSystem.xml");
                return "";
            }
            return LabelOf((string)Variables[key]);
        }

        public double Farenheit2C(double FarTemp) { return (FarTemp - 32.0) / 1.8; }

        public string GetStdUOM(StdVar APrecVariable) {
            string s = APrecVariable.ToString();
            return (string)Variables[s];
        }
        #endregion        
    }
}
