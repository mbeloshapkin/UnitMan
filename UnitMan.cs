#region License Information (Ms-PL)
// Copyright � 2016 Mikhail Beloshapkin
// All rights reserved.
//
// Author: Mikhail Beloshapkin mbeloshapkin@gmail.com
//
// Licensed under the Microsoft Public License (Ms-PL)
//
#endregion

using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AeroGIS.Common {
    // The names should be exactly as inside XML and must me always synchronized
    public enum StdVar { Mass, Volume, HarvestMass, Yield, Density, AppVolRate, HarvestRate, PaddockArea, AppMassRate,
                    DefaultPlantingRate }
    public enum StdList { PurchasingUnits, AppVolRate, AppMassRate, Concentration }


    /// <summary>
    /// Single class API for UOMs conversion. The entire system of UOMs could be defined as single XML file.
    /// Redundant conversion coefficients effectively eliminated, UnitMan is able to calculate conversion coefficients in many cases even these coefficients are not pre-defined.
    /// </summary>    
    [DebuggerDisplay("UnitMan({_ISO639Code})")]
    public class UnitMan {

        protected string _ISO639Code = "";        
        /// <value>ISO 639-2 three letters languge code. The value should be defined in XML definition file.</value>
        public string ISO639Code { get { return _ISO639Code; } } 

        protected string _Description;        
        /// <value>User defined description of units system</value>
        public string Description { get { return _Description; } }

        /// <summary>
        /// The regex defines chars which are valid for UOM labels.
        /// For example "[A-Z,a-z]".
        /// </summary>
        protected string UOMRegex;
                
        protected Dictionary<string, UOM> UOMs;

        /// <summary>
        /// The list of UOM domains.
        /// </summary>
        public List<string> Domains { get { return UOMs.Values.GroupBy(d => d.Domain).Select(g => g.Key).ToList<string>(); } }

        /// <summary>
        /// Load an user defined unit system from XML document.
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
                u.Master = UOMs[xu.Attributes["Master"].InnerText];                
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

        /// <summary>
        /// Check if an UOM is defined in source XML. If UOM is not defined and loaded, 
        /// then that does not mean it could not be converted. The conversion coefficients
        /// for undefined UOMs will be calculated if it is possible to find master UOMs
        /// </summary>
        /// <param name="ASignature">UOM signature</param>
        /// <returns>True is the signature is pre-defined in source XML</returns>
        public bool IsDefined(string ASignature) { return UOMs.ContainsKey(ASignature); }

        /// <summary>
        /// Translate a value from one UOM to another. Source and destination UOMs should be compatible, i.e. they shall be of the same domain.
        /// </summary>
        /// <param name="x">Any value</param>
        /// <param name="ASrcSignature">UOM in wich the value is represented</param>
        /// <param name="ADstSignature">UOM in which return value wil be represented</param>
        /// <returns>Source value translated into destination UOM</returns>
        public double Convert(double x, string ASrcSignature, string ADstSignature) {
            if (ASrcSignature == ADstSignature) return x;
#if DEBUG
            CheckSignature(ASrcSignature); CheckSignature(ADstSignature);
#endif
            UOM srcu = UOMs[ASrcSignature];
            UOM dstu = UOMs[ADstSignature];            

            if (srcu.IsCompatible(dstu)) return x * srcu.Scale / dstu.Scale;

            throw new Exception("UnitMan failed to convert incompatible units: " +
                srcu.Signature + " to " + dstu.Signature);
        }

        /// <summary>
        /// Crete new UOMs if these UOMs are not defined and translate
        /// </summary>
        /// <param name="x">Any value</param>
        /// <param name="ASrcSignature">UOM in wich the value is represented</param>
        /// <param name="ADstSignature">UOM in which return value wil be represented</param>
        /// <returns>Source value translated into destination UOM</returns>
        public virtual double ForceConvert(double x, string ASrcSignature, string ADstSignature) {
            if (!UOMs.ContainsKey(ASrcSignature)) Compose(ASrcSignature);
            if (!UOMs.ContainsKey(ADstSignature)) Compose(ADstSignature);
            return Convert(x, ASrcSignature, ADstSignature);
        }

        #region UOM attributes by signature
        /// <summary>
        /// Get domain name of UOM
        /// </summary>        
        public string DomainOf(string ASignature) {
#if DEBUG
            CheckSignature(ASignature);
#endif
            return UOMs[ASignature].Domain;
        }

        /// <summary>
        /// Human readable label for UOM. If UOM signature not found in the dictionry then the signature will be returned instead label.
        /// </summary>        
        public string LabelOf(string ASignature) {
#if DEBUG
            CheckSignature(ASignature);
#endif
            return UOMs[ASignature].Label;           
 
        }

        /// <summary>
        /// Human readable name of UOM, such as "meter" 
        /// </summary>        
        public string NameOf(string ASignature) {
#if DEBUG
            CheckSignature(ASignature);
#endif
            return UOMs[ASignature].Name;
        }

        /// <summary>
        /// Human readable name of UOM in plural form, such as "meters" 
        /// </summary>        
        public string PluralOf(string ASignature) {
#if DEBUG
            CheckSignature(ASignature);
#endif
            return UOMs[ASignature].Plural;
        }
        #endregion

        /// <summary>
        /// List of UOM signatures of domain.
        /// </summary>        
        public IEnumerable<string> UOMsOf(string ADomain) {
            return UOMs.Keys.Where(uom => UOMs[uom].Domain == ADomain);            
        }

        /// <summary>
        /// Parse UOM signature using its human readable label like "m/s", "kg/m2" and so on. 
        /// Simple and rational UOM labels are supported only. 
        /// </summary>
        /// <param name="ALabel">A human readable label</param>
        /// <returns>UOM standard signature like "m1s-1", kg1m-2 and so on. If UOM is not matched than empty string returns.</returns>
        public string ParseLabel(string ALabel) {
            if (Regex.IsMatch(ALabel, "^" + UOMRegex + "{1,3}[23]?$")) {  // Simple UOM                
                return MatchSimpleLabel(ALabel);
            }

            if (Regex.IsMatch(ALabel, "^" + UOMRegex + "{1,3}[23]?/" + UOMRegex + "{1,3}[23]?$")) { // Rational UOM
                string[] sims = ALabel.Split('/');
                Match numerator = Regex.Match(sims[0], "(?<uom>^" + UOMRegex + "{1,3})(?<pow>[23]?$)");
                Match denom = Regex.Match(sims[1], "(?<uom>^" + UOMRegex + "{1,3})(?<pow>[23]?$)");
                string rstr = MatchSimpleLabel(numerator.Groups["uom"].Value);
                if (rstr.Length > 0) {
                    if (numerator.Groups["pow"].Length > 0) rstr += numerator.Groups["pow"].Value;
                    else rstr += "1";
                    string mden = MatchSimpleLabel(denom.Groups["uom"].Value);
                    if (mden.Length > 0) {
                        rstr += mden;
                        if (denom.Groups["pow"].Length > 0) rstr += "-" + denom.Groups["pow"].Value;
                        else rstr += "-1";
                        return rstr;
                    }
                }
            }
            return "";
        }

        #region Protected functions
        /// <summary>
        /// Decompose a complex UOM into atomic UOMs  
        /// </summary>
        /// <param name="AComplexSignature">UOM signature of any complexity</param>
        /// <returns>Array of atomic UOMs</returns>
        /// <seealso cref="Recombine"/>
        protected Atom[] Atomize(string AComplexSignature) {
            string[] uoms = Regex.Split(AComplexSignature, @"[-,1-9]+"); // Extract uoms
            string[] pows = Regex.Split(AComplexSignature, UOMRegex + "+"); // Extract powers
            Atom[] ret = new Atom[uoms.Length - 1];
            for (int ct = 0; ct < ret.Length; ct++) {
                ret[ct] = new Atom(uoms[ct], int.Parse(pows[ct + 1]));
            }
            return ret;
        }

        /// <summary>
        /// Compose UOM signature from array of atomic UOMs
        /// </summary>
        /// <param name="AnAtoms">Array of atomic UOMs</param>
        /// <returns>UOM signature</returns>
        protected string Recombine(Atom[] AnAtoms){
            string rstr = AnAtoms[0].Signature;
            for (int ct = 1; ct < AnAtoms.Length; ct++) rstr += AnAtoms[ct].Signature;
            return rstr;
        }

        /// <summary>
        /// Sort signature atoms by next rules: 
        /// 1. Positive powers are going first
        /// 2. Atoms of same exponent sign sorted in alphabet order        
        /// </summary> 
        /// <returns>The signature, sorted as above</returns>
        protected string NormalizeSignature(string ASignature) {
            Atom[] atoms = Atomize(ASignature);
            Array.Sort<Atom>(atoms);
            return Recombine(atoms);
        }
        
        /// <summary>
        /// Compose UOM from signature which is not defined in source definition file. Find master UOMs, calculate scale, generate label. The signature could be of any complexity.
        /// </summary>
        /// <param name="ANewSignature">A new signature</param>
        /// <returns>Newly composed UOM. Please, note, the new UOM signature could differ on source signature.</returns>
        protected UOM Compose(string ANewSignature) {
#if DEBUG
            CheckSignature(ANewSignature);
#endif
            Atom[] atoms = Atomize(ANewSignature);                        
            
            // Search for master UOMs
            UOM[] masters = new UOM[atoms.Length];
            UOM newUOM = new UOM(); newUOM.Scale = 1.0; newUOM.Signature = ANewSignature;
            for (int ct = 0; ct < atoms.Length; ct++) {
                Atom atom = atoms[ct];
                if (!UOMs.ContainsKey(atom.AtomicUOM + "1")) throw new Exception("UnitMan failed to compose new UOM for signature " +
                     ANewSignature + ". Atomic UOM " + atom.AtomicUOM + " not found.");
                // Atomic UOM found in list. Check if it is master and update scale of new UOM if not.
                UOM src = UOMs[atom.AtomicUOM + "1"].Clone();
                if (src.IsMaster) masters[ct] = src;
                else {
                    UOM master = src.Master;
                    // Update scale of new UOM
                    for (int cte = 0; cte < Atom.Abs(atom.Exponent); cte++) newUOM.Scale *= atom.Exponent > 0 ? src.Scale : 1.0 / src.Scale;
                    masters[ct] = master.Clone();
                }                
            }

            // Masters are of positive exponents here. The exponents could be large than 1.

            for (int ct = 0; ct < masters.Length; ct++) masters[ct].Signature = masters[ct].Signature.Replace("1", Atom.Abs(atoms[ct].Exponent).ToString()); // That's why I need clone master UOM's

            // Create the master signature
            string masterSignature = masters[0].Signature;
            for (int ct = 1; ct < masters.Length; ct++) masterSignature += masters[ct].Signature;
            Atom[] masterAtoms = Atomize(masterSignature);

            for (int ct = 0; ct < masterAtoms.Length; ct++) if (atoms[ct].Exponent < 0) masterAtoms[ct].Neg(); // Restore signs
            Array.Sort(masterAtoms);
            masterSignature = Recombine(masterAtoms);

            // Check the master and domain exists
            if(!UOMs.ContainsKey(masterSignature)) throw new Exception("Domain for UOM " + ANewSignature + " not found");
            UOM masterUOM = UOMs[masterSignature];            
            
            // Generate label
            bool slash = false; newUOM.Label = "";           
            for (int ct = 0; ct < atoms.Length; ct++) {
                // check the slash shall be inserted
                if (!slash && atoms[ct].Exponent < 0) {
                    if (ct == 0) newUOM.Label = "1/"; else newUOM.Label += "/";
                    slash = true;
                }
                newUOM.Label += atoms[ct].AtomicUOM;
                if (Math.Abs(atoms[ct].Exponent) > 1) newUOM.Label += (int)Math.Abs(atoms[ct].Exponent);
            }

            newUOM.Name = newUOM.Label;
            newUOM.Plural = newUOM.Label;                                                
            newUOM.Master = masterUOM; 
            UOMs.Add(newUOM.Signature, newUOM); 
            return newUOM;
        }

        /// <summary>
        /// Check if the simple UOM is defined by it's label. Simple UOM is UOM which consists of single atomic UOM.
        /// </summary>
        /// <param name="ALabel">UOM label</param>
        /// <returns>If UOM is defined then UOM signature. Otherwise - empty string</returns>
        protected string MatchSimpleLabel(string ALabel) {
            string lcu = ALabel.ToLower(); // Good hope this will be standard unit signature            
            if (IsDefined(lcu)) return lcu;
            // Ok, try to use it with first capitall letter            
            string cap = ALabel.Substring(0, 1).ToUpper();
            if (lcu.Length > 1) cap += lcu.Substring(1, lcu.Length - 1);
            if (IsDefined(cap)) return cap;
            return "";
        }
        #endregion

        #region Protected classes

        [DebuggerDisplay("{Name} {Signature} {Master}")]
        protected class UOM {

            public string Signature;
            public string Label;    /// Human readable label
            public string Name;     /// Human readable name
            public string Plural;   /// The name in plural form                        
            public double Scale;    /// Scale for conversion into master unit. It shall be 1.0 for all masters

            private bool _IsMaster;
            public bool IsMaster { get { return _IsMaster; } }

            private UOM _Master = null;
            public UOM Master { get { return _Master ?? this; } set { _Master = value; } }            

            private string _Domain;
            public string Domain { get { return IsMaster ? _Domain : _Master.Domain; } }   /// Domain name. Units of same domain could be converted one into another           
            
            public string MasterSignature { get { return IsMaster? Signature : Master.Signature; } }

            public void Load(XmlNode xn, bool IsMaster) {                
                Signature = xn.Attributes["Signature"].InnerText;
                Label = xn.Attributes["Label"].InnerText;
                Name = xn.Attributes["Name"].InnerText;
                Plural = xn.Attributes["Plural"].InnerText;                
                _IsMaster = IsMaster;
                if (_IsMaster) { Scale = 1.0; _Domain = xn.Attributes["Domain"].InnerText; }
                else Scale = double.Parse(xn.Attributes["Scale"].InnerText);
            }
            
            public bool IsCompatible(UOM u) { return MasterSignature == u.MasterSignature; }

            public UOM Clone() {
                UOM uom = new UOM(); uom.Signature = Signature; uom.Label = Label; uom.Name = Name;
                uom.Plural = Plural; uom.Scale = Scale; uom._IsMaster = _IsMaster; uom._Master = _Master;
                return uom;
            }
        }


        /// <summary>
        /// Atomic UOM
        /// </summary>
        [DebuggerDisplay("Atom {Signature}")]
        protected class Atom : IComparable<Atom> {
            /// <summary>
            /// Text label
            /// </summary>
            public string AtomicUOM;
            /// <summary>
            /// Signed exponent of UOM in range 1..9. In practice exponents larger than 4 appears rarely.
            /// </summary>
            public int Exponent;            
            /// <summary>
            /// Atomic UOM signature
            /// </summary>
            public string Signature { get { return AtomicUOM + Exponent; } }            

            public Atom(string AnAtomicUOM, int APower) { AtomicUOM = AnAtomicUOM; Exponent = APower; }
            /// <summary>
            /// Sort order for atoms is: positive powers sorted by alphabet, negative powers sorted by alphabet.
            /// That provides the unique signatures for automatically composed UOMs.
            /// </summary>            
            public int CompareTo(Atom other) {
                if (Exponent > 0 && other.Exponent < 0) return -1;
                if (Exponent < 0 && other.Exponent > 0) return 1;
                return AtomicUOM.CompareTo(other.AtomicUOM);
            }

            public void Neg() { Exponent = -Exponent; }

            public static int Abs(int AnExponent) { return AnExponent > 0 ? AnExponent : -AnExponent; }
            public static string Abs(string ASignature) { return ASignature.Replace("-", ""); }
        }
        #endregion

        #region Debug only
#if DEBUG        
        protected void CheckSignature(string ASignature) {            
            if(NormalizeSignature(ASignature) != ASignature) throw new Exception("Wrong sequence of atoms in UOM Signature " + ASignature);            
        }
#endif
        #endregion

        #region Code to move into inherited class
        protected Dictionary<string, string> Variables;
        protected Dictionary<string, string[]> Lists;

        public string LabelOf(StdVar AVariable) {
            string key = AVariable.ToString();
            if (!Variables.ContainsKey(key)) throw new Exception("Variable: " + key + " is undefined. " +
                    "Please, check the XML definition file");                            
            return LabelOf((string)Variables[key]);
        }

        /// <summary>
        /// Unusual UOM Farenheit degree could not be converted like others
        /// </summary>
        /// <param name="FarTemp">Temperature in Farenheit degrees</param>
        /// <returns>Temperature in Celsius degrees</returns>
        public double Farenheit2C(double FarTemp) { return (FarTemp - 32.0) / 1.8; }

        public string GetStdUOM(StdVar APrecVariable) {
            string s = APrecVariable.ToString();
            return (string)Variables[s];
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
        #endregion        
    }
}
