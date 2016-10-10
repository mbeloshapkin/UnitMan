using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace AeroGIS.Common {
    partial class UnitMan {        

        protected string MatchPrimaryUnit(string AUnit) {            
            string lcu = AUnit.ToLower(); // Good hope this will be standard unit signature
            if (Contains(lcu)) return lcu;
            // Ok, try to use it with first capitall letter            
            string cap = AUnit.Substring(0, 1).ToUpper();
            if (lcu.Length > 1) cap += lcu.Substring(1, lcu.Length - 1);
            if (Contains(cap)) return cap;
            return "";
        }

        protected string MatchRationalUnit(string AUnit) {
            if (AUnit.Length < 3) return ""; // Can not be a rational unit
            int idx = AUnit.IndexOf("/");
            if (idx < 1) return ""; // Not a rational unit
            string u1 = AUnit.Substring(0, idx);
            string u2 = AUnit.Substring(idx + 1);
            u1 = MatchPrimaryUnit(u1); u2 = MatchPrimaryUnit(u2);
            if (u1.Length == 0 || u2.Length == 0) return "";

            int dig = u2.IndexOfAny(Digits);
            if (dig > 0) u2 = u2.Substring(0, dig) + "-" + u2.Substring(dig);

            if (u1.IndexOfAny(Digits) < 0) u1 += "1";
            if (dig < 0) u2 += "-1";
            return u1 + u2;
        }

        /// <summary>
        /// Match UOM using its usual label like "m/s", "kg/m2" and so on
        /// </summary>
        /// <param name="ALabel"></param>
        /// <returns></returns>
        public string MatchLabel(string ALabel) {
            // Check if it could be matched
            if (Regex.IsMatch(ALabel, "^([a-z]|[A-Z]){1,3}[23]?$")) {  // Simple UOM
                //Match mgr = Regex.Match("cxc2", "(?<uom>^([a-z]|[A-Z]){1,3})(?<pow>[23]?$)");
                return MatchPrimaryUnit(ALabel);
            }

            if (Regex.IsMatch(ALabel, "^([a-z]|[A-Z]){1,3}[23]?/([a-z]|[A-Z]){1,3}[23]?$")) { // Rational UOM


            }

            bool cnt = true;
            while (cnt) {
                

                Match m2 = Regex.Match(ALabel, "^([a-z]|[A-Z]){1,3}[23]?/([a-z]|[A-Z]){1,3}[23]?$");

                Match mgr2 = Regex.Match(ALabel, "^([a-z]|[A-Z]){1,3}[23]?/([a-z]|[A-Z]){1,3}[23]?$");

                //if (AUnit.IndexOf("/") < 0) return MatchPrimaryUnit(AUnit);
                //return MatchRationalUnit(AUnit);
                cnt = true;
            }
            return "";
        }
        
    }
}
