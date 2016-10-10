// Copyright © 2016 Mikhail Beloshapkin
// All rights reserved.
//
// Author: Mikhail Beloshapkin mbeloshapkin@gmail.com
//
// Licensed under the Microsoft Public License (Ms-PL)
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace AeroGIS.Common {
    partial class UnitMan {

        protected string UOMChars;

        protected string MatchPrimaryUnit(string AUnit) {            
            string lcu = AUnit.ToLower(); // Good hope this will be standard unit signature
            if (Contains(lcu)) return lcu;
            // Ok, try to use it with first capitall letter            
            string cap = AUnit.Substring(0, 1).ToUpper();
            if (lcu.Length > 1) cap += lcu.Substring(1, lcu.Length - 1);
            if (Contains(cap)) return cap;
            return "";
        }
        
        /// <summary>
        /// Match UOM using its human readable label like "m/s", "kg/m2" and so on. Simple and rational UOM labels are supported only.
        /// </summary>
        /// <param name="ALabel">A human readable label</param>
        /// <returns>UOM standard signature like "m1s-1", kg1m-2 and so on. If UOM is not matched than empty string returns.</returns>
        public string MatchLabel(string ALabel) {

            if (Regex.IsMatch(ALabel, "^" + UOMRegex + "{1,3}[23]?$")) {  // Simple UOM                
                return MatchPrimaryUnit(ALabel);
            }

            if (Regex.IsMatch(ALabel, "^" + UOMRegex + "{1,3}[23]?/" + UOMRegex + "{1,3}[23]?$")) { // Rational UOM
                string[] sims = ALabel.Split('/');
                Match numerator = Regex.Match(sims[0], "(?<uom>^" + UOMRegex + "{1,3})(?<pow>[23]?$)");
                Match denom = Regex.Match(sims[1], "(?<uom>^"+ UOMRegex + "{1,3})(?<pow>[23]?$)");
                string rstr = MatchPrimaryUnit(numerator.Groups["uom"].Value);
                if (rstr.Length > 0) {
                    if (numerator.Groups["pow"].Length > 0) rstr += numerator.Groups["pow"].Value;
                    else rstr += "1";
                    string mden = MatchPrimaryUnit(denom.Groups["uom"].Value);
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
    }
}
